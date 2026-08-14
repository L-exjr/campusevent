using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EventManagement.Api.Data;
using EventManagement.Api.DTOs.Payments;
using EventManagement.Api.Infrastructure;
using EventManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Api.Services;

public interface IPaymentService
{
    Task<PaymentInitializationResponse> InitializeAsync(
        Guid eventId,
        Guid studentId,
        Guid? ticketTierId,
        string? couponCode,
        CancellationToken cancellationToken);
    Task<PaymentStatusResponse> GetStatusAsync(
        string reference,
        Guid studentId,
        CancellationToken cancellationToken);
    Task ProcessWebhookAsync(
        string providerName, string payload, string? signature,
        CancellationToken cancellationToken);
}

public sealed class PaymentService(
    AppDbContext dbContext,
    IPaymentProviderResolver providers,
    IConfiguration configuration,
    TimeProvider timeProvider,
    ILogger<PaymentService> logger) : IPaymentService
{
    public async Task<PaymentInitializationResponse> InitializeAsync(
        Guid eventId,
        Guid studentId,
        Guid? ticketTierId,
        string? couponCode,
        CancellationToken cancellationToken)
    {
        var provider = providers.Active;
        PaymentOrder order;
        string studentEmail;
        await using (var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken))
        {
            var student = await dbContext.Users.SingleOrDefaultAsync(
                user => user.Id == studentId,
                cancellationToken)
                ?? throw new ApiException(StatusCodes.Status404NotFound, "Student account not found.");
            if (!student.IsActive)
                throw new ApiException(StatusCodes.Status403Forbidden, "An active account is required to pay for events.");

            var eventEntity = await dbContext.Events
                .FromSqlInterpolated($"SELECT * FROM \"Events\" WHERE \"Id\" = {eventId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken)
                ?? throw new ApiException(StatusCodes.Status404NotFound, "Event not found.");
            var now = timeProvider.GetUtcNow();
            if (!eventEntity.IsPublished)
                throw new ApiException(StatusCodes.Status404NotFound, "Event not found.");
            if (!eventEntity.TicketingEnabled)
                throw new ApiException(StatusCodes.Status409Conflict, "Ticketing is not enabled for this event.");
            if (eventEntity.Date <= now)
                throw new ApiException(StatusCodes.Status409Conflict, "Registration has closed for this event.");
            var tier = await dbContext.TicketTiers
                .Where(item => item.EventId == eventId && item.IsActive &&
                    (ticketTierId == null || item.Id == ticketTierId))
                .OrderBy(item => item.Position)
                .FirstOrDefaultAsync(cancellationToken);
            if (tier is null)
                throw new ApiException(StatusCodes.Status400BadRequest, "Choose an available ticket tier.");
            if (tier.PriceMinor <= 0)
                throw new ApiException(StatusCodes.Status409Conflict, "This event does not require payment.");
            if (eventEntity.SalesStartsAt is null || eventEntity.SalesEndsAt is null ||
                now < eventEntity.SalesStartsAt || now >= eventEntity.SalesEndsAt)
                throw new ApiException(StatusCodes.Status409Conflict, "Ticket sales are not currently open for this event.");
            if (await dbContext.EventRegistrations.AnyAsync(
                registration => registration.EventId == eventId && registration.StudentId == studentId,
                cancellationToken))
            {
                throw new ApiException(StatusCodes.Status409Conflict, "You are already registered for this event.");
            }

            var expired = await dbContext.PaymentOrders
                .Where(item => item.EventId == eventId &&
                    item.Status == PaymentOrderStatus.Pending &&
                    item.ExpiresAt <= now)
                .ToListAsync(cancellationToken);
            foreach (var item in expired)
            {
                item.Status = PaymentOrderStatus.Expired;
                item.UpdatedAt = now;
            }

            var existing = await dbContext.PaymentOrders.SingleOrDefaultAsync(
                item => item.EventId == eventId &&
                    item.StudentId == studentId &&
                    item.TicketTierId == tier.Id &&
                    item.Status == PaymentOrderStatus.Pending &&
                    item.ExpiresAt > now,
                cancellationToken);
            if (existing is not null)
            {
                if (string.IsNullOrWhiteSpace(existing.AuthorizationUrl))
                    throw new ApiException(
                        StatusCodes.Status409Conflict,
                        "Payment initialization is already in progress. Please retry shortly.");
                await transaction.CommitAsync(cancellationToken);
                return ToInitialization(existing);
            }

            var confirmedCount = await dbContext.EventRegistrations.CountAsync(
                registration => registration.PaymentOrder != null &&
                    registration.PaymentOrder.TicketTierId == tier.Id, cancellationToken);
            var reservedCount = await dbContext.PaymentOrders.CountAsync(
                item => item.TicketTierId == tier.Id &&
                    item.Status == PaymentOrderStatus.Pending &&
                    item.ExpiresAt > now,
                cancellationToken);
            if (confirmedCount + reservedCount >= tier.Capacity)
                throw new ApiException(StatusCodes.Status409Conflict, "This event is at capacity.");

            Coupon? coupon = null;
            var normalizedCoupon = couponCode?.Trim().ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(normalizedCoupon))
            {
                coupon = await dbContext.Coupons
                    .FromSqlInterpolated($"SELECT * FROM \"Coupons\" WHERE \"Code\" = {normalizedCoupon} FOR UPDATE")
                    .SingleOrDefaultAsync(cancellationToken);
                if (coupon is null || !coupon.IsActive || coupon.OrganizerId != eventEntity.OrganizerId ||
                    (coupon.EventId.HasValue && coupon.EventId != eventId))
                    throw new ApiException(StatusCodes.Status400BadRequest, "Coupon code is invalid.");
                if (coupon.ExpiresAt <= now)
                    throw new ApiException(StatusCodes.Status400BadRequest, "Coupon code has expired.");
                if (coupon.UsageLimit.HasValue && await dbContext.PaymentOrders.CountAsync(item =>
                    item.CouponId == coupon.Id &&
                    (item.Status == PaymentOrderStatus.Verified ||
                     item.Status == PaymentOrderStatus.Pending && item.ExpiresAt > now),
                    cancellationToken) >= coupon.UsageLimit.Value)
                    throw new ApiException(StatusCodes.Status409Conflict, "Coupon usage limit has been reached.");
            }
            var discountAmount = coupon is null ? 0L : tier.PriceMinor * coupon.PercentageDiscount / 100;
            var payableAmount = tier.PriceMinor - discountAmount;

            var configuredPendingMinutes =
                int.TryParse(configuration["PAYMENTS_PENDING_MINUTES"], out var environmentMinutes)
                    ? environmentMinutes
                    : configuration.GetValue("Payments:PendingMinutes", 15);
            var pendingMinutes = Math.Clamp(configuredPendingMinutes, 5, 60);
            order = new PaymentOrder
            {
                EventId = eventId,
                StudentId = studentId,
                TicketTierId = tier.Id,
                TicketTier = tier,
                CouponId = coupon?.Id,
                Coupon = coupon,
                OriginalAmountMinor = tier.PriceMinor,
                DiscountAmountMinor = discountAmount,
                AmountMinor = payableAmount,
                Currency = eventEntity.Currency,
                Provider = provider.Name,
                ProviderReference = $"ems_{Guid.NewGuid():N}",
                ExpiresAt = now.AddMinutes(pendingMinutes),
                CreatedAt = now,
                UpdatedAt = now
            };
            dbContext.PaymentOrders.Add(order);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            studentEmail = student.Email;
        }

        try
        {
            var frontendBaseUrl = configuration["Frontend:BaseUrl"]?.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(frontendBaseUrl))
                throw new PaymentProviderException("The payment callback URL is not configured.");
            var callbackUrl = $"{frontendBaseUrl}/payment/callback?reference={Uri.EscapeDataString(order.ProviderReference)}";
            var initialized = await provider.InitializeAsync(
                studentEmail,
                order.AmountMinor,
                order.Currency,
                order.ProviderReference,
                callbackUrl,
                order.Id,
                order.EventId,
                order.StudentId,
                cancellationToken);
            if (!string.Equals(initialized.Reference, order.ProviderReference, StringComparison.Ordinal))
                throw new PaymentProviderException($"{provider.Name} returned an unexpected payment reference.");
            order.AuthorizationUrl = initialized.AuthorizationUrl;
            order.UpdatedAt = timeProvider.GetUtcNow();
            await dbContext.SaveChangesAsync(cancellationToken);
            return ToInitialization(order);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            order.Status = PaymentOrderStatus.Failed;
            order.UpdatedAt = timeProvider.GetUtcNow();
            await dbContext.SaveChangesAsync(CancellationToken.None);
            logger.LogError(exception, "Payment initialization failed for order {OrderId}.", order.Id);
            throw new ApiException(
                StatusCodes.Status503ServiceUnavailable,
                "Payment checkout is temporarily unavailable. Please try again.");
        }
    }

    public async Task<PaymentStatusResponse> GetStatusAsync(
        string reference,
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var order = await dbContext.PaymentOrders.AsNoTracking()
            .Include(item => item.Registration)
            .SingleOrDefaultAsync(
            item => item.ProviderReference == reference && item.StudentId == studentId,
            cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Payment not found.");
        return new PaymentStatusResponse(
            order.ProviderReference,
            order.Status,
            order.AmountMinor,
            order.Currency,
            order.Registration?.Id,
            order.ExpiresAt);
    }

    public async Task ProcessWebhookAsync(
        string providerName, string payload, string? signature,
        CancellationToken cancellationToken)
    {
        var provider = providers.Get(providerName);
        if (!provider.HasValidSignature(payload, signature))
            throw new ApiException(StatusCodes.Status401Unauthorized, "The webhook signature is invalid.");
        if (!provider.TryGetSuccessfulWebhook(payload, out var notification) || notification is null) return;
        var eventType = notification.EventType;
        var reference = notification.Reference;
        var verification = await provider.VerifyAsync(reference, cancellationToken);
        var receiptId = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes($"{provider.Name}\n{payload}"))).ToLowerInvariant();
        var refundRequired = false;
        PaymentOrder? refundOrder = null;

        await using (var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken))
        {
            if (await dbContext.PaymentWebhookReceipts.AnyAsync(
                receipt => receipt.Id == receiptId,
                cancellationToken))
            {
                await transaction.CommitAsync(cancellationToken);
                return;
            }

            var order = await dbContext.PaymentOrders
                .FromSqlInterpolated(
                    $"SELECT * FROM \"PaymentOrders\" WHERE \"ProviderReference\" = {reference} AND \"Provider\" = {provider.Name} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            var outcome = "IgnoredUnknownReference";
            if (order is not null)
            {
                var now = timeProvider.GetUtcNow();
                if (order.Status == PaymentOrderStatus.Verified)
                {
                    outcome = "DuplicateVerified";
                }
                else if (!verification.IsSuccessful ||
                    !string.Equals(verification.Reference, order.ProviderReference, StringComparison.Ordinal) ||
                    verification.AmountMinor != order.AmountMinor ||
                    !string.Equals(verification.Currency, order.Currency, StringComparison.Ordinal))
                {
                    order.Status = PaymentOrderStatus.Failed;
                    order.UpdatedAt = now;
                    outcome = "VerificationMismatch";
                    logger.LogWarning("Payment verification did not match order {OrderId}.", order.Id);
                }
                else
                {
                    var eventEntity = await dbContext.Events
                        .FromSqlInterpolated(
                            $"SELECT * FROM \"Events\" WHERE \"Id\" = {order.EventId} FOR UPDATE")
                        .SingleAsync(cancellationToken);
                    var alreadyRegistered = await dbContext.EventRegistrations.AnyAsync(
                        registration => registration.EventId == order.EventId &&
                            registration.StudentId == order.StudentId,
                        cancellationToken);
                    var tierCapacity = order.TicketTierId.HasValue
                        ? await dbContext.TicketTiers.Where(item => item.Id == order.TicketTierId)
                            .Select(item => item.Capacity).SingleAsync(cancellationToken)
                        : eventEntity.Capacity;
                    var confirmedCount = await dbContext.EventRegistrations.CountAsync(
                        registration => registration.PaymentOrder != null &&
                            registration.PaymentOrder.TicketTierId == order.TicketTierId,
                        cancellationToken);
                    var otherReservations = await dbContext.PaymentOrders.CountAsync(
                        item => item.TicketTierId == order.TicketTierId &&
                            item.Id != order.Id &&
                            item.Status == PaymentOrderStatus.Pending &&
                            item.ExpiresAt > now,
                        cancellationToken);

                    if (alreadyRegistered)
                    {
                        order.Status = PaymentOrderStatus.Verified;
                        order.VerifiedAt = now;
                        order.UpdatedAt = now;
                        outcome = "AlreadyRegistered";
                    }
                    else if (order.ExpiresAt <= now &&
                        confirmedCount + otherReservations >= tierCapacity)
                    {
                        order.Status = PaymentOrderStatus.RefundPending;
                        order.VerifiedAt = now;
                        order.UpdatedAt = now;
                        outcome = "LatePaymentRefundRequired";
                        refundRequired = true;
                        refundOrder = order;
                    }
                    else
                    {
                        var registration = new EventRegistration
                        {
                            EventId = order.EventId,
                            StudentId = order.StudentId,
                            PaymentOrderId = order.Id,
                            RegisteredAt = now
                        };
                        dbContext.EventRegistrations.Add(registration);
                        order.Status = PaymentOrderStatus.Verified;
                        order.VerifiedAt = now;
                        order.UpdatedAt = now;
                        EmailOutbox.EnqueueDomainMessage(
                            dbContext,
                            $"registration-confirmation:{registration.Id}",
                            EmailOutbox.RegistrationConfirmationKind,
                            registration.Id);
                        outcome = "RegistrationConfirmed";
                    }
                }
            }
            dbContext.PaymentWebhookReceipts.Add(new PaymentWebhookReceipt
            {
                Id = receiptId,
                Provider = provider.Name,
                EventType = eventType!,
                ProviderReference = reference,
                Outcome = outcome,
                ProcessedAt = timeProvider.GetUtcNow()
            });
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        if (refundRequired && refundOrder is not null)
        {
            var accepted = await provider.RequestRefundAsync(
                refundOrder.ProviderReference,
                refundOrder.AmountMinor,
                cancellationToken);
            if (!accepted)
            {
                refundOrder.Status = PaymentOrderStatus.RefundFailed;
                refundOrder.UpdatedAt = timeProvider.GetUtcNow();
                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogError(
                    "Automatic refund request failed for payment order {OrderId}.",
                    refundOrder.Id);
            }
        }
    }

    private static PaymentInitializationResponse ToInitialization(PaymentOrder order) => new(
        order.ProviderReference,
        order.AuthorizationUrl!,
        order.AmountMinor,
        order.OriginalAmountMinor,
        order.DiscountAmountMinor,
        order.TicketTierId,
        order.TicketTier?.Name,
        order.Coupon?.Code,
        order.Currency,
        order.ExpiresAt);
}

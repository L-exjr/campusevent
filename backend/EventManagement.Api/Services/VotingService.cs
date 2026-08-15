using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EventManagement.Api.Data;
using EventManagement.Api.DTOs.Voting;
using EventManagement.Api.Infrastructure;
using EventManagement.Api.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EventManagement.Api.Services;

public interface IVotingService
{
    Task<VotingCampaignResponse> GetCampaignAsync(
        Guid eventId, Guid? actorId, UserRole? actorRole, CancellationToken cancellationToken);
    Task<VotingCampaignResponse> UpsertCampaignAsync(
        Guid eventId, Guid actorId, UserRole actorRole,
        VotingCampaignUpsertRequest request, CancellationToken cancellationToken);
    Task<VoteAcceptedResponse> CastFreeVoteAsync(
        Guid categoryId, Guid nomineeId, Guid voterId, CancellationToken cancellationToken);
    Task<VotingPaymentInitializationResponse> InitializePaidVoteAsync(
        Guid categoryId, Guid nomineeId, int quantity, Guid voterId,
        CancellationToken cancellationToken);
    Task<VotingPaymentStatusResponse> GetPaymentStatusAsync(
        string reference, Guid voterId, CancellationToken cancellationToken);
    Task ProcessWebhookAsync(
        string providerName, string payload, string? signature, CancellationToken cancellationToken);
}

public sealed class VotingService(
    AppDbContext dbContext,
    IEventAuthorizationService authorizationService,
    IPaymentProviderResolver providers,
    IConfiguration configuration,
    TimeProvider timeProvider,
    ILogger<VotingService> logger) : IVotingService
{
    public async Task<VotingCampaignResponse> GetCampaignAsync(
        Guid eventId,
        Guid? actorId,
        UserRole? actorRole,
        CancellationToken cancellationToken)
    {
        var campaign = await CampaignQuery().AsNoTracking()
            .SingleOrDefaultAsync(item => item.EventId == eventId, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Voting campaign not found.");
        var canManage = CanManage(campaign.Event.OrganizerId, actorId, actorRole);
        if (!campaign.IsPublished && !canManage)
            throw new ApiException(StatusCodes.Status404NotFound, "Voting campaign not found.");
        return ToResponse(campaign, actorId, canManage, timeProvider.GetUtcNow());
    }

    public async Task<VotingCampaignResponse> UpsertCampaignAsync(
        Guid eventId,
        Guid actorId,
        UserRole actorRole,
        VotingCampaignUpsertRequest request,
        CancellationToken cancellationToken)
    {
        ValidateCampaign(request);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var eventEntity = await dbContext.Events
            .FromSqlInterpolated($"SELECT * FROM \"Events\" WHERE \"Id\" = {eventId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Event not found.");
        await authorizationService.EnsureCanAsync(eventId, eventEntity.OrganizerId, actorId, actorRole,
            EventCapability.ManageOperations, cancellationToken);

        var existing = await dbContext.VotingCampaigns
            .Include(item => item.Categories)
                .ThenInclude(category => category.Nominees)
            .SingleOrDefaultAsync(item => item.EventId == eventId, cancellationToken);
        if (existing is not null)
        {
            var categoryIds = existing.Categories.Select(item => item.Id).ToArray();
            var hasActivity = await dbContext.VoteRecords.AnyAsync(
                    vote => categoryIds.Contains(vote.CategoryId), cancellationToken) ||
                await dbContext.VotingPaymentOrders.AnyAsync(
                    order => categoryIds.Contains(order.CategoryId), cancellationToken);
            if (hasActivity)
            {
                if (!HasSameStructure(existing, request))
                    throw new ApiException(StatusCodes.Status409Conflict,
                        "Categories, prices, and nominees cannot change after voting or checkout has started.");
                existing.ClosesAt = request.ClosesAt;
                existing.IsPublished = request.IsPublished;
                existing.ShowLiveResults = request.ShowLiveResults;
                existing.UpdatedAt = timeProvider.GetUtcNow();
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                var saved = await CampaignQuery().AsNoTracking()
                    .SingleAsync(item => item.Id == existing.Id, cancellationToken);
                return ToResponse(saved, actorId, true, timeProvider.GetUtcNow());
            }
            dbContext.VotingCampaigns.Remove(existing);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var now = timeProvider.GetUtcNow();
        var campaign = new VotingCampaign
        {
            EventId = eventId,
            Event = eventEntity,
            OpensAt = request.OpensAt,
            ClosesAt = request.ClosesAt,
            IsPublished = request.IsPublished,
            ShowLiveResults = request.ShowLiveResults,
            CreatedAt = now,
            UpdatedAt = now,
            Categories = request.Categories.Select((category, categoryPosition) =>
                new VotingCategory
                {
                    Name = category.Name.Trim(),
                    Description = NormalizeOptional(category.Description),
                    Mode = category.Mode,
                    PricePerVoteMinor = category.Mode == VotingMode.Paid
                        ? category.PricePerVoteMinor
                        : 0,
                    Currency = "GHS",
                    Position = categoryPosition,
                    Nominees = category.Nominees.Select((nominee, nomineePosition) =>
                        new VotingNominee
                        {
                            Name = nominee.Name.Trim(),
                            Description = NormalizeOptional(nominee.Description),
                            Position = nomineePosition
                        }).ToList()
                }).ToList()
        };
        dbContext.VotingCampaigns.Add(campaign);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToResponse(campaign, actorId, true, now);
    }

    public async Task<VoteAcceptedResponse> CastFreeVoteAsync(
        Guid categoryId,
        Guid nomineeId,
        Guid voterId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var category = await dbContext.VotingCategories
            .FromSqlInterpolated(
                $"SELECT * FROM \"VotingCategories\" WHERE \"Id\" = {categoryId} FOR UPDATE")
            .Include(item => item.Campaign)
            .Include(item => item.Nominees)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Voting category not found.");
        EnsureVotingOpen(category, VotingMode.Free);
        if (category.Nominees.All(item => item.Id != nomineeId))
            throw new ApiException(StatusCodes.Status400BadRequest,
                "The nominee does not belong to this voting category.");
        if (await dbContext.VoteRecords.AnyAsync(
            item => item.CategoryId == categoryId && item.VoterId == voterId &&
                item.VotingPaymentOrderId == null,
            cancellationToken))
        {
            throw new ApiException(StatusCodes.Status409Conflict,
                "You have already voted in this category.");
        }
        var now = timeProvider.GetUtcNow();
        dbContext.VoteRecords.Add(new VoteRecord
        {
            CategoryId = categoryId,
            NomineeId = nomineeId,
            VoterId = voterId,
            Quantity = 1,
            CastAt = now
        });
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ApiException(StatusCodes.Status409Conflict,
                "You have already voted in this category.");
        }
        await transaction.CommitAsync(cancellationToken);
        return new VoteAcceptedResponse(categoryId, nomineeId, 1, now);
    }

    public async Task<VotingPaymentInitializationResponse> InitializePaidVoteAsync(
        Guid categoryId,
        Guid nomineeId,
        int quantity,
        Guid voterId,
        CancellationToken cancellationToken)
    {
        var provider = providers.Active;
        if (quantity is < 1 or > 100)
            throw new ApiException(StatusCodes.Status400BadRequest,
                "Choose between 1 and 100 votes per checkout.");
        VotingPaymentOrder order;
        string voterEmail;
        Guid eventId;
        await using (var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken))
        {
            var category = await dbContext.VotingCategories
                .FromSqlInterpolated(
                    $"SELECT * FROM \"VotingCategories\" WHERE \"Id\" = {categoryId} FOR UPDATE")
                .Include(item => item.Campaign)
                .Include(item => item.Nominees)
                .SingleOrDefaultAsync(cancellationToken)
                ?? throw new ApiException(StatusCodes.Status404NotFound, "Voting category not found.");
            EnsureVotingOpen(category, VotingMode.Paid);
            if (category.Nominees.All(item => item.Id != nomineeId))
                throw new ApiException(StatusCodes.Status400BadRequest,
                    "The nominee does not belong to this voting category.");
            var voter = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(
                item => item.Id == voterId, cancellationToken)
                ?? throw new ApiException(StatusCodes.Status404NotFound, "Voter account not found.");
            var now = timeProvider.GetUtcNow();
            var expired = await dbContext.VotingPaymentOrders.Where(item =>
                item.CategoryId == categoryId && item.VoterId == voterId &&
                item.Status == PaymentOrderStatus.Pending && item.ExpiresAt <= now)
                .ToListAsync(cancellationToken);
            foreach (var item in expired)
            {
                item.Status = PaymentOrderStatus.Expired;
                item.UpdatedAt = now;
            }
            var existing = await dbContext.VotingPaymentOrders.SingleOrDefaultAsync(item =>
                item.CategoryId == categoryId && item.NomineeId == nomineeId &&
                item.VoterId == voterId && item.Quantity == quantity &&
                item.Status == PaymentOrderStatus.Pending && item.ExpiresAt > now,
                cancellationToken);
            if (existing is not null)
            {
                if (string.IsNullOrWhiteSpace(existing.AuthorizationUrl))
                    throw new ApiException(StatusCodes.Status409Conflict,
                        "Vote checkout initialization is already in progress. Please retry shortly.");
                await transaction.CommitAsync(cancellationToken);
                return ToInitialization(existing);
            }
            var pendingMinutes = Math.Clamp(
                int.TryParse(configuration["PAYMENTS_PENDING_MINUTES"], out var configured)
                    ? configured
                    : configuration.GetValue("Payments:PendingMinutes", 15), 5, 60);
            long amount;
            try { amount = checked(category.PricePerVoteMinor * quantity); }
            catch (OverflowException)
            {
                throw new ApiException(StatusCodes.Status400BadRequest, "The vote total is too large.");
            }
            order = new VotingPaymentOrder
            {
                CategoryId = categoryId,
                NomineeId = nomineeId,
                VoterId = voterId,
                Quantity = quantity,
                UnitPriceMinor = category.PricePerVoteMinor,
                AmountMinor = amount,
                Currency = category.Currency,
                Provider = provider.Name,
                ProviderReference = $"vote_{Guid.NewGuid():N}",
                ExpiresAt = now.AddMinutes(pendingMinutes),
                CreatedAt = now,
                UpdatedAt = now
            };
            dbContext.VotingPaymentOrders.Add(order);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            voterEmail = voter.Email;
            eventId = category.Campaign.EventId;
        }

        try
        {
            var frontendBaseUrl = configuration["Frontend:BaseUrl"]?.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(frontendBaseUrl))
                throw new PaymentProviderException("The payment callback URL is not configured.");
            var callbackUrl =
                $"{frontendBaseUrl}/voting/payment/callback?reference={Uri.EscapeDataString(order.ProviderReference)}";
            var initialized = await provider.InitializeAsync(
                voterEmail, order.AmountMinor, order.Currency, order.ProviderReference,
                callbackUrl, order.Id, eventId, order.VoterId, cancellationToken);
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
            logger.LogError(exception, "Voting payment initialization failed for order {OrderId}.", order.Id);
            throw new ApiException(StatusCodes.Status503ServiceUnavailable,
                "Vote checkout is temporarily unavailable. Please try again.");
        }
    }

    public async Task<VotingPaymentStatusResponse> GetPaymentStatusAsync(
        string reference,
        Guid voterId,
        CancellationToken cancellationToken)
    {
        var order = await dbContext.VotingPaymentOrders.AsNoTracking()
            .Include(item => item.Vote)
            .SingleOrDefaultAsync(item =>
                item.ProviderReference == reference && item.VoterId == voterId,
                cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Voting payment not found.");
        return ToStatus(order);
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

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        if (await dbContext.VotingWebhookReceipts.AnyAsync(
            item => item.Id == receiptId, cancellationToken))
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }
        var order = await dbContext.VotingPaymentOrders
            .FromSqlInterpolated(
                $"SELECT * FROM \"VotingPaymentOrders\" WHERE \"ProviderReference\" = {reference} AND \"Provider\" = {provider.Name} FOR UPDATE")
            .Include(item => item.Vote)
            .SingleOrDefaultAsync(cancellationToken);
        var outcome = "IgnoredUnknownReference";
        if (order is not null)
        {
            var now = timeProvider.GetUtcNow();
            if (order.Vote is not null || order.Status == PaymentOrderStatus.Verified)
            {
                outcome = "DuplicateVerified";
            }
            else if (order.ExpiresAt <= now)
            {
                order.Status = PaymentOrderStatus.Expired;
                order.UpdatedAt = now;
                outcome = "ExpiredOrderRejected";
            }
            else if (order.CreatedAt >= await dbContext.VotingCategories
                .Where(item => item.Id == order.CategoryId)
                .Select(item => item.Campaign.ClosesAt)
                .SingleAsync(cancellationToken))
            {
                order.Status = PaymentOrderStatus.Failed;
                order.UpdatedAt = now;
                outcome = "OrderCreatedAfterDeadline";
            }
            else if (!verification.IsSuccessful ||
                verification.Reference != order.ProviderReference ||
                verification.AmountMinor != order.AmountMinor ||
                !string.Equals(verification.Currency, order.Currency, StringComparison.Ordinal))
            {
                order.Status = PaymentOrderStatus.Failed;
                order.UpdatedAt = now;
                outcome = "VerificationMismatch";
                logger.LogWarning("Voting payment verification did not match order {OrderId}.", order.Id);
            }
            else
            {
                dbContext.VoteRecords.Add(new VoteRecord
                {
                    CategoryId = order.CategoryId,
                    NomineeId = order.NomineeId,
                    VoterId = order.VoterId,
                    Quantity = order.Quantity,
                    VotingPaymentOrderId = order.Id,
                    CastAt = now
                });
                order.Status = PaymentOrderStatus.Verified;
                order.VerifiedAt = now;
                order.UpdatedAt = now;
                outcome = "VotesRecorded";
            }
        }
        dbContext.VotingWebhookReceipts.Add(new VotingWebhookReceipt
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

    private IQueryable<VotingCampaign> CampaignQuery() => dbContext.VotingCampaigns
        .Include(item => item.Event)
        .Include(item => item.Categories.OrderBy(category => category.Position))
            .ThenInclude(category => category.Nominees.OrderBy(nominee => nominee.Position))
        .Include(item => item.Categories)
            .ThenInclude(category => category.Votes);

    private VotingCampaignResponse ToResponse(
        VotingCampaign campaign,
        Guid? actorId,
        bool canManage,
        DateTimeOffset now)
    {
        var resultsVisible = canManage || campaign.ShowLiveResults || now >= campaign.ClosesAt;
        var status = !campaign.IsPublished ? "Draft" :
            now < campaign.OpensAt ? "Scheduled" :
            now >= campaign.ClosesAt ? "Closed" : "Open";
        return new VotingCampaignResponse(
            campaign.Id, campaign.EventId, campaign.Event.Title,
            campaign.OpensAt, campaign.ClosesAt, campaign.IsPublished, campaign.ShowLiveResults,
            status, canManage, resultsVisible,
            campaign.Categories.OrderBy(item => item.Position).Select(category =>
                new VotingCategoryResponse(
                    category.Id, category.Name, category.Description, category.Mode,
                    category.PricePerVoteMinor, category.Currency,
                    actorId.HasValue && category.Votes.Any(vote =>
                        vote.VoterId == actorId && vote.VotingPaymentOrderId == null),
                    category.Nominees.OrderBy(item => item.Position).Select(nominee =>
                        new VotingNomineeResponse(
                            nominee.Id, nominee.Name, nominee.Description,
                            resultsVisible
                                ? category.Votes.Where(vote => vote.NomineeId == nominee.Id)
                                    .Sum(vote => (long)vote.Quantity)
                                : null)).ToList())).ToList());
    }

    private void EnsureVotingOpen(VotingCategory category, VotingMode expectedMode)
    {
        var now = timeProvider.GetUtcNow();
        if (!category.Campaign.IsPublished || now < category.Campaign.OpensAt ||
            now >= category.Campaign.ClosesAt)
            throw new ApiException(StatusCodes.Status409Conflict, "Voting is not currently open.");
        if (category.Mode != expectedMode)
            throw new ApiException(StatusCodes.Status409Conflict,
                expectedMode == VotingMode.Free
                    ? "This category requires payment."
                    : "This category does not require payment.");
    }

    private static bool CanManage(Guid ownerId, Guid? actorId, UserRole? actorRole) =>
        actorRole == UserRole.Admin ||
        actorId == ownerId;

    private static void ValidateCampaign(VotingCampaignUpsertRequest request)
    {
        if (request.ClosesAt <= request.OpensAt)
            throw new ApiException(StatusCodes.Status400BadRequest,
                "Voting must close after it opens.");
        if (request.Categories.Count > 50)
            throw new ApiException(StatusCodes.Status400BadRequest,
                "A campaign may contain at most 50 categories.");
        var duplicateCategory = request.Categories.GroupBy(
            item => item.Name.Trim(), StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1);
        if (duplicateCategory)
            throw new ApiException(StatusCodes.Status400BadRequest,
                "Voting category names must be unique.");
        foreach (var category in request.Categories)
        {
            if (category.Mode == VotingMode.Paid && category.PricePerVoteMinor <= 0)
                throw new ApiException(StatusCodes.Status400BadRequest,
                    "Paid categories require a price per vote.");
            if (category.Nominees.Count > 100)
                throw new ApiException(StatusCodes.Status400BadRequest,
                    "A category may contain at most 100 nominees.");
            if (category.Nominees.GroupBy(
                item => item.Name.Trim(), StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
                throw new ApiException(StatusCodes.Status400BadRequest,
                    $"Nominee names in '{category.Name}' must be unique.");
        }
    }

    private static bool HasSameStructure(
        VotingCampaign existing,
        VotingCampaignUpsertRequest request)
    {
        var categories = existing.Categories.OrderBy(item => item.Position).ToArray();
        if (categories.Length != request.Categories.Count) return false;
        for (var categoryIndex = 0; categoryIndex < categories.Length; categoryIndex++)
        {
            var stored = categories[categoryIndex];
            var supplied = request.Categories[categoryIndex];
            if (!string.Equals(stored.Name, supplied.Name.Trim(), StringComparison.Ordinal) ||
                !string.Equals(stored.Description, NormalizeOptional(supplied.Description), StringComparison.Ordinal) ||
                stored.Mode != supplied.Mode ||
                stored.PricePerVoteMinor != (supplied.Mode == VotingMode.Paid ? supplied.PricePerVoteMinor : 0))
                return false;
            var nominees = stored.Nominees.OrderBy(item => item.Position).ToArray();
            if (nominees.Length != supplied.Nominees.Count) return false;
            for (var nomineeIndex = 0; nomineeIndex < nominees.Length; nomineeIndex++)
            {
                if (!string.Equals(nominees[nomineeIndex].Name,
                        supplied.Nominees[nomineeIndex].Name.Trim(), StringComparison.Ordinal) ||
                    !string.Equals(nominees[nomineeIndex].Description,
                        NormalizeOptional(supplied.Nominees[nomineeIndex].Description), StringComparison.Ordinal))
                    return false;
            }
        }
        return true;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static VotingPaymentInitializationResponse ToInitialization(VotingPaymentOrder order) =>
        new(order.ProviderReference, order.AuthorizationUrl!, order.CategoryId, order.NomineeId,
            order.Quantity, order.AmountMinor, order.Currency, order.ExpiresAt);

    private static VotingPaymentStatusResponse ToStatus(VotingPaymentOrder order) =>
        new(order.ProviderReference, order.Status, order.CategoryId, order.NomineeId,
            order.Quantity, order.AmountMinor, order.Currency, order.Vote is not null,
            order.ExpiresAt);
}

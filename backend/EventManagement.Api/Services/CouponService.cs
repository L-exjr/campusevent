using EventManagement.Api.Data;
using EventManagement.Api.DTOs.Coupons;
using EventManagement.Api.Infrastructure;
using EventManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Api.Services;

public interface ICouponService
{
    Task<IReadOnlyList<CouponResponse>> GetAsync(Guid organizerId, CancellationToken cancellationToken);
    Task<CouponResponse> CreateAsync(Guid organizerId, CouponUpsertRequest request, CancellationToken cancellationToken);
    Task<CouponResponse> UpdateAsync(Guid id, Guid organizerId, CouponUpsertRequest request, CancellationToken cancellationToken);
}

public sealed class CouponService(AppDbContext dbContext) : ICouponService
{
    public async Task<IReadOnlyList<CouponResponse>> GetAsync(Guid organizerId, CancellationToken cancellationToken) =>
        await Project(dbContext.Coupons.AsNoTracking().Where(item => item.OrganizerId == organizerId))
            .OrderBy(item => item.Code).ToListAsync(cancellationToken);

    public async Task<CouponResponse> CreateAsync(
        Guid organizerId, CouponUpsertRequest request, CancellationToken cancellationToken)
    {
        await ValidateEventAsync(organizerId, request.EventId, cancellationToken);
        var coupon = new Coupon { OrganizerId = organizerId, Code = NormalizeCode(request.Code) };
        Apply(coupon, request);
        dbContext.Coupons.Add(coupon);
        await SaveAsync(cancellationToken);
        return await GetOneAsync(coupon.Id, organizerId, cancellationToken);
    }

    public async Task<CouponResponse> UpdateAsync(
        Guid id, Guid organizerId, CouponUpsertRequest request, CancellationToken cancellationToken)
    {
        var coupon = await dbContext.Coupons.SingleOrDefaultAsync(
            item => item.Id == id && item.OrganizerId == organizerId, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Coupon not found.");
        await ValidateEventAsync(organizerId, request.EventId, cancellationToken);
        coupon.Code = NormalizeCode(request.Code);
        Apply(coupon, request);
        await SaveAsync(cancellationToken);
        return await GetOneAsync(coupon.Id, organizerId, cancellationToken);
    }

    private async Task ValidateEventAsync(Guid organizerId, Guid? eventId, CancellationToken cancellationToken)
    {
        if (eventId.HasValue && !await dbContext.Events.AnyAsync(
            item => item.Id == eventId && item.OrganizerId == organizerId, cancellationToken))
            throw new ApiException(StatusCodes.Status400BadRequest, "Coupon event must be one of your events.");
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException)
        {
            throw new ApiException(StatusCodes.Status409Conflict, "Coupon code is already in use.");
        }
    }

    private Task<CouponResponse> GetOneAsync(Guid id, Guid organizerId, CancellationToken cancellationToken) =>
        Project(dbContext.Coupons.AsNoTracking().Where(item => item.Id == id && item.OrganizerId == organizerId))
            .SingleAsync(cancellationToken);

    private static IQueryable<CouponResponse> Project(IQueryable<Coupon> query) => query.Select(item =>
        new CouponResponse(item.Id, item.Code, item.PercentageDiscount, item.UsageLimit,
            item.PaymentOrders.Count(order => order.Status == PaymentOrderStatus.Verified),
            item.EventId, item.Event != null ? item.Event.Title : null, item.ExpiresAt, item.IsActive));

    private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();
    private static void Apply(Coupon coupon, CouponUpsertRequest request)
    {
        coupon.PercentageDiscount = request.PercentageDiscount;
        coupon.UsageLimit = request.UsageLimit;
        coupon.EventId = request.EventId;
        coupon.ExpiresAt = request.ExpiresAt;
        coupon.IsActive = request.IsActive;
    }
}

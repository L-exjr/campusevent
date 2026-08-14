using System.ComponentModel.DataAnnotations;

namespace EventManagement.Api.DTOs.Coupons;

public sealed record CouponUpsertRequest(
    [param: Required, StringLength(40, MinimumLength = 3)] string Code,
    [param: Range(1, 99)] int PercentageDiscount,
    [param: Range(1, int.MaxValue)] int? UsageLimit,
    Guid? EventId,
    DateTimeOffset? ExpiresAt,
    bool IsActive = true);

public sealed record CouponResponse(
    Guid Id, string Code, int PercentageDiscount, int? UsageLimit, int Used,
    Guid? EventId, string? EventTitle, DateTimeOffset? ExpiresAt, bool IsActive);

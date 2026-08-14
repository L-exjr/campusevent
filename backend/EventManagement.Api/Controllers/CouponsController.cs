using EventManagement.Api.DTOs.Coupons;
using EventManagement.Api.Infrastructure;
using EventManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Api.Controllers;

[ApiController, Route("api/coupons"), Authorize(Roles = "Student,Organizer")]
public sealed class CouponsController(ICouponService couponService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CouponResponse>>> Get(CancellationToken cancellationToken) =>
        Ok(await couponService.GetAsync(User.GetRequiredUserId(), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<CouponResponse>> Create(
        CouponUpsertRequest request, CancellationToken cancellationToken)
    {
        var coupon = await couponService.CreateAsync(User.GetRequiredUserId(), request, cancellationToken);
        return CreatedAtAction(nameof(Get), coupon);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CouponResponse>> Update(
        Guid id, CouponUpsertRequest request, CancellationToken cancellationToken) =>
        Ok(await couponService.UpdateAsync(id, User.GetRequiredUserId(), request, cancellationToken));
}

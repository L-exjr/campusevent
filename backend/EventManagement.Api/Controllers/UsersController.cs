using EventManagement.Api.DTOs.Auth;
using EventManagement.Api.DTOs.Common;
using EventManagement.Api.DTOs.Users;
using EventManagement.Api.Infrastructure;
using EventManagement.Api.Models;
using EventManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PaginatedResponse<UserResponse>>> Get(
        [FromQuery] string? search,
        [FromQuery] UserRole? role,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await userService.GetAsync(
            search,
            role,
            isActive,
            page,
            pageSize,
            cancellationToken));

    [HttpPut("{id:guid}/role")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserResponse>> UpdateRole(
        Guid id,
        UpdateUserRoleRequest request,
        CancellationToken cancellationToken) =>
        Ok(await userService.UpdateRoleAsync(
            id,
            request.Role,
            User.GetRequiredUserId(),
            cancellationToken));

    [HttpPut("{id:guid}/deactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(
        Guid id,
        CancellationToken cancellationToken)
    {
        await userService.DeactivateAsync(id, User.GetRequiredUserId(), cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/profile")]
    public async Task<ActionResult<UserResponse>> UpdateProfile(
        Guid id,
        UpdateUserProfileRequest request,
        CancellationToken cancellationToken) =>
        Ok(await userService.UpdateProfileAsync(
            id,
            User.GetRequiredUserId(),
            request.ImageUrl,
            cancellationToken));
}

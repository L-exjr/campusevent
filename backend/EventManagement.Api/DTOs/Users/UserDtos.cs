using System.ComponentModel.DataAnnotations;
using EventManagement.Api.Models;

namespace EventManagement.Api.DTOs.Users;

public sealed record UpdateUserRoleRequest(UserRole Role);

public sealed record UpdateUserProfileRequest(
    [param: Url, StringLength(2048)] string? ImageUrl);

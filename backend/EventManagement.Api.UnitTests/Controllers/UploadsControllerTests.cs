using System.Reflection;
using System.Security.Claims;
using EventManagement.Api.Controllers;
using EventManagement.Api.Infrastructure;
using EventManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventManagement.Api.UnitTests.Controllers;

public sealed class UploadsControllerTests
{
    [Fact]
    public void Controller_requires_authentication_and_event_upload_requires_privileged_role()
    {
        var controllerAuthorization = typeof(UploadsController)
            .GetCustomAttribute<AuthorizeAttribute>();
        var eventAuthorization = typeof(UploadsController)
            .GetMethod(nameof(UploadsController.UploadEventImage))!
            .GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(controllerAuthorization);
        Assert.Equal("Organizer,Admin", eventAuthorization?.Roles);
    }

    [Fact]
    public async Task UploadProfileImage_rejects_spoofed_content_type_before_storage_call()
    {
        var lifecycle = new Mock<IImageLifecycleService>();
        var controller = new UploadsController(
            lifecycle.Object,
            Mock.Of<IAuthRateLimitService>(),
            Mock.Of<ILogger<UploadsController>>());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(JwtClaimNames.UserId, Guid.NewGuid().ToString())],
                    "test"))
            }
        };
        await using var stream = new MemoryStream("not a png"u8.ToArray());
        var file = new FormFile(stream, 0, stream.Length, "file", "spoofed.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };

        var result = await controller.UploadProfileImage(file, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        lifecycle.VerifyNoOtherCalls();
    }
}

using System.Reflection;
using System.Security.Claims;
using EventManagement.Api.Controllers;
using EventManagement.Api.Infrastructure;
using EventManagement.Api.Models;
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

    [Theory]
    [InlineData(
        ImageStorageFailureKind.Configuration,
        StatusCodes.Status503ServiceUnavailable,
        "Image storage is not configured. Please contact support.")]
    [InlineData(
        ImageStorageFailureKind.ProviderRejected,
        StatusCodes.Status502BadGateway,
        "Image storage rejected the upload. Please contact support.")]
    [InlineData(
        ImageStorageFailureKind.ProviderUnavailable,
        StatusCodes.Status503ServiceUnavailable,
        "Image storage is temporarily unavailable. Please try again.")]
    public async Task UploadProfileImage_distinguishes_storage_failures(
        ImageStorageFailureKind failureKind,
        int expectedStatus,
        string expectedMessage)
    {
        var lifecycle = new Mock<IImageLifecycleService>();
        lifecycle.Setup(service => service.CreatePendingAsync(
                It.IsAny<Stream>(),
                "image/png",
                "profile-images",
                "png",
                It.IsAny<Guid>(),
                ImageUploadKind.Profile,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ImageStorageException(failureKind, "Storage failure."));
        var controller = CreateController(lifecycle.Object);
        await using var stream = new MemoryStream(
            [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0]);
        var file = new FormFile(stream, 0, stream.Length, "file", "image.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };

        var result = await controller.UploadProfileImage(file, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(expectedStatus, objectResult.StatusCode);
        Assert.Equal(expectedMessage, objectResult.Value?.GetType().GetProperty("error")?.GetValue(objectResult.Value));
    }

    private static UploadsController CreateController(IImageLifecycleService lifecycle)
    {
        var controller = new UploadsController(
            lifecycle,
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
        return controller;
    }
}

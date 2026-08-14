using System.Security.Claims;
using EventManagement.Api.Controllers;
using EventManagement.Api.DTOs.Events;
using EventManagement.Api.Infrastructure;
using EventManagement.Api.Models;
using EventManagement.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace EventManagement.Api.UnitTests.Controllers;

public sealed class EventsControllerTests
{
    [Fact]
    public async Task Update_forwards_authenticated_actor_identity_and_role_to_service()
    {
        var actorId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var request = Request();
        var expected = Response(eventId, actorId);
        var service = new Mock<IEventService>(MockBehavior.Strict);
        service.Setup(item => item.UpdateAsync(
                eventId,
                actorId,
                UserRole.Organizer,
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = CreateController(service.Object, actorId, UserRole.Organizer);

        var result = await controller.Update(eventId, request, CancellationToken.None);

        Assert.Same(expected, Assert.IsType<OkObjectResult>(result.Result).Value);
        service.VerifyAll();
    }

    [Fact]
    public async Task Delete_forwards_admin_role_instead_of_treating_admin_as_owner()
    {
        var adminId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var service = new Mock<IEventService>(MockBehavior.Strict);
        service.Setup(item => item.DeleteAsync(
                eventId,
                adminId,
                UserRole.Admin,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var controller = CreateController(service.Object, adminId, UserRole.Admin);

        var result = await controller.Delete(eventId, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        service.VerifyAll();
    }

    private static EventsController CreateController(
        IEventService service,
        Guid actorId,
        UserRole role)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(JwtClaimNames.UserId, actorId.ToString()),
            new Claim(JwtClaimNames.Role, role.ToString())
        ], "Test");
        return new EventsController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            }
        };
    }

    private static EventUpsertRequest Request() => new(
        "Test event",
        "A sufficiently detailed test description.",
        DateTimeOffset.UtcNow.AddDays(7),
        "Test Hall",
        10,
        "Startup & Tech",
        null);

    private static EventResponse Response(Guid eventId, Guid organizerId) => new(
        eventId,
        "Test event",
        "A sufficiently detailed test description.",
        DateTimeOffset.UtcNow.AddDays(7),
        "Test Hall",
        10,
        "Startup & Tech",
        organizerId,
        "Organizer",
        0,
        DateTimeOffset.UtcNow,
        null,
        true,
        1);
}

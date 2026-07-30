using EventManagement.Api.Infrastructure;
using EventManagement.Api.Models;
using EventManagement.Api.Services;
using Microsoft.AspNetCore.Http;

namespace EventManagement.Api.UnitTests.Services;

public sealed class EventAuthorizationServiceTests
{
    private readonly EventAuthorizationService _service = new();

    [Theory]
    [InlineData(UserRole.Organizer, true, true)]
    [InlineData(UserRole.Organizer, false, false)]
    [InlineData(UserRole.Admin, true, true)]
    [InlineData(UserRole.Admin, false, true)]
    [InlineData(UserRole.Student, true, false)]
    [InlineData(UserRole.Student, false, false)]
    public void EnsureCanManage_enforces_role_and_ownership_matrix(
        UserRole role,
        bool ownsEvent,
        bool allowed)
    {
        var ownerId = Guid.NewGuid();
        var actorId = ownsEvent ? ownerId : Guid.NewGuid();

        var exception = Record.Exception(() =>
            _service.EnsureCanManage(ownerId, actorId, role));

        if (allowed)
        {
            Assert.Null(exception);
        }
        else
        {
            var apiException = Assert.IsType<ApiException>(exception);
            Assert.Equal(StatusCodes.Status403Forbidden, apiException.StatusCode);
        }
    }
}

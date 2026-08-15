using EventManagement.Api.Infrastructure;
using EventManagement.Api.Models;
using EventManagement.Api.Services;
using Microsoft.AspNetCore.Http;

namespace EventManagement.Api.UnitTests.Services;

public sealed class EventAuthorizationServiceTests
{
    public static TheoryData<EventTeamRole?, EventCapability, bool> TeamMatrix => new()
    {
        { EventTeamRole.Admin, EventCapability.Edit, true },
        { EventTeamRole.Admin, EventCapability.ViewRevenue, true },
        { EventTeamRole.Admin, EventCapability.ManageTeam, true },
        { EventTeamRole.Admin, EventCapability.Delete, true },
        { EventTeamRole.Member, EventCapability.Edit, true },
        { EventTeamRole.Member, EventCapability.ManageOperations, true },
        { EventTeamRole.Member, EventCapability.ViewAttendees, true },
        { EventTeamRole.Member, EventCapability.CheckIn, true },
        { EventTeamRole.Member, EventCapability.ViewRevenue, false },
        { EventTeamRole.Member, EventCapability.ManageTeam, false },
        { EventTeamRole.Member, EventCapability.Delete, false },
        { EventTeamRole.CheckInStaff, EventCapability.ViewAttendees, true },
        { EventTeamRole.CheckInStaff, EventCapability.CheckIn, true },
        { EventTeamRole.CheckInStaff, EventCapability.Edit, false },
        { EventTeamRole.CheckInStaff, EventCapability.ManageOperations, false },
        { EventTeamRole.CheckInStaff, EventCapability.ViewRevenue, false },
        { EventTeamRole.CheckInStaff, EventCapability.ManageTeam, false },
        { EventTeamRole.CheckInStaff, EventCapability.Delete, false },
        { null, EventCapability.ViewAttendees, false }
    };

    [Theory, MemberData(nameof(TeamMatrix))]
    public void Team_roles_enforce_exact_capability_boundaries(
        EventTeamRole? role, EventCapability capability, bool allowed)
    {
        var exception = Record.Exception(() =>
            EventAuthorizationService.EnsureTeamCapability(role, capability));
        if (allowed) Assert.Null(exception);
        else Assert.Equal(StatusCodes.Status403Forbidden, Assert.IsType<ApiException>(exception).StatusCode);
    }
}

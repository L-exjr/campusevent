using EventManagement.Api.Services;

namespace EventManagement.Api.UnitTests.Services;

public sealed class RegistrationConfirmationEmailPolicyTests
{
    [Fact]
    public void Existing_registration_for_active_student_is_deliverable() =>
        Assert.True(RegistrationConfirmationEmailPolicy.ShouldDeliver(true, true));

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public void Missing_registration_or_inactive_student_is_not_deliverable(
        bool registrationExists,
        bool studentIsActive) =>
        Assert.False(RegistrationConfirmationEmailPolicy.ShouldDeliver(
            registrationExists,
            studentIsActive));
}

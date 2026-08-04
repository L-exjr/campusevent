using EventManagement.Api.Services;

namespace EventManagement.Api.UnitTests.Services;

public sealed class PasswordResetEmailPolicyTests
{
    private static readonly DateTimeOffset Now = new(2030, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Active_unused_future_token_is_deliverable() =>
        Assert.True(PasswordResetEmailPolicy.ShouldDeliver(true, null, Now.AddMinutes(1), Now));

    [Theory]
    [InlineData(false, false, 1)]
    [InlineData(true, true, 1)]
    [InlineData(true, false, 0)]
    [InlineData(true, false, -1)]
    public void Invalid_token_state_is_not_deliverable(
        bool userIsActive,
        bool used,
        int expiryOffsetMinutes) =>
        Assert.False(PasswordResetEmailPolicy.ShouldDeliver(
            userIsActive,
            used ? Now : null,
            Now.AddMinutes(expiryOffsetMinutes),
            Now));
}

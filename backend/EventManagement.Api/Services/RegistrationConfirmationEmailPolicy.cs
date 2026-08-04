namespace EventManagement.Api.Services;

public static class RegistrationConfirmationEmailPolicy
{
    public static bool ShouldDeliver(bool registrationExists, bool studentIsActive) =>
        registrationExists && studentIsActive;
}

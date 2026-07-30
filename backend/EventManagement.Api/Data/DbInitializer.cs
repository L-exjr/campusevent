using EventManagement.Api.Models;
using EventManagement.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Api.Data;

public static class DbInitializer
{
    public const string DevelopmentAdminEmail = "admin@dev.local";
    public const string DevelopmentAdminPassword = "Dev-Admin-Password-123!";
    public const string DevelopmentOrganizerEmail = "organizer@dev.local";
    public const string DevelopmentOrganizerPassword = "Dev-Organizer-Password-123!";

    public static async Task SeedDevelopmentUsersAsync(
        IServiceProvider services,
        IHostEnvironment environment,
        ILogger logger)
    {
        // Keep the environment check inside the seed as a second line of defense;
        // Program.cs also never invokes this method outside Development.
        if (!environment.IsDevelopment()) return;

        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        var admin = await dbContext.Users.SingleOrDefaultAsync(
            user => user.Email == DevelopmentAdminEmail);
        var adminCreated = admin is null;
        if (admin is null)
        {
            admin = new User
            {
                Name = "Development Administrator",
                Email = DevelopmentAdminEmail,
                PasswordHash = hasher.Hash(DevelopmentAdminPassword),
                Role = UserRole.Admin,
                IsActive = true
            };
            dbContext.Users.Add(admin);
            await dbContext.SaveChangesAsync();
        }

        var organizer = await dbContext.Users.SingleOrDefaultAsync(
            user => user.Email == DevelopmentOrganizerEmail);
        var organizerCreated = organizer is null;
        if (organizer is null)
        {
            // Registration creates Students. Persist that same initial role, then
            // promote through UserService so normal Organizer role rules run.
            organizer = new User
            {
                Name = "Development Organizer",
                Email = DevelopmentOrganizerEmail,
                PasswordHash = hasher.Hash(DevelopmentOrganizerPassword),
                Role = UserRole.Student,
                IsActive = true
            };
            dbContext.Users.Add(organizer);
            await dbContext.SaveChangesAsync();
        }
        if (organizer.Role == UserRole.Student)
        {
            await userService.UpdateRoleAsync(
                organizer.Id,
                UserRole.Organizer,
                admin.Id,
                CancellationToken.None);
        }

        if (adminCreated)
        {
            logger.LogWarning(
                "Development Admin seeded. Email: {Email} Password: {Password}",
                DevelopmentAdminEmail,
                DevelopmentAdminPassword);
        }
        if (organizerCreated)
        {
            logger.LogWarning(
                "Development Organizer seeded. Email: {Email} Password: {Password}",
                DevelopmentOrganizerEmail,
                DevelopmentOrganizerPassword);
        }
    }

    public static async Task SeedAdminAsync(IServiceProvider services, IConfiguration configuration)
    {
        var email = configuration["BootstrapAdmin:Email"]?.Trim().ToLowerInvariant();
        var password = configuration["BootstrapAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return;

        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (await dbContext.Users.AnyAsync(user => user.Email == email)) return;

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        dbContext.Users.Add(new User
        {
            Name = configuration["BootstrapAdmin:Name"]?.Trim() ?? "System Administrator",
            Email = email,
            PasswordHash = hasher.Hash(password),
            Role = UserRole.Admin,
            IsActive = true
        });
        await dbContext.SaveChangesAsync();
    }
}

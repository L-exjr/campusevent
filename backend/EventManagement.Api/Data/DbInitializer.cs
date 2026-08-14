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

    public static async Task SeedDemoDataAsync(
        IServiceProvider services,
        IConfiguration configuration,
        ILogger logger)
    {
        if (!configuration.GetValue("DemoData:Enabled", false)) return;

        var password = configuration["DemoData:Password"];
        if (string.IsNullOrWhiteSpace(password) || password.Length < 12)
        {
            logger.LogWarning(
                "Demo data was not seeded because DemoData:Password is missing or shorter than 12 characters.");
            return;
        }

        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var organizers = await EnsureUsersAsync(
            dbContext,
            hasher,
            password,
            UserRole.Organizer,
            [
                ("Akua Mensah", "akua.mensah.demo@knust.example"),
                ("Yaw Owusu", "yaw.owusu.demo@knust.example"),
                ("Ama Boateng", "ama.boateng.demo@knust.example")
            ]);
        var students = await EnsureUsersAsync(
            dbContext,
            hasher,
            password,
            UserRole.Student,
            [
                ("Kwame Asare", "kwame.asare.demo@knust.example"),
                ("Esi Agyeman", "esi.agyeman.demo@knust.example"),
                ("Kojo Antwi", "kojo.antwi.demo@knust.example"),
                ("Abena Osei", "abena.osei.demo@knust.example"),
                ("Kofi Badu", "kofi.badu.demo@knust.example"),
                ("Adwoa Nyarko", "adwoa.nyarko.demo@knust.example")
            ]);

        await dbContext.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;
        var events = new[]
        {
            new DemoEvent("KNUST Research & Innovation Showcase", "Students and researchers presented practical campus research, prototypes, and social-impact ideas.", now.AddDays(-21), "Great Hall, KNUST", 350, "Education & Learning", organizers[0]),
            new DemoEvent("Inter-Hall Football Finals", "A spirited inter-hall final celebrating teamwork, campus pride, and student sport.", now.AddDays(-5), "KNUST Sports Stadium", 1_200, "Festivals", organizers[1]),
            new DemoEvent("Campus Career & Innovation Fair", "Employers, alumni, and student founders are connecting over internships, graduate roles, and new ventures.", now.AddHours(-1), "College of Engineering Forecourt", 500, "Conferences", organizers[2]),
            new DemoEvent("KNUST Tech & AI Student Summit", "Hands-on sessions on responsible AI, software careers, and student-built technology.", now.AddDays(4), "E-Learning Centre Auditorium", 400, "Startup & Tech", organizers[0]),
            new DemoEvent("Republic Hall Cultural Night", "An evening of music, dance, food, and performances from across Ghanaian cultures.", now.AddDays(13), "Republic Hall Courtyard", 700, "Cultural Events", organizers[1]),
            new DemoEvent("Wellness Week: Mind and Body", "Student-led sessions on wellbeing, healthy routines, and accessing campus support.", now.AddDays(27), "University Hospital Conference Room", 180, "Health & Wellness", organizers[2])
        };

        foreach (var demoEvent in events)
        {
            var eventEntity = await dbContext.Events.SingleOrDefaultAsync(
                item => item.Title == demoEvent.Title);
            if (eventEntity is null)
            {
                eventEntity = new EventEntity
                {
                    Title = demoEvent.Title,
                    Description = demoEvent.Description,
                    Date = demoEvent.Date,
                    EndDate = demoEvent.Date.AddHours(2),
                    Location = demoEvent.Location,
                    Capacity = demoEvent.Capacity,
                    Category = demoEvent.Category,
                    OrganizerId = demoEvent.Organizer.Id,
                    Organizer = demoEvent.Organizer,
                    IsPublished = true,
                    RegistrationsEnabled = true
                };
                dbContext.Events.Add(eventEntity);
            }
        }
        await dbContext.SaveChangesAsync();

        var pastEvents = await dbContext.Events
            .Where(item => item.Title == events[0].Title || item.Title == events[1].Title)
            .OrderBy(item => item.Date)
            .ToListAsync();
        foreach (var (student, index) in students.Select((student, index) => (student, index)))
        {
            var pastEvent = pastEvents[index % pastEvents.Count];
            if (!await dbContext.EventRegistrations.AnyAsync(registration =>
                    registration.EventId == pastEvent.Id && registration.StudentId == student.Id))
            {
                dbContext.EventRegistrations.Add(new EventRegistration
                {
                    EventId = pastEvent.Id,
                    Event = pastEvent,
                    StudentId = student.Id,
                    Student = student,
                    RegisteredAt = pastEvent.Date.AddDays(-3),
                    Attended = index % 3 != 0
                });
            }
        }
        await dbContext.SaveChangesAsync();
        logger.LogInformation("KNUST demonstration data is ready.");
    }

    private static async Task<List<User>> EnsureUsersAsync(
        AppDbContext dbContext,
        IPasswordHasher hasher,
        string password,
        UserRole role,
        IEnumerable<(string Name, string Email)> definitions)
    {
        var users = new List<User>();
        foreach (var (name, email) in definitions)
        {
            var user = await dbContext.Users.SingleOrDefaultAsync(item => item.Email == email);
            if (user is null)
            {
                user = new User
                {
                    Name = name,
                    Email = email,
                    PasswordHash = hasher.Hash(password),
                    AuthProvider = AuthProvider.Local,
                    Role = role,
                    IsActive = true
                };
                dbContext.Users.Add(user);
            }
            users.Add(user);
        }
        return users;
    }

    private sealed record DemoEvent(
        string Title,
        string Description,
        DateTimeOffset Date,
        string Location,
        int Capacity,
        string Category,
        User Organizer);
}

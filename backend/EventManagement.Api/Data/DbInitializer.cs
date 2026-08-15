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
                ("Ama Boateng", "ama.boateng.demo@knust.example"),
                ("Nana Kusi", "nana.kusi.demo@knust.example"),
                ("Efua Sarpong", "efua.sarpong.demo@knust.example"),
                ("Kwaku Addai", "kwaku.addai.demo@knust.example")
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
            // Sourced event. KNUST SRC listing: https://src.knust.edu.gh/index.php?module=event
            new DemoEvent("Kumasi Art Experience Exhibition 2025", "A KNUST SRC-listed visual-art exhibition. DEMO CATALOG RECORD — not a live booking.", Utc(2025, 3, 14, 9), "Gardiner Conference Room, KNUST", 250, "Art & Exhibition", organizers[0]),
            // Sourced event. E-Week programme: https://elearning.knust.edu.gh/e-week-2026-programme-outline
            new DemoEvent("ELIC 2026 Poster Exhibition and Lightning Talks", "Poster viewing and lightning talks from the published E-Learning International Conference programme. DEMO CATALOG RECORD.", Utc(2026, 7, 16, 14), "Great Hall, KNUST", 500, "Art & Exhibition", organizers[1]),
            // Sourced event: https://www.knust.edu.gh/ (July 2026 event notice)
            new DemoEvent("Vice-Chancellor's Students' Excellence Awards 2026", "KNUST awards celebrating student achievement in academics, leadership, innovation and service. DEMO CATALOG RECORD.", Utc(2026, 7, 27, 14), "Great Hall, KNUST", 900, "Awards Event", organizers[2]),
            // Sourced event: https://ghanachieftaincyawards.com/
            new DemoEvent("Ghana Chieftaincy Awards & Royal Investment Summit 2026", "Awards and summit themed around environmental awareness, investment and land tenure. DEMO CATALOG RECORD.", Utc(2026, 5, 8, 9), "Great Hall, KNUST", 1000, "Awards Event", organizers[0]),

            // PLACEHOLDER: no sufficiently specific, sourced second local comedy listing was found.
            new DemoEvent("KNUST Campus Comedy Open Mic (Demo Placeholder)", "DEMO PLACEHOLDER — a plausible student comedy open mic; not a claimed real event.", now.AddDays(31), "Republic Hall JCR, KNUST", 180, "Comedy Shows", organizers[1]),
            // PLACEHOLDER: based only on the existence of Kumasi comedy programming, not a real listing.
            new DemoEvent("Kumasi Golden City Comedy Night (Demo Placeholder)", "DEMO PLACEHOLDER — a fictional demo listing; no organizer endorsement is implied.", now.AddDays(45), "Kumasi Cultural Centre", 350, "Comedy Shows", organizers[2]),
            // Sourced event: https://www.nydjlive.com/kumasi-set-for-a-transformational-encounter-as-invasion-2026-hits-pensa-knust/
            new DemoEvent("Invasion 2026", "A worship gathering featuring music ministers and speakers, catalogued here as music. DEMO CATALOG RECORD.", Utc(2026, 8, 2, 16), "PENSA KNUST, Ayeduase", 900, "Concerts & Music", organizers[0]),
            // PLACEHOLDER: BSIFF confirms a concert programme but does not publish its exact date/venue.
            new DemoEvent("Kumasi Emerging Artists Concert (Demo Placeholder)", "DEMO PLACEHOLDER — a fictional emerging-artists concert; not the separately advertised BSIFF concert.", now.AddDays(47), "Kumasi Cultural Centre", 450, "Concerts & Music", organizers[1]),
            // Sourced event: https://elearningconference.knust.edu.gh/
            new DemoEvent("E-Learning International Conference 2026", "Conference on inclusive, equitable and ethical learning ecosystems powered by AI and emerging technology. DEMO CATALOG RECORD.", Utc(2026, 7, 16, 8), "Great Hall, KNUST", 500, "Conferences", organizers[2]),
            // Sourced event: https://www.knust.edu.gh/announcements/general/mineral-waste-valorization-research-conference-2026
            new DemoEvent("Mineral Waste Valorization Research Conference 2026", "Academic and industry conference on sustainable mineral-waste recovery and resource valorization. DEMO CATALOG RECORD.", Utc(2026, 6, 25, 9), "KNUST, Kumasi", 300, "Conferences", organizers[0]),
            // Sourced event: same Invasion 2026 source above; classified here as a faith/community gathering.
            new DemoEvent("Invasion 2026 Community Gathering", "A sourced faith and community gathering. This separate demo record illustrates cultural-event discovery, not a second occurrence.", Utc(2026, 8, 2, 16), "PENSA KNUST, Ayeduase", 900, "Cultural Events", organizers[1]),
            // PLACEHOLDER: no second event with sufficiently precise source facts was found.
            new DemoEvent("Adinkra Heritage Evening (Demo Placeholder)", "DEMO PLACEHOLDER — a plausible KNUST cultural programme; not a claimed real event.", now.AddDays(38), "Centre for Cultural and African Studies, KNUST", 300, "Cultural Events", organizers[2]),
            // Sourced event: https://elearning.knust.edu.gh/e-week-2026-programme-outline
            new DemoEvent("E-Learning Open Day for Basic Schools 2026", "An E-Week open day for pupils from KNUST Basic School and an IDL-adopted school. DEMO CATALOG RECORD.", Utc(2026, 7, 13, 9), "KNUST Library Mall", 150, "Education & Learning", organizers[0]),
            // Sourced event: https://cos.knust.edu.gh/
            new DemoEvent("High Skuul Maths League 2026", "A College of Science-listed mathematics education event. DEMO CATALOG RECORD.", Utc(2026, 5, 28, 9), "Great Hall, KNUST", 600, "Education & Learning", organizers[1]),
            // User-confirmed event; public source URL was not discoverable in this research pass.
            new DemoEvent("KNUST Fashion Show 2026", "Theme: Becoming: From Excellence to Eminence. User-confirmed demo catalog record; no platform endorsement is implied.", Utc(2026, 8, 7, 18), "Great Hall, KNUST", 1000, "Fashion & Beauty", organizers[2]),
            // PLACEHOLDER: no second precise KNUST/Kumasi fashion listing was found.
            new DemoEvent("Kumasi Student Designers Showcase (Demo Placeholder)", "DEMO PLACEHOLDER — a fictional student design showcase for interface demonstrations.", now.AddDays(52), "KNUST Commercial Area", 280, "Fashion & Beauty", organizers[0]),
            // Sourced event: https://bsiff.org/
            new DemoEvent("Black Star International Film Festival 2026", "The 11th BSIFF, hosted in Kumasi from 24–27 September 2026. DEMO CATALOG RECORD.", Utc(2026, 9, 24, 9), "Golden Eagle Cinemas, Kumasi", 500, "Festivals", organizers[1]),
            // PLACEHOLDER: no second precise local festival listing was found.
            new DemoEvent("KNUST Halls Heritage Festival (Demo Placeholder)", "DEMO PLACEHOLDER — a fictional campus festival used only as seed data.", now.AddDays(60), "KNUST Royal Parade Grounds", 1200, "Festivals", organizers[2]),

            // PLACEHOLDERS: the research pass found no sufficiently specific local Food & Drink listings.
            new DemoEvent("Kumasi Campus Food Fair (Demo Placeholder)", "DEMO PLACEHOLDER — a fictional food fair; not a live booking.", now.AddDays(34), "KNUST Botanical Garden", 500, "Food & Drink", organizers[0]),
            new DemoEvent("Ashanti Flavours Tasting Night (Demo Placeholder)", "DEMO PLACEHOLDER — a fictional culinary showcase; no vendor endorsement is implied.", now.AddDays(69), "Kumasi Cultural Centre", 240, "Food & Drink", organizers[1]),
            // PLACEHOLDERS: the research pass found no sufficiently specific local Gaming & Esports listings.
            new DemoEvent("KNUST Inter-Hall Esports Cup (Demo Placeholder)", "DEMO PLACEHOLDER — a fictional campus esports tournament.", now.AddDays(28), "KNUST E-Learning Centre", 200, "Gaming & Esports", organizers[2]),
            new DemoEvent("Kumasi FIFA Community Tournament (Demo Placeholder)", "DEMO PLACEHOLDER — a fictional gaming event for seed coverage.", now.AddDays(57), "Adum Community Hub, Kumasi", 160, "Gaming & Esports", organizers[0]),
            // Sourced event: https://kems.knust.edu.gh/ (September 2026 listing)
            new DemoEvent("Ghana Data Science Regional Hackathon", "A KNUST Events Management System-listed regional data-science hackathon. DEMO CATALOG RECORD.", Utc(2026, 9, 14, 9), "KNUST, Kumasi", 250, "Hackathons", organizers[1]),
            // Sourced event: https://www.knust.edu.gh/news/news-items/knust-cocoa-innovation-hackathon-promotes-youth-led-solutions-sustainable-cocoa-sector
            // Date/time/location corroboration: https://gh.linkedin.com/in/baffour-gyimah-kwame
            new DemoEvent("Ghana Cocoa Innovation Hackathon 2026", "Department of Horticulture-led hackathon on practical solutions for a sustainable cocoa value chain. DEMO CATALOG RECORD.", Utc(2026, 7, 22, 8), "NCB Ground Floor, CANR-KNUST", 180, "Hackathons", organizers[2]),
            // Sourced event: https://kems.knust.edu.gh/ (September 2026 listing)
            new DemoEvent("Convention for Biomedical Research Ghana (COBREG 2026)", "A three-day biomedical research convention listed by KNUST KEMS. DEMO CATALOG RECORD.", Utc(2026, 9, 9, 9), "KNUST, Kumasi", 350, "Health & Wellness", organizers[0]),
            // Sourced event: https://kems.knust.edu.gh/ (September 2026 listing)
            new DemoEvent("NYANSAPO Photonics School for Health and Safety", "A six-day photonics school listed by KNUST KEMS. DEMO CATALOG RECORD.", Utc(2026, 9, 21, 9), "KNUST, Kumasi", 180, "Health & Wellness", organizers[1]),
            // Sourced event: https://bsiff.org/
            new DemoEvent("BSIFF 2026 Film Screenings", "Independent African film screenings within the 24–27 September festival. DEMO CATALOG RECORD.", Utc(2026, 9, 24, 10), "Golden Eagle Cinemas, Kumasi", 450, "Movies & Film", organizers[2]),
            // PLACEHOLDER: BSIFF confirms the festival dates but not an exact 2026 awards-session date.
            new DemoEvent("KNUST Student Short Film Night (Demo Placeholder)", "DEMO PLACEHOLDER — a fictional student screening; not a claimed BSIFF session.", now.AddDays(54), "CCB Auditorium, KNUST", 260, "Movies & Film", organizers[0]),
            // User-confirmed event; public source URL was not discoverable in this research pass.
            new DemoEvent("KNUST Alumni Day 2026", "User-confirmed alumni event. DEMO CATALOG RECORD — not a live booking.", Utc(2026, 8, 12, 10), "College of Science Auditorium, KNUST", 350, "Other", organizers[1]),
            // Sourced event: https://www.knust.edu.gh/ (August 2026 notice)
            new DemoEvent("Investiture of Professor Christian Agyare", "Investiture of KNUST's 13th Vice-Chancellor. DEMO CATALOG RECORD.", Utc(2026, 8, 1, 10), "Great Hall, KNUST", 1000, "Other", organizers[2]),
            // Sourced listing: https://app.jedevent.com/
            new DemoEvent("MISS KNUST 2026", "A KNUST pageant/voting listing visible on Jedevent. DEMO CATALOG RECORD.", Utc(2026, 6, 1, 18), "KNUST, Kumasi", 800, "Pageant", organizers[0]),
            // Sourced listing: https://app.jedevent.com/
            new DemoEvent("MISS COHSS 2026", "A KNUST pageant/voting listing visible on Jedevent. DEMO CATALOG RECORD.", Utc(2026, 7, 10, 18), "KNUST, Kumasi", 600, "Pageant", organizers[1]),
            // Sourced listing: https://app.jedevent.com/
            new DemoEvent("Alonzy Akwaaba Night", "A KNUST nightlife listing visible on Jedevent. DEMO CATALOG RECORD.", Utc(2026, 2, 28, 19), "KNUST, Kumasi", 500, "Parties & Nightlife", organizers[2]),
            // PLACEHOLDER: no second sufficiently precise local nightlife listing was found.
            new DemoEvent("Tech Junction Campus Night (Demo Placeholder)", "DEMO PLACEHOLDER — a fictional social night; not a claimed real event.", now.AddDays(41), "Ayeduase, Kumasi", 450, "Parties & Nightlife", organizers[0]),
            // Sourced event: https://www.knust.edu.gh/events/general/femstem-africa-2026-convening
            new DemoEvent("FemSTEM Africa 2026 Convening", "Women-led STEM, health innovation and entrepreneurship convening. DEMO CATALOG RECORD.", Utc(2026, 6, 1, 9), "Great Hall and Impact Building, KNUST", 500, "Startup & Tech", organizers[1]),
            // Sourced event: https://elearning.knust.edu.gh/e-week-2026-programme-outline
            new DemoEvent("EdTech Spotlight: Student Innovation Showcase", "Student showcase featuring home-grown education technology projects at ELIC 2026. DEMO CATALOG RECORD.", Utc(2026, 7, 17, 10), "Great Hall, KNUST", 500, "Startup & Tech", organizers[2]),
            // Sourced event: https://elearning.knust.edu.gh/e-week-2026-programme-outline
            new DemoEvent("E-Week 2026 Parallel Workshops and Tutorials", "Published hands-on workshop programme on AI and digital learning. DEMO CATALOG RECORD.", Utc(2026, 7, 15, 9), "KNUST Library Mall", 300, "Workshops & Training", organizers[0]),
            // PLACEHOLDER: a real ToT programme was reported, but exact session dates were not published.
            new DemoEvent("Campus Event Operations Bootcamp (Demo Placeholder)", "DEMO PLACEHOLDER — a fictional practical training programme for student organizers.", now.AddDays(63), "KNUST School of Business", 120, "Workshops & Training", organizers[1])
        };

        var organizerProfiles = new[]
        {
            new DemoOrganizerProfile(VerificationStatus.Verified, true,
                "Demo organizer focused on campus arts, learning programmes and community gatherings.",
                "https://placehold.co/1200x320/4338ca/ffffff?text=Akua+Mensah+Demo",
                "https://instagram.com/akua.mensah.demo", "https://x.com/akuamensahdemo", null,
                "https://akua-mensah.demo.example"),
            new DemoOrganizerProfile(VerificationStatus.Verified, true,
                "Demo events team presenting conferences, festivals and student innovation showcases.",
                "https://placehold.co/1200x320/047857/ffffff?text=Yaw+Owusu+Demo",
                "https://instagram.com/yaw.owusu.demo", null, "https://facebook.com/yaw.owusu.demo",
                "https://yaw-owusu.demo.example"),
            new DemoOrganizerProfile(VerificationStatus.Pending, true,
                "Demo campus producer working across awards, culture and creative student experiences.",
                "https://placehold.co/1200x320/b45309/ffffff?text=Ama+Boateng+Demo",
                "https://instagram.com/ama.boateng.demo", "https://x.com/amaboatengdemo", null,
                "https://ama-boateng.demo.example"),
            new DemoOrganizerProfile(VerificationStatus.Pending, false,
                "Demo programme coordinator for workshops, wellbeing and technology events.",
                "https://placehold.co/1200x320/7c3aed/ffffff?text=Nana+Kusi+Demo",
                null, "https://x.com/nanakusidemo", "https://facebook.com/nana.kusi.demo",
                "https://nana-kusi.demo.example"),
            new DemoOrganizerProfile(VerificationStatus.Unverified, false, null, null, null, null, null, null),
            new DemoOrganizerProfile(VerificationStatus.Unverified, false, null, null, null, null, null, null)
        };

        for (var index = 0; index < organizers.Count; index++)
        {
            var organizer = organizers[index];
            var profile = organizerProfiles[index];
            organizer.VerificationStatus = profile.VerificationStatus;
            organizer.IsOrganizerDirectoryVisible = profile.IsDirectoryVisible;
            organizer.OrganizerBio = profile.Bio;
            organizer.OrganizerBannerUrl = profile.BannerUrl;
            organizer.OrganizerInstagramUrl = profile.InstagramUrl;
            organizer.OrganizerTwitterUrl = profile.TwitterUrl;
            organizer.OrganizerFacebookUrl = profile.FacebookUrl;
            organizer.OrganizerWebsiteUrl = profile.WebsiteUrl;
        }

        foreach (var organizer in organizers.Where(item =>
                     item.VerificationStatus == VerificationStatus.Pending))
        {
            var hasPendingApplication = await dbContext.OrganizerApplications.AnyAsync(application =>
                application.UserId == organizer.Id && application.Status == ApplicationStatus.Pending);
            if (!hasPendingApplication)
            {
                dbContext.OrganizerApplications.Add(new OrganizerApplication
                {
                    UserId = organizer.Id,
                    User = organizer,
                    Reason = "Demo verification request for testing the administrator review workflow.",
                    Status = ApplicationStatus.Pending,
                    SubmittedAt = now.AddDays(-2)
                });
            }
        }

        foreach (var (demoEvent, index) in events.Select((item, index) => (item, index)))
        {
            var assignedOrganizer = organizers[index % organizers.Count];
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
                    OrganizerId = assignedOrganizer.Id,
                    Organizer = assignedOrganizer,
                    // Upcoming demo records are discoverable; historical catalog records remain drafts.
                    IsPublished = demoEvent.Date > now,
                    RegistrationsEnabled = true
                };
                dbContext.Events.Add(eventEntity);
            }
            else
            {
                // Converge repeat seed runs as fixed-date records move into the past.
                eventEntity.IsPublished = demoEvent.Date > now;
                eventEntity.OrganizerId = assignedOrganizer.Id;
                eventEntity.Organizer = assignedOrganizer;
            }
        }

        for (var index = 0; index < organizers.Count; index++)
        {
            var organizer = organizers[index];
            var expectedSpecialties = events
                .Select((item, eventIndex) => (item.Category, eventIndex))
                .Where(item => item.eventIndex % organizers.Count == index)
                .Select(item => item.Category)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var existingSpecialties = await dbContext.OrganizerSpecialties
                .Where(item => item.OrganizerId == organizer.Id)
                .ToListAsync();
            dbContext.OrganizerSpecialties.RemoveRange(
                existingSpecialties.Where(item => !expectedSpecialties.Contains(item.Category)));
            foreach (var category in expectedSpecialties.Where(category =>
                         existingSpecialties.All(item => !string.Equals(
                             item.Category, category, StringComparison.OrdinalIgnoreCase))))
            {
                dbContext.OrganizerSpecialties.Add(new OrganizerSpecialty
                {
                    OrganizerId = organizer.Id,
                    Organizer = organizer,
                    Category = category
                });
            }
        }

        var retiredDemoTitles = new[]
        {
            "KNUST Research & Innovation Showcase", "Inter-Hall Football Finals",
            "Campus Career & Innovation Fair", "KNUST Tech & AI Student Summit",
            "Republic Hall Cultural Night", "Wellness Week: Mind and Body"
        };
        await dbContext.Events
            .Where(item => retiredDemoTitles.Contains(item.Title) &&
                           item.Organizer.Email.EndsWith(".demo@knust.example"))
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.IsPublished, false));
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

    private sealed record DemoOrganizerProfile(
        VerificationStatus VerificationStatus,
        bool IsDirectoryVisible,
        string? Bio,
        string? BannerUrl,
        string? InstagramUrl,
        string? TwitterUrl,
        string? FacebookUrl,
        string? WebsiteUrl);

    private static DateTimeOffset Utc(int year, int month, int day, int hour) =>
        new(year, month, day, hour, 0, 0, TimeSpan.Zero);
}

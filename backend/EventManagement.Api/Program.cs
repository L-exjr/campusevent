using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json.Serialization;
using EventManagement.Api.Data;
using EventManagement.Api.Infrastructure;
using EventManagement.Api.Middleware;
using EventManagement.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(allowIntegerValues: false)));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

// The signing key must come from .NET User Secrets in local development or
// Jwt__SigningKey in the deployed environment; tracked settings contain no key.
var jwtSigningKey = builder.Configuration["Jwt:SigningKey"];
if (string.IsNullOrWhiteSpace(jwtSigningKey) || jwtSigningKey.Length < 32)
    throw new InvalidOperationException(
        "Jwt:SigningKey must be configured with at least 32 characters.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "EventManagement.Api";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "EventManagement.Frontend";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
            NameClaimType = JwtRegisteredClaimNames.Name,
            RoleClaimType = JwtClaimNames.Role,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsJsonAsync(new { error = "Authentication is required." });
            },
            OnForbidden = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsJsonAsync(new { error = "You do not have permission to perform this action." });
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    ForwardedHeadersConfiguration.Configure(options);
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter =
                Math.Ceiling(retryAfter.TotalSeconds).ToString();
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { error = "Too many requests. Please wait and try again." },
            cancellationToken);
    };
    options.AddPolicy("PublicBookingRequests", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var message = context.ModelState.Values
            .SelectMany(value => value.Errors)
            .Select(error => error.ErrorMessage)
            .FirstOrDefault(error => !string.IsNullOrWhiteSpace(error))
            ?? "The request body is invalid.";
        return new BadRequestObjectResult(new { error = message });
    };
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthRateLimitService, AuthRateLimitService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
builder.Services.AddScoped<IBookingRequestService, BookingRequestService>();
builder.Services.AddScoped<IOrganizerApplicationService, OrganizerApplicationService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEventAuthorizationService, EventAuthorizationService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IImageStorageService, SupabaseImageStorageService>();
builder.Services.AddScoped<IImageLifecycleService, ImageLifecycleService>();
builder.Services.AddScoped<IEmailService, MailtrapEmailService>();
builder.Services.AddScoped<EventReminderEnqueuer>();
builder.Services.AddScoped<IEmailOutboxHandler, EventReminderEmailOutboxHandler>();
builder.Services.AddScoped<IEmailOutboxHandler, PayloadEmailOutboxHandler>();
builder.Services.AddScoped<EmailOutboxMessageProcessor>();
// A database-backed scheduler such as Hangfire is the natural upgrade if
// reminder volume, retry guarantees, or timing precision outgrow this worker.
builder.Services.AddHostedService<EmailOutboxBackgroundService>();
builder.Services.AddHostedService<ImageCleanupBackgroundService>();

var app = builder.Build();

// Railway enables this explicitly with Database__ApplyMigrations=true. Running
// migrations before the health endpoint is available prevents a deployment
// from serving traffic against an older schema. Other environments remain
// unchanged unless they deliberately opt in.
if (builder.Configuration.GetValue("Database:ApplyMigrations", false))
{
    await using var migrationScope = app.Services.CreateAsyncScope();
    var migrationDbContext = migrationScope.ServiceProvider.GetRequiredService<AppDbContext>();
    app.Logger.LogInformation("Applying pending EF Core database migrations.");
    await migrationDbContext.Database.MigrateAsync();
    app.Logger.LogInformation("EF Core database migrations are up to date.");
}

app.UseForwardedHeaders();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
if (!app.Environment.IsDevelopment()) app.UseHsts();
app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseMiddleware<AuthIpRateLimitMiddleware>();
app.UseRateLimiter();
app.UseStatusCodePages(async statusCodeContext =>
{
    var response = statusCodeContext.HttpContext.Response;
    if (response.HasStarted || response.ContentLength.HasValue) return;
    var message = response.StatusCode switch
    {
        StatusCodes.Status404NotFound => "The requested resource was not found.",
        StatusCodes.Status405MethodNotAllowed => "The HTTP method is not allowed for this endpoint.",
        _ => "The request could not be completed."
    };
    response.ContentType = "application/json";
    await response.WriteAsJsonAsync(new { error = message });
});
app.UseMiddleware<ActiveUserMiddleware>();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    await DbInitializer.SeedDevelopmentUsersAsync(
        app.Services,
        app.Environment,
        app.Logger);
}
await DbInitializer.SeedAdminAsync(app.Services, app.Configuration);
await DbInitializer.SeedDemoDataAsync(app.Services, app.Configuration, app.Logger);

app.Run();

public partial class Program;

using EventManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<OrganizerApplication> OrganizerApplications => Set<OrganizerApplication>();
    public DbSet<EventEntity> Events => Set<EventEntity>();
    public DbSet<EventRegistration> EventRegistrations => Set<EventRegistration>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<BookingRequest> BookingRequests => Set<BookingRequest>();
    public DbSet<EmailOutboxMessage> EmailOutboxMessages => Set<EmailOutboxMessage>();
    public DbSet<ImageUpload> ImageUploads => Set<ImageUpload>();
    public DbSet<AuthRateLimitBucket> AuthRateLimitBuckets => Set<AuthRateLimitBucket>();
    public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Name).HasMaxLength(150).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(320).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(500);
            entity.Property(user => user.AuthProvider).HasConversion<string>().HasMaxLength(30);
            entity.Property(user => user.GoogleSubject).HasMaxLength(255);
            entity.Property(user => user.ImageUrl).HasMaxLength(2048);
            entity.Property(user => user.ImageObjectKey).HasMaxLength(1024);
            entity.Property(user => user.Role).HasConversion<string>().HasMaxLength(30);
            entity.Property(user => user.SessionVersion).HasDefaultValue(1);
            entity.HasIndex(user => user.Email).IsUnique();
            entity.HasIndex(user => user.GoogleSubject).IsUnique();
        });

        modelBuilder.Entity<OrganizerApplication>(entity =>
        {
            entity.HasKey(application => application.Id);
            entity.Property(application => application.Reason).HasMaxLength(2000).IsRequired();
            entity.Property(application => application.RejectionReason).HasMaxLength(1000);
            entity.Property(application => application.Status).HasConversion<string>().HasMaxLength(30);
            entity.HasIndex(application => application.UserId)
                .IsUnique()
                .HasFilter("\"Status\" = 'Pending'");
            entity.HasOne(application => application.User)
                .WithMany(user => user.OrganizerApplications)
                .HasForeignKey(application => application.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(application => application.ReviewedByAdmin)
                .WithMany()
                .HasForeignKey(application => application.ReviewedByAdminId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<EventEntity>(entity =>
        {
            entity.ToTable("Events");
            entity.HasKey(eventEntity => eventEntity.Id);
            entity.Property(eventEntity => eventEntity.Title).HasMaxLength(200).IsRequired();
            entity.Property(eventEntity => eventEntity.Description).HasMaxLength(5000).IsRequired();
            entity.Property(eventEntity => eventEntity.Location).HasMaxLength(300).IsRequired();
            entity.Property(eventEntity => eventEntity.Category).HasMaxLength(100).IsRequired();
            entity.Property(eventEntity => eventEntity.ImageUrl).HasMaxLength(2048);
            entity.Property(eventEntity => eventEntity.ImageObjectKey).HasMaxLength(1024);
            entity.Property(eventEntity => eventEntity.Version).HasDefaultValue(1).IsConcurrencyToken();
            entity.HasIndex(eventEntity => eventEntity.Date);
            entity.HasIndex(eventEntity => eventEntity.Category);
            entity.HasOne(eventEntity => eventEntity.Organizer)
                .WithMany(user => user.OrganizedEvents)
                .HasForeignKey(eventEntity => eventEntity.OrganizerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EventRegistration>(entity =>
        {
            entity.HasKey(registration => registration.Id);
            entity.HasIndex(registration => new { registration.EventId, registration.StudentId }).IsUnique();
            entity.HasOne(registration => registration.Event)
                .WithMany(eventEntity => eventEntity.Registrations)
                .HasForeignKey(registration => registration.EventId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(registration => registration.Student)
                .WithMany(user => user.Registrations)
                .HasForeignKey(registration => registration.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(token => token.Id);
            entity.Property(token => token.TokenHash).HasMaxLength(64).IsRequired();
            entity.HasIndex(token => token.TokenHash).IsUnique();
            entity.HasIndex(token => new { token.UserId, token.ExpiresAt });
            entity.HasOne(token => token.User)
                .WithMany(user => user.PasswordResetTokens)
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BookingRequest>(entity =>
        {
            entity.HasKey(request => request.Id);
            entity.Property(request => request.OrganizationName).HasMaxLength(200).IsRequired();
            entity.Property(request => request.ContactName).HasMaxLength(150).IsRequired();
            entity.Property(request => request.Email).HasMaxLength(320).IsRequired();
            entity.Property(request => request.Phone).HasMaxLength(50).IsRequired();
            entity.Property(request => request.EventType).HasMaxLength(150).IsRequired();
            entity.Property(request => request.AlternativeDates).HasMaxLength(500);
            entity.Property(request => request.FlexibilityNote).HasMaxLength(1000);
            entity.Property(request => request.PreferredOrganizer).HasMaxLength(200);
            entity.Property(request => request.Description).HasMaxLength(5000).IsRequired();
            entity.Property(request => request.OrganizerResponseNote).HasMaxLength(1000);
            entity.Property(request => request.Status).HasConversion<string>().HasMaxLength(30);
            entity.HasIndex(request => request.Status);
            entity.HasIndex(request => new { request.Status, request.UpdatedAt });
            entity.HasIndex(request => request.SubmittedAt);
            entity.HasOne(request => request.AssignedOrganizer)
                .WithMany(user => user.AssignedBookingRequests)
                .HasForeignKey(request => request.AssignedOrganizerId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(request => request.DraftEvent)
                .WithOne(eventEntity => eventEntity.SourceBookingRequest)
                .HasForeignKey<BookingRequest>(request => request.DraftEventId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<EmailOutboxMessage>(entity =>
        {
            entity.HasKey(message => message.Id);
            entity.Property(message => message.IdempotencyKey).HasMaxLength(200).IsRequired();
            entity.Property(message => message.Kind).HasMaxLength(100).IsRequired();
            entity.Property(message => message.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(message => message.LastError).HasMaxLength(2000);
            entity.Property(message => message.PayloadJson).HasMaxLength(20000);
            entity.HasIndex(message => message.IdempotencyKey).IsUnique();
            entity.HasIndex(message => new { message.Status, message.AvailableAt });
            entity.HasIndex(message => message.ClaimedBy);
        });

        modelBuilder.Entity<ImageUpload>(entity =>
        {
            entity.HasKey(upload => upload.Id);
            entity.Property(upload => upload.Bucket).HasMaxLength(100).IsRequired();
            entity.Property(upload => upload.ObjectKey).HasMaxLength(1024).IsRequired();
            entity.Property(upload => upload.PublicUrl).HasMaxLength(2048).IsRequired();
            entity.Property(upload => upload.Kind).HasConversion<string>().HasMaxLength(30);
            entity.Property(upload => upload.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(upload => upload.LastError).HasMaxLength(2000);
            entity.HasIndex(upload => new { upload.Bucket, upload.ObjectKey }).IsUnique();
            entity.HasIndex(upload => upload.PublicUrl).IsUnique();
            entity.HasIndex(upload => new { upload.Status, upload.AvailableAt });
            entity.HasIndex(upload => upload.DeletionClaimedBy);
            entity.HasOne(upload => upload.Owner)
                .WithMany(user => user.ImageUploads)
                .HasForeignKey(upload => upload.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuthRateLimitBucket>(entity =>
        {
            entity.HasKey(bucket => bucket.Key);
            entity.Property(bucket => bucket.Key).HasMaxLength(160);
            entity.HasIndex(bucket => bucket.UpdatedAt);
        });

        modelBuilder.Entity<AdminAuditLog>(entity =>
        {
            entity.HasKey(log => log.Id);
            entity.Property(log => log.Action).HasMaxLength(100).IsRequired();
            entity.Property(log => log.TargetType).HasMaxLength(100).IsRequired();
            entity.Property(log => log.TargetId).HasMaxLength(200).IsRequired();
            entity.Property(log => log.DetailsJson).HasColumnType("jsonb").IsRequired();
            entity.HasIndex(log => log.CreatedAt);
            entity.HasIndex(log => new { log.TargetType, log.TargetId });
            entity.HasOne(log => log.ActorUser)
                .WithMany()
                .HasForeignKey(log => log.ActorUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

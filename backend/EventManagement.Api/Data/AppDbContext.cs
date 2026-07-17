using EventManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<OrganizerApplication> OrganizerApplications => Set<OrganizerApplication>();
    public DbSet<EventEntity> Events => Set<EventEntity>();
    public DbSet<EventRegistration> EventRegistrations => Set<EventRegistration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Name).HasMaxLength(150).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(320).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(user => user.Role).HasConversion<string>().HasMaxLength(30);
            entity.HasIndex(user => user.Email).IsUnique();
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
    }
}

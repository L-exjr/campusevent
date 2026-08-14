using EventManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<OrganizerSpecialty> OrganizerSpecialties => Set<OrganizerSpecialty>();
    public DbSet<OrganizerApplication> OrganizerApplications => Set<OrganizerApplication>();
    public DbSet<EventEntity> Events => Set<EventEntity>();
    public DbSet<EventRegistration> EventRegistrations => Set<EventRegistration>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<BookingRequest> BookingRequests => Set<BookingRequest>();
    public DbSet<EmailOutboxMessage> EmailOutboxMessages => Set<EmailOutboxMessage>();
    public DbSet<ImageUpload> ImageUploads => Set<ImageUpload>();
    public DbSet<AuthRateLimitBucket> AuthRateLimitBuckets => Set<AuthRateLimitBucket>();
    public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();
    public DbSet<PaymentOrder> PaymentOrders => Set<PaymentOrder>();
    public DbSet<TicketTier> TicketTiers => Set<TicketTier>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<PaymentWebhookReceipt> PaymentWebhookReceipts => Set<PaymentWebhookReceipt>();
    public DbSet<VotingCampaign> VotingCampaigns => Set<VotingCampaign>();
    public DbSet<VotingCategory> VotingCategories => Set<VotingCategory>();
    public DbSet<VotingNominee> VotingNominees => Set<VotingNominee>();
    public DbSet<VoteRecord> VoteRecords => Set<VoteRecord>();
    public DbSet<VotingPaymentOrder> VotingPaymentOrders => Set<VotingPaymentOrder>();
    public DbSet<VotingWebhookReceipt> VotingWebhookReceipts => Set<VotingWebhookReceipt>();

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
            entity.Property(user => user.OrganizerBio).HasMaxLength(3000);
            entity.Property(user => user.OrganizerBannerUrl).HasMaxLength(2048);
            entity.Property(user => user.OrganizerBannerObjectKey).HasMaxLength(1024);
            entity.Property(user => user.OrganizerInstagramUrl).HasMaxLength(2048);
            entity.Property(user => user.OrganizerTwitterUrl).HasMaxLength(2048);
            entity.Property(user => user.OrganizerFacebookUrl).HasMaxLength(2048);
            entity.Property(user => user.OrganizerWebsiteUrl).HasMaxLength(2048);
            entity.Property(user => user.IsOrganizerDirectoryVisible).HasDefaultValue(false);
            entity.Property(user => user.Role).HasConversion<string>().HasMaxLength(30);
            entity.Property(user => user.SessionVersion).HasDefaultValue(1);
            entity.HasIndex(user => user.Email).IsUnique();
            entity.HasIndex(user => user.GoogleSubject).IsUnique();
        });

        modelBuilder.Entity<OrganizerSpecialty>(entity =>
        {
            entity.HasKey(item => new { item.OrganizerId, item.Category });
            entity.Property(item => item.Category).HasMaxLength(100).IsRequired();
            entity.HasIndex(item => item.Category);
            entity.HasOne(item => item.Organizer)
                .WithMany(user => user.OrganizerSpecialties)
                .HasForeignKey(item => item.OrganizerId)
                .OnDelete(DeleteBehavior.Cascade);
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
            entity.Property(eventEntity => eventEntity.Format)
                .HasMaxLength(20)
                .HasDefaultValue("Physical")
                .IsRequired();
            entity.Property(eventEntity => eventEntity.MeetingUrl).HasMaxLength(2048);
            entity.Property(eventEntity => eventEntity.VirtualPlatform).HasMaxLength(40);
            entity.Property(eventEntity => eventEntity.InstagramUrl).HasMaxLength(2048);
            entity.Property(eventEntity => eventEntity.TwitterUrl).HasMaxLength(2048);
            entity.Property(eventEntity => eventEntity.FacebookUrl).HasMaxLength(2048);
            entity.Property(eventEntity => eventEntity.WebsiteUrl).HasMaxLength(2048);
            entity.Property(eventEntity => eventEntity.Category).HasMaxLength(100).IsRequired();
            entity.Property(eventEntity => eventEntity.Currency)
                .HasMaxLength(3)
                .HasDefaultValue("GHS")
                .IsRequired();
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
            entity.Property(registration => registration.TicketCode).HasMaxLength(20).IsRequired();
            entity.HasIndex(registration => registration.TicketCode).IsUnique();
            entity.Property(registration => registration.CertificateObjectKey).HasMaxLength(1024);
            entity.HasIndex(registration => new { registration.EventId, registration.StudentId }).IsUnique();
            entity.HasOne(registration => registration.Event)
                .WithMany(eventEntity => eventEntity.Registrations)
                .HasForeignKey(registration => registration.EventId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(registration => registration.Student)
                .WithMany(user => user.Registrations)
                .HasForeignKey(registration => registration.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(registration => registration.PaymentOrder)
                .WithOne(order => order.Registration)
                .HasForeignKey<EventRegistration>(registration => registration.PaymentOrderId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(registration => registration.PaymentOrderId).IsUnique();
        });

        modelBuilder.Entity<PaymentOrder>(entity =>
        {
            entity.HasKey(order => order.Id);
            entity.Property(order => order.Currency).HasMaxLength(3).IsRequired();
            entity.Property(order => order.Provider).HasMaxLength(30).IsRequired();
            entity.Property(order => order.ProviderReference).HasMaxLength(100).IsRequired();
            entity.Property(order => order.AuthorizationUrl).HasMaxLength(2048);
            entity.Property(order => order.Status).HasConversion<string>().HasMaxLength(30);
            entity.HasIndex(order => order.ProviderReference).IsUnique();
            entity.HasIndex(order => new { order.EventId, order.StudentId, order.Status });
            entity.HasIndex(order => new { order.Status, order.ExpiresAt });
            entity.HasOne(order => order.Event)
                .WithMany(eventEntity => eventEntity.PaymentOrders)
                .HasForeignKey(order => order.EventId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(order => order.Student)
                .WithMany()
                .HasForeignKey(order => order.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(order => order.TicketTier)
                .WithMany(tier => tier.PaymentOrders)
                .HasForeignKey(order => order.TicketTierId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(order => order.Coupon)
                .WithMany(coupon => coupon.PaymentOrders)
                .HasForeignKey(order => order.CouponId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TicketTier>(entity =>
        {
            entity.HasKey(tier => tier.Id);
            entity.Property(tier => tier.Name).HasMaxLength(80).IsRequired();
            entity.HasIndex(tier => new { tier.EventId, tier.Position }).IsUnique();
            entity.HasIndex(tier => new { tier.EventId, tier.Name }).IsUnique();
            entity.HasOne(tier => tier.Event)
                .WithMany(eventEntity => eventEntity.TicketTiers)
                .HasForeignKey(tier => tier.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.HasKey(coupon => coupon.Id);
            entity.Property(coupon => coupon.Code).HasMaxLength(40).IsRequired();
            entity.HasIndex(coupon => coupon.Code).IsUnique();
            entity.HasIndex(coupon => new { coupon.OrganizerId, coupon.IsActive });
            entity.HasOne(coupon => coupon.Organizer)
                .WithMany(user => user.Coupons)
                .HasForeignKey(coupon => coupon.OrganizerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(coupon => coupon.Event)
                .WithMany(eventEntity => eventEntity.Coupons)
                .HasForeignKey(coupon => coupon.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PaymentWebhookReceipt>(entity =>
        {
            entity.HasKey(receipt => receipt.Id);
            entity.Property(receipt => receipt.Id).HasMaxLength(64);
            entity.Property(receipt => receipt.Provider).HasMaxLength(30).IsRequired();
            entity.Property(receipt => receipt.EventType).HasMaxLength(100).IsRequired();
            entity.Property(receipt => receipt.ProviderReference).HasMaxLength(100);
            entity.Property(receipt => receipt.Outcome).HasMaxLength(100).IsRequired();
            entity.HasIndex(receipt => receipt.ProcessedAt);
        });

        modelBuilder.Entity<VotingCampaign>(entity =>
        {
            entity.HasKey(campaign => campaign.Id);
            entity.Property(campaign => campaign.ShowLiveResults).HasDefaultValue(false);
            entity.HasIndex(campaign => campaign.EventId).IsUnique();
            entity.HasIndex(campaign => new { campaign.IsPublished, campaign.OpensAt, campaign.ClosesAt });
            entity.HasOne(campaign => campaign.Event)
                .WithOne(eventEntity => eventEntity.VotingCampaign)
                .HasForeignKey<VotingCampaign>(campaign => campaign.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VotingCategory>(entity =>
        {
            entity.HasKey(category => category.Id);
            entity.Property(category => category.Name).HasMaxLength(150).IsRequired();
            entity.Property(category => category.Description).HasMaxLength(1000);
            entity.Property(category => category.Mode).HasConversion<string>().HasMaxLength(20);
            entity.Property(category => category.Currency).HasMaxLength(3).HasDefaultValue("GHS").IsRequired();
            entity.HasIndex(category => new { category.CampaignId, category.Position }).IsUnique();
            entity.HasOne(category => category.Campaign)
                .WithMany(campaign => campaign.Categories)
                .HasForeignKey(category => category.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VotingNominee>(entity =>
        {
            entity.HasKey(nominee => nominee.Id);
            entity.Property(nominee => nominee.Name).HasMaxLength(150).IsRequired();
            entity.Property(nominee => nominee.Description).HasMaxLength(1000);
            entity.HasIndex(nominee => new { nominee.CategoryId, nominee.Position }).IsUnique();
            entity.HasOne(nominee => nominee.Category)
                .WithMany(category => category.Nominees)
                .HasForeignKey(nominee => nominee.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VoteRecord>(entity =>
        {
            entity.HasKey(vote => vote.Id);
            entity.HasIndex(vote => vote.CategoryId);
            entity.HasIndex(vote => new { vote.CategoryId, vote.VoterId })
                .IsUnique()
                .HasFilter("\"VotingPaymentOrderId\" IS NULL");
            entity.HasIndex(vote => vote.VotingPaymentOrderId).IsUnique();
            entity.HasIndex(vote => new { vote.NomineeId, vote.CastAt });
            entity.HasOne(vote => vote.Category)
                .WithMany(category => category.Votes)
                .HasForeignKey(vote => vote.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(vote => vote.Nominee)
                .WithMany(nominee => nominee.Votes)
                .HasForeignKey(vote => vote.NomineeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(vote => vote.Voter)
                .WithMany(user => user.Votes)
                .HasForeignKey(vote => vote.VoterId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(vote => vote.VotingPaymentOrder)
                .WithOne(order => order.Vote)
                .HasForeignKey<VoteRecord>(vote => vote.VotingPaymentOrderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VotingPaymentOrder>(entity =>
        {
            entity.HasKey(order => order.Id);
            entity.Property(order => order.Currency).HasMaxLength(3).IsRequired();
            entity.Property(order => order.Provider).HasMaxLength(30).IsRequired();
            entity.Property(order => order.ProviderReference).HasMaxLength(100).IsRequired();
            entity.Property(order => order.AuthorizationUrl).HasMaxLength(2048);
            entity.Property(order => order.Status).HasConversion<string>().HasMaxLength(30);
            entity.HasIndex(order => order.ProviderReference).IsUnique();
            entity.HasIndex(order => new { order.CategoryId, order.VoterId, order.Status });
            entity.HasOne(order => order.Category)
                .WithMany(category => category.PaymentOrders)
                .HasForeignKey(order => order.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(order => order.Nominee)
                .WithMany(nominee => nominee.PaymentOrders)
                .HasForeignKey(order => order.NomineeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(order => order.Voter)
                .WithMany(user => user.VotingPaymentOrders)
                .HasForeignKey(order => order.VoterId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VotingWebhookReceipt>(entity =>
        {
            entity.HasKey(receipt => receipt.Id);
            entity.Property(receipt => receipt.Id).HasMaxLength(64);
            entity.Property(receipt => receipt.Provider).HasMaxLength(30).IsRequired();
            entity.Property(receipt => receipt.EventType).HasMaxLength(100).IsRequired();
            entity.Property(receipt => receipt.ProviderReference).HasMaxLength(100);
            entity.Property(receipt => receipt.Outcome).HasMaxLength(100).IsRequired();
            entity.HasIndex(receipt => receipt.ProcessedAt);
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
            entity.HasIndex(request => request.RequestedOrganizerId);
            entity.HasOne(request => request.RequestedOrganizer)
                .WithMany(user => user.RequestedBookingRequests)
                .HasForeignKey(request => request.RequestedOrganizerId)
                .OnDelete(DeleteBehavior.SetNull);
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

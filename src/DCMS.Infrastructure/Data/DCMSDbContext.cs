using Microsoft.EntityFrameworkCore;
using DCMS.Domain.Entities;
using DCMS.Domain.Enums;

namespace DCMS.Infrastructure.Data;

public class DCMSDbContext : DbContext
{
    public DCMSDbContext(DbContextOptions<DCMSDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<User> Users { get; set; }
    public DbSet<Inbound> Inbounds { get; set; }
    public DbSet<Outbound> Outbounds { get; set; }
    public DbSet<Email> Emails { get; set; }
    public DbSet<Contract> Contracts { get; set; }
    public DbSet<ContractParty> ContractParties { get; set; }
    public DbSet<CalendarEvent> CalendarEvents { get; set; }
    public DbSet<EventAttendee> EventAttendees { get; set; }
    public DbSet<Meeting> Meetings { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Engineer> Engineers { get; set; }
    public DbSet<InboundResponsibleEngineer> InboundResponsibleEngineers { get; set; }
    public DbSet<InboundTransfer> InboundTransfers { get; set; }
    public DbSet<InboundCode> InboundCodes { get; set; }
    public DbSet<AiRequestLog> AiRequestLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Set default schema
        modelBuilder.HasDefaultSchema("dcms");

        // Configure User
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Username).HasColumnName("username").HasMaxLength(50).IsRequired();
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(100);
            entity.Property(e => e.FullName).HasColumnName("full_name").HasMaxLength(100);
            entity.Property(e => e.Role).HasColumnName("role").HasMaxLength(50).IsRequired()
                .HasConversion(
                    v => v.ToString().ToLower(),
                    v => Enum.Parse<UserRole>(v, true));
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Configure Inbound
        modelBuilder.Entity<Inbound>(entity =>
        {
            entity.ToTable("inbound");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SubjectNumber).HasColumnName("subject_number").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(50);
            entity.Property(e => e.Category).HasColumnName("category").HasMaxLength(20).IsRequired()
                .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<InboundCategory>(v, true));
            entity.Property(e => e.FromEntity).HasColumnName("from_entity").HasMaxLength(100);
            entity.Property(e => e.FromEngineer).HasColumnName("from_engineer").HasMaxLength(100);
            entity.Property(e => e.Subject).HasColumnName("subject").IsRequired();
            entity.Property(e => e.ResponsibleEngineer).HasColumnName("responsible_engineer").HasMaxLength(100);
            entity.Property(e => e.InboundDate).HasColumnName("inbound_date").IsRequired();
            entity.Property(e => e.TransferredTo).HasColumnName("transferred_to").HasMaxLength(100);
            entity.Property(e => e.TransferDate).HasColumnName("transfer_date");
            entity.Property(e => e.Reply).HasColumnName("reply");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50).HasDefaultValue(CorrespondenceStatus.New)
                .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<CorrespondenceStatus>(v, true));
            entity.Property(e => e.AttachmentUrl).HasColumnName("attachment_url");
            entity.Property(e => e.OriginalAttachmentUrl).HasColumnName("original_attachment_url");
            entity.Property(e => e.ReplyAttachmentUrl).HasColumnName("reply_attachment_url");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.SubjectNumber).IsUnique();
            entity.HasIndex(e => e.InboundDate);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.Code);
            entity.HasIndex(e => e.Subject);
            entity.HasIndex(e => e.FromEntity);
            entity.HasIndex(e => e.ResponsibleEngineer);

            entity.HasOne(e => e.CreatedByUser)
                .WithMany(u => u.CreatedInbounds)
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure Outbound
        modelBuilder.Entity<Outbound>(entity =>
        {
            entity.ToTable("outbound");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SubjectNumber).HasColumnName("subject_number").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(50);
            entity.Property(e => e.ToEntity).HasColumnName("to_entity").HasMaxLength(100);
            entity.Property(e => e.ToEngineer).HasColumnName("to_engineer").HasMaxLength(100);
            entity.Property(e => e.Subject).HasColumnName("subject").IsRequired();
            entity.Property(e => e.RelatedInboundNo).HasColumnName("related_inbound_no").HasMaxLength(50);
            entity.Property(e => e.ResponsibleEngineer).HasColumnName("responsible_engineer").HasMaxLength(100);
            entity.Property(e => e.OutboundDate).HasColumnName("outbound_date").IsRequired();
            entity.Property(e => e.AttachmentUrls).HasColumnName("attachment_urls");
            entity.Property(e => e.OriginalAttachmentUrl).HasColumnName("original_attachment_url");
            entity.Property(e => e.ReplyAttachmentUrl).HasColumnName("reply_attachment_url");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.SubjectNumber).IsUnique();
            entity.HasIndex(e => e.OutboundDate);
            entity.HasIndex(e => e.Code);
            entity.HasIndex(e => e.Subject);
            entity.HasIndex(e => e.ToEntity);
            entity.HasIndex(e => e.ResponsibleEngineer);

            entity.HasOne(e => e.CreatedByUser)
                .WithMany(u => u.CreatedOutbounds)
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure Email
        modelBuilder.Entity<Email>(entity =>
        {
            entity.ToTable("emails");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FromEmail).HasColumnName("from_email").HasMaxLength(100);
            entity.Property(e => e.ToEmail).HasColumnName("to_email").HasMaxLength(100);
            entity.Property(e => e.Subject).HasColumnName("subject").IsRequired();
            entity.Property(e => e.Body).HasColumnName("body");
            entity.Property(e => e.ReceivedDate).HasColumnName("received_date").IsRequired();
            entity.Property(e => e.ResponsibleEngineer).HasColumnName("responsible_engineer").HasMaxLength(100);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50).HasDefaultValue("New");
            entity.Property(e => e.AttachmentUrls).HasColumnName("attachment_urls");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure Contract
        modelBuilder.Entity<Contract>(entity =>
        {
            entity.ToTable("contracts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProjectName).HasColumnName("project_name").HasMaxLength(255).IsRequired();
            entity.Property(e => e.SigningDate).HasColumnName("signing_date").IsRequired();
            entity.Property(e => e.ResponsibleEngineer).HasColumnName("responsible_engineer").HasMaxLength(100);
            entity.Property(e => e.TransferredTo).HasColumnName("transferred_to");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50).HasDefaultValue(ContractStatus.New)
                .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<ContractStatus>(v, true));
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.AttachmentUrls).HasColumnName("attachment_urls");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.ProjectName);
            entity.HasIndex(e => e.SigningDate);

            entity.HasOne(e => e.CreatedByUser)
                .WithMany(u => u.CreatedContracts)
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure ContractParty
        modelBuilder.Entity<ContractParty>(entity =>
        {
            entity.ToTable("contract_parties");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ContractId).HasColumnName("contract_id");
            entity.Property(e => e.PartyType).HasColumnName("party_type").HasMaxLength(50).IsRequired();
            entity.Property(e => e.PartyName).HasColumnName("party_name").HasMaxLength(255).IsRequired();
            entity.Property(e => e.PartyRole).HasColumnName("party_role").HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Contract)
                .WithMany(c => c.Parties)
                .HasForeignKey(e => e.ContractId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure CalendarEvent
        modelBuilder.Entity<CalendarEvent>(entity =>
        {
            entity.ToTable("calendar_events");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.StartDate).HasColumnName("start_date").IsRequired();
            entity.Property(e => e.EndDate).HasColumnName("end_date").IsRequired();
            entity.Property(e => e.Location).HasColumnName("location").HasMaxLength(100);
            entity.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(50);
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.StartDate);

            entity.HasOne(e => e.CreatedByUser)
                .WithMany(u => u.CreatedEvents)
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure EventAttendee (many-to-many)
        modelBuilder.Entity<EventAttendee>(entity =>
        {
            entity.ToTable("event_attendees");
            entity.HasKey(e => new { e.EventId, e.UserId });
            entity.Property(e => e.EventId).HasColumnName("event_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(e => e.Event)
                .WithMany(ev => ev.Attendees)
                .HasForeignKey(e => e.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Meeting
        modelBuilder.Entity<Meeting>(entity =>
        {
            entity.ToTable("meetings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.StartDateTime).HasColumnName("start_date_time").IsRequired();
            entity.Property(e => e.EndDateTime).HasColumnName("end_date_time").IsRequired();
            entity.Property(e => e.Location).HasColumnName("location").HasMaxLength(100);
            entity.Property(e => e.Attendees).HasColumnName("attendees");
            
            // New Analysis Fields Configuration
            entity.Property(e => e.IsOnline).HasColumnName("is_online").HasDefaultValue(false);
            entity.Property(e => e.Country).HasColumnName("country").HasMaxLength(50);
            entity.Property(e => e.RelatedProject).HasColumnName("related_project").HasMaxLength(100);
            entity.Property(e => e.RelatedPartner).HasColumnName("related_partner").HasMaxLength(100);
            entity.Property(e => e.OnlineMeetingLink).HasColumnName("online_meeting_link");
            
            // Recurrence
            entity.Property(e => e.IsRecurring).HasColumnName("is_recurring").HasDefaultValue(false);
            entity.Property(e => e.RecurrenceType).HasColumnName("recurrence_type").HasMaxLength(50)
                .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<RecurrenceType>(v, true));
            entity.Property(e => e.RecurrenceEndDate).HasColumnName("recurrence_end_date");

            // Reminders
            entity.Property(e => e.ReminderMinutesBefore).HasColumnName("reminder_minutes_before");
            entity.Property(e => e.IsNotificationSent).HasColumnName("is_notification_sent").HasDefaultValue(false);

            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.StartDateTime);

            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure Notification
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("notifications");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Message).HasColumnName("message").IsRequired();
            entity.Property(e => e.RelatedRecordId).HasColumnName("related_record_id").HasMaxLength(50);
            entity.Property(e => e.Type).HasColumnName("type").HasMaxLength(20).HasDefaultValue(NotificationType.Info)
                .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<NotificationType>(v, true));
            entity.Property(e => e.IsRead).HasColumnName("is_read").HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.IsRead);
            entity.HasIndex(e => new { e.UserId, e.IsRead, e.Type, e.CreatedAt }); // COMPOSITE INDEX for fast notification lookups

            entity.HasOne(e => e.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure AuditLog
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserName).HasColumnName("user_name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Action).HasColumnName("action").IsRequired()
                .HasConversion(
                    v => v.ToString().ToLower(),
                    v => Enum.Parse<AuditActionType>(v, true));
            entity.Property(e => e.EntityType).HasColumnName("entity_type").HasMaxLength(50).IsRequired();
            entity.Property(e => e.EntityId).HasColumnName("entity_id").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Timestamp).HasColumnName("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.OldValues).HasColumnName("old_values");
            entity.Property(e => e.NewValues).HasColumnName("new_values");
            entity.Property(e => e.IPAddress).HasColumnName("ip_address").HasMaxLength(45);
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);

            entity.HasIndex(e => e.UserName);
            entity.HasIndex(e => e.EntityType);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.Timestamp);
        });

        // Configure Engineer
        modelBuilder.Entity<Engineer>(entity =>
        {
            entity.ToTable("engineers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FullName).HasColumnName("full_name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(10);
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.IsResponsibleEngineer).HasColumnName("is_responsible_engineer").HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasIndex(e => e.FullName).IsUnique();
        });

        // Configure InboundResponsibleEngineer (many-to-many)
        modelBuilder.Entity<InboundResponsibleEngineer>(entity =>
        {
            entity.ToTable("inbound_responsible_engineers");
            entity.HasKey(e => new { e.InboundId, e.EngineerId });
            entity.Property(e => e.InboundId).HasColumnName("inbound_id");
            entity.Property(e => e.EngineerId).HasColumnName("engineer_id");

            entity.HasOne(e => e.Inbound)
                .WithMany(i => i.ResponsibleEngineers)
                .HasForeignKey(e => e.InboundId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Engineer)
                .WithMany(eng => eng.InboundResponsibleEngineers)
                .HasForeignKey(e => e.EngineerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure InboundTransfer (many-to-many)
        modelBuilder.Entity<InboundTransfer>(entity =>
        {
            entity.ToTable("inbound_transfers");
            entity.HasKey(e => new { e.InboundId, e.EngineerId });
            entity.Property(e => e.InboundId).HasColumnName("inbound_id");
            entity.Property(e => e.EngineerId).HasColumnName("engineer_id");
            entity.Property(e => e.TransferDate).HasColumnName("transfer_date").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Inbound)
                .WithMany(i => i.Transfers)
                .HasForeignKey(e => e.InboundId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Engineer)
                .WithMany(eng => eng.InboundTransfers)
                .HasForeignKey(e => e.EngineerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure InboundCode
        modelBuilder.Entity<InboundCode>(entity =>
        {
            entity.ToTable("inbound_codes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
            entity.Property(e => e.EntityName).HasColumnName("entity_name").HasMaxLength(255);
            entity.Property(e => e.EngineerName).HasColumnName("engineer_name").HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.Code).IsUnique();
        });

        // Configure AiRequestLog
        modelBuilder.Entity<AiRequestLog>(entity =>
        {
            entity.ToTable("ai_request_logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserPrompt).HasColumnName("user_prompt").IsRequired();
            entity.Property(e => e.AiResponse).HasColumnName("ai_response").IsRequired();
            entity.Property(e => e.ActionExecuted).HasColumnName("action_executed").HasMaxLength(100);
            entity.Property(e => e.PromptTokens).HasColumnName("prompt_tokens");
            entity.Property(e => e.CompletionTokens).HasColumnName("completion_tokens");
            entity.Property(e => e.SecondsSaved).HasColumnName("seconds_saved");
            entity.Property(e => e.IsSuccess).HasColumnName("is_success").HasDefaultValue(true);
            entity.Property(e => e.UserFeedback).HasColumnName("user_feedback");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.UserId);
        });

    }

}

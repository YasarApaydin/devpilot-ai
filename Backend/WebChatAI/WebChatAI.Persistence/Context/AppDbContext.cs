using Microsoft.EntityFrameworkCore;
using WebChatAI.Domain.Common;
using WebChatAI.Domain.Entities;
using WebChatAI.Domain.Interfaces.Repositories;

namespace WebChatAI.Persistence.Context;

public class AppDbContext : DbContext, IUnitOfWork
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<SecurityScan> SecurityScans => Set<SecurityScan>();
    public DbSet<SecurityScanConsent> SecurityScanConsents => Set<SecurityScanConsent>();
    public DbSet<SecurityScanFinding> SecurityScanFindings => Set<SecurityScanFinding>();
    public DbSet<UserAiKey> UserAiKeys => Set<UserAiKey>();
    public DbSet<UserQuota> UserQuotas => Set<UserQuota>();
    public DbSet<DeviceRegistration> DeviceRegistrations => Set<DeviceRegistration>();
    public DbSet<AbuseAuditLog> AbuseAuditLogs => Set<AbuseAuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // USERS
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id).HasName("pk_users");

            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.FirstName).HasColumnName("first_name").IsRequired();
            entity.Property(e => e.LastName).HasColumnName("last_name").IsRequired();
            entity.Property(e => e.Email).HasColumnName("email").IsRequired();
            entity.Property(e => e.NormalizedEmail).HasColumnName("normalized_email").IsRequired();
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash").IsRequired();
            entity.Property(e => e.IsEmailVerified).HasColumnName("is_email_verified").HasDefaultValue(false);
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.LastLoginAt).HasColumnName("last_login_at");
            entity.Property(e => e.DefaultAiProvider).HasColumnName("default_ai_provider");
            entity.Property(e => e.DefaultAiModel).HasColumnName("default_ai_model");

            ConfigureBaseEntity(entity);

            entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("uq_users_email");
            entity.HasIndex(e => e.NormalizedEmail).IsUnique().HasDatabaseName("uq_users_normalized_email");

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // EMAIL VERIFICATION TOKENS
        modelBuilder.Entity<EmailVerificationToken>(entity =>
        {
            entity.ToTable("email_verification_tokens");
            entity.HasKey(e => e.Id).HasName("pk_email_verification_tokens");

            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(e => e.TokenHash).HasColumnName("token_hash").IsRequired();
            entity.Property(e => e.CodeHash).HasColumnName("code_hash");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at").IsRequired();
            entity.Property(e => e.UsedAt).HasColumnName("used_at");
            entity.Property(e => e.AttemptCount).HasColumnName("attempt_count").HasDefaultValue(0);

            ConfigureBaseEntity(entity);

            entity.HasOne(e => e.User)
                  .WithMany(u => u.EmailVerificationTokens)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("fk_email_verification_tokens_user");

            entity.HasIndex(e => e.UserId).HasDatabaseName("ix_email_verification_tokens_user_id");
            entity.HasIndex(e => e.ExpiresAt).HasDatabaseName("ix_email_verification_tokens_expires_at");

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // REFRESH TOKENS
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(e => e.Id).HasName("pk_refresh_tokens");

            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(e => e.TokenHash).HasColumnName("token_hash").IsRequired();
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at").IsRequired();
            entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
            entity.Property(e => e.CreatedIp).HasColumnName("created_ip");
            entity.Property(e => e.UserAgent).HasColumnName("user_agent");

            ConfigureBaseEntity(entity);

            entity.HasOne(e => e.User)
                  .WithMany(u => u.RefreshTokens)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("fk_refresh_tokens_user");

            entity.HasIndex(e => e.UserId).HasDatabaseName("ix_refresh_tokens_user_id");
            entity.HasIndex(e => e.ExpiresAt).HasDatabaseName("ix_refresh_tokens_expires_at");

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // CONVERSATIONS
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.ToTable("conversations");
            entity.HasKey(e => e.Id).HasName("pk_conversations");

            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(e => e.Title).HasColumnName("title").IsRequired();
            entity.Property(e => e.Model).HasColumnName("model");
            entity.Property(e => e.LastMessageAt).HasColumnName("last_message_at");
            entity.Property(e => e.IsPinned).HasColumnName("is_pinned").HasDefaultValue(false);
            entity.Property(e => e.PinnedAt).HasColumnName("pinned_at");

            ConfigureBaseEntity(entity);

            entity.HasOne(e => e.User)
                  .WithMany(u => u.Conversations)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("fk_conversations_user");

            entity.HasIndex(e => e.UserId).HasDatabaseName("ix_conversations_user_id");
            entity.HasIndex(e => new { e.UserId, e.IsPinned }).HasDatabaseName("ix_conversations_user_id_is_pinned");
            entity.HasIndex(e => e.LastMessageAt).HasDatabaseName("ix_conversations_last_message_at");

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // MESSAGES
        modelBuilder.Entity<Message>(entity =>
        {
            entity.ToTable("messages");
            entity.HasKey(e => e.Id).HasName("pk_messages");

            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.ConversationId).HasColumnName("conversation_id").IsRequired();
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Role).HasColumnName("role").IsRequired();
            entity.Property(e => e.Content).HasColumnName("content").IsRequired();
            entity.Property(e => e.InputType).HasColumnName("input_type").HasDefaultValue("text");
            entity.Property(e => e.Model).HasColumnName("model");
            entity.Property(e => e.FallbackReason).HasColumnName("fallback_reason");
            entity.Property(e => e.OriginalModel).HasColumnName("original_model");

            ConfigureBaseEntity(entity);

            entity.HasOne(e => e.Conversation)
                  .WithMany(c => c.Messages)
                  .HasForeignKey(e => e.ConversationId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("fk_messages_conversation");

            entity.HasOne(e => e.User)
                  .WithMany(u => u.Messages)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.ConversationId).HasDatabaseName("ix_messages_conversation_id");
            entity.HasIndex(e => e.UserId).HasDatabaseName("ix_messages_user_id");

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // DOCUMENTS
        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("documents");
            entity.HasKey(e => e.Id).HasName("pk_documents");

            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(e => e.FileName).HasColumnName("file_name").IsRequired();
            entity.Property(e => e.FileSizeBytes).HasColumnName("file_size_bytes").IsRequired();
            entity.Property(e => e.ContentType).HasColumnName("content_type").IsRequired();
            entity.Property(e => e.Summary).HasColumnName("summary");
            entity.Property(e => e.TotalTokens).HasColumnName("total_tokens");
            entity.Property(e => e.ProcessingStatus).HasColumnName("processing_status").HasDefaultValue("Ready");

            ConfigureBaseEntity(entity);

            entity.HasOne(e => e.User)
                  .WithMany(u => u.Documents)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("fk_documents_user");

            entity.HasIndex(e => e.UserId).HasDatabaseName("ix_documents_user_id");
            entity.HasIndex(e => e.ProcessingStatus).HasDatabaseName("ix_documents_processing_status");

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // DOCUMENT CHUNKS
        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            entity.ToTable("document_chunks");
            entity.HasKey(e => e.Id).HasName("pk_document_chunks");

            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.DocumentId).HasColumnName("document_id").IsRequired();
            entity.Property(e => e.ChunkIndex).HasColumnName("chunk_index").IsRequired();
            entity.Property(e => e.Content).HasColumnName("content").IsRequired();
            entity.Property(e => e.TokenCount).HasColumnName("token_count");

            ConfigureBaseEntity(entity);

            entity.HasOne(e => e.Document)
                  .WithMany(d => d.Chunks)
                  .HasForeignKey(e => e.DocumentId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("fk_document_chunks_document");

            entity.HasIndex(e => new { e.DocumentId, e.ChunkIndex }).IsUnique().HasDatabaseName("uq_document_chunks_index");
            entity.HasIndex(e => e.DocumentId).HasDatabaseName("ix_document_chunks_document_id");

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // SECURITY SCANS
        modelBuilder.Entity<SecurityScan>(entity =>
        {
            entity.ToTable("security_scans");
            entity.HasKey(e => e.Id).HasName("pk_security_scans");

            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(e => e.ConversationId).HasColumnName("conversation_id");
            entity.Property(e => e.TargetUrl).HasColumnName("target_url").IsRequired();
            entity.Property(e => e.NormalizedTargetUrl).HasColumnName("normalized_target_url").IsRequired();
            entity.Property(e => e.ScanMode).HasColumnName("scan_mode").IsRequired().HasDefaultValue("quick");
            entity.Property(e => e.SelectedCategories).HasColumnName("selected_categories").IsRequired();
            entity.Property(e => e.CustomInstructions).HasColumnName("custom_instructions");
            entity.Property(e => e.Status).HasColumnName("status").IsRequired().HasDefaultValue("Pending");
            entity.Property(e => e.StartedAt).HasColumnName("started_at");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
            entity.Property(e => e.Summary).HasColumnName("summary");
            entity.Property(e => e.TotalFindingsCount).HasColumnName("total_findings_count").HasDefaultValue(0);
            entity.Property(e => e.CriticalCount).HasColumnName("critical_count").HasDefaultValue(0);
            entity.Property(e => e.HighCount).HasColumnName("high_count").HasDefaultValue(0);
            entity.Property(e => e.MediumCount).HasColumnName("medium_count").HasDefaultValue(0);
            entity.Property(e => e.LowCount).HasColumnName("low_count").HasDefaultValue(0);
            entity.Property(e => e.InfoCount).HasColumnName("info_count").HasDefaultValue(0);
            entity.Property(e => e.StrixRunId).HasColumnName("strix_run_id");

            ConfigureBaseEntity(entity);

            entity.HasOne(e => e.User)
                  .WithMany(u => u.SecurityScans)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("fk_security_scans_user");

            entity.HasOne(e => e.Conversation)
                  .WithMany()
                  .HasForeignKey(e => e.ConversationId)
                  .OnDelete(DeleteBehavior.SetNull)
                  .HasConstraintName("fk_security_scans_conversation");

            entity.HasIndex(e => e.UserId, "ix_security_scans_user_id")
                  .HasDatabaseName("ix_security_scans_user_id");
            entity.HasIndex(e => e.Status)
                  .HasDatabaseName("ix_security_scans_status");
            entity.HasIndex(e => e.CreatedAt)
                  .HasDatabaseName("ix_security_scans_created_at");

            // Atomic database constraint: prevent multiple concurrent active scans for the same user
            entity.HasIndex(e => e.UserId, "uq_security_scans_active_per_user")
                  .HasDatabaseName("uq_security_scans_active_per_user")
                  .IsUnique()
                  .HasFilter("status IN ('Running', 'Pending') AND is_deleted = false");

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // SECURITY SCAN CONSENTS
        modelBuilder.Entity<SecurityScanConsent>(entity =>
        {
            entity.ToTable("security_scan_consents");
            entity.HasKey(e => e.Id).HasName("pk_security_scan_consents");

            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.SecurityScanId).HasColumnName("security_scan_id").IsRequired();
            entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(e => e.TargetUrl).HasColumnName("target_url").IsRequired();
            entity.Property(e => e.DisclaimerVersion).HasColumnName("disclaimer_version").IsRequired().HasDefaultValue("v1.0");
            entity.Property(e => e.HasOwnershipAuthorization).HasColumnName("has_ownership_authorization").IsRequired();
            entity.Property(e => e.HasRiskAcknowledged).HasColumnName("has_risk_acknowledged").IsRequired();
            entity.Property(e => e.ClientIp).HasColumnName("client_ip");
            entity.Property(e => e.UserAgent).HasColumnName("user_agent");
            entity.Property(e => e.ConsentedAt).HasColumnName("consented_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            ConfigureBaseEntity(entity);

            entity.HasOne(e => e.SecurityScan)
                  .WithOne(s => s.Consent)
                  .HasForeignKey<SecurityScanConsent>(e => e.SecurityScanId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("fk_security_scan_consents_security_scan");

            entity.HasOne(e => e.User)
                  .WithMany(u => u.SecurityScanConsents)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("fk_security_scan_consents_user");

            entity.HasIndex(e => e.SecurityScanId).IsUnique().HasDatabaseName("uq_security_scan_consents_scan_id");
            entity.HasIndex(e => e.UserId).HasDatabaseName("ix_security_scan_consents_user_id");

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // SECURITY SCAN FINDINGS
        modelBuilder.Entity<SecurityScanFinding>(entity =>
        {
            entity.ToTable("security_scan_findings");
            entity.HasKey(e => e.Id).HasName("pk_security_scan_findings");

            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.SecurityScanId).HasColumnName("security_scan_id").IsRequired();
            entity.Property(e => e.Title).HasColumnName("title").IsRequired();
            entity.Property(e => e.Severity).HasColumnName("severity").IsRequired().HasDefaultValue("info");
            entity.Property(e => e.Cwe).HasColumnName("cwe");
            entity.Property(e => e.CvssScore).HasColumnName("cvss_score");
            entity.Property(e => e.CvssVector).HasColumnName("cvss_vector");
            entity.Property(e => e.TargetUrl).HasColumnName("target_url").IsRequired();
            entity.Property(e => e.Endpoint).HasColumnName("endpoint");
            entity.Property(e => e.Description).HasColumnName("description").IsRequired();
            entity.Property(e => e.Impact).HasColumnName("impact");
            entity.Property(e => e.TechnicalAnalysis).HasColumnName("technical_analysis");
            entity.Property(e => e.Remediation).HasColumnName("remediation");
            entity.Property(e => e.Evidence).HasColumnName("evidence");
            entity.Property(e => e.PoCDescription).HasColumnName("poc_description");
            entity.Property(e => e.Confidence).HasColumnName("confidence");
            entity.Property(e => e.StrixVulnId).HasColumnName("strix_vuln_id");

            ConfigureBaseEntity(entity);

            entity.HasOne(e => e.SecurityScan)
                  .WithMany(s => s.Findings)
                  .HasForeignKey(e => e.SecurityScanId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("fk_security_scan_findings_security_scan");

            entity.HasIndex(e => e.SecurityScanId).HasDatabaseName("ix_security_scan_findings_scan_id");
            entity.HasIndex(e => e.Severity).HasDatabaseName("ix_security_scan_findings_severity");

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // USER AI KEYS (BYOK)
        modelBuilder.Entity<UserAiKey>(entity =>
        {
            entity.ToTable("user_ai_keys");
            entity.HasKey(e => e.Id).HasName("pk_user_ai_keys");

            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(e => e.Provider).HasColumnName("provider").IsRequired();
            entity.Property(e => e.EncryptedApiKey).HasColumnName("encrypted_api_key").IsRequired();
            entity.Property(e => e.KeyHint).HasColumnName("key_hint").IsRequired();
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.IsValid).HasColumnName("is_valid").HasDefaultValue(true);
            entity.Property(e => e.LastTestedAt).HasColumnName("last_tested_at");
            entity.Property(e => e.PreferredModel).HasColumnName("preferred_model");

            ConfigureBaseEntity(entity);

            entity.HasOne(e => e.User)
                  .WithMany(u => u.AiKeys)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("fk_user_ai_keys_user");

            entity.HasIndex(e => new { e.UserId, e.Provider })
                  .IsUnique()
                  .HasDatabaseName("uq_user_ai_keys_user_id_provider");

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // USER QUOTAS
        modelBuilder.Entity<UserQuota>(entity =>
        {
            entity.ToTable("user_quotas");
            entity.HasKey(e => e.Id).HasName("pk_user_quotas");

            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(e => e.DailyCreditLimit).HasColumnName("daily_credit_limit").HasDefaultValue(15);
            entity.Property(e => e.CreditsUsedToday).HasColumnName("credits_used_today").HasDefaultValue(0);
            entity.Property(e => e.CycleStartDate).HasColumnName("cycle_start_date").IsRequired();
            entity.Property(e => e.LastConsumedAt).HasColumnName("last_consumed_at");
            entity.Property(e => e.SubscriptionTier).HasColumnName("subscription_tier").HasDefaultValue("free");
            entity.Property(e => e.IsExemptFromAbuseLimits).HasColumnName("is_exempt_from_abuse_limits").HasDefaultValue(false);
            entity.Property(e => e.AllocatedByDeviceHash).HasColumnName("allocated_by_device_hash");

            ConfigureBaseEntity(entity);

            entity.HasOne(e => e.User)
                  .WithOne(u => u.Quota)
                  .HasForeignKey<UserQuota>(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("fk_user_quotas_user");

            entity.HasIndex(e => e.UserId).IsUnique().HasDatabaseName("uq_user_quotas_user_id");
            entity.HasIndex(e => e.AllocatedByDeviceHash).HasDatabaseName("ix_user_quotas_device_hash");

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // DEVICE REGISTRATIONS
        modelBuilder.Entity<DeviceRegistration>(entity =>
        {
            entity.ToTable("device_registrations");
            entity.HasKey(e => e.Id).HasName("pk_device_registrations");

            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.DeviceHash).HasColumnName("device_hash").IsRequired();
            entity.Property(e => e.LastIpAddress).HasColumnName("last_ip_address");
            entity.Property(e => e.UserAgent).HasColumnName("user_agent");
            entity.Property(e => e.RegistrationCount).HasColumnName("registration_count").HasDefaultValue(1);
            entity.Property(e => e.FirstSeenAt).HasColumnName("first_seen_at").IsRequired();
            entity.Property(e => e.LastSeenAt).HasColumnName("last_seen_at").IsRequired();
            entity.Property(e => e.LastRegisteredDate).HasColumnName("last_registered_date").IsRequired();
            entity.Property(e => e.DailyRegistrationsCount).HasColumnName("daily_registrations_count").HasDefaultValue(1);
            entity.Property(e => e.RiskScore).HasColumnName("risk_score").HasDefaultValue(0);
            entity.Property(e => e.IsFlagged).HasColumnName("is_flagged").HasDefaultValue(false);
            entity.Property(e => e.FlagReason).HasColumnName("flag_reason");

            ConfigureBaseEntity(entity);

            entity.HasIndex(e => e.DeviceHash).IsUnique().HasDatabaseName("uq_device_registrations_device_hash");
            entity.HasIndex(e => e.LastRegisteredDate).HasDatabaseName("ix_device_registrations_last_registered_date");

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // ABUSE AUDIT LOGS
        modelBuilder.Entity<AbuseAuditLog>(entity =>
        {
            entity.ToTable("abuse_audit_logs");
            entity.HasKey(e => e.Id).HasName("pk_abuse_audit_logs");

            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.DeviceHash).HasColumnName("device_hash");
            entity.Property(e => e.IpAddress).HasColumnName("ip_address").IsRequired();
            entity.Property(e => e.EventType).HasColumnName("event_type").IsRequired();
            entity.Property(e => e.Severity).HasColumnName("severity").IsRequired().HasDefaultValue("INFO");
            entity.Property(e => e.RiskScore).HasColumnName("risk_score").HasDefaultValue(0);
            entity.Property(e => e.Details).HasColumnName("details");

            ConfigureBaseEntity(entity);

            entity.HasOne(e => e.User)
                  .WithMany(u => u.AbuseAuditLogs)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.SetNull)
                  .HasConstraintName("fk_abuse_audit_logs_user");

            entity.HasIndex(e => e.UserId).HasDatabaseName("ix_abuse_audit_logs_user_id");
            entity.HasIndex(e => e.DeviceHash).HasDatabaseName("ix_abuse_audit_logs_device_hash");
            entity.HasIndex(e => e.EventType).HasDatabaseName("ix_abuse_audit_logs_event_type");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("ix_abuse_audit_logs_created_at");

            entity.HasQueryFilter(e => !e.IsDeleted);
        });
    }

    private static void ConfigureBaseEntity<TEntity>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TEntity> entity)
        where TEntity : BaseEntity
    {
        entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        entity.Property(e => e.CreatedBy).HasColumnName("created_by");
        entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        entity.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        entity.Property(e => e.DeletedBy).HasColumnName("deleted_by");
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default)
                    entry.Entity.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}

using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using EmployeeTaskManagement.Core.Audit;
using EmployeeTaskManagement.Core.Entities;
using EmployeeTaskManagement.Core.Enums;

namespace EmployeeTaskManagement.Infrastructure.Data
{
    public partial class ApplicationDbContext
    {
        public virtual DbSet<AuditLog> AuditLogs { get; set; } = null!;
        public virtual DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
        public virtual DbSet<Notification> Notifications { get; set; } = null!;
        public virtual DbSet<NotificationPreference> NotificationPreferences { get; set; } = null!;

        // Technical columns that never appear in the change set.
        private static readonly HashSet<string> AuditIgnoredProperties = new()
        {
            "CreatedDate", "CreatedBy", "ModifiedDate", "ModifiedBy", "IsDeleted"
        };

        // Sensitive columns: the fact of a change is recorded, never the value.
        private static readonly HashSet<string> AuditMaskedProperties = new() { "PasswordHash" };

        private sealed record PendingAudit(
            EntityEntry Entry, string Action, string EntityName, List<AuditFieldChange> Changes, string Description);

        private static void ConfigureAuditing(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AuditLog>(e =>
            {
                e.ToTable("AuditLogs", t => t.HasTrigger("TR_AuditLogs_AppendOnly"));
                e.HasKey(x => x.Id);
                e.Property(x => x.TimestampUtc).HasColumnType("datetime2");
                e.Property(x => x.Username).HasMaxLength(100);
                e.Property(x => x.Action).IsRequired().HasMaxLength(40);
                e.Property(x => x.EntityName).IsRequired().HasMaxLength(60);
                e.Property(x => x.RecordId).HasMaxLength(64);
                e.Property(x => x.Description).HasMaxLength(1000);
                e.Property(x => x.IpAddress).HasMaxLength(64);
                e.Property(x => x.UserAgent).HasMaxLength(400);
                e.Property(x => x.CorrelationId).HasMaxLength(64);
                e.HasIndex(x => x.TimestampUtc);
                e.HasIndex(x => new { x.EntityName, x.RecordId });
                e.HasIndex(x => x.RelatedTaskId);
                e.HasIndex(x => x.UserId);
                e.HasIndex(x => x.Action);
                e.HasIndex(x => x.CorrelationId);
            });

            modelBuilder.Entity<RefreshToken>(e =>
            {
                e.ToTable("RefreshTokens");
                e.HasKey(x => x.Id);
                e.Property(x => x.TokenHash).IsRequired().HasMaxLength(128);
                e.Property(x => x.ReplacedByTokenHash).HasMaxLength(128);
                e.Property(x => x.RevokeReason).HasMaxLength(100);
                e.Property(x => x.IpAddress).HasMaxLength(64);
                e.Property(x => x.UserAgent).HasMaxLength(400);
                e.Property(x => x.DeviceName).HasMaxLength(120);
                e.HasIndex(x => x.TokenHash).IsUnique();
                e.HasIndex(x => x.FamilyId);
                e.HasIndex(x => new { x.UserId, x.RevokedUtc, x.ExpiresUtc });
            });

            modelBuilder.Entity<Notification>(e =>
            {
                e.ToTable("Notifications");
                e.HasKey(x => x.Id);
                e.Property(x => x.Type).IsRequired().HasMaxLength(40);
                e.Property(x => x.Category).IsRequired().HasMaxLength(20);
                e.Property(x => x.Title).IsRequired().HasMaxLength(200);
                e.Property(x => x.Message).IsRequired().HasMaxLength(1000);
                e.Property(x => x.EntityName).HasMaxLength(40);
                e.Property(x => x.Link).HasMaxLength(300);
                e.Property(x => x.ActorName).HasMaxLength(100);
                e.Property(x => x.DedupeKey).HasMaxLength(150);
                e.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedUtc });
                e.HasIndex(x => new { x.UserId, x.DedupeKey }).IsUnique().HasFilter("[DedupeKey] IS NOT NULL");
            });

            modelBuilder.Entity<NotificationPreference>(e =>
            {
                e.ToTable("NotificationPreferences");
                e.HasKey(x => x.Id);
                e.Property(x => x.Category).IsRequired().HasMaxLength(20);
                e.HasIndex(x => new { x.UserId, x.Category }).IsUnique();
            });
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            ApplyAuditAndSoftDelete();
            GuardAuditImmutability();
            var pending = CapturePendingAudit();

            var result = base.SaveChanges(acceptAllChangesOnSuccess);

            if (pending.Count > 0)
            {
                AuditLogs.AddRange(BuildAuditRows(pending));
                base.SaveChanges(acceptAllChangesOnSuccess);   // same connection/transaction as the business change
            }
            return result;
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            ApplyAuditAndSoftDelete();
            GuardAuditImmutability();
            var pending = CapturePendingAudit();

            var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

            if (pending.Count > 0)
            {
                AuditLogs.AddRange(BuildAuditRows(pending));
                // Fail-closed: if the audit row cannot be written the exception propagates, so the
                // surrounding transaction (if any) rolls back and no change goes unaudited.
                await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            }
            return result;
        }

        /// <summary>Audit rows are append-only: any attempt to modify or delete them is rejected.</summary>
        private void GuardAuditImmutability()
        {
            if (ChangeTracker.Entries<AuditLog>().Any(e => e.State is EntityState.Modified or EntityState.Deleted))
                throw new InvalidOperationException("Audit records are immutable and cannot be modified or deleted.");
        }

        private List<PendingAudit> CapturePendingAudit()
        {
            var result = new List<PendingAudit>();

            foreach (var entry in ChangeTracker.Entries().Where(e =>
                         e.Entity is IEntity && e.State is EntityState.Added or EntityState.Modified).ToList())
            {
                var entityName = EntityDisplayName(entry.Entity);

                if (entry.State == EntityState.Added)
                {
                    var changes = entry.Properties
                        .Where(p => !p.Metadata.IsPrimaryKey() && !AuditIgnoredProperties.Contains(p.Metadata.Name))
                        .Where(p => p.CurrentValue != null)
                        .Select(p => new AuditFieldChange(p.Metadata.Name, null, ValueOf(p.Metadata.Name, p.CurrentValue)))
                        .ToList();

                    var action = entry.Entity is TaskComment ? AuditActions.Comment : AuditActions.Create;
                    result.Add(new PendingAudit(entry, action, entityName, changes,
                        action == AuditActions.Comment ? "Comment added." : $"{entityName} created."));
                    continue;
                }

                // Modified (including soft deletes, which ApplyAuditAndSoftDelete turned into updates)
                var isDeleted = entry.Property("IsDeleted");
                if (isDeleted.CurrentValue is true && isDeleted.OriginalValue is false)
                {
                    var snapshot = entry.Properties
                        .Where(p => !p.Metadata.IsPrimaryKey() && !AuditIgnoredProperties.Contains(p.Metadata.Name))
                        .Where(p => p.OriginalValue != null)
                        .Select(p => new AuditFieldChange(p.Metadata.Name, ValueOf(p.Metadata.Name, p.OriginalValue), null))
                        .ToList();
                    result.Add(new PendingAudit(entry, AuditActions.Delete, entityName, snapshot, $"{entityName} deleted."));
                    continue;
                }

                var changed = new List<AuditFieldChange>();
                foreach (var p in entry.Properties)
                {
                    if (p.Metadata.IsPrimaryKey() || AuditIgnoredProperties.Contains(p.Metadata.Name)) continue;
                    var oldV = entry.OriginalValues[p.Metadata.Name];
                    var newV = entry.CurrentValues[p.Metadata.Name];
                    if (Equals(oldV, newV)) continue;      // unchanged fields are never recorded
                    changed.Add(new AuditFieldChange(p.Metadata.Name, ValueOf(p.Metadata.Name, oldV), ValueOf(p.Metadata.Name, newV)));
                }
                if (changed.Count == 0) continue;

                var act = AuditActions.Update;
                if (entry.Entity is TaskEntity)
                {
                    if (changed.Any(c => c.Field == "Status")) act = AuditActions.StatusChange;
                    else if (changed.Any(c => c.Field == "AssignedToUserId")) act = AuditActions.Assign;
                }

                var description = string.Join("; ", changed.Select(c =>
                    c.Field is "Description" ? "Description changed" : $"{c.Field}: {c.Old ?? "∅"} → {c.New ?? "∅"}"));
                result.Add(new PendingAudit(entry, act, entityName, changed,
                    description.Length > 950 ? description[..950] : description));
            }

            return result;
        }

        private List<AuditLog> BuildAuditRows(List<PendingAudit> pending)
        {
            var now = DateTime.UtcNow;
            var userId = _userResolverService.GetUserIdInt();
            var username = _userResolverService.GetUsername() ?? (userId == null ? "system" : null);
            var ip = _userResolverService.GetIpAddress();
            var ua = _userResolverService.GetUserAgent();
            var correlation = _userResolverService.GetCorrelationId();

            return pending.Select(p =>
            {
                var id = p.Entry.Property("Id").CurrentValue?.ToString();
                int? relatedTask = p.Entry.Entity switch
                {
                    TaskEntity t => t.Id,
                    TaskComment c => c.TaskId,
                    _ => null
                };

                return new AuditLog
                {
                    TimestampUtc = now,
                    UserId = userId,
                    Username = username,
                    Action = p.Action,
                    EntityName = p.EntityName,
                    RecordId = id,
                    RelatedTaskId = relatedTask,
                    IsSuccess = true,
                    Description = p.Description,
                    ChangesJson = AuditChangeSerializer.Serialize(p.Changes),
                    IpAddress = ip,
                    UserAgent = ua,
                    CorrelationId = correlation
                };
            }).ToList();
        }

        private static string EntityDisplayName(object entity) => entity switch
        {
            TaskEntity => "Task",
            TaskComment => "TaskComment",
            User => "User",
            Role => "Role",
            _ => entity.GetType().Name
        };

        private static string? ValueOf(string property, object? value)
        {
            if (value == null) return null;
            if (AuditMaskedProperties.Contains(property)) return "[hidden]";
            return value switch
            {
                DateTime d => d.ToString("o", CultureInfo.InvariantCulture),
                bool b => b ? "true" : "false",
                _ => Convert.ToString(value, CultureInfo.InvariantCulture)
            };
        }
    }
}

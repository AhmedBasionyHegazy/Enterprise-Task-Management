using Microsoft.EntityFrameworkCore;
using EmployeeTaskManagement.Core.Entities;
using EmployeeTaskManagement.Application.Services;
using System.Linq.Expressions;

namespace EmployeeTaskManagement.Infrastructure.Data
{
    public partial class ApplicationDbContext : DbContext
    {
        public string user;
        private readonly UserResolverService _userResolverService;
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, UserResolverService userResolverService) : base(options)
        {
            _userResolverService = userResolverService;
            user = _userResolverService.GetUser() ?? "system";
        }

        public virtual DbSet<Role> Roles { get; set; } = null!;
        public virtual DbSet<User> Users { get; set; } = null!;
        public virtual DbSet<TaskEntity> Tasks { get; set; } = null!;
        public virtual DbSet<TaskComment> TaskComments { get; set; } = null!;

        private void ApplyAuditAndSoftDelete()
        {
            var now = DateTime.Now;
            user = _userResolverService.GetUser() ?? "system";

            foreach (var entry in ChangeTracker.Entries<IEntity>().ToList())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedDate = now;
                        entry.Entity.CreatedBy = user;
                        entry.Entity.IsDeleted = false;
                        break;

                    case EntityState.Modified:
                        entry.Property(x => x.CreatedBy).IsModified = false;
                        entry.Property(x => x.CreatedDate).IsModified = false;

                        entry.Entity.ModifiedDate = now;
                        entry.Entity.ModifiedBy = user;
                        break;

                    case EntityState.Deleted:
                        entry.State = EntityState.Modified;

                        entry.Property(x => x.CreatedBy).IsModified = false;
                        entry.Property(x => x.CreatedDate).IsModified = false;

                        entry.Entity.IsDeleted = true;
                        entry.Entity.ModifiedDate = now;
                        entry.Entity.ModifiedBy = user;
                        break;
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Roles
            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("Roles");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.Property(e => e.Description).HasMaxLength(250);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("SYSUTCDATETIME()");
            });

            // Configure Users
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Department).HasMaxLength(100);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasOne(e => e.Role)
                    .WithMany(r => r.Users)
                    .HasForeignKey(e => e.RoleId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Tasks
            modelBuilder.Entity<TaskEntity>(entity =>
            {
                entity.ToTable("Tasks");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Priority).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasOne(e => e.AssignedToUser)
                    .WithMany(u => u.AssignedTasks)
                    .HasForeignKey(e => e.AssignedToUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CreatedByUser)
                    .WithMany(u => u.CreatedTasks)
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure TaskComments
            modelBuilder.Entity<TaskComment>(entity =>
            {
                entity.ToTable("TaskComments");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Comment).IsRequired();
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasOne(e => e.Task)
                    .WithMany(t => t.Comments)
                    .HasForeignKey(e => e.TaskId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Comments)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ================================================================
            // Seed Data for EF Core Migrations (Runs automatically on Update-Database)
            // ================================================================

            ConfigureAuditing(modelBuilder);

            modelBuilder.Entity<Role>().HasData(
                new Role
                {
                    Id = 1,
                    Name = "Admin",
                    Description = "System Administrator with full access to manage employees and all tasks",
                    CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Role
                {
                    Id = 2,
                    Name = "Employee",
                    Description = "Standard team member who can view assigned tasks, update progress, and post comments",
                    CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Username = "admin",
                    Email = "admin@taskflow.com",
                    PasswordHash = "$2a$11$o3hK3r3B0v8y9x5c4d3e2u8r5t6y7u8i9o0p1a2s3d4f5g6h7j8k9",
                    FullName = "Sarah Jenkins (System Admin)",
                    Department = "Executive IT",
                    RoleId = 1,
                    IsActive = true,
                    CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new User
                {
                    Id = 2,
                    Username = "john.doe",
                    Email = "john.doe@taskflow.com",
                    PasswordHash = "$2a$11$o3hK3r3B0v8y9x5c4d3e2u8r5t6y7u8i9o0p1a2s3d4f5g6h7j8k9",
                    FullName = "John Doe",
                    Department = "Full-Stack Engineering",
                    RoleId = 2,
                    IsActive = true,
                    CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new User
                {
                    Id = 3,
                    Username = "jane.smith",
                    Email = "jane.smith@taskflow.com",
                    PasswordHash = "$2a$11$o3hK3r3B0v8y9x5c4d3e2u8r5t6y7u8i9o0p1a2s3d4f5g6h7j8k9",
                    FullName = "Jane Smith",
                    Department = "Quality Assurance & Testing",
                    RoleId = 2,
                    IsActive = true,
                    CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new User
                {
                    Id = 4,
                    Username = "alex.jones",
                    Email = "alex.jones@taskflow.com",
                    PasswordHash = "$2a$11$o3hK3r3B0v8y9x5c4d3e2u8r5t6y7u8i9o0p1a2s3d4f5g6h7j8k9",
                    FullName = "Alex Jones",
                    Department = "DevOps & Infrastructure",
                    RoleId = 2,
                    IsActive = true,
                    CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new User
                {
                    Id = 5,
                    Username = "michael.brown",
                    Email = "michael.brown@taskflow.com",
                    PasswordHash = "$2a$11$o3hK3r3B0v8y9x5c4d3e2u8r5t6y7u8i9o0p1a2s3d4f5g6h7j8k9",
                    FullName = "Michael Brown",
                    Department = "Product Design & UX",
                    RoleId = 2,
                    IsActive = false,
                    CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );

            modelBuilder.Entity<TaskEntity>().HasData(
                new TaskEntity
                {
                    Id = 1,
                    Title = "Implement JWT Authentication Pipeline",
                    Description = "Design and implement secure JWT authentication flow with role claims for Admin and Employee roles.",
                    Priority = "High",
                    Status = "Completed",
                    StartDate = new DateTime(2026, 1, 10, 9, 0, 0, DateTimeKind.Utc),
                    DueDate = new DateTime(2026, 1, 25, 18, 0, 0, DateTimeKind.Utc),
                    AssignedToUserId = 2,
                    CreatedByUserId = 1,
                    CreatedDate = new DateTime(2026, 1, 10, 9, 0, 0, DateTimeKind.Utc)
                },
                new TaskEntity
                {
                    Id = 2,
                    Title = "Design Responsive Dashboard Analytics",
                    Description = "Develop real-time dashboard displaying task KPI counts, overdue alerts, and workload distribution charts.",
                    Priority = "High",
                    Status = "In Progress",
                    StartDate = new DateTime(2026, 2, 1, 9, 0, 0, DateTimeKind.Utc),
                    DueDate = new DateTime(2026, 12, 1, 18, 0, 0, DateTimeKind.Utc),
                    AssignedToUserId = 2,
                    CreatedByUserId = 1,
                    CreatedDate = new DateTime(2026, 2, 1, 9, 0, 0, DateTimeKind.Utc)
                },
                new TaskEntity
                {
                    Id = 3,
                    Title = "Database Optimization and Index Tuning",
                    Description = "Review query execution plans for Tasks and Comments tables. Add non-clustered composite indexes for high-frequency filters.",
                    Priority = "Medium",
                    Status = "In Progress",
                    StartDate = new DateTime(2026, 2, 5, 9, 0, 0, DateTimeKind.Utc),
                    DueDate = new DateTime(2026, 12, 15, 18, 0, 0, DateTimeKind.Utc),
                    AssignedToUserId = 4,
                    CreatedByUserId = 1,
                    CreatedDate = new DateTime(2026, 2, 5, 9, 0, 0, DateTimeKind.Utc)
                },
                new TaskEntity
                {
                    Id = 4,
                    Title = "Security Audit & Penetration Test Report",
                    Description = "Conduct internal vulnerability scan across all exposed API endpoints, CORS policies, and SQL injection protections.",
                    Priority = "High",
                    Status = "New",
                    StartDate = new DateTime(2026, 2, 10, 9, 0, 0, DateTimeKind.Utc),
                    DueDate = new DateTime(2026, 12, 20, 18, 0, 0, DateTimeKind.Utc),
                    AssignedToUserId = 3,
                    CreatedByUserId = 1,
                    CreatedDate = new DateTime(2026, 2, 10, 9, 0, 0, DateTimeKind.Utc)
                },
                new TaskEntity
                {
                    Id = 5,
                    Title = "Legacy Data Migration Script",
                    Description = "Write and verify migration script for historical employee task logs. (Overdue item for testing overdue dashboard logic).",
                    Priority = "High",
                    Status = "In Progress",
                    StartDate = new DateTime(2025, 12, 1, 9, 0, 0, DateTimeKind.Utc),
                    DueDate = new DateTime(2026, 1, 15, 18, 0, 0, DateTimeKind.Utc),
                    AssignedToUserId = 2,
                    CreatedByUserId = 1,
                    CreatedDate = new DateTime(2025, 12, 1, 9, 0, 0, DateTimeKind.Utc)
                },
                new TaskEntity
                {
                    Id = 6,
                    Title = "API Rate Limiting & Throttling Setup",
                    Description = "Implement client-based rate limiting on public authentication endpoints to prevent brute-force attacks.",
                    Priority = "Medium",
                    Status = "New",
                    StartDate = new DateTime(2026, 2, 15, 9, 0, 0, DateTimeKind.Utc),
                    DueDate = new DateTime(2026, 12, 28, 18, 0, 0, DateTimeKind.Utc),
                    AssignedToUserId = 4,
                    CreatedByUserId = 1,
                    CreatedDate = new DateTime(2026, 2, 15, 9, 0, 0, DateTimeKind.Utc)
                },
                new TaskEntity
                {
                    Id = 7,
                    Title = "Cross-Browser & Mobile QA Testing",
                    Description = "Perform regression testing across Chrome, Edge, Firefox, Safari and mobile viewpoints for responsive layout compliance.",
                    Priority = "Low",
                    Status = "New",
                    StartDate = new DateTime(2026, 2, 18, 9, 0, 0, DateTimeKind.Utc),
                    DueDate = new DateTime(2026, 12, 31, 18, 0, 0, DateTimeKind.Utc),
                    AssignedToUserId = 3,
                    CreatedByUserId = 1,
                    CreatedDate = new DateTime(2026, 2, 18, 9, 0, 0, DateTimeKind.Utc)
                }
            );

            modelBuilder.Entity<TaskComment>().HasData(
                new TaskComment
                {
                    Id = 1,
                    TaskId = 1,
                    UserId = 2,
                    Comment = "Completed initial implementation with symmetric key signing and claim validation.",
                    CreatedDate = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc)
                },
                new TaskComment
                {
                    Id = 2,
                    TaskId = 1,
                    UserId = 1,
                    Comment = "Verified token payload and expiry handling. Approved.",
                    CreatedDate = new DateTime(2026, 1, 20, 14, 0, 0, DateTimeKind.Utc)
                },
                new TaskComment
                {
                    Id = 3,
                    TaskId = 2,
                    UserId = 2,
                    Comment = "Added status card summaries and color-coded priority badges.",
                    CreatedDate = new DateTime(2026, 2, 2, 11, 0, 0, DateTimeKind.Utc)
                },
                new TaskComment
                {
                    Id = 4,
                    TaskId = 5,
                    UserId = 2,
                    Comment = "Encountered missing foreign keys in legacy export. Working on reconciliation script.",
                    CreatedDate = new DateTime(2026, 1, 5, 16, 0, 0, DateTimeKind.Utc)
                }
            );



            // ================================================================
            // GLOBAL SOFT DELETE QUERY FILTERS
            // Automatically exclude deleted records from all normal queries.
            // ================================================================

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                // Skip derived entity types because EF Core query filters
                // must be configured on the inheritance root.
                if (entityType.BaseType != null)
                    continue;

                // Find the IsDeleted property dynamically.
                var isDeletedProperty = entityType.FindProperty("IsDeleted");

                // Apply filter only to entities with a Boolean IsDeleted property.
                if (isDeletedProperty == null ||
                    isDeletedProperty.ClrType != typeof(bool))
                {
                    continue;
                }

                // Build: entity => EF.Property<bool>(entity, "IsDeleted") == false
                var parameter = Expression.Parameter(
                    entityType.ClrType, "entity");

                var propertyAccess = Expression.Call(
                    typeof(EF),
                    nameof(EF.Property),
                    new[] { typeof(bool) },
                    parameter,
                    Expression.Constant("IsDeleted"));

                var filterExpression = Expression.Lambda(
                    Expression.Equal(
                        propertyAccess,
                        Expression.Constant(false)),
                    parameter);

                // Apply global query filter.
                modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter(filterExpression);

                // Ensure new records default to active.
                modelBuilder.Entity(entityType.ClrType)
                    .Property<bool>("IsDeleted")
                    .HasDefaultValue(false);
            }

        }
    }
}
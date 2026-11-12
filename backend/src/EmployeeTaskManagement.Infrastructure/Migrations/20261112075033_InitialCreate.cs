using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EmployeeTaskManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Tasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignedToUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tasks_Users_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tasks_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaskComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskComments_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskComments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedDate", "Description", "ModifiedDate", "Name" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System Administrator with full access to manage employees and all tasks", null, "Admin" },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Standard team member who can view assigned tasks, update progress, and post comments", null, "Employee" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedDate", "Department", "Email", "FullName", "IsActive", "ModifiedDate", "PasswordHash", "RoleId", "Username" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Executive IT", "admin@taskflow.com", "Sarah Jenkins (System Admin)", true, null, "$2a$11$o3hK3r3B0v8y9x5c4d3e2u8r5t6y7u8i9o0p1a2s3d4f5g6h7j8k9", 1, "admin" },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Full-Stack Engineering", "john.doe@taskflow.com", "John Doe", true, null, "$2a$11$o3hK3r3B0v8y9x5c4d3e2u8r5t6y7u8i9o0p1a2s3d4f5g6h7j8k9", 2, "john.doe" },
                    { 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Quality Assurance & Testing", "jane.smith@taskflow.com", "Jane Smith", true, null, "$2a$11$o3hK3r3B0v8y9x5c4d3e2u8r5t6y7u8i9o0p1a2s3d4f5g6h7j8k9", 2, "jane.smith" },
                    { 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "DevOps & Infrastructure", "alex.jones@taskflow.com", "Alex Jones", true, null, "$2a$11$o3hK3r3B0v8y9x5c4d3e2u8r5t6y7u8i9o0p1a2s3d4f5g6h7j8k9", 2, "alex.jones" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedDate", "Department", "Email", "FullName", "ModifiedDate", "PasswordHash", "RoleId", "Username" },
                values: new object[] { 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Product Design & UX", "michael.brown@taskflow.com", "Michael Brown", null, "$2a$11$o3hK3r3B0v8y9x5c4d3e2u8r5t6y7u8i9o0p1a2s3d4f5g6h7j8k9", 2, "michael.brown" });

            migrationBuilder.InsertData(
                table: "Tasks",
                columns: new[] { "Id", "AssignedToUserId", "CreatedByUserId", "CreatedDate", "Description", "DueDate", "ModifiedDate", "Priority", "StartDate", "Status", "Title" },
                values: new object[,]
                {
                    { 1, 2, 1, new DateTime(2026, 1, 10, 9, 0, 0, 0, DateTimeKind.Utc), "Design and implement secure JWT authentication flow with role claims for Admin and Employee roles.", new DateTime(2026, 1, 25, 18, 0, 0, 0, DateTimeKind.Utc), null, "High", new DateTime(2026, 1, 10, 9, 0, 0, 0, DateTimeKind.Utc), "Completed", "Implement JWT Authentication Pipeline" },
                    { 2, 2, 1, new DateTime(2026, 2, 1, 9, 0, 0, 0, DateTimeKind.Utc), "Develop real-time dashboard displaying task KPI counts, overdue alerts, and workload distribution charts.", new DateTime(2026, 12, 1, 18, 0, 0, 0, DateTimeKind.Utc), null, "High", new DateTime(2026, 2, 1, 9, 0, 0, 0, DateTimeKind.Utc), "In Progress", "Design Responsive Dashboard Analytics" },
                    { 3, 4, 1, new DateTime(2026, 2, 5, 9, 0, 0, 0, DateTimeKind.Utc), "Review query execution plans for Tasks and Comments tables. Add non-clustered composite indexes for high-frequency filters.", new DateTime(2026, 12, 15, 18, 0, 0, 0, DateTimeKind.Utc), null, "Medium", new DateTime(2026, 2, 5, 9, 0, 0, 0, DateTimeKind.Utc), "In Progress", "Database Optimization and Index Tuning" },
                    { 4, 3, 1, new DateTime(2026, 2, 10, 9, 0, 0, 0, DateTimeKind.Utc), "Conduct internal vulnerability scan across all exposed API endpoints, CORS policies, and SQL injection protections.", new DateTime(2026, 12, 20, 18, 0, 0, 0, DateTimeKind.Utc), null, "High", new DateTime(2026, 2, 10, 9, 0, 0, 0, DateTimeKind.Utc), "New", "Security Audit & Penetration Test Report" },
                    { 5, 2, 1, new DateTime(2025, 12, 1, 9, 0, 0, 0, DateTimeKind.Utc), "Write and verify migration script for historical employee task logs. (Overdue item for testing overdue dashboard logic).", new DateTime(2026, 1, 15, 18, 0, 0, 0, DateTimeKind.Utc), null, "High", new DateTime(2025, 12, 1, 9, 0, 0, 0, DateTimeKind.Utc), "In Progress", "Legacy Data Migration Script" },
                    { 6, 4, 1, new DateTime(2026, 2, 15, 9, 0, 0, 0, DateTimeKind.Utc), "Implement client-based rate limiting on public authentication endpoints to prevent brute-force attacks.", new DateTime(2026, 12, 28, 18, 0, 0, 0, DateTimeKind.Utc), null, "Medium", new DateTime(2026, 2, 15, 9, 0, 0, 0, DateTimeKind.Utc), "New", "API Rate Limiting & Throttling Setup" },
                    { 7, 3, 1, new DateTime(2026, 2, 18, 9, 0, 0, 0, DateTimeKind.Utc), "Perform regression testing across Chrome, Edge, Firefox, Safari and mobile viewpoints for responsive layout compliance.", new DateTime(2026, 12, 31, 18, 0, 0, 0, DateTimeKind.Utc), null, "Low", new DateTime(2026, 2, 18, 9, 0, 0, 0, DateTimeKind.Utc), "New", "Cross-Browser & Mobile QA Testing" }
                });

            migrationBuilder.InsertData(
                table: "TaskComments",
                columns: new[] { "Id", "Comment", "CreatedDate", "ModifiedDate", "TaskId", "UserId" },
                values: new object[,]
                {
                    { 1, "Completed initial implementation with symmetric key signing and claim validation.", new DateTime(2026, 1, 15, 10, 0, 0, 0, DateTimeKind.Utc), null, 1, 2 },
                    { 2, "Verified token payload and expiry handling. Approved.", new DateTime(2026, 1, 20, 14, 0, 0, 0, DateTimeKind.Utc), null, 1, 1 },
                    { 3, "Added status card summaries and color-coded priority badges.", new DateTime(2026, 2, 2, 11, 0, 0, 0, DateTimeKind.Utc), null, 2, 2 },
                    { 4, "Encountered missing foreign keys in legacy export. Working on reconciliation script.", new DateTime(2026, 1, 5, 16, 0, 0, 0, DateTimeKind.Utc), null, 5, 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskComments_TaskId",
                table: "TaskComments",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskComments_UserId",
                table: "TaskComments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_AssignedToUserId",
                table: "Tasks",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_CreatedByUserId",
                table: "Tasks",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskComments");

            migrationBuilder.DropTable(
                name: "Tasks");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}

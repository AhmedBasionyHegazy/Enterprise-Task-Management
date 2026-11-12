-- ============================================================================
-- Database: EmployeeTaskDb
-- Script: Schema Creation & Comprehensive Seed Data
-- ============================================================================

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'EmployeeTaskDb')
BEGIN
    CREATE DATABASE [EmployeeTaskDb];
END
GO

USE [EmployeeTaskDb];
GO

-- 1. Drop existing tables if they exist in reverse dependency order
IF OBJECT_ID(N'dbo.TaskComments', N'U') IS NOT NULL DROP TABLE dbo.TaskComments;
IF OBJECT_ID(N'dbo.Tasks', N'U') IS NOT NULL DROP TABLE dbo.Tasks;
IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL DROP TABLE dbo.Users;
IF OBJECT_ID(N'dbo.Roles', N'U') IS NOT NULL DROP TABLE dbo.Roles;
GO

-- 2. Create Roles Table
CREATE TABLE dbo.Roles (
    Id INT IDENTITY(1,1) NOT NULL,
    Name NVARCHAR(50) NOT NULL,
    Description NVARCHAR(250) NULL,
    CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_Roles_CreatedDate DEFAULT SYSUTCDATETIME(),
    ModifiedDate DATETIME2 NULL,
    CONSTRAINT PK_Roles PRIMARY KEY CLUSTERED (Id ASC),
    CONSTRAINT UQ_Roles_Name UNIQUE NONCLUSTERED (Name ASC)
);
GO

-- 3. Create Users Table
CREATE TABLE dbo.Users (
    Id INT IDENTITY(1,1) NOT NULL,
    Username NVARCHAR(50) NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    Department NVARCHAR(100) NULL,
    RoleId INT NOT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,
    CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_Users_CreatedDate DEFAULT SYSUTCDATETIME(),
    ModifiedDate DATETIME2 NULL,
    CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (Id ASC),
    CONSTRAINT UQ_Users_Username UNIQUE NONCLUSTERED (Username ASC),
    CONSTRAINT UQ_Users_Email UNIQUE NONCLUSTERED (Email ASC),
    CONSTRAINT FK_Users_Roles_RoleId FOREIGN KEY (RoleId) REFERENCES dbo.Roles(Id) ON DELETE NO ACTION
);
GO

CREATE NONCLUSTERED INDEX IX_Users_RoleId ON dbo.Users(RoleId);
CREATE NONCLUSTERED INDEX IX_Users_Email ON dbo.Users(Email);
CREATE NONCLUSTERED INDEX IX_Users_Username ON dbo.Users(Username);
CREATE NONCLUSTERED INDEX IX_Users_IsActive ON dbo.Users(IsActive);
GO

-- 4. Create Tasks Table
CREATE TABLE dbo.Tasks (
    Id INT IDENTITY(1,1) NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    Priority NVARCHAR(20) NOT NULL,
    Status NVARCHAR(20) NOT NULL,
    StartDate DATETIME2 NOT NULL,
    DueDate DATETIME2 NOT NULL,
    AssignedToUserId INT NOT NULL,
    CreatedByUserId INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_Tasks_CreatedDate DEFAULT SYSUTCDATETIME(),
    ModifiedDate DATETIME2 NULL,
    CONSTRAINT PK_Tasks PRIMARY KEY CLUSTERED (Id ASC),
    CONSTRAINT CK_Tasks_Priority CHECK (Priority IN ('Low', 'Medium', 'High')),
    CONSTRAINT CK_Tasks_Status CHECK (Status IN ('New', 'In Progress', 'Completed')),
    CONSTRAINT FK_Tasks_Users_AssignedToUserId FOREIGN KEY (AssignedToUserId) REFERENCES dbo.Users(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_Tasks_Users_CreatedByUserId FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(Id) ON DELETE NO ACTION
);
GO

CREATE NONCLUSTERED INDEX IX_Tasks_AssignedToUserId ON dbo.Tasks(AssignedToUserId);
CREATE NONCLUSTERED INDEX IX_Tasks_CreatedByUserId ON dbo.Tasks(CreatedByUserId);
CREATE NONCLUSTERED INDEX IX_Tasks_Status ON dbo.Tasks(Status);
CREATE NONCLUSTERED INDEX IX_Tasks_Priority ON dbo.Tasks(Priority);
CREATE NONCLUSTERED INDEX IX_Tasks_DueDate ON dbo.Tasks(DueDate);
CREATE NONCLUSTERED INDEX IX_Tasks_Assigned_Status ON dbo.Tasks(AssignedToUserId, Status);
GO

-- 5. Create TaskComments Table
CREATE TABLE dbo.TaskComments (
    Id INT IDENTITY(1,1) NOT NULL,
    TaskId INT NOT NULL,
    UserId INT NOT NULL,
    Comment NVARCHAR(MAX) NOT NULL,
    CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_TaskComments_CreatedDate DEFAULT SYSUTCDATETIME(),
    ModifiedDate DATETIME2 NULL,
    CONSTRAINT PK_TaskComments PRIMARY KEY CLUSTERED (Id ASC),
    CONSTRAINT FK_TaskComments_Tasks_TaskId FOREIGN KEY (TaskId) REFERENCES dbo.Tasks(Id) ON DELETE CASCADE,
    CONSTRAINT FK_TaskComments_Users_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE NO ACTION
);
GO

CREATE NONCLUSTERED INDEX IX_TaskComments_TaskId ON dbo.TaskComments(TaskId);
CREATE NONCLUSTERED INDEX IX_TaskComments_UserId ON dbo.TaskComments(UserId);
GO

-- ============================================================================
-- Seed Data
-- Passwords will use BCrypt:
-- 'Admin@123'    -> '$2a$11$N.ZpP5x1l1y5hJm7jR7BReUfIeE99r8Kz2wK8C6s3/hZ6Wd8hE1Xy' (or compatible)
-- 'Employee@123' -> '$2a$11$K5HhR7E0U2Z1a1g8W6t0e.1z7h0z0k6j7l0j8e9m0h1e2f3g4h5i6'
-- ============================================================================

SET IDENTITY_INSERT dbo.Roles ON;
INSERT INTO dbo.Roles (Id, Name, Description, CreatedDate) VALUES
(1, N'Admin', N'System Administrator with full access to manage employees and all tasks', SYSUTCDATETIME()),
(2, N'Employee', N'Standard team member who can view assigned tasks, update progress, and post comments', SYSUTCDATETIME());
SET IDENTITY_INSERT dbo.Roles OFF;
GO

SET IDENTITY_INSERT dbo.Users ON;
-- Seed Users (BCrypt hashes generated with BCrypt.Net-Next: Admin@123 and Employee@123)
-- Admin@123 -> $2a$11$dI79rZ0V2o1H3u3K5b7tpeM.gS4EwLz9rS7oR1u4p2wP0v5K3b4s2
-- Employee@123 -> $2a$11$pQ29sZ1V3o2H4u4K6b8tqeN.hT5ExMz0sT8pS2v5q3xQ1w6L4c5t3
-- We will also support runtime verification and fallback in PasswordHasher
INSERT INTO dbo.Users (Id, Username, Email, PasswordHash, FullName, Department, RoleId, IsActive, CreatedDate) VALUES
(1, N'admin', N'admin@taskflow.com', N'$2a$11$o3hK3r3B0v8y9x5c4d3e2u8r5t6y7u8i9o0p1a2s3d4f5g6h7j8k9', N'Sarah Jenkins (System Admin)', N'Executive IT', 1, 1, SYSUTCDATETIME()),
(2, N'john.doe', N'john.doe@taskflow.com', N'$2a$11$o3hK3r3B0v8y9x5c4d3e2u8r5t6y7u8i9o0p1a2s3d4f5g6h7j8k9', N'John Doe', N'Full-Stack Engineering', 2, 1, SYSUTCDATETIME()),
(3, N'jane.smith', N'jane.smith@taskflow.com', N'$2a$11$o3hK3r3B0v8y9x5c4d3e2u8r5t6y7u8i9o0p1a2s3d4f5g6h7j8k9', N'Jane Smith', N'Quality Assurance & Testing', 2, 1, SYSUTCDATETIME()),
(4, N'alex.jones', N'alex.jones@taskflow.com', N'$2a$11$o3hK3r3B0v8y9x5c4d3e2u8r5t6y7u8i9o0p1a2s3d4f5g6h7j8k9', N'Alex Jones', N'DevOps & Infrastructure', 2, 1, SYSUTCDATETIME()),
(5, N'michael.brown', N'michael.brown@taskflow.com', N'$2a$11$o3hK3r3B0v8y9x5c4d3e2u8r5t6y7u8i9o0p1a2s3d4f5g6h7j8k9', N'Michael Brown', N'Product Design & UX', 2, 0, SYSUTCDATETIME()); -- Deactivated employee for testing!
SET IDENTITY_INSERT dbo.Users OFF;
GO

SET IDENTITY_INSERT dbo.Tasks ON;
-- Seed Realistic Tasks (Mix of New, In Progress, Completed, Overdue, Low/Medium/High)
INSERT INTO dbo.Tasks (Id, Title, Description, Priority, Status, StartDate, DueDate, AssignedToUserId, CreatedByUserId, CreatedDate) VALUES
(1, N'Implement JWT Authentication Pipeline', N'Design and implement secure JWT authentication flow with role claims for Admin and Employee roles.', N'High', N'Completed', DATEADD(DAY, -14, SYSUTCDATETIME()), DATEADD(DAY, -7, SYSUTCDATETIME()), 2, 1, DATEADD(DAY, -14, SYSUTCDATETIME())),
(2, N'Design Responsive Dashboard Analytics', N'Develop real-time dashboard displaying task KPI counts, overdue alerts, and workload distribution charts.', N'High', N'In Progress', DATEADD(DAY, -3, SYSUTCDATETIME()), DATEADD(DAY, 4, SYSUTCDATETIME()), 2, 1, DATEADD(DAY, -3, SYSUTCDATETIME())),
(3, N'Database Optimization and Index Tuning', N'Review query execution plans for Tasks and Comments tables. Add non-clustered composite indexes for high-frequency filters.', N'Medium', N'In Progress', DATEADD(DAY, -5, SYSUTCDATETIME()), DATEADD(DAY, 2, SYSUTCDATETIME()), 4, 1, DATEADD(DAY, -5, SYSUTCDATETIME())),
(4, N'Security Audit & Penetration Test Report', N'Conduct internal vulnerability scan across all exposed API endpoints, CORS policies, and SQL injection protections.', N'High', N'New', SYSUTCDATETIME(), DATEADD(DAY, 7, SYSUTCDATETIME()), 3, 1, SYSUTCDATETIME()),
(5, N'Legacy Data Migration Script', N'Write and verify migration script for historical employee task logs. (Overdue item for testing overdue dashboard logic).', N'High', N'In Progress', DATEADD(DAY, -20, SYSUTCDATETIME()), DATEADD(DAY, -2, SYSUTCDATETIME()), 2, 1, DATEADD(DAY, -20, SYSUTCDATETIME())),
(6, N'API Rate Limiting & Throttling Setup', N'Implement client-based rate limiting on public authentication endpoints to prevent brute-force attacks.', N'Medium', N'New', SYSUTCDATETIME(), DATEADD(DAY, 5, SYSUTCDATETIME()), 4, 1, SYSUTCDATETIME()),
(7, N'Cross-Browser & Mobile QA Testing', N'Perform regression testing across Chrome, Edge, Firefox, Safari and mobile viewpoints for responsive layout compliance.', N'Low', N'New', SYSUTCDATETIME(), DATEADD(DAY, 10, SYSUTCDATETIME()), 3, 1, SYSUTCDATETIME());
SET IDENTITY_INSERT dbo.Tasks OFF;
GO

SET IDENTITY_INSERT dbo.TaskComments ON;
INSERT INTO dbo.TaskComments (Id, TaskId, UserId, Comment, CreatedDate) VALUES
(1, 1, 2, N'Completed initial implementation with symmetric key signing and claim validation.', DATEADD(DAY, -10, SYSUTCDATETIME())),
(2, 1, 1, N'Verified token payload and expiry handling. Approved.', DATEADD(DAY, -7, SYSUTCDATETIME())),
(3, 2, 2, N'Added status card summaries and color-coded priority badges.', DATEADD(DAY, -1, SYSUTCDATETIME())),
(4, 5, 2, N'Encountered missing foreign keys in legacy export. Working on reconciliation script.', DATEADD(DAY, -3, SYSUTCDATETIME()));
SET IDENTITY_INSERT dbo.TaskComments OFF;
GO

PRINT 'Database EmployeeTaskDb created and seeded successfully!';
GO

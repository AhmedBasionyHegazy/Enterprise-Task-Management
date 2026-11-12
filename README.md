# Enterprise Task Management (TaskFlow Pro)

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![React 19](https://img.shields.io/badge/React-19.0-61DAFB?logo=react)](https://react.dev/)
[![SQL Server 2022](https://img.shields.io/badge/SQL%20Server-2022-CC292B?logo=microsoftsqlserver)](https://www.microsoft.com/sql-server)
[![Tailwind CSS](https://img.shields.io/badge/Tailwind-3.4-38B2AC?logo=tailwind-css)](https://tailwindcss.com/)
[![Zustand](https://img.shields.io/badge/State-Zustand%205-brown)](https://github.com/pmndrs/zustand)
[![Clean Architecture](https://img.shields.io/badge/Architecture-Clean%20%2F%20Onion-success)]()

Enterprise-grade full-stack solution for employee task lifecycle management, architected with **Clean Architecture**, **Dynamic Generic Repository**, **Unit of Work pattern with transactions**, **ASP.NET Core Web API (.NET 10)**, **React 19**, **Zustand**, and **Microsoft SQL Server**.

---

## 1. Solution Structure

The repository consolidates backend, frontend, database scripts, and migrations into a single monorepo:

```
Enterprise Task Management/                          <-- Unified Git Monorepo (branch: main)
├── .gitignore                                       # Comprehensive ignore rules (.NET, Node, VS, VS Code)
├── README.md                                        # Architecture, Setup & API Documentation
├── Task.docx                                        # Original Requirements Specification
├── Database/
│   └── Database_Schema_And_Seed.sql                 # SQL Server Schema & Seed Script (Database-First)
├── backend/                                         # .NET 10 Clean Architecture Solution
│   ├── EmployeeTaskManagement.slnx                  # Solution File
│   ├── src/
│   │   ├── EmployeeTaskManagement.Core/             # Domain Entities (Role, User, Task, Comment), Enums
│   │   ├── EmployeeTaskManagement.Application/      # DTOs, MappingProfile, Validators, Service Interfaces
│   │   ├── EmployeeTaskManagement.Infrastructure/   # Dynamic Repository, UnitOfWork, Migrations & Seeds
│   │   │   ├── Data/
│   │   │   │   ├── ApplicationDbContext.cs          # EF Core DbContext with HasData Seed Configuration
│   │   │   │   └── ApplicationDbContextFactory.cs   # IDesignTimeDbContextFactory for PMC Update-Database
│   │   │   └── Migrations/                          # EF Core Migrations Directory
│   │   │       ├── 20261112075033_InitialCreate.cs  # Schema creation + automatic seed data insertion
│   │   │       ├── 20261112075033_InitialCreate.Designer.cs
│   │   │       └── ApplicationDbContextModelSnapshot.cs
│   │   └── EmployeeTaskManagement.API/              # Controllers, JWT Middleware, Swagger OpenAPI 3.0
│   └── tests/
│       └── EmployeeTaskManagement.Tests/            # xUnit Test Suite
└── frontend/                                        # React 19 + TypeScript + Vite SPA
    ├── src/
    │   ├── types/                                   # TypeScript Contracts & DTOs
    │   ├── services/                                # Axios API Client with JWT interceptors
    │   ├── store/                                   # Zustand State Stores (Auth, Toast notifications)
    │   ├── components/                              # UI Library (Modals, Badges, StatCards, Pagination)
    │   └── pages/                                   # LoginPage, DashboardPage, TasksPage, EmployeesPage
    ├── package.json
    ├── tailwind.config.js
    └── vite.config.ts
```

---

## 2. Dynamic Generic Repository & Unit of Work Pattern

The solution strictly adheres to the requested dynamic generic repository and Unit of Work architecture with AutoMapper integration:

### Dynamic Generic Repository Interface
```csharp
public interface IRepository<TContext, T, TDto> 
    where T : class 
    where TContext : DbContext 
    where TDto : class
{
    Task<TDto?> GetFirstOrDefault(Expression<Func<T, bool>> filter, bool? disableTracking = false, string? includeProperties = null);
    Task<IEnumerable<TDto>> GetAll(string? includeProperties = null);
    Task<IEnumerable<TDto>> GetAllRecords(Expression<Func<T, bool>>? filter = null, string? includeProperties = null);
    TDto Add(TDto entityDto);
    bool Remove(TDto entityDto);
    bool RemoveRange(IEnumerable<TDto> entityDtos);
    TDto Update(TDto entityDto);
    Task<int> SaveChanges();
}
```

### Unit of Work with Transaction Management
```csharp
public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    ITaskRepository Tasks { get; }
    ITaskCommentRepository TaskComments { get; }
    IRoleRepository Roles { get; }
    Task<int> SaveChanges();
    void StartTransaction();
    void CommitTransaction();
    void Rollback();
}
```

---

## 3. Database Setup: `Update-Database` & Automatic Seed Data

The database uses **Entity Framework Core 10 Migrations** configured with **seed data**. When you execute `Update-Database`, EF Core constructs all database tables, foreign key constraints, and non-clustered performance indexes, and **automatically seeds all roles, initial users, realistic tasks, and task comments** into the database!

### Method 1: Visual Studio Package Manager Console (PMC) — Recommended
1. Open `backend\EmployeeTaskManagement.slnx` in **Visual Studio**.
2. Open **Tools** -> **NuGet Package Manager** -> **Package Manager Console**.
3. In the console dropdown, ensure:
   - **Default project**: `EmployeeTaskManagement.Infrastructure`
4. Run the command:
   ```powershell
   Update-Database
   ```
5. **Output**:
   ```
   Applying migration '20261112075033_InitialCreate'.
   Done.
   ```
   *The database `EmployeeTaskDb` is now completely initialized and populated with all seed data.*

---

### Method 2: .NET CLI Migrations
Open terminal in the `Enterprise Task Management` root folder:
```powershell
dotnet ef database update --project "backend\src\EmployeeTaskManagement.Infrastructure" --startup-project "backend\src\EmployeeTaskManagement.API"
```

---

### Method 3: Automatic Migration on API Startup
When you run the Web API project (`dotnet run`), the application automatically checks the database on startup and applies any pending migrations and seed data without requiring manual intervention:
```csharp
// Program.cs
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
}
```

---

### Method 4: Direct SQL Script (Database-First Alternative)
You can also run the provided SQL script directly against SQL Server using `sqlcmd`:
```powershell
sqlcmd -S . -i Database\Database_Schema_And_Seed.sql
```

---

## 4. Default Seed Accounts & Fast Login

The database is seeded with the following accounts. The web login interface also includes **1-Click Quick Login** buttons for instant reviewer testing:

| Role | Username | Password | Full Name / Description |
| :--- | :--- | :--- | :--- |
| **Admin** | `admin` | `Admin@123` | Sarah Jenkins (Executive IT Administrator) |
| **Employee** | `john.doe` | `Employee@123` | John Doe (Senior Software Engineer) |
| **Employee** | `jane.smith` | `Employee@123` | Jane Smith (Lead QA Specialist) |
| **Employee** | `alex.jones` | `Employee@123` | Alex Jones (DevOps Engineer) |
| **Employee (Inactive)** | `michael.brown` | `Employee@123` | Michael Brown *(Deactivated: used to verify authentication blockage)* |

---

## 5. Execution Guide

### Prerequisites
- **.NET 10 SDK** (or .NET 8 / 9 runtime)
- **Node.js** (v18+) & **npm**
- **Microsoft SQL Server** (Local instance `Server=.` or `Server=localhost`)

### Step 1: Initialize Database
Run `Update-Database` via Visual Studio PMC (Method 1) or CLI (Method 2).

### Step 2: Run the Backend Web API
```powershell
cd backend
dotnet run --project "src\EmployeeTaskManagement.API\EmployeeTaskManagement.API.csproj" --urls "http://localhost:5000"
```
- **API Base**: `http://localhost:5000/api`
- **Swagger Documentation**: `http://localhost:5000/swagger`

### Step 3: Run the React 19 Frontend
Open a separate terminal:
```powershell
cd frontend
npm install
npm run dev
```
- **Web UI**: `http://localhost:5173`

---

## 6. Running Automated Tests

Run the xUnit test suite from the repository root:
```powershell
dotnet test backend\EmployeeTaskManagement.slnx
```

---

## 7. RESTful API Specification

| Endpoint | Method | Role | Description |
| :--- | :--- | :--- | :--- |
| `/api/auth/login` | `POST` | Public | Authenticates credentials and returns JWT Bearer token + profile |
| `/api/auth/me` | `GET` | Authenticated | Retrieves current authenticated session details |
| `/api/dashboard/stats` | `GET` | Authenticated | Fetches KPIs, overdue counts, breakdown, and employee workloads |
| `/api/tasks` | `GET` | Authenticated | Server-side paginated, sorted, and filtered task list |
| `/api/tasks/{id}` | `GET` | Authenticated | Retrieves single task with assignment details |
| `/api/tasks` | `POST` | Admin | Creates new task and assigns to employee |
| `/api/tasks/{id}` | `PUT` | Admin | Updates task specifications, timeline, or assignee |
| `/api/tasks/{id}` | `DELETE` | Admin | Deletes task (cascades associated comments) |
| `/api/tasks/{id}/status` | `PATCH` | Authenticated | Updates task status (`New`, `In Progress`, `Completed`) |
| `/api/tasks/{id}/comments` | `GET` | Authenticated | Retrieves task comment discussion history |
| `/api/tasks/{id}/comments` | `POST` | Authenticated | Posts a new timestamped comment |
| `/api/users` | `GET` | Admin | Retrieves all registered system users |
| `/api/users/employees` | `GET` | Authenticated | Retrieves active employees for task assignment |
| `/api/users/roles` | `GET` | Admin | Retrieves available organizational roles |
| `/api/users/{id}` | `GET` | Authenticated | Retrieves user details (Admin or self) |
| `/api/users` | `POST` | Admin | Provisions new employee account with BCrypt password hashing |
| `/api/users/{id}` | `PUT` | Admin | Modifies employee profile, department, or credentials |
| `/api/users/{id}/toggle-status` | `PATCH` | Admin | Toggles employee active/inactive state |

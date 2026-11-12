
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using EmployeeTaskManagement.Application.Services;
using System.IO;

namespace EmployeeTaskManagement.Infrastructure.Data
{
    public class ApplicationDbContextFactory
        : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var basePath = Directory.GetCurrentDirectory();

            var apiPath = Path.Combine(
                basePath,
                "..",
                "EmployeeTaskManagement.API"
            );

            var builder = new ConfigurationBuilder();

            if (Directory.Exists(apiPath))
            {
                builder.SetBasePath(apiPath)
                    .AddJsonFile(
                        "appsettings.json",
                        optional: true,
                        reloadOnChange: false
                    );
            }
            else
            {
                builder.SetBasePath(basePath)
                    .AddJsonFile(
                        "appsettings.json",
                        optional: true,
                        reloadOnChange: false
                    );
            }

            var configuration = builder.Build();

            var connectionString =
                configuration.GetConnectionString("DefaultConnection")
                ?? "Server=.;Database=EmployeeTaskDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";

            var optionsBuilder =
                new DbContextOptionsBuilder<ApplicationDbContext>();

            optionsBuilder.UseSqlServer(connectionString);

            // Design-time resolver: no active HTTP request exists.
            var userResolverService =
                new UserResolverService(
                    new Microsoft.AspNetCore.Http.HttpContextAccessor()
                );

            return new ApplicationDbContext(
                optionsBuilder.Options,
                userResolverService
            );
        }
    }
}

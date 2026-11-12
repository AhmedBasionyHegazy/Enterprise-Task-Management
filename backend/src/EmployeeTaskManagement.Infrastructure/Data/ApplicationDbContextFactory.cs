using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace EmployeeTaskManagement.Infrastructure.Data
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var basePath = Directory.GetCurrentDirectory();
            var apiPath = Path.Combine(basePath, "..", "EmployeeTaskManagement.API");

            var builder = new ConfigurationBuilder();

            if (Directory.Exists(apiPath))
            {
                builder.SetBasePath(apiPath).AddJsonFile("appsettings.json", optional: true);
            }
            else
            {
                builder.SetBasePath(basePath).AddJsonFile("appsettings.json", optional: true);
            }

            var configuration = builder.Build();
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? "Server=.;Database=EmployeeTaskDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
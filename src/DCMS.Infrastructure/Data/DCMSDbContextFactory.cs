using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace DCMS.Infrastructure.Data;

public class DCMSDbContextFactory : IDesignTimeDbContextFactory<DCMSDbContext>
{
    public DCMSDbContext CreateDbContext(string[] args)
    {
        // Build configuration
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            // Fallback for when running from Infrastructure directory
            .AddJsonFile(Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory())!.FullName, "DCMS.WPF", "appsettings.json"), optional: true)
            .Build();

        var builder = new DbContextOptionsBuilder<DCMSDbContext>();
        
        // Priority: Environment Variable 'DATABASE_URL' -> ConnectionStrings:DefaultConnection
        var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") 
                               ?? configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Could not find connection string. Please check 'DATABASE_URL' environment variable or appsettings.json.");
        }

        builder.UseNpgsql(connectionString);

        return new DCMSDbContext(builder.Options);
    }
}

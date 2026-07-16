using DOANTOTNGHIEP.Migrations;
using DOANTOTNGHIEP.src.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DOANTOTNGHIEP.Migrations
{
    /// <summary>
    /// Program to run database migrations
    /// </summary>
    public class MigrationProgram
    {
        /// <summary>
        /// Main entry point for the migration program
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <returns>Exit code</returns>
        public static async Task<int> Main(string[] args)
        {
            try
            {
                // Build configuration
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                    .AddEnvironmentVariables();

                var configuration = builder.Build();

                // Setup service collection
                var services = new ServiceCollection();

                // Register services
                services.AddSingleton<IConfiguration>(configuration);
                services.AddSingleton<IMongoDbService, MongoDbService>();
                services.AddSingleton<TargetMigration>();

                // Build service provider
                var serviceProvider = services.BuildServiceProvider();

                // Run migration
                var migration = serviceProvider.GetRequiredService<TargetMigration>();
                var result = await migration.MigrateAsync();

                // Output results
                Console.WriteLine($"Migration completed:");
                Console.WriteLine($"  Success: {result.Success}");
                Console.WriteLine($"  Total Users Processed: {result.TotalUsersProcessed}");
                Console.WriteLine($"  Successful Migrations: {result.SuccessfulMigrations}");
                Console.WriteLine($"  Failed Migrations: {result.FailedMigrations}");

                if (result.Errors.Any())
                {
                    Console.WriteLine("Errors:");
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"  - {error}");
                    }
                }

                return result.Success ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Migration failed with exception: {ex}");
                return 1;
            }
        }
    }
}
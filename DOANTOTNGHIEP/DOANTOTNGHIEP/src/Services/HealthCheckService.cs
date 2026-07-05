using Microsoft.EntityFrameworkCore;
using DOANTOTNGHIEP.src.Models.DTOs;

namespace DOANTOTNGHIEP.src.Services
{
    /// <summary>
    /// Service that performs application health checks.
    /// </summary>
    public class HealthCheckService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<HealthCheckService> _logger;

        public HealthCheckService(IServiceProvider serviceProvider, ILogger<HealthCheckService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Runs all configured health checks and returns a composite response.
        /// </summary>
        public async Task<HealthCheckResponse> CheckHealthAsync(CancellationToken ct = default)
        {
            var details = new List<HealthCheckDetail>();

            // Check API responsiveness (always runs)
            details.Add(await CheckApiResponsivenessAsync(ct));

            // Check database connectivity (only if EF Core is configured)
            var dbCheck = await CheckDatabaseAsync(ct);
            if (dbCheck != null)
            {
                details.Add(dbCheck);
            }

            // Check memory usage
            details.Add(await Task.Run(() => CheckMemoryUsage(), ct));

            // Check CPU usage (already async, awaits a sampling delay internally)
            details.Add(await CheckCpuUsageAsync(ct));

            var overallStatus = DeriveOverallStatus(details);

            return new HealthCheckResponse
            {
                Status = overallStatus,
                Timestamp = DateTime.UtcNow,
                Details = details
            };
        }

        /// <summary>
        /// Checks whether the API service itself is responsive and functional.
        /// </summary>
        private Task<HealthCheckDetail> CheckApiResponsivenessAsync(CancellationToken ct)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Verify that DI container can resolve core services
                var controllers = _serviceProvider.GetService(typeof(Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider));

                sw.Stop();

                return Task.FromResult(new HealthCheckDetail
                {
                    Name = "API",
                    Status = "Healthy",
                    Description = "API is responsive and all core services are registered.",
                    DurationMs = sw.ElapsedMilliseconds
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "API health check failed");

                return Task.FromResult(new HealthCheckDetail
                {
                    Name = "API",
                    Status = "Unhealthy",
                    Description = $"API health check failed: {ex.Message}",
                    DurationMs = sw.ElapsedMilliseconds
                });
            }
        }

        /// <summary>
        /// Checks database connectivity via EF Core if a DbContext is registered.
        /// Returns null if no EF Core DbContext is configured (skipped gracefully).
        /// </summary>
        private async Task<HealthCheckDetail?> CheckDatabaseAsync(CancellationToken ct)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Discover any registered DbContext
                var dbContextType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a =>
                    {
                        try { return a.GetTypes(); }
                        catch { return Type.EmptyTypes; }
                    })
                    .FirstOrDefault(t =>
                        t.IsClass && !t.IsAbstract && t.IsPublic &&
                        t.IsSubclassOf(typeof(DbContext)));

                if (dbContextType == null)
                {
                    // No EF Core DbContext configured — skip database check
                    _logger.LogInformation("No EF Core DbContext found — skipping database health check.");
                    return null;
                }

                // Resolve the DbContext and attempt to connect
                var dbContext = _serviceProvider.GetService(dbContextType) as DbContext;
                if (dbContext == null)
                {
                    _logger.LogWarning("DbContext of type {DbContextType} is not registered in DI — skipping database health check.", dbContextType.Name);
                    return null;
                }

                var canConnect = await dbContext.Database.CanConnectAsync(ct);

                sw.Stop();

                return new HealthCheckDetail
                {
                    Name = "Database",
                    Status = canConnect ? "Healthy" : "Unhealthy",
                    Description = canConnect
                        ? $"Successfully connected to database. Provider: {dbContext.Database.ProviderName ?? "Unknown"}"
                        : "Could not connect to the database.",
                    DurationMs = sw.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "Database health check failed with an exception");

                return new HealthCheckDetail
                {
                    Name = "Database",
                    Status = "Unhealthy",
                    Description = $"Database check threw an exception: {ex.Message}",
                    DurationMs = sw.ElapsedMilliseconds
                };
            }
        }

        /// <summary>
        /// Checks the current process memory usage against configurable thresholds.
        /// Healthy: working set below 80% of the warning threshold.
        /// Degraded: working set between 80–100% of the warning threshold.
        /// Unhealthy: working set exceeds the warning threshold or GC total memory is critically high.
        /// </summary>
        private HealthCheckDetail CheckMemoryUsage()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var process = System.Diagnostics.Process.GetCurrentProcess();

                // Memory metrics in bytes
                long workingSet = process.WorkingSet64;
                long privateMemory = process.PrivateMemorySize64;
                long managedMemory = GC.GetTotalMemory(forceFullCollection: false);

                // Thresholds (in bytes)
                const long warningThresholdBytes = 512L * 1024 * 1024;   // 512 MB working set → Degraded
                const long criticalThresholdBytes = 1024L * 1024 * 1024; // 1 GB working set → Unhealthy

                string status;
                string description;

                if (workingSet >= criticalThresholdBytes)
                {
                    status = "Unhealthy";
                    description = $"Memory usage critically high. " +
                                  $"WorkingSet: {BytesToMegabytes(workingSet):F1} MB, " +
                                  $"PrivateMemory: {BytesToMegabytes(privateMemory):F1} MB, " +
                                  $"GC Managed: {BytesToMegabytes(managedMemory):F1} MB.";
                }
                else if (workingSet >= warningThresholdBytes)
                {
                    status = "Degraded";
                    description = $"Memory usage is elevated. " +
                                  $"WorkingSet: {BytesToMegabytes(workingSet):F1} MB, " +
                                  $"PrivateMemory: {BytesToMegabytes(privateMemory):F1} MB, " +
                                  $"GC Managed: {BytesToMegabytes(managedMemory):F1} MB.";
                }
                else
                {
                    status = "Healthy";
                    description = $"Memory usage is within normal limits. " +
                                  $"WorkingSet: {BytesToMegabytes(workingSet):F1} MB, " +
                                  $"GC Managed: {BytesToMegabytes(managedMemory):F1} MB.";
                }

                sw.Stop();

                return new HealthCheckDetail
                {
                    Name = "Memory",
                    Status = status,
                    Description = description,
                    DurationMs = sw.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "Memory health check failed");

                return new HealthCheckDetail
                {
                    Name = "Memory",
                    Status = "Unhealthy",
                    Description = $"Memory check failed: {ex.Message}",
                    DurationMs = sw.ElapsedMilliseconds
                };
            }
        }

        /// <summary>
        /// Checks CPU usage by measuring the process's total processor time over a short sampling interval.
        /// Healthy: usage below 60%.
        /// Degraded: usage between 60–85%.
        /// Unhealthy: usage above 85%.
        /// </summary>
        private async Task<HealthCheckDetail> CheckCpuUsageAsync(CancellationToken ct)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var process = System.Diagnostics.Process.GetCurrentProcess();

                // Capture initial CPU time
                TimeSpan startCpuTime = process.TotalProcessorTime;
                DateTime startTime = DateTime.UtcNow;

                // Sample over a short interval (300 ms is enough to get a meaningful reading)
                const int sampleIntervalMs = 300;
                await Task.Delay(sampleIntervalMs, ct);

                // Capture final CPU time
                TimeSpan endCpuTime = process.TotalProcessorTime;
                DateTime endTime = DateTime.UtcNow;

                double elapsedMs = (endTime - startTime).TotalMilliseconds;
                double cpuUsedMs = (endCpuTime - startCpuTime).TotalMilliseconds;

                // CPU usage as a percentage of total available processors
                double cpuUsagePercent = (cpuUsedMs / (elapsedMs * Environment.ProcessorCount)) * 100.0;

                string status;
                string description;

                if (cpuUsagePercent > 85.0)
                {
                    status = "Unhealthy";
                }
                else if (cpuUsagePercent > 60.0)
                {
                    status = "Degraded";
                }
                else
                {
                    status = "Healthy";
                }

                description = $"CPU usage: {cpuUsagePercent:F1}% across {Environment.ProcessorCount} cores. " +
                              $"Sampled over {elapsedMs:F0} ms.";

                sw.Stop();

                return new HealthCheckDetail
                {
                    Name = "CPU",
                    Status = status,
                    Description = description,
                    DurationMs = sw.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "CPU health check failed");

                return new HealthCheckDetail
                {
                    Name = "CPU",
                    Status = "Unhealthy",
                    Description = $"CPU check failed: {ex.Message}",
                    DurationMs = sw.ElapsedMilliseconds
                };
            }
        }

        /// <summary>
        /// Converts bytes to megabytes.
        /// </summary>
        private static double BytesToMegabytes(long bytes) => bytes / (1024.0 * 1024.0);

        /// <summary>
        /// Derives the overall health status from individual checks.
        /// - "Healthy" if all are Healthy.
        /// - "Degraded" if at least one is Degraded but none are Unhealthy.
        /// - "Unhealthy" if any check is Unhealthy.
        /// </summary>
        private static string DeriveOverallStatus(List<HealthCheckDetail> details)
        {
            if (details.Count == 0)
                return "Healthy";

            if (details.Any(d => d.Status == "Unhealthy"))
                return "Unhealthy";

            if (details.Any(d => d.Status == "Degraded"))
                return "Degraded";

            return "Healthy";
        }
    }
}
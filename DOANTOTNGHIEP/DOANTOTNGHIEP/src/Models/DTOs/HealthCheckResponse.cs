namespace DOANTOTNGHIEP.src.Models.DTOs
{
    /// <summary>
    /// Represents the health check response returned by the /health endpoint.
    /// </summary>
    public class HealthCheckResponse
    {
        /// <summary>
        /// Overall health status: Healthy, Degraded, or Unhealthy.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the health check was performed (ISO 8601 UTC).
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Individual health check results.
        /// </summary>
        public List<HealthCheckDetail> Details { get; set; } = new();
    }

    /// <summary>
    /// A single health check component result.
    /// </summary>
    public class HealthCheckDetail
    {
        /// <summary>
        /// Name of the health check component (e.g., "API", "Database").
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Status of this component: Healthy, Degraded, or Unhealthy.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Optional description with details about this component's health.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// How long this check took to execute (in milliseconds).
        /// </summary>
        public long DurationMs { get; set; }
    }
}
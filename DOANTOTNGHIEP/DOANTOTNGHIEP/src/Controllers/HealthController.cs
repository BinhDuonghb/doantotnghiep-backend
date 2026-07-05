using Microsoft.AspNetCore.Mvc;
using DOANTOTNGHIEP.src.Models.DTOs;
using DOANTOTNGHIEP.src.Services;

namespace DOANTOTNGHIEP.src.Controllers
{
    /// <summary>
    /// Controller for application health check endpoints.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly HealthCheckService _healthCheckService;

        public HealthController(HealthCheckService healthCheckService)
        {
            _healthCheckService = healthCheckService;
        }

        /// <summary>
        /// GET /health
        /// Returns the overall health status of the application.
        /// 200 OK for Healthy/Degraded, 503 Service Unavailable for Unhealthy.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetHealth(CancellationToken ct)
        {
            var response = await _healthCheckService.CheckHealthAsync(ct);

            if (response.Status == "Unhealthy")
            {
                return StatusCode(503, response);
            }

            return Ok(response);
        }
    }
}
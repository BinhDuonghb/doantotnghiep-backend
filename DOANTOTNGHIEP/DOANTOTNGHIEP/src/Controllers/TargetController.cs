using DOANTOTNGHIEP.src.Models;
using DOANTOTNGHIEP.src.Models.DTOs;
using DOANTOTNGHIEP.src.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DOANTOTNGHIEP.src.Controllers
{
    /// <summary>
    /// Controller for target-related endpoints with progress tracking
    /// </summary>
    [ApiController]
    [Route("api/targets")]
    [Authorize]
    public class TargetController : ControllerBase
    {
        private readonly ITargetService _targetService;

        /// <summary>
        /// Initializes a new instance of the TargetController class
        /// </summary>
        /// <param name="targetService">Target service</param>
        public TargetController(ITargetService targetService)
        {
            _targetService = targetService;
        }

        /// <summary>
        /// Gets targets for the current user
        /// GET /api/targets
        /// </summary>
        /// <returns>List of targets (200)</returns>
        [HttpGet]
        [ProducesResponseType(typeof(GetUserTargetsResponseDto), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        [ProducesResponseType(typeof(ProblemDetails), 404)]
        public async Task<ActionResult<GetUserTargetsResponseDto>> GetUserTargets()
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ProblemDetails
                {
                    Title = "Unauthorized",
                    Status = 401,
                    Detail = "Invalid or missing access token."
                });
            }

            try
            {
                var targets = await _targetService.GetUserTargetsAsync(userId);

                var targetDtos = targets.Select(target => new TargetResponseDto
                {
                    Id = target.Id,
                    UserId = target.UserId,
                    Title = target.Title,
                    TargetType = target.TargetType,
                    TargetValue = target.TargetValue,
                    CurrentProgress = target.CurrentProgress,
                    TargetUnit = target.TargetUnit,
                    IsCovered = target.IsCovered,
                    CoveredAt = target.CoveredAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    CreatedAt = target.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    UpdatedAt = target.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
                }).ToList();

                return Ok(new GetUserTargetsResponseDto
                {
                    Targets = targetDtos,
                    TotalCount = targetDtos.Count
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "User not found",
                    Status = 404,
                    Detail = "The requested user was not found."
                });
            }
            catch (Exception)
            {
                return Problem(
                    title: "Internal server error",
                    detail: "Failed to retrieve targets.",
                    statusCode: 500
                );
            }
        }

        /// <summary>
        /// Creates a new target for the current user
        /// POST /api/targets
        /// </summary>
        /// <param name="request">Create target request data</param>
        /// <returns>Created target information (201)</returns>
        [HttpPost]
        [ProducesResponseType(typeof(TargetResponseDto), 201)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        public async Task<ActionResult<TargetResponseDto>> CreateTarget([FromBody] CreateTargetRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ProblemDetails
                {
                    Title = "Unauthorized",
                    Status = 401,
                    Detail = "Invalid or missing access token."
                });
            }

            try
            {
                var target = await _targetService.CreateTargetAsync(userId, request);

                var response = new TargetResponseDto
                {
                    Id = target.Id,
                    UserId = target.UserId,
                    Title = target.Title,
                    TargetType = target.TargetType,
                    TargetValue = target.TargetValue,
                    CurrentProgress = target.CurrentProgress,
                    TargetUnit = target.TargetUnit,
                    IsCovered = target.IsCovered,
                    CoveredAt = target.CoveredAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    CreatedAt = target.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    UpdatedAt = target.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                return CreatedAtAction(nameof(GetTargetDetail), new { targetId = target.Id }, response);
            }
            catch (Exception)
            {
                return Problem(
                    title: "Internal server error",
                    detail: "Failed to create target.",
                    statusCode: 500
                );
            }
        }

        /// <summary>
        /// Updates an existing target
        /// PUT /api/targets/{targetId}
        /// </summary>
        /// <param name="targetId">Target ID</param>
        /// <param name="request">Update target request data</param>
        /// <returns>Updated target information (200)</returns>
        [HttpPut("{targetId}")]
        [ProducesResponseType(typeof(TargetResponseDto), 200)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        [ProducesResponseType(typeof(ProblemDetails), 404)]
        public async Task<ActionResult<TargetResponseDto>> UpdateTarget(string targetId, [FromBody] UpdateTargetRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ProblemDetails
                {
                    Title = "Unauthorized",
                    Status = 401,
                    Detail = "Invalid or missing access token."
                });
            }

            try
            {
                var target = await _targetService.UpdateTargetAsync(targetId, request);

                // Check if the target belongs to the current user
                if (target.UserId != userId)
                {
                    return NotFound(new ProblemDetails
                    {
                        Title = "Target not found",
                        Status = 404,
                        Detail = "The requested target was not found."
                    });
                }

                var response = new TargetResponseDto
                {
                    Id = target.Id,
                    UserId = target.UserId,
                    Title = target.Title,
                    TargetType = target.TargetType,
                    TargetValue = target.TargetValue,
                    CurrentProgress = target.CurrentProgress,
                    TargetUnit = target.TargetUnit,
                    IsCovered = target.IsCovered,
                    CoveredAt = target.CoveredAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    CreatedAt = target.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    UpdatedAt = target.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                return Ok(response);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Target not found",
                    Status = 404,
                    Detail = "The requested target was not found."
                });
            }
            catch (Exception)
            {
                return Problem(
                    title: "Internal server error",
                    detail: "Failed to retrieve target.",
                    statusCode: 500
                );
            }
        }

        /// <summary>
        /// Deletes a target
        /// DELETE /api/targets/{targetId}
        /// </summary>
        /// <param name="targetId">Target ID</param>
        /// <returns>Deletion confirmation (200)</returns>
        [HttpDelete("{targetId}")]
        [ProducesResponseType(typeof(DeleteTargetResponseDto), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        [ProducesResponseType(typeof(ProblemDetails), 404)]
        public async Task<ActionResult<DeleteTargetResponseDto>> DeleteTarget(string targetId)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ProblemDetails
                {
                    Title = "Unauthorized",
                    Status = 401,
                    Detail = "Invalid or missing access token."
                });
            }

            try
            {
                var result = await _targetService.DeleteTargetAsync(targetId, userId);

                if (!result)
                {
                    return NotFound(new ProblemDetails
                    {
                        Title = "Target not found",
                        Status = 404,
                        Detail = "The requested target was not found."
                    });
                }

                return Ok(new DeleteTargetResponseDto
                {
                    Success = true,
                    Message = "Target deleted successfully"
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Target not found",
                    Status = 404,
                    Detail = "The requested target was not found."
                });
            }
            catch (Exception)
            {
                return Problem(
                    title: "Internal server error",
                    detail: "Failed to delete target.",
                    statusCode: 500
                );
            }
        }

        /// <summary>
        /// Covers/Completes a target
        /// PATCH /api/targets/{targetId}/cover
        /// </summary>
        /// <param name="targetId">Target ID</param>
        /// <param name="request">Cover target request data</param>
        /// <returns>Covered target information (200)</returns>
        [HttpPatch("{targetId}/cover")]
        [ProducesResponseType(typeof(TargetResponseDto), 200)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        [ProducesResponseType(typeof(ProblemDetails), 404)]
        public async Task<ActionResult<TargetResponseDto>> CoverTarget(string targetId, [FromBody] CoverTargetRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ProblemDetails
                {
                    Title = "Unauthorized",
                    Status = 401,
                    Detail = "Invalid or missing access token."
                });
            }

            try
            {
                var target = await _targetService.CoverTargetAsync(targetId, request);

                // Check if the target belongs to the current user
                if (target.UserId != userId)
                {
                    return NotFound(new ProblemDetails
                    {
                        Title = "Target not found",
                        Status = 404,
                        Detail = "The requested target was not found."
                    });
                }

                var response = new TargetResponseDto
                {
                    Id = target.Id,
                    UserId = target.UserId,
                    Title = target.Title,
                    TargetType = target.TargetType,
                    TargetValue = target.TargetValue,
                    CurrentProgress = target.CurrentProgress,
                    TargetUnit = target.TargetUnit,
                    IsCovered = target.IsCovered,
                    CoveredAt = target.CoveredAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    CreatedAt = target.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    UpdatedAt = target.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                return Ok(response);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Target not found",
                    Status = 404,
                    Detail = "The requested target was not found."
                });
            }
            catch (Exception)
            {
                return Problem(
                    title: "Internal server error",
                    detail: "Failed to cover target.",
                    statusCode: 500
                );
            }
        }

        /// <summary>
        /// Gets a specific target detail by ID
        /// GET /api/targets/{targetId}
        /// </summary>
        /// <param name="targetId">Target ID</param>
        /// <returns>Target detail information (200)</returns>
        [HttpGet("{targetId}")]
        [ProducesResponseType(typeof(TargetDetailResponseDto), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        [ProducesResponseType(typeof(ProblemDetails), 404)]
        public async Task<ActionResult<TargetDetailResponseDto>> GetTargetDetail(string targetId)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ProblemDetails
                {
                    Title = "Unauthorized",
                    Status = 401,
                    Detail = "Invalid or missing access token."
                });
            }

            try
            {
                var target = await _targetService.GetTargetByIdAsync(targetId);

                // Check if the target belongs to the current user
                if (target.UserId != userId)
                {
                    return NotFound(new ProblemDetails
                    {
                        Title = "Target not found",
                        Status = 404,
                        Detail = "The requested target was not found."
                    });
                }

                var response = new TargetDetailResponseDto
                {
                    Id = target.Id,
                    UserId = target.UserId,
                    Title = target.Title,
                    TargetType = target.TargetType,
                    TargetValue = target.TargetValue,
                    CurrentProgress = target.CurrentProgress,
                    TargetUnit = target.TargetUnit,
                    IsCovered = target.IsCovered,
                    CoveredAt = target.CoveredAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    CreatedAt = target.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    UpdatedAt = target.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    ProgressHistory = target.ProgressHistory.Select(ph => new ProgressHistoryItemDto
                    {
                        Value = ph.Value,
                        RecordedAt = ph.RecordedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
                    }).ToList()
                };

                return Ok(response);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Target not found",
                    Status = 404,
                    Detail = "The requested target was not found."
                });
            }
            catch (Exception)
            {
                return Problem(
                    title: "Internal server error",
                    detail: "Failed to retrieve target.",
                    statusCode: 500
                );
            }
        }

        /// <summary>
        /// Extracts the current user's ID from JWT claims
        /// </summary>
        /// <returns>The user ID string, or null if not found</returns>
        private string? GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}
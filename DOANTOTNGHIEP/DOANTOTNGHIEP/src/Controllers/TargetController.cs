using DOANTOTNGHIEP.src.Models;
using DOANTOTNGHIEP.src.Models.DTOs;
using DOANTOTNGHIEP.src.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DOANTOTNGHIEP.src.Controllers
{
    /// <summary>
    /// Controller for target-related endpoints.
    /// Targets represent user goals with a timeframe and an AI-generated plan.
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

                var targetDtos = targets.Select(MapToResponseDto).ToList();

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
                var response = MapToResponseDto(target);

                return CreatedAtAction(nameof(GetTargetDetail), new { targetId = target.Id }, response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid request",
                    Status = 400,
                    Detail = ex.Message
                });
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

                return Ok(MapToResponseDto(target));
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
            catch (ArgumentException ex)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid request",
                    Status = 400,
                    Detail = ex.Message
                });
            }
            catch (Exception)
            {
                return Problem(
                    title: "Internal server error",
                    detail: "Failed to update target.",
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
        /// Gets a specific target detail by ID (includes plan + progress stats)
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

                var response = MapToDetailResponseDto(target);
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
        /// Gets the AI-generated plan for a target
        /// GET /api/targets/{targetId}/plan
        /// </summary>
        /// <param name="targetId">Target ID</param>
        /// <returns>The plan data for the target (200)</returns>
        [HttpGet("{targetId}/plan")]
        [ProducesResponseType(typeof(PlanDataDto), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        [ProducesResponseType(typeof(ProblemDetails), 404)]
        public async Task<ActionResult<PlanDataDto>> GetTargetPlan(string targetId)
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

                // Check ownership
                if (target.UserId != userId)
                {
                    return NotFound(new ProblemDetails
                    {
                        Title = "Target not found",
                        Status = 404,
                        Detail = "The requested target was not found."
                    });
                }

                if (target.Plan == null)
                {
                    return NotFound(new ProblemDetails
                    {
                        Title = "Plan not found",
                        Status = 404,
                        Detail = "No plan has been generated for this target yet."
                    });
                }

                return Ok(MapPlanToDto(target.Plan));
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
                    detail: "Failed to retrieve plan.",
                    statusCode: 500
                );
            }
        }

        /// <summary>
        /// Updates the completion status of a checklist item within a milestone.
        /// PATCH /api/targets/{targetId}/milestones/{milestoneOrder}/checklist/{checklistIndex}
        /// </summary>
        /// <param name="targetId">Target ID</param>
        /// <param name="milestoneOrder">1-based milestone order number (e.g., 1 for first milestone)</param>
        /// <param name="checklistIndex">0-based index of the checklist item within the milestone</param>
        /// <param name="request">Request body with isCompleted flag</param>
        /// <returns>Updated plan data with progress stats (200)</returns>
        [HttpPatch("{targetId}/milestones/{milestoneOrder}/checklist/{checklistIndex}")]
        [ProducesResponseType(typeof(TargetDetailResponseDto), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        [ProducesResponseType(typeof(ProblemDetails), 403)]
        [ProducesResponseType(typeof(ProblemDetails), 404)]
        public async Task<ActionResult<TargetDetailResponseDto>> UpdateChecklistItem(
            string targetId, int milestoneOrder, int checklistIndex,
            [FromBody] UpdateChecklistItemRequest request)
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
                // Verify ownership first
                var target = await _targetService.GetTargetByIdAsync(targetId);
                if (target == null || target.UserId != userId)
                {
                    return NotFound(new ProblemDetails
                    {
                        Title = "Target not found",
                        Status = 404,
                        Detail = "The requested target was not found."
                    });
                }

                var success = await _targetService.UpdateChecklistItemAsync(
                    targetId, milestoneOrder, checklistIndex, request.IsCompleted);

                if (!success)
                {
                    return NotFound(new ProblemDetails
                    {
                        Title = "Checklist item not found",
                        Status = 404,
                        Detail = $"Checklist item at index {checklistIndex} in milestone {milestoneOrder} was not found."
                    });
                }

                // Fetch the updated target to return full state
                target = await _targetService.GetTargetByIdAsync(targetId);
                return Ok(MapToDetailResponseDto(target));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Not found",
                    Status = 404,
                    Detail = ex.Message
                });
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid checklist index",
                    Status = 400,
                    Detail = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid operation",
                    Status = 400,
                    Detail = ex.Message
                });
            }
            catch (Exception)
            {
                return Problem(
                    title: "Internal server error",
                    detail: "Failed to update checklist item.",
                    statusCode: 500
                );
            }
        }

        // ==================== Mapping Helpers ====================

        /// <summary>
        /// Maps a Target entity to a TargetResponseDto
        /// </summary>
        private static TargetResponseDto MapToResponseDto(Target target)
        {
            return new TargetResponseDto
            {
                Id = target.Id,
                UserId = target.UserId,
                Title = target.Title,
                Description = target.Description,
                Duration = target.Duration,
                DurationUnit = target.DurationUnit,
                Status = target.Status,
                StartedAt = target.StartedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                CompletedAt = target.CompletedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                HasPlan = target.Plan != null,
                CreatedAt = target.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                UpdatedAt = target.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
        }

        /// <summary>
        /// Maps a Target entity to a TargetDetailResponseDto (includes plan + stats)
        /// </summary>
        private static TargetDetailResponseDto MapToDetailResponseDto(Target target)
        {
            var plan = target.Plan;
            var totalChecklistItems = plan?.Milestones
                .SelectMany(m => m.ChecklistItems)
                .Count() ?? 0;
            var completedChecklistItems = plan?.Milestones
                .SelectMany(m => m.ChecklistItems)
                .Count(c => c.IsCompleted) ?? 0;

            return new TargetDetailResponseDto
            {
                Id = target.Id,
                UserId = target.UserId,
                Title = target.Title,
                Description = target.Description,
                Duration = target.Duration,
                DurationUnit = target.DurationUnit,
                Status = target.Status,
                StartedAt = target.StartedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                CompletedAt = target.CompletedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                HasPlan = plan != null,
                CreatedAt = target.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                UpdatedAt = target.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Plan = plan != null ? MapPlanToDto(plan) : null,
                TotalMilestones = plan?.Milestones.Count ?? 0,
                CompletedChecklistItems = completedChecklistItems,
                TotalChecklistItems = totalChecklistItems
            };
        }

        /// <summary>
        /// Maps a PlanData entity to a PlanDataDto
        /// </summary>
        private static PlanDataDto MapPlanToDto(PlanData plan)
        {
            return new PlanDataDto
            {
                Summary = plan.Summary,
                GeneratedAt = plan.GeneratedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Milestones = plan.Milestones.Select(MapMilestoneToDto).ToList(),
                Quizzes = plan.Quizzes.Select(MapPlanQuizToDto).ToList()
            };
        }

        /// <summary>
        /// Maps a MilestoneItem entity to a MilestoneItemDto
        /// </summary>
        private static MilestoneItemDto MapMilestoneToDto(MilestoneItem milestone)
        {
            return new MilestoneItemDto
            {
                Title = milestone.Title,
                Description = milestone.Description,
                Order = milestone.Order,
                DurationEstimate = milestone.DurationEstimate,
                ChecklistItems = milestone.ChecklistItems.Select(MapChecklistItemToDto).ToList()
            };
        }

        /// <summary>
        /// Maps a MilestoneChecklistItem entity to a MilestoneChecklistItemDto
        /// </summary>
        private static MilestoneChecklistItemDto MapChecklistItemToDto(MilestoneChecklistItem item)
        {
            return new MilestoneChecklistItemDto
            {
                Title = item.Title,
                IsCompleted = item.IsCompleted
            };
        }

        /// <summary>
        /// Maps a PlanQuizItem entity to a PlanQuizItemDto
        /// </summary>
        private static PlanQuizItemDto MapPlanQuizToDto(PlanQuizItem quiz)
        {
            return new PlanQuizItemDto
            {
                Question = quiz.Question,
                Options = quiz.Options ?? new List<string>(),
                CorrectAnswer = quiz.CorrectAnswer,
                MilestoneTitle = quiz.MilestoneTitle
            };
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
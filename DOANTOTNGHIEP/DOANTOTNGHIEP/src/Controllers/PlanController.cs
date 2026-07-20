using DOANTOTNGHIEP.src.Models.DTOs;
using DOANTOTNGHIEP.src.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DOANTOTNGHIEP.src.Controllers
{
    /// <summary>
    /// Controller for AI plan generation endpoints.
    /// Generates structured study plans with milestones, checklists, and quizzes
    /// based on a user's target.
    /// </summary>
    [ApiController]
    [Route("api/plan")]
    [Authorize]
    public class PlanController : ControllerBase
    {
        private readonly ITargetService _targetService;
        private readonly IAiAssistantService _aiAssistantService;

        /// <summary>
        /// Initializes a new instance of the PlanController class
        /// </summary>
        /// <param name="targetService">Target service for target operations</param>
        /// <param name="aiAssistantService">AI assistant service for plan generation</param>
        public PlanController(ITargetService targetService, IAiAssistantService aiAssistantService)
        {
            _targetService = targetService;
            _aiAssistantService = aiAssistantService;
        }

        /// <summary>
        /// Generates an AI-powered study plan for a target.
        /// If feedback is provided in the request, the AI will refine the existing plan.
        /// POST /api/plan/generate
        /// </summary>
        /// <param name="request">Generate plan request with target ID and optional feedback</param>
        /// <returns>The generated or refined plan (201)</returns>
        [HttpPost("generate")]
        [ProducesResponseType(typeof(GeneratePlanResponse), 201)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        [ProducesResponseType(typeof(ProblemDetails), 403)]
        [ProducesResponseType(typeof(ProblemDetails), 404)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        [ProducesResponseType(typeof(ProblemDetails), 503)]
        public async Task<ActionResult<GeneratePlanResponse>> GeneratePlan([FromBody] GeneratePlanRequest request)
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
                // Fetch the target and verify ownership
                var target = await _targetService.GetTargetByIdAsync(request.TargetId);

                if (target == null)
                {
                    return NotFound(new ProblemDetails
                    {
                        Title = "Target not found",
                        Status = 404,
                        Detail = $"Target with ID '{request.TargetId}' was not found."
                    });
                }

                if (target.UserId != userId)
                {
                    return StatusCode(403, new ProblemDetails
                    {
                        Title = "Target not accessible",
                        Status = 403,
                        Detail = "You do not have access to the specified target."
                    });
                }

                // Generate or regenerate the plan
                Models.PlanData planData;
                string message;

                if (!string.IsNullOrWhiteSpace(request.Feedback))
                {
                    planData = await _aiAssistantService.RegeneratePlanAsync(target, request.Feedback);
                    message = "Plan refined successfully based on your feedback.";
                }
                else
                {
                    planData = await _aiAssistantService.GeneratePlanAsync(target);
                    message = "Plan generated successfully.";
                }

                // Save the plan to the target
                await _targetService.SetPlanAsync(request.TargetId, planData);

                // Build the response
                return StatusCode(201, new GeneratePlanResponse
                {
                    TargetId = request.TargetId,
                    Plan = MapPlanToDto(planData),
                    Message = message
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Target not found",
                    Status = 404,
                    Detail = $"Target with ID '{request.TargetId}' was not found."
                });
            }
            catch (AiAssistantServiceUnavailableException ex)
            {
                return Problem(
                    title: "AI service unavailable",
                    detail: ex.Message,
                    statusCode: 503
                );
            }
            catch (AiAssistantApiException ex)
            {
                return Problem(
                    title: "AI service error",
                    detail: ex.Message,
                    statusCode: (int)ex.StatusCode >= 400 && (int)ex.StatusCode < 600
                        ? (int)ex.StatusCode
                        : 500
                );
            }
            catch (Exception ex)
            {
                return Problem(
                    title: "Internal server error",
                    detail: ex.Message,
                    statusCode: 500
                );
            }
        }

        // ==================== Mapping Helpers ====================

        /// <summary>
        /// Maps a PlanData entity to a PlanDataDto
        /// </summary>
        private static PlanDataDto MapPlanToDto(Models.PlanData plan)
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
        private static MilestoneItemDto MapMilestoneToDto(Models.MilestoneItem milestone)
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
        private static MilestoneChecklistItemDto MapChecklistItemToDto(Models.MilestoneChecklistItem item)
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
        private static PlanQuizItemDto MapPlanQuizToDto(Models.PlanQuizItem quiz)
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
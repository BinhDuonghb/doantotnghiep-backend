using DOANTOTNGHIEP.src.Models.DTOs;
using DOANTOTNGHIEP.src.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DOANTOTNGHIEP.src.Controllers
{
    /// <summary>
    /// Controller for quiz-related endpoints
    /// </summary>
    [ApiController]
    [Authorize]
    public class QuizController : ControllerBase
    {
        private readonly IQuizService _quizService;
        private readonly IUserService _userService;
        private readonly IAiAssistantService _aiAssistantService;
        private readonly ITargetService _targetService;

        /// <summary>
        /// Initializes a new instance of the QuizController class
        /// </summary>
        /// <param name="quizService">Quiz service</param>
        /// <param name="userService">User service</param>
        /// <param name="aiQuizService">AI quiz generation service</param>
        /// <param name="targetService">Target service</param>
        public QuizController(
            IQuizService quizService,
            IUserService userService,
            IAiAssistantService aiQuizService,
            ITargetService targetService)
        {
            _quizService = quizService;
            _userService = userService;
            _aiAssistantService = aiQuizService;
            _targetService = targetService;
        }

        /// <summary>
        /// Gets quizzes for the current authenticated user.
        /// Returns quizzes only if the user has a target set.
        /// GET /api/quiz
        /// </summary>
        /// <returns>Quizzes with userTargetSet flag (200)</returns>
        [HttpGet("/api/quiz")]
        [ProducesResponseType(typeof(GetQuizzesResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        [ProducesResponseType(typeof(ProblemDetails), 403)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        public async Task<ActionResult<GetQuizzesResponse>> GetQuizzes()
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

            // Verify user exists
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return Unauthorized(new ProblemDetails
                {
                    Title = "Unauthorized",
                    Status = 401,
                    Detail = "User account not found."
                });
            }

            try
            {
                var result = await _quizService.GetQuizzesAsync(userId);

                // If user target is not set, return 403 per API contract
                if (!result.UserTargetSet)
                {
                    return StatusCode(403, new ProblemDetails
                    {
                        Title = "User target not set",
                        Status = 403,
                        Detail = "You must set a target before accessing quizzes."
                    });
                }

                return Ok(result);
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

        /// <summary>
        /// Generates quizzes (admin only).
        /// POST /api/admin/quiz/generate
        /// </summary>
        /// <param name="request">Generation parameters: count, category, difficulty</param>
        /// <returns>Generated quizzes with count (201)</returns>
        [HttpPost("/api/admin/quiz/generate")]
        [Authorize(Policy = "Admin")]
        [ProducesResponseType(typeof(GenerateQuizzesResponse), 201)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        [ProducesResponseType(typeof(ProblemDetails), 403)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        public async Task<ActionResult<GenerateQuizzesResponse>> GenerateQuizzes([FromBody] GenerateQuizzesRequest request)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            try
            {
                var result = await _quizService.GenerateQuizzesAsync(request.Count, request.Category, request.Difficulty);

                return StatusCode(201, result);
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

        /// <summary>
        /// Generates AI-powered quizzes from the user's target.
        /// Calls OpenRouter API to create relevant quiz questions.
        /// POST /api/quiz/generate-from-target
        /// </summary>
        /// <param name="request">Generation parameters: targetId (optional), count, category, difficulty</param>
        /// <returns>AI-generated quizzes with target info (201)</returns>
        [HttpPost("/api/quiz/generate-from-target")]
        [ProducesResponseType(typeof(GenerateQuizzesFromTargetResponse), 201)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        [ProducesResponseType(typeof(ProblemDetails), 403)]
        [ProducesResponseType(typeof(ProblemDetails), 404)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        [ProducesResponseType(typeof(ProblemDetails), 503)]
        public async Task<ActionResult<GenerateQuizzesFromTargetResponse>> GenerateQuizzesFromTarget(
            [FromBody] GenerateQuizzesFromTargetRequest request)
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
                // Resolve the target to use
                DOANTOTNGHIEP.src.Models.Target? target;

                if (!string.IsNullOrEmpty(request.TargetId))
                {
                    // Use specified target ID — must belong to the user
                    target = await _targetService.GetTargetByIdAsync(request.TargetId);

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
                }
                else
                {
                    // Use the user's first (active) target
                    var targets = await _targetService.GetUserTargetsAsync(userId);
                    target = targets.FirstOrDefault();

                    if (target == null)
                    {
                        return StatusCode(403, new ProblemDetails
                        {
                            Title = "Target not set",
                            Status = 403,
                            Detail = "You must set a target before generating quizzes."
                        });
                    }
                }

                // Call AI service to generate quizzes
                var result = await _aiAssistantService.GenerateQuizzesFromTargetAsync(
                    target,
                    request.Count,
                    request.Category,
                    request.Difficulty);

                return StatusCode(201, result);
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

        /// <summary>
        /// Gets the status of an asynchronous quiz generation job.
        /// GET /api/quiz/generation-status/{generationId}
        /// </summary>
        /// <param name="generationId">The generation job ID</param>
        /// <returns>Generation status (200)</returns>
        [HttpGet("/api/quiz/generation-status/{generationId}")]
        [ProducesResponseType(typeof(GenerationStatusResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        [ProducesResponseType(typeof(ProblemDetails), 404)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        public ActionResult<GenerationStatusResponse> GetGenerationStatus(string generationId)
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

            // Note: The current implementation is synchronous.
            // This endpoint returns a default "completed" status since
            // generation happens inline. In a future async implementation,
            // this would query a background job store.
            return Ok(new GenerationStatusResponse
            {
                GenerationId = generationId,
                Status = "completed",
                GeneratedCount = 0,
                RequestedCount = 0,
                CreatedAt = DateTime.UtcNow.ToString("o"),
                CompletedAt = DateTime.UtcNow.ToString("o")
            });
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
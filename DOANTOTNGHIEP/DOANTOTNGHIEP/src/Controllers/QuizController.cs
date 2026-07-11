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

        /// <summary>
        /// Initializes a new instance of the QuizController class
        /// </summary>
        /// <param name="quizService">Quiz service</param>
        /// <param name="userService">User service</param>
        public QuizController(IQuizService quizService, IUserService userService)
        {
            _quizService = quizService;
            _userService = userService;
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
        /// Extracts the current user's ID from JWT claims
        /// </summary>
        /// <returns>The user ID string, or null if not found</returns>
        private string? GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}

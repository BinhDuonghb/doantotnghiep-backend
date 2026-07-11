using DOANTOTNGHIEP.src.Models.DTOs;
using DOANTOTNGHIEP.src.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DOANTOTNGHIEP.src.Controllers
{
    /// <summary>
    /// Controller for user-related endpoints (profile, target management)
    /// </summary>
    [ApiController]
    [Route("api/user")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        /// <summary>
        /// Initializes a new instance of the UserController class
        /// </summary>
        /// <param name="userService">User service</param>
        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Gets the current user's target
        /// GET /api/user/target
        /// </summary>
        /// <returns>User target information with target, targetType, isSet (200)</returns>
        [HttpGet("target")]
        [ProducesResponseType(typeof(GetUserTargetResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        [ProducesResponseType(typeof(ProblemDetails), 404)]
        public async Task<ActionResult<GetUserTargetResponse>> GetUserTarget()
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

            // Check if user exists
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "User not found",
                    Status = 404,
                    Detail = "The user account was not found."
                });
            }

            var targetInfo = await _userService.GetUserTargetAsync(userId);

            return Ok(new GetUserTargetResponse
            {
                Target = targetInfo?.target,
                TargetType = targetInfo?.targetType,
                IsSet = targetInfo?.isSet ?? false
            });
        }

        /// <summary>
        /// Sets the current user's target
        /// POST /api/user/target
        /// </summary>
        /// <param name="request">Set target request with target and targetType</param>
        /// <returns>Success response (200)</returns>
        [HttpPost("target")]
        [ProducesResponseType(typeof(SetUserTargetResponse), 200)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        public async Task<ActionResult<SetUserTargetResponse>> SetUserTarget([FromBody] SetUserTargetRequest request)
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

            // Validate target and targetType are not empty
            if (string.IsNullOrWhiteSpace(request.Target) || string.IsNullOrWhiteSpace(request.TargetType))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid target data",
                    Status = 400,
                    Detail = "Target and targetType must be non-empty strings."
                });
            }

            var success = await _userService.UpdateUserTargetAsync(userId, request.Target, request.TargetType);
            if (!success)
            {
                return Problem(
                    title: "Internal server error",
                    detail: "Failed to update user target.",
                    statusCode: 500
                );
            }

            return Ok(new SetUserTargetResponse
            {
                Success = true,
                Message = "User target updated successfully"
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
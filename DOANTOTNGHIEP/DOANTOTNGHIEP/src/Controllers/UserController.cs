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
        /// <returns>User target information (200)</returns>
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

            var target = await _userService.GetUserTargetAsync(userId);
            var hasTarget = !string.IsNullOrWhiteSpace(target);

            return Ok(new GetUserTargetResponse
            {
                Target = target,
                HasTarget = hasTarget
            });
        }

        /// <summary>
        /// Updates the current user's target
        /// PUT /api/user/target
        /// </summary>
        /// <param name="request">Target update request</param>
        /// <returns>Updated target information (200)</returns>
        [HttpPut("target")]
        [ProducesResponseType(typeof(UpdateUserTargetResponse), 200)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        [ProducesResponseType(typeof(ProblemDetails), 404)]
        public async Task<ActionResult<UpdateUserTargetResponse>> UpdateUserTarget([FromBody] UpdateUserTargetRequest request)
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

            // Validate target value (must not be empty or whitespace)
            if (string.IsNullOrWhiteSpace(request.Target))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid target value",
                    Status = 400,
                    Detail = "Target must be a non-empty string."
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

            var success = await _userService.UpdateUserTargetAsync(userId, request.Target);
            if (!success)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "User not found",
                    Status = 404,
                    Detail = "The user account was not found."
                });
            }

            return Ok(new UpdateUserTargetResponse
            {
                Target = request.Target,
                Message = "Target updated successfully"
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
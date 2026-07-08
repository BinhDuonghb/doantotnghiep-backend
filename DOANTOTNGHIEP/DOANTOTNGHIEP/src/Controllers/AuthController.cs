using DOANTOTNGHIEP.src.Models.DTOs;
using DOANTOTNGHIEP.src.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DOANTOTNGHIEP.src.Controllers
{
    /// <summary>
    /// Controller for authentication-related endpoints
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IPasswordHasherService _passwordHasherService;
        private readonly IJwtService _jwtService;

        /// <summary>
        /// Initializes a new instance of the AuthController class
        /// </summary>
        /// <param name="userService">User service</param>
        /// <param name="passwordHasherService">Password hasher service</param>
        /// <param name="jwtService">JWT service</param>
        public AuthController(IUserService userService, IPasswordHasherService passwordHasherService, IJwtService jwtService)
        {
            _userService = userService;
            _passwordHasherService = passwordHasherService;
            _jwtService = jwtService;
        }

        /// <summary>
        /// Registers a new user
        /// POST /api/auth/register
        /// </summary>
        /// <param name="request">Registration request data</param>
        /// <returns>Authentication response with access and refresh tokens (201)</returns>
        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponse), 201)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 409)]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            // Check model state validation (including password confirmation)
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            // Check if user already exists
            var existingUser = await _userService.GetUserByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return Conflict(new ProblemDetails
                {
                    Title = "Email already exists",
                    Status = 409,
                    Detail = "A user with this email address already exists."
                });
            }

            // Create new user
            var user = await _userService.CreateUserAsync(request.Email, request.Password, request.FullName);

            // Generate tokens
            var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email);
            var refreshToken = _jwtService.GenerateRefreshToken();

            // Update user with refresh token (stored in MongoDB)
            await _userService.UpdateRefreshTokenAsync(user.Id, refreshToken, DateTime.UtcNow.AddDays(_jwtService.RefreshTokenExpiryDays));

            var response = new AuthResponse
            {
                User = new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.FullName,
                    CreatedAt = user.CreatedAt
                },
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = _jwtService.AccessTokenExpiryMinutes * 60
            };

            return CreatedAtAction(nameof(GetCurrentUser), response);
        }

        /// <summary>
        /// Authenticates a user and returns access and refresh tokens
        /// POST /api/auth/login
        /// </summary>
        /// <param name="request">Login request data</param>
        /// <returns>Authentication response with access and refresh tokens (200)</returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            // Find user by email
            var user = await _userService.GetUserByEmailAsync(request.Email);
            if (user == null)
            {
                return Unauthorized(new ProblemDetails
                {
                    Title = "Invalid credentials",
                    Status = 401,
                    Detail = "The provided email or password is incorrect."
                });
            }

            // Verify password
            if (!_passwordHasherService.VerifyPassword(request.Password, user.PasswordHash))
            {
                return Unauthorized(new ProblemDetails
                {
                    Title = "Invalid credentials",
                    Status = 401,
                    Detail = "The provided email or password is incorrect."
                });
            }

            // Generate tokens
            var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email);
            var refreshToken = _jwtService.GenerateRefreshToken();

            // Update user with refresh token (stored in MongoDB)
            await _userService.UpdateRefreshTokenAsync(user.Id, refreshToken, DateTime.UtcNow.AddDays(_jwtService.RefreshTokenExpiryDays));

            var response = new AuthResponse
            {
                User = new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.FullName,
                    CreatedAt = user.CreatedAt
                },
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = _jwtService.AccessTokenExpiryMinutes * 60
            };

            return Ok(response);
        }

        /// <summary>
        /// Refreshes an access token using a valid refresh token (token rotation)
        /// POST /api/auth/refresh
        /// </summary>
        /// <param name="request">Refresh token request data</param>
        /// <returns>New access and refresh tokens (200)</returns>
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(RefreshTokenResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        public async Task<ActionResult<RefreshTokenResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            // Find user by the provided refresh token (valid and non-expired)
            var user = await _userService.GetUserByRefreshTokenAsync(request.RefreshToken);
            if (user == null)
            {
                return Unauthorized(new ProblemDetails
                {
                    Title = "Invalid or expired refresh token",
                    Status = 401,
                    Detail = "The provided refresh token is invalid, expired, or has been revoked."
                });
            }

            // Generate new token pair (token rotation — old refresh token gets replaced)
            var newAccessToken = _jwtService.GenerateAccessToken(user.Id, user.Email);
            var newRefreshToken = _jwtService.GenerateRefreshToken();

            // Update user with the new refresh token and expiry
            await _userService.UpdateRefreshTokenAsync(user.Id, newRefreshToken, DateTime.UtcNow.AddDays(_jwtService.RefreshTokenExpiryDays));

            var response = new RefreshTokenResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresIn = _jwtService.AccessTokenExpiryMinutes * 60
            };

            return Ok(response);
        }

        /// <summary>
        /// Gets the current authenticated user's information from JWT claims
        /// GET /api/auth/me
        /// </summary>
        /// <returns>User information (200)</returns>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(UserInfo), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        public async Task<ActionResult<UserInfo>> GetCurrentUser()
        {
            // Extract user ID from the authenticated JWT claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new ProblemDetails
                {
                    Title = "Unauthorized",
                    Status = 401,
                    Detail = "Invalid or missing access token."
                });
            }

            // Look up the user in MongoDB
            var user = await _userService.GetUserByIdAsync(userIdClaim);
            if (user == null)
            {
                return Unauthorized(new ProblemDetails
                {
                    Title = "Unauthorized",
                    Status = 401,
                    Detail = "User not found. The account may have been deleted."
                });
            }

            var userInfo = new UserInfo
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.FullName,
                CreatedAt = user.CreatedAt
            };

            return Ok(userInfo);
        }

        /// <summary>
        /// Logs out a user by revoking their refresh token
        /// POST /api/auth/logout
        /// </summary>
        /// <param name="request">Logout request containing the refresh token to revoke</param>
        /// <returns>Success message (200)</returns>
        [HttpPost("logout")]
        [ProducesResponseType(typeof(LogoutResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        public async Task<ActionResult<LogoutResponse>> Logout([FromBody] RefreshTokenRequest request)
        {
            // Find user by the refresh token
            var user = await _userService.GetUserByRefreshTokenAsync(request.RefreshToken);
            if (user == null)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid refresh token",
                    Status = 400,
                    Detail = "The provided refresh token is invalid, expired, or already revoked."
                });
            }

            // Revoke the refresh token by clearing it from the user's document
            await _userService.RevokeRefreshTokenAsync(user.Id);

            return Ok(new LogoutResponse
            {
                Message = "Successfully logged out"
            });
        }
    }
}
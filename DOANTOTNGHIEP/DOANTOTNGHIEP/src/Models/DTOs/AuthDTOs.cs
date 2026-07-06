using System.ComponentModel.DataAnnotations;

namespace DOANTOTNGHIEP.src.Models.DTOs
{
    /// <summary>
    /// DTO for user registration request
    /// </summary>
    public class RegisterRequest
    {
        /// <summary>
        /// User's email address
        /// </summary>
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// User's password
        /// </summary>
        [Required]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// User's full name
        /// </summary>
        [Required]
        public string FullName { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for user login request
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// User's email address
        /// </summary>
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// User's password
        /// </summary>
        [Required]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for refresh token request
    /// </summary>
    public class RefreshTokenRequest
    {
        /// <summary>
        /// Refresh token
        /// </summary>
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for authentication response
    /// </summary>
    public class AuthResponse
    {
        /// <summary>
        /// Authenticated user information
        /// </summary>
        public UserInfo User { get; set; } = new();

        /// <summary>
        /// JWT access token
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// Refresh token
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for refresh token response (no user info per API contract)
    /// </summary>
    public class RefreshTokenResponse
    {
        /// <summary>
        /// New JWT access token
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// New refresh token
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for logout response
    /// </summary>
    public class LogoutResponse
    {
        /// <summary>
        /// Success message
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for user information in authentication response
    /// </summary>
    public class UserInfo
    {
        /// <summary>
        /// User's unique identifier
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// User's email address
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// User's full name
        /// </summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// User creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
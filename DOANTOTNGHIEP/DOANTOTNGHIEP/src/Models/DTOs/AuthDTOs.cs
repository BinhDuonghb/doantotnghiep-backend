using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

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
        [JsonPropertyName("name")]
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// Password confirmation (must match Password)
        /// </summary>
        [Required]
        [Compare("Password", ErrorMessage = "Password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
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

        /// <summary>
        /// Access token expiry time in seconds
        /// </summary>
        public int ExpiresIn { get; set; }
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

        /// <summary>
        /// Access token expiry time in seconds
        /// </summary>
        public int ExpiresIn { get; set; }
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
        /// User's name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// User creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO for setting user target request (POST /api/user/target)
    /// </summary>
    public class SetUserTargetRequest
    {
        /// <summary>
        /// The user's target value (e.g., "Weight Loss", "Muscle Gain")
        /// </summary>
        [Required]
        public string Target { get; set; } = string.Empty;

        /// <summary>
        /// The type of target (e.g., "fitness", "nutrition")
        /// </summary>
        [Required]
        public string TargetType { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for getting user target response (GET /api/user/target)
    /// </summary>
    public class GetUserTargetResponse
    {
        /// <summary>
        /// The user's target value
        /// </summary>
        public string? Target { get; set; }

        /// <summary>
        /// The type of the user's target
        /// </summary>
        public string? TargetType { get; set; }

        /// <summary>
        /// Whether the user has a target set
        /// </summary>
        public bool IsSet { get; set; }
    }

    /// <summary>
    /// DTO for setting user target response (POST /api/user/target)
    /// </summary>
    public class SetUserTargetResponse
    {
        /// <summary>
        /// Indicates the operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Success message
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    // ==================== Quiz DTOs ====================

    /// <summary>
    /// DTO for a single quiz item in API responses
    /// </summary>
    public class QuizDto
    {
        /// <summary>
        /// Quiz unique identifier
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The quiz question text
        /// </summary>
        public string Question { get; set; } = string.Empty;

        /// <summary>
        /// List of answer options
        /// </summary>
        public List<string> Options { get; set; } = new List<string>();

        /// <summary>
        /// The correct answer
        /// </summary>
        public string CorrectAnswer { get; set; } = string.Empty;

        /// <summary>
        /// Quiz category (e.g., "Vocabulary", "Grammar", "Reading")
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Difficulty level (e.g., "Easy", "Medium", "Hard")
        /// </summary>
        public string Difficulty { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for GET /api/quiz response
    /// </summary>
    public class GetQuizzesResponse
    {
        /// <summary>
        /// List of quizzes for the user
        /// </summary>
        public List<QuizDto> Quizzes { get; set; } = new List<QuizDto>();

        /// <summary>
        /// Whether the user has a target set
        /// </summary>
        public bool UserTargetSet { get; set; }
    }

    /// <summary>
    /// DTO for POST /api/admin/quiz/generate request
    /// </summary>
    public class GenerateQuizzesRequest
    {
        /// <summary>
        /// Number of quizzes to generate
        /// </summary>
        [Required]
        [Range(1, 100, ErrorMessage = "Count must be between 1 and 100")]
        public int Count { get; set; }

        /// <summary>
        /// Category of quizzes to generate
        /// </summary>
        [Required]
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Difficulty level for generated quizzes
        /// </summary>
        [Required]
        public string Difficulty { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for POST /api/admin/quiz/generate response
    /// </summary>
    public class GenerateQuizzesResponse
    {
        /// <summary>
        /// Number of quizzes that were generated
        /// </summary>
        public int Generated { get; set; }

        /// <summary>
        /// The list of generated quizzes
        /// </summary>
        public List<QuizDto> Quizzes { get; set; } = new List<QuizDto>();
    }
}
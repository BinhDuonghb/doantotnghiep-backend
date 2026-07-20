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

    // Target DTOs moved to src/Models/DTOs/TargetDTOs.cs

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

    // ==================== AI Quiz Generation DTOs ====================

    /// <summary>
    /// DTO for POST /api/quiz/generate-from-target request
    /// </summary>
    public class GenerateQuizzesFromTargetRequest
    {
        /// <summary>
        /// Optional target ID. If not provided, uses the user's active target.
        /// </summary>
        public string? TargetId { get; set; }

        /// <summary>
        /// Number of quizzes to generate (default: 5, max: 20)
        /// </summary>
        [Range(1, 20, ErrorMessage = "Count must be between 1 and 20")]
        public int Count { get; set; } = 5;

        /// <summary>
        /// Optional category override. If not provided, uses target's category.
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// Difficulty level (optional, default: Medium)
        /// </summary>
        public string Difficulty { get; set; } = "Medium";
    }

    /// <summary>
    /// Summary of the target used for AI quiz generation
    /// </summary>
    public class TargetUsedInfo
    {
        /// <summary>
        /// Target unique identifier
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Target title
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Target description (what the target is about)
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Current status of the target
        /// </summary>
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for POST /api/quiz/generate-from-target response
    /// </summary>
    public class GenerateQuizzesFromTargetResponse
    {
        /// <summary>
        /// Number of quizzes that were generated
        /// </summary>
        public int Generated { get; set; }

        /// <summary>
        /// The list of AI-generated quizzes
        /// </summary>
        public List<QuizDto> Quizzes { get; set; } = new List<QuizDto>();

        /// <summary>
        /// Information about the target used for generation
        /// </summary>
        public TargetUsedInfo TargetUsed { get; set; } = new();
    }

    /// <summary>
    /// DTO for GET /api/quiz/generation-status/{generationId} response
    /// </summary>
    public class GenerationStatusResponse
    {
        /// <summary>
        /// Generation job unique identifier
        /// </summary>
        public string GenerationId { get; set; } = string.Empty;

        /// <summary>
        /// Status of the generation job (pending, completed, failed)
        /// </summary>
        public string Status { get; set; } = "pending";

        /// <summary>
        /// Number of quizzes generated so far
        /// </summary>
        public int GeneratedCount { get; set; }

        /// <summary>
        /// Number of quizzes requested
        /// </summary>
        public int RequestedCount { get; set; }

        /// <summary>
        /// Error message if the generation failed
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// Timestamp when the generation was created (ISO 8601)
        /// </summary>
        public string CreatedAt { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the generation completed (ISO 8601, optional)
        /// </summary>
        public string? CompletedAt { get; set; }
    }

    // ==================== AI Plan Generation DTOs ====================

    /// <summary>
    /// DTO for POST /api/plan/generate request
    /// </summary>
    public class GeneratePlanRequest
    {
        /// <summary>
        /// The target ID to generate a plan for
        /// </summary>
        [Required]
        public string TargetId { get; set; } = string.Empty;

        /// <summary>
        /// Optional feedback for plan regeneration/refinement.
        /// If provided, the AI will refine the existing plan based on this feedback.
        /// </summary>
        [MaxLength(1000, ErrorMessage = "Feedback must be at most 1000 characters long")]
        public string? Feedback { get; set; }
    }

    /// <summary>
    /// DTO for POST /api/plan/generate response
    /// </summary>
    public class GeneratePlanResponse
    {
        /// <summary>
        /// The target ID the plan was generated for
        /// </summary>
        public string TargetId { get; set; } = string.Empty;

        /// <summary>
        /// The generated plan data
        /// </summary>
        public PlanDataDto Plan { get; set; } = new();

        /// <summary>
        /// Success message
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}
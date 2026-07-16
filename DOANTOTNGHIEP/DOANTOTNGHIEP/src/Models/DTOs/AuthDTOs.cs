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

    // ==================== Target DTOs ====================

    /// <summary>
    /// DTO for creating a new target
    /// </summary>
    public class CreateTargetRequest
    {
        /// <summary>
        /// Title of the target
        /// </summary>
        [Required]
        [MinLength(1, ErrorMessage = "Title must be at least 1 character long")]
        [MaxLength(100, ErrorMessage = "Title must be at most 100 characters long")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Description of the target
        /// </summary>
        [MaxLength(500, ErrorMessage = "Description must be at most 500 characters long")]
        public string? Description { get; set; }

        /// <summary>
        /// Type of the target
        /// </summary>
        [Required]
        [RegularExpression("^(questions|time|streak|accuracy)$", ErrorMessage = "Invalid target type")]
        public string TargetType { get; set; } = string.Empty;

        /// <summary>
        /// Target value to achieve
        /// </summary>
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Target value must be at least 1")]
        public int TargetValue { get; set; }

        /// <summary>
        /// Deadline for the target
        /// </summary>
        public DateTime? Deadline { get; set; }

        /// <summary>
        /// Fields that need to be completed to achieve the target
        /// </summary>
        public List<string>? FieldsToComplete { get; set; }
    }

    /// <summary>
    /// DTO for updating an existing target
    /// </summary>
    public class UpdateTargetRequest
    {
        /// <summary>
        /// Target unique identifier
        /// </summary>
        [Required]
        public string TargetId { get; set; } = string.Empty;

        /// <summary>
        /// Title of the target
        /// </summary>
        [MinLength(1, ErrorMessage = "Title must be at least 1 character long")]
        [MaxLength(100, ErrorMessage = "Title must be at most 100 characters long")]
        public string? Title { get; set; }

        /// <summary>
        /// Description of the target
        /// </summary>
        [MaxLength(500, ErrorMessage = "Description must be at most 500 characters long")]
        public string? Description { get; set; }

        /// <summary>
        /// Target value to achieve
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Target value must be at least 1")]
        public int? TargetValue { get; set; }

        /// <summary>
        /// Deadline for the target
        /// </summary>
        public DateTime? Deadline { get; set; }

        /// <summary>
        /// Fields that need to be completed to achieve the target
        /// </summary>
        public List<string>? FieldsToComplete { get; set; }

        /// <summary>
        /// Status of the target
        /// </summary>
        [RegularExpression("^(active|paused)$", ErrorMessage = "Invalid status")]
        public string? Status { get; set; }
    }

    /// <summary>
    /// DTO for target progress history item
    /// </summary>
    public class ProgressHistoryItem
    {
        /// <summary>
        /// Date of the progress update
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Value at the time of progress update
        /// </summary>
        public int Value { get; set; }

        /// <summary>
        /// Field that was completed
        /// </summary>
        public string? FieldCompleted { get; set; }
    }

    /// <summary>
    /// DTO for completed field history item
    /// </summary>
    public class CompletedFieldHistoryItem
    {
        /// <summary>
        /// Name of the field that was completed
        /// </summary>
        public string FieldName { get; set; } = string.Empty;

        /// <summary>
        /// Date when the field was completed
        /// </summary>
        public DateTime CompletedAt { get; set; }

        /// <summary>
        /// Value at the time of completion
        /// </summary>
        public int ValueAtCompletion { get; set; }
    }

    /// <summary>
    /// DTO for target response
    /// </summary>
    public class TargetResponse
    {
        /// <summary>
        /// Target unique identifier
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Title of the target
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Description of the target
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Type of the target
        /// </summary>
        public string TargetType { get; set; } = string.Empty;

        /// <summary>
        /// Target value to achieve
        /// </summary>
        public int TargetValue { get; set; }

        /// <summary>
        /// Current value of the target
        /// </summary>
        public int CurrentValue { get; set; }

        /// <summary>
        /// Deadline for the target
        /// </summary>
        public DateTime? Deadline { get; set; }

        /// <summary>
        /// Fields that need to be completed to achieve the target
        /// </summary>
        public List<string> FieldsToComplete { get; set; } = new();

        /// <summary>
        /// Status of the target
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Progress history of the target
        /// </summary>
        public List<ProgressHistoryItem> ProgressHistory { get; set; } = new();

        /// <summary>
        /// Creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Last update timestamp
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// DTO for updating target progress
    /// </summary>
    public class TargetProgressRequest
    {
        /// <summary>
        /// Target unique identifier
        /// </summary>
        [Required]
        public string TargetId { get; set; } = string.Empty;

        /// <summary>
        /// Field that was completed
        /// </summary>
        [Required]
        [RegularExpression("^(daily_questions|weekly_time|streak_days|accuracy_rate)$", ErrorMessage = "Invalid field completed")]
        public string FieldCompleted { get; set; } = string.Empty;

        /// <summary>
        /// Value to increment the target by
        /// </summary>
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Increment value must be at least 1")]
        public int IncrementValue { get; set; }

        /// <summary>
        /// Optional notes about the progress update
        /// </summary>
        [MaxLength(200, ErrorMessage = "Notes must be at most 200 characters long")]
        public string? Notes { get; set; }
    }

    /// <summary>
    /// DTO for completing a target field
    /// </summary>
    public class CompleteFieldRequest
    {
        /// <summary>
        /// Target unique identifier
        /// </summary>
        [Required]
        public string TargetId { get; set; } = string.Empty;

        /// <summary>
        /// Name of the field to complete
        /// </summary>
        [Required]
        [RegularExpression("^(daily_questions|weekly_time|streak_days|accuracy_rate)$", ErrorMessage = "Invalid field name")]
        public string FieldName { get; set; } = string.Empty;

        /// <summary>
        /// Value at completion
        /// </summary>
        [Required]
        public int CompletedValue { get; set; }
    }

    /// <summary>
    /// DTO for target statistics response
    /// </summary>
    public class TargetStatsResponse
    {
        /// <summary>
        /// Total number of targets
        /// </summary>
        public int TotalTargets { get; set; }

        /// <summary>
        /// Number of active targets
        /// </summary>
        public int ActiveTargets { get; set; }

        /// <summary>
        /// Number of completed targets
        /// </summary>
        public int CompletedTargets { get; set; }

        /// <summary>
        /// Number of expired targets
        /// </summary>
        public int ExpiredTargets { get; set; }

        /// <summary>
        /// Overall progress percentage
        /// </summary>
        public double OverallProgress { get; set; }

        /// <summary>
        /// Current streak
        /// </summary>
        public int CurrentStreak { get; set; }

        /// <summary>
        /// Total questions answered
        /// </summary>
        public int TotalQuestionsAnswered { get; set; }

        /// <summary>
        /// Total time spent (in minutes)
        /// </summary>
        public int TotalTimeSpent { get; set; }
    }

    // ==================== Target Response DTOs ====================

    /// <summary>
    /// DTO for target list item in GetTargets response
    /// </summary>
    public class TargetListItem
    {
        /// <summary>
        /// Target unique identifier
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Title of the target
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Description of the target
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Type of the target
        /// </summary>
        public string TargetType { get; set; } = string.Empty;

        /// <summary>
        /// Target value to achieve
        /// </summary>
        public int TargetValue { get; set; }

        /// <summary>
        /// Current value of the target
        /// </summary>
        public int CurrentValue { get; set; }

        /// <summary>
        /// Deadline for the target
        /// </summary>
        public DateTime? Deadline { get; set; }

        /// <summary>
        /// Fields that need to be completed to achieve the target
        /// </summary>
        public List<string> FieldsToComplete { get; set; } = new();

        /// <summary>
        /// Status of the target
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Progress percentage
        /// </summary>
        public double ProgressPercentage { get; set; }

        /// <summary>
        /// Days remaining until deadline
        /// </summary>
        public int DaysRemaining { get; set; }

        /// <summary>
        /// Creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Last update timestamp
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// DTO for getting targets response
    /// </summary>
    public class GetTargetsResponse
    {
        /// <summary>
        /// List of targets
        /// </summary>
        public List<TargetListItem> Targets { get; set; } = new();

        /// <summary>
        /// Total number of targets
        /// </summary>
        public int Total { get; set; }
    }

    /// <summary>
    /// DTO for detailed target response
    /// </summary>
    public class TargetDetailResponse
    {
        /// <summary>
        /// Target unique identifier
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Title of the target
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Description of the target
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Type of the target
        /// </summary>
        public string TargetType { get; set; } = string.Empty;

        /// <summary>
        /// Target value to achieve
        /// </summary>
        public int TargetValue { get; set; }

        /// <summary>
        /// Current value of the target
        /// </summary>
        public int CurrentValue { get; set; }

        /// <summary>
        /// Deadline for the target
        /// </summary>
        public DateTime? Deadline { get; set; }

        /// <summary>
        /// Fields that need to be completed to achieve the target
        /// </summary>
        public List<string> FieldsToComplete { get; set; } = new();

        /// <summary>
        /// Status of the target
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Progress percentage
        /// </summary>
        public double ProgressPercentage { get; set; }

        /// <summary>
        /// Days remaining until deadline
        /// </summary>
        public int DaysRemaining { get; set; }

        /// <summary>
        /// Progress history of the target
        /// </summary>
        public List<ProgressHistoryItem> ProgressHistory { get; set; } = new();

        /// <summary>
        /// History of completed fields
        /// </summary>
        public List<CompletedFieldHistoryItem> CompletedFieldsHistory { get; set; } = new();

        /// <summary>
        /// Creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Last update timestamp
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// DTO for target progress update response
    /// </summary>
    public class TargetProgressResponse
    {
        /// <summary>
        /// Target unique identifier
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Current value of the target
        /// </summary>
        public int CurrentValue { get; set; }

        /// <summary>
        /// Progress percentage
        /// </summary>
        public double ProgressPercentage { get; set; }

        /// <summary>
        /// Status of the target
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// List of completed fields
        /// </summary>
        public List<string> CompletedFields { get; set; } = new();

        /// <summary>
        /// Last update timestamp
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// DTO for complete field response
    /// </summary>
    public class CompleteFieldResponse
    {
        /// <summary>
        /// Target unique identifier
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// List of completed fields
        /// </summary>
        public List<string> CompletedFields { get; set; } = new();

        /// <summary>
        /// Timestamp when the field was completed
        /// </summary>
        public DateTime FieldCompletedAt { get; set; }

        /// <summary>
        /// Whether all fields are completed
        /// </summary>
        public bool AllFieldsCompleted { get; set; }

        /// <summary>
        /// Current target status
        /// </summary>
        public string TargetStatus { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for delete target response
    /// </summary>
    public class DeleteTargetResponse
    {
        /// <summary>
        /// Success message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Deletion timestamp
        /// </summary>
        public DateTime DeletedAt { get; set; }
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
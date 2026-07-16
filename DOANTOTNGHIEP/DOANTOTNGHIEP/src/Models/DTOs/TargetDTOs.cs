using System.ComponentModel.DataAnnotations;

namespace DOANTOTNGHIEP.src.Models.DTOs
{
    // ==================== Target Request DTOs ====================

    /// <summary>
    /// DTO for creating a new target
    /// </summary>
    public class CreateTargetRequestDto
    {
        /// <summary>
        /// Title of the target
        /// </summary>
        [Required]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Type of the target
        /// </summary>
        [Required]
        public string TargetType { get; set; } = string.Empty;

        /// <summary>
        /// Target value to achieve
        /// </summary>
        [Required]
        public string TargetValue { get; set; } = string.Empty;

        /// <summary>
        /// Unit of the target
        /// </summary>
        [Required]
        public string TargetUnit { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for updating an existing target
    /// </summary>
    public class UpdateTargetRequestDto
    {
        /// <summary>
        /// Title of the target
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Target value to achieve
        /// </summary>
        public string? TargetValue { get; set; }

        /// <summary>
        /// Unit of the target
        /// </summary>
        public string? TargetUnit { get; set; }

        /// <summary>
        /// Current progress value
        /// </summary>
        public int? CurrentProgress { get; set; }
    }

    /// <summary>
    /// DTO for covering a target
    /// </summary>
    public class CoverTargetRequestDto
    {
        /// <summary>
        /// Timestamp when the target was covered
        /// </summary>
        [Required]
        public string CoveredAt { get; set; } = string.Empty;
    }

    // ==================== Target Response DTOs ====================

    /// <summary>
    /// DTO for target progress history item
    /// </summary>
    public class ProgressHistoryItemDto
    {
        /// <summary>
        /// Value at the time of update
        /// </summary>
        public int Value { get; set; }

        /// <summary>
        /// Timestamp when the progress was recorded
        /// </summary>
        public string RecordedAt { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for target response (used in multiple endpoints)
    /// </summary>
    public class TargetResponseDto
    {
        /// <summary>
        /// Unique identifier for the target
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// User identifier who owns this target
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Title of the target
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Type of the target
        /// </summary>
        public string TargetType { get; set; } = string.Empty;

        /// <summary>
        /// Target value to achieve
        /// </summary>
        public string TargetValue { get; set; } = string.Empty;

        /// <summary>
        /// Current progress value achieved
        /// </summary>
        public int CurrentProgress { get; set; }

        /// <summary>
        /// Unit of the target
        /// </summary>
        public string TargetUnit { get; set; } = string.Empty;

        /// <summary>
        /// Whether the target has been covered/completed
        /// </summary>
        public bool IsCovered { get; set; }

        /// <summary>
        /// Timestamp when the target was covered/completed
        /// </summary>
        public string? CoveredAt { get; set; }

        /// <summary>
        /// Timestamp when the target was created
        /// </summary>
        public string CreatedAt { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the target was last updated
        /// </summary>
        public string UpdatedAt { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for detailed target response
    /// </summary>
    public class TargetDetailResponseDto : TargetResponseDto
    {
        /// <summary>
        /// History of progress updates
        /// </summary>
        public List<ProgressHistoryItemDto> ProgressHistory { get; set; } = new List<ProgressHistoryItemDto>();
    }

    /// <summary>
    /// DTO for getting user targets response
    /// </summary>
    public class GetUserTargetsResponseDto
    {
        /// <summary>
        /// List of targets
        /// </summary>
        public List<TargetResponseDto> Targets { get; set; } = new List<TargetResponseDto>();

        /// <summary>
        /// Total count of targets
        /// </summary>
        public int TotalCount { get; set; }
    }

    /// <summary>
    /// DTO for delete target response
    /// </summary>
    public class DeleteTargetResponseDto
    {
        /// <summary>
        /// Success indicator
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Response message
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}
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
        [MinLength(1, ErrorMessage = "Title must be at least 1 character long")]
        [MaxLength(100, ErrorMessage = "Title must be at most 100 characters long")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Description of what the target is about
        /// </summary>
        [MaxLength(500, ErrorMessage = "Description must be at most 500 characters long")]
        public string? Description { get; set; }

        /// <summary>
        /// Duration value for completing the target (e.g., 3)
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Duration must be at least 1")]
        public int? Duration { get; set; }

        /// <summary>
        /// Unit of the duration. Must be one of: hour, day, week, month.
        /// </summary>
        [RegularExpression("^(hour|day|week|month)$", ErrorMessage = "DurationUnit must be one of: hour, day, week, month")]
        public string? DurationUnit { get; set; }
    }

    /// <summary>
    /// DTO for updating an existing target
    /// </summary>
    public class UpdateTargetRequestDto
    {
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
        /// Duration value for completing the target
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Duration must be at least 1")]
        public int? Duration { get; set; }

        /// <summary>
        /// Unit of the duration
        /// </summary>
        [RegularExpression("^(hour|day|week|month)$", ErrorMessage = "DurationUnit must be one of: hour, day, week, month")]
        public string? DurationUnit { get; set; }

        /// <summary>
        /// Status of the target. Must be one of: not_started, in_progress, completed.
        /// Setting to in_progress auto-sets StartedAt if not already set.
        /// Setting to completed auto-sets CompletedAt.
        /// </summary>
        [RegularExpression("^(not_started|in_progress|completed)$", ErrorMessage = "Status must be one of: not_started, in_progress, completed")]
        public string? Status { get; set; }
    }

    /// <summary>
    /// DTO for updating a checklist item's completion status
    /// PATCH /api/targets/{id}/milestones/{order}/checklist/{index}
    /// </summary>
    public class UpdateChecklistItemRequest
    {
        /// <summary>
        /// Whether the checklist item is completed
        /// </summary>
        [Required]
        public bool IsCompleted { get; set; }
    }

    // ==================== Plan DTOs ====================

    /// <summary>
    /// DTO for plan summary in responses
    /// </summary>
    public class PlanDataDto
    {
        /// <summary>
        /// Overall summary of the plan
        /// </summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// When the plan was generated (ISO 8601)
        /// </summary>
        public string GeneratedAt { get; set; } = string.Empty;

        /// <summary>
        /// Milestones in the plan
        /// </summary>
        public List<MilestoneItemDto> Milestones { get; set; } = new();

        /// <summary>
        /// Quizzes associated with milestones
        /// </summary>
        public List<PlanQuizItemDto> Quizzes { get; set; } = new();
    }

    /// <summary>
    /// DTO for a milestone item
    /// </summary>
    public class MilestoneItemDto
    {
        /// <summary>
        /// Title of the milestone
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Description of what this milestone entails
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Order/sequence (1-based)
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Estimated duration (e.g., "1 week", "3 days")
        /// </summary>
        public string DurationEstimate { get; set; } = string.Empty;

        /// <summary>
        /// Checklist tasks within this milestone
        /// </summary>
        public List<MilestoneChecklistItemDto> ChecklistItems { get; set; } = new();
    }

    /// <summary>
    /// DTO for a milestone checklist item
    /// </summary>
    public class MilestoneChecklistItemDto
    {
        /// <summary>
        /// Title/description of the task
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Whether this task has been completed
        /// </summary>
        public bool IsCompleted { get; set; }
    }

    /// <summary>
    /// DTO for a quiz item embedded in a plan
    /// </summary>
    public class PlanQuizItemDto
    {
        /// <summary>
        /// The quiz question text
        /// </summary>
        public string Question { get; set; } = string.Empty;

        /// <summary>
        /// List of answer options
        /// </summary>
        public List<string> Options { get; set; } = new();

        /// <summary>
        /// The correct answer
        /// </summary>
        public string CorrectAnswer { get; set; } = string.Empty;

        /// <summary>
        /// The milestone title this quiz is associated with
        /// </summary>
        public string MilestoneTitle { get; set; } = string.Empty;
    }

    // ==================== Target Response DTOs ====================

    /// <summary>
    /// DTO for target response (used in list and basic responses)
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
        /// Description of the target
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Duration value for completing the target
        /// </summary>
        public int Duration { get; set; }

        /// <summary>
        /// Unit of the duration
        /// </summary>
        public string DurationUnit { get; set; } = string.Empty;

        /// <summary>
        /// Current status
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// When the user started working on the target
        /// </summary>
        public string? StartedAt { get; set; }

        /// <summary>
        /// When the target was completed
        /// </summary>
        public string? CompletedAt { get; set; }

        /// <summary>
        /// Whether an AI-generated plan exists for this target
        /// </summary>
        public bool HasPlan { get; set; }

        /// <summary>
        /// Creation timestamp
        /// </summary>
        public string CreatedAt { get; set; } = string.Empty;

        /// <summary>
        /// Last update timestamp
        /// </summary>
        public string UpdatedAt { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for detailed target response (includes plan + stats)
    /// </summary>
    public class TargetDetailResponseDto : TargetResponseDto
    {
        /// <summary>
        /// The AI-generated plan, if any
        /// </summary>
        public PlanDataDto? Plan { get; set; }

        /// <summary>
        /// Total number of milestones in the plan
        /// </summary>
        public int TotalMilestones { get; set; }

        /// <summary>
        /// Number of completed checklist items across all milestones
        /// </summary>
        public int CompletedChecklistItems { get; set; }

        /// <summary>
        /// Total number of checklist items across all milestones
        /// </summary>
        public int TotalChecklistItems { get; set; }
    }

    /// <summary>
    /// DTO for getting user targets response
    /// </summary>
    public class GetUserTargetsResponseDto
    {
        /// <summary>
        /// List of targets
        /// </summary>
        public List<TargetResponseDto> Targets { get; set; } = new();

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
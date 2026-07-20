using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace DOANTOTNGHIEP.src.Models
{
    /// <summary>
    /// Represents a user target/goal with AI-generated plan support.
    /// Simplified model: users define a goal with a timeframe,
    /// and AI generates a structured plan to guide them.
    /// </summary>
    [BsonIgnoreExtraElements]
    public class Target
    {
        /// <summary>
        /// Unique identifier for the target
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// User identifier who owns this target
        /// </summary>
        [BsonElement("user_id")]
        [Required]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Title of the target/goal
        /// </summary>
        [BsonElement("title")]
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Description of what the target is about
        /// </summary>
        [BsonElement("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Duration value for completing the target (e.g., 3)
        /// </summary>
        [BsonElement("duration")]
        public int Duration { get; set; }

        /// <summary>
        /// Unit of the duration (hour, day, week, month)
        /// </summary>
        [BsonElement("duration_unit")]
        public string DurationUnit { get; set; } = "day";

        /// <summary>
        /// Current status of the target
        /// Possible values: not_started, in_progress, completed
        /// </summary>
        [BsonElement("status")]
        public string Status { get; set; } = "not_started";

        /// <summary>
        /// When the user started working on this target
        /// </summary>
        [BsonElement("started_at")]
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// When the target was completed
        /// </summary>
        [BsonElement("completed_at")]
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// AI-generated plan embedded in the target.
        /// Contains milestones, checklist items, and quizzes.
        /// Null if no plan has been generated yet.
        /// </summary>
        [BsonElement("plan")]
        public PlanData? Plan { get; set; }

        /// <summary>
        /// Timestamp when the target was created
        /// </summary>
        [BsonElement("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the target was last updated
        /// </summary>
        [BsonElement("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// AI-generated plan data embedded in a Target.
    /// Contains a summary, milestones with checklists, and quizzes tied to milestones.
    /// </summary>
    public class PlanData
    {
        /// <summary>
        /// Overall summary of the plan
        /// </summary>
        [BsonElement("summary")]
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// When the plan was generated
        /// </summary>
        [BsonElement("generated_at")]
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Ordered list of milestones in the plan
        /// </summary>
        [BsonElement("milestones")]
        public List<MilestoneItem> Milestones { get; set; } = new();

        /// <summary>
        /// Quizzes associated with specific milestones
        /// </summary>
        [BsonElement("quizzes")]
        public List<PlanQuizItem> Quizzes { get; set; } = new();
    }

    /// <summary>
    /// A single milestone within a plan, containing its own checklist items.
    /// </summary>
    public class MilestoneItem
    {
        /// <summary>
        /// Title of the milestone (e.g., "Learn Basic Setup")
        /// </summary>
        [BsonElement("title")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Description of what this milestone entails
        /// </summary>
        [BsonElement("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Order/sequence of this milestone (1-based)
        /// </summary>
        [BsonElement("order")]
        public int Order { get; set; }

        /// <summary>
        /// Estimated duration to complete this milestone
        /// (e.g., "1 week", "3 days")
        /// </summary>
        [BsonElement("duration_estimate")]
        public string DurationEstimate { get; set; } = string.Empty;

        /// <summary>
        /// Checklist of tasks to complete within this milestone
        /// </summary>
        [BsonElement("checklist_items")]
        public List<MilestoneChecklistItem> ChecklistItems { get; set; } = new();
    }

    /// <summary>
    /// A single checklist item within a milestone.
    /// </summary>
    public class MilestoneChecklistItem
    {
        /// <summary>
        /// Title/description of the task
        /// </summary>
        [BsonElement("title")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Whether this task has been completed
        /// </summary>
        [BsonElement("is_completed")]
        public bool IsCompleted { get; set; }
    }

    /// <summary>
    /// A quiz question embedded within a plan, tied to a specific milestone.
    /// </summary>
    public class PlanQuizItem
    {
        /// <summary>
        /// The quiz question text
        /// </summary>
        [BsonElement("question")]
        public string Question { get; set; } = string.Empty;

        /// <summary>
        /// List of answer options
        /// </summary>
        [BsonElement("options")]
        public List<string> Options { get; set; } = new();

        /// <summary>
        /// The correct answer (must match one of the options)
        /// </summary>
        [BsonElement("correct_answer")]
        public string CorrectAnswer { get; set; } = string.Empty;

        /// <summary>
        /// The milestone title this quiz question is associated with
        /// </summary>
        [BsonElement("milestone_title")]
        public string MilestoneTitle { get; set; } = string.Empty;
    }
}
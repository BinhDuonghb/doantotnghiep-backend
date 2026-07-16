using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace DOANTOTNGHIEP.src.Models
{
    /// <summary>
    /// Represents a user target with progress tracking
    /// </summary>
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
        /// Title of the target
        /// </summary>
        [BsonElement("title")]
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Type of the target
        /// </summary>
        [BsonElement("target_type")]
        [Required]
        public string TargetType { get; set; } = string.Empty;

        /// <summary>
        /// Target value to achieve
        /// </summary>
        [BsonElement("target_value")]
        [Required]
        public string TargetValue { get; set; } = string.Empty;

        /// <summary>
        /// Current progress value achieved
        /// </summary>
        [BsonElement("current_progress")]
        public int CurrentProgress { get; set; } = 0;

        /// <summary>
        /// Unit of the target (e.g., "questions", "minutes", "days")
        /// </summary>
        [BsonElement("target_unit")]
        public string TargetUnit { get; set; } = string.Empty;

        /// <summary>
        /// Whether the target has been covered/completed
        /// </summary>
        [BsonElement("is_covered")]
        public bool IsCovered { get; set; } = false;

        /// <summary>
        /// Timestamp when the target was covered/completed
        /// </summary>
        [BsonElement("covered_at")]
        public DateTime? CoveredAt { get; set; }

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

        /// <summary>
        /// History of progress updates
        /// </summary>
        [BsonElement("progress_history")]
        public List<ProgressHistoryItem> ProgressHistory { get; set; } = new List<ProgressHistoryItem>();
    }

    /// <summary>
    /// Represents a progress history item
    /// </summary>
    public class ProgressHistoryItem
    {
        /// <summary>
        /// Value at the time of update
        /// </summary>
        [BsonElement("value")]
        public int Value { get; set; }

        /// <summary>
        /// Timestamp when the progress was recorded
        /// </summary>
        [BsonElement("recorded_at")]
        public DateTime RecordedAt { get; set; }
    }
}
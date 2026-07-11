using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DOANTOTNGHIEP.src.Models
{
    /// <summary>
    /// Quiz entity for MongoDB storage
    /// </summary>
    public class Quiz
    {
        /// <summary>
        /// Unique identifier for the quiz
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The quiz question text
        /// </summary>
        [BsonElement("question")]
        public string Question { get; set; } = string.Empty;

        /// <summary>
        /// JSON array of answer options
        /// </summary>
        [BsonElement("options")]
        public List<string> Options { get; set; } = new List<string>();

        /// <summary>
        /// The correct answer
        /// </summary>
        [BsonElement("correct_answer")]
        public string CorrectAnswer { get; set; } = string.Empty;

        /// <summary>
        /// Category of the quiz (e.g., "Vocabulary", "Grammar", "Reading")
        /// </summary>
        [BsonElement("category")]
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Difficulty level (e.g., "Easy", "Medium", "Hard")
        /// </summary>
        [BsonElement("difficulty")]
        public string Difficulty { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the quiz was created
        /// </summary>
        [BsonElement("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
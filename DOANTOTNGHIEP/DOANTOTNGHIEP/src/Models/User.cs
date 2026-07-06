using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace DOANTOTNGHIEP.src.Models
{
    /// <summary>
    /// User entity for MongoDB storage
    /// </summary>
    public class User
    {
        /// <summary>
        /// Unique identifier for the user
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// User's email address (unique)
        /// </summary>
        [BsonElement("email")]
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Hashed password
        /// </summary>
        [BsonElement("password")]
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// User's full name
        /// </summary>
        [BsonElement("full_name")]
        [Required]
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// Refresh token for the user
        /// </summary>
        [BsonElement("refresh_token")]
        public string? RefreshToken { get; set; }

        /// <summary>
        /// Expiration date for the refresh token
        /// </summary>
        [BsonElement("refresh_token_expiry")]
        public DateTime? RefreshTokenExpiry { get; set; }

        /// <summary>
        /// Timestamp when the user was created
        /// </summary>
        [BsonElement("created_at")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Timestamp when the user was last updated
        /// </summary>
        [BsonElement("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
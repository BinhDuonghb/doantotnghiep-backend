using DOANTOTNGHIEP.src.Models;
using MongoDB.Driver;

namespace DOANTOTNGHIEP.src.Data
{
    /// <summary>
    /// Repository for user-related database operations
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Finds a user by email
        /// </summary>
        /// <param name="email">User's email</param>
        /// <returns>The user if found, null otherwise</returns>
        Task<User?> FindByEmailAsync(string email);

        /// <summary>
        /// Creates a new user
        /// </summary>
        /// <param name="user">User to create</param>
        /// <returns>The created user</returns>
        Task<User> CreateAsync(User user);

        /// <summary>
        /// Stores a refresh token for a user
        /// </summary>
        /// <param name="userId">User's ID</param>
        /// <param name="refreshToken">Refresh token</param>
        /// <param name="expiryDate">Refresh token expiry date</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> StoreRefreshTokenAsync(string userId, string refreshToken, DateTime expiryDate);

        /// <summary>
        /// Revokes a refresh token for a user
        /// </summary>
        /// <param name="userId">User's ID</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> RevokeRefreshTokenAsync(string userId);

        /// <summary>
        /// Finds a user by ID
        /// </summary>
        /// <param name="userId">User's ID</param>
        /// <returns>The user if found, null otherwise</returns>
        Task<User?> FindByIdAsync(string userId);

        /// <summary>
        /// Validates a refresh token
        /// </summary>
        /// <param name="userId">User's ID</param>
        /// <param name="refreshToken">Refresh token to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken);

        /// <summary>
        /// Finds a user by their refresh token value (non-expired)
        /// </summary>
        /// <param name="refreshToken">Refresh token value</param>
        /// <returns>The user if found, null otherwise</returns>
        Task<User?> FindByRefreshTokenAsync(string refreshToken);

        /// <summary>
        /// Gets a user's target information
        /// </summary>
        /// <param name="userId">User's ID</param>
        /// <returns>A tuple with target, targetType, and isSet; null if user not found</returns>
        Task<(string? target, string? targetType, bool isSet)?> GetTargetAsync(string userId);

        /// <summary>
        /// Updates a user's target and target type
        /// </summary>
        /// <param name="userId">User's ID</param>
        /// <param name="target">New target value</param>
        /// <param name="targetType">New target type</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> UpdateTargetAsync(string userId, string target, string targetType);
    }

    /// <summary>
    /// Implementation of IUserRepository
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly IMongoDbService _mongoDbService;

        /// <summary>
        /// Initializes a new instance of the UserRepository class
        /// </summary>
        /// <param name="mongoDbService">MongoDB service</param>
        public UserRepository(IMongoDbService mongoDbService)
        {
            _mongoDbService = mongoDbService;
        }

        /// <summary>
        /// Finds a user by email
        /// </summary>
        /// <param name="email">User's email</param>
        /// <returns>The user if found, null otherwise</returns>
        public async Task<User?> FindByEmailAsync(string email)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Email, email);
            var user = await _mongoDbService.Users.Find(filter).FirstOrDefaultAsync();
            return user;
        }

        /// <summary>
        /// Creates a new user
        /// </summary>
        /// <param name="user">User to create</param>
        /// <returns>The created user</returns>
        public async Task<User> CreateAsync(User user)
        {
            await _mongoDbService.Users.InsertOneAsync(user);
            return user;
        }

        /// <summary>
        /// Stores a refresh token for a user
        /// </summary>
        /// <param name="userId">User's ID</param>
        /// <param name="refreshToken">Refresh token</param>
        /// <param name="expiryDate">Refresh token expiry date</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> StoreRefreshTokenAsync(string userId, string refreshToken, DateTime expiryDate)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update
                .Set(u => u.RefreshToken, refreshToken)
                .Set(u => u.RefreshTokenExpiry, expiryDate)
                .Set(u => u.UpdatedAt, DateTime.UtcNow);

            var result = await _mongoDbService.Users.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        /// <summary>
        /// Revokes a refresh token for a user
        /// </summary>
        /// <param name="userId">User's ID</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> RevokeRefreshTokenAsync(string userId)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update
                .Set(u => u.RefreshToken, null)
                .Set(u => u.RefreshTokenExpiry, null)
                .Set(u => u.UpdatedAt, DateTime.UtcNow);

            var result = await _mongoDbService.Users.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        /// <summary>
        /// Finds a user by ID
        /// </summary>
        /// <param name="userId">User's ID</param>
        /// <returns>The user if found, null otherwise</returns>
        public async Task<User?> FindByIdAsync(string userId)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var user = await _mongoDbService.Users.Find(filter).FirstOrDefaultAsync();
            return user;
        }

        /// <summary>
        /// Validates a refresh token
        /// </summary>
        /// <param name="userId">User's ID</param>
        /// <param name="refreshToken">Refresh token to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public async Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken)
        {
            var filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(u => u.Id, userId),
                Builders<User>.Filter.Eq(u => u.RefreshToken, refreshToken),
                Builders<User>.Filter.Gt(u => u.RefreshTokenExpiry, DateTime.UtcNow)
            );

            var user = await _mongoDbService.Users.Find(filter).FirstOrDefaultAsync();
            return user != null;
        }

        /// <summary>
        /// Finds a user by their refresh token value (non-expired)
        /// </summary>
        /// <param name="refreshToken">Refresh token value</param>
        /// <returns>The user if found, null otherwise</returns>
        public async Task<User?> FindByRefreshTokenAsync(string refreshToken)
        {
            var filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(u => u.RefreshToken, refreshToken),
                Builders<User>.Filter.Gt(u => u.RefreshTokenExpiry, DateTime.UtcNow)
            );

            return await _mongoDbService.Users.Find(filter).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Gets a user's target information
        /// </summary>
        /// <param name="userId">User's ID</param>
        /// <returns>A tuple with target, targetType, and isSet; null if user not found</returns>
        public async Task<(string? target, string? targetType, bool isSet)?> GetTargetAsync(string userId)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var projection = Builders<User>.Projection
                .Include(u => u.Target)
                .Include(u => u.TargetType)
                .Include(u => u.IsSet);
            var result = await _mongoDbService.Users.Find(filter).Project<User>(projection).FirstOrDefaultAsync();
            if (result == null) return null;
            return (result.Target, result.TargetType, result.IsSet);
        }

        /// <summary>
        /// Updates a user's target and target type
        /// </summary>
        /// <param name="userId">User's ID</param>
        /// <param name="target">New target value</param>
        /// <param name="targetType">New target type</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> UpdateTargetAsync(string userId, string target, string targetType)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update
                .Set(u => u.Target, target)
                .Set(u => u.TargetType, targetType)
                .Set(u => u.IsSet, true)
                .Set(u => u.UpdatedAt, DateTime.UtcNow);

            var result = await _mongoDbService.Users.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }
    }
}
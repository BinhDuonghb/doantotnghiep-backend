using DOANTOTNGHIEP.src.Models;
using MongoDB.Driver;

namespace DOANTOTNGHIEP.src.Data
{
    /// <summary>
    /// Repository for target-related database operations
    /// </summary>
    public interface ITargetRepository
    {
        /// <summary>
        /// Gets targets by user ID
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>List of targets</returns>
        Task<List<Target>> GetTargetsByUserIdAsync(string userId);

        /// <summary>
        /// Gets a target by ID
        /// </summary>
        /// <param name="targetId">Target ID</param>
        /// <returns>The target if found, null otherwise</returns>
        Task<Target?> GetTargetByIdAsync(string targetId);

        /// <summary>
        /// Creates a new target
        /// </summary>
        /// <param name="target">Target to create</param>
        /// <returns>The created target</returns>
        Task<Target> CreateTargetAsync(Target target);

        /// <summary>
        /// Updates a target
        /// </summary>
        /// <param name="target">Target to update</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> UpdateTargetAsync(Target target);

        /// <summary>
        /// Deletes a target
        /// </summary>
        /// <param name="targetId">Target ID</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> DeleteTargetAsync(string targetId);

        /// <summary>
        /// Marks a target as covered
        /// </summary>
        /// <param name="targetId">Target ID</param>
        /// <param name="coveredAt">Timestamp when the target was covered</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> CoverTargetAsync(string targetId, DateTime coveredAt);
    }

    /// <summary>
    /// Implementation of ITargetRepository
    /// </summary>
    public class TargetRepository : ITargetRepository
    {
        private readonly IMongoDbService _mongoDbService;

        /// <summary>
        /// Initializes a new instance of the TargetRepository class
        /// </summary>
        /// <param name="mongoDbService">MongoDB service</param>
        public TargetRepository(IMongoDbService mongoDbService)
        {
            _mongoDbService = mongoDbService;
        }

        /// <summary>
        /// Gets targets by user ID
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>List of targets</returns>
        public async Task<List<Target>> GetTargetsByUserIdAsync(string userId)
        {
            var filter = Builders<Target>.Filter.Eq(t => t.UserId, userId);
            return await _mongoDbService.Targets.Find(filter).ToListAsync();
        }

        /// <summary>
        /// Gets a target by ID
        /// </summary>
        /// <param name="targetId">Target ID</param>
        /// <returns>The target if found, null otherwise</returns>
        public async Task<Target?> GetTargetByIdAsync(string targetId)
        {
            var filter = Builders<Target>.Filter.Eq(t => t.Id, targetId);
            return await _mongoDbService.Targets.Find(filter).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Creates a new target
        /// </summary>
        /// <param name="target">Target to create</param>
        /// <returns>The created target</returns>
        public async Task<Target> CreateTargetAsync(Target target)
        {
            await _mongoDbService.Targets.InsertOneAsync(target);
            return target;
        }

        /// <summary>
        /// Updates a target
        /// </summary>
        /// <param name="target">Target to update</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> UpdateTargetAsync(Target target)
        {
            target.UpdatedAt = DateTime.UtcNow;
            var filter = Builders<Target>.Filter.Eq(t => t.Id, target.Id);
            var result = await _mongoDbService.Targets.ReplaceOneAsync(filter, target);
            return result.ModifiedCount > 0;
        }

        /// <summary>
        /// Deletes a target
        /// </summary>
        /// <param name="targetId">Target ID</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> DeleteTargetAsync(string targetId)
        {
            var filter = Builders<Target>.Filter.Eq(t => t.Id, targetId);
            var result = await _mongoDbService.Targets.DeleteOneAsync(filter);
            return result.DeletedCount > 0;
        }

        /// <summary>
        /// Marks a target as covered
        /// </summary>
        /// <param name="targetId">Target ID</param>
        /// <param name="coveredAt">Timestamp when the target was covered</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> CoverTargetAsync(string targetId, DateTime coveredAt)
        {
            var filter = Builders<Target>.Filter.Eq(t => t.Id, targetId);
            var update = Builders<Target>.Update
                .Set(t => t.IsCovered, true)
                .Set(t => t.CoveredAt, coveredAt)
                .Set(t => t.UpdatedAt, DateTime.UtcNow);

            var result = await _mongoDbService.Targets.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }
    }
}
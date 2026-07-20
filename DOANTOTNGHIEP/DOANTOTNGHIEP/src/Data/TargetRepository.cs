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
        /// Updates a target (full document replace)
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
        /// Sets or updates the AI-generated plan on a target (partial update).
        /// Only modifies the plan and updated_at fields.
        /// </summary>
        /// <param name="targetId">Target ID</param>
        /// <param name="plan">The plan data to store</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> SetPlanAsync(string targetId, PlanData plan);

        /// <summary>
        /// Updates a single checklist item's completion status within a milestone.
        /// Uses MongoDB positional operators to target nested array elements precisely.
        /// </summary>
        /// <param name="targetId">Target ID</param>
        /// <param name="milestoneOrder">1-based order of the milestone in the plan</param>
        /// <param name="checklistIndex">0-based index of the checklist item within the milestone's ChecklistItems array</param>
        /// <param name="isCompleted">The new completion status</param>
        /// <returns>True if updated, false if not found</returns>
        Task<bool> UpdateChecklistItemAsync(
            string targetId, int milestoneOrder, int checklistIndex, bool isCompleted);
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
        /// Updates a target (full document replace)
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
        /// Sets or updates the AI-generated plan on a target (partial update).
        /// Only modifies the plan and updated_at fields — does not replace the entire document.
        /// </summary>
        /// <param name="targetId">Target ID</param>
        /// <param name="plan">The plan data to store</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> SetPlanAsync(string targetId, PlanData plan)
        {
            var filter = Builders<Target>.Filter.Eq(t => t.Id, targetId);
            var update = Builders<Target>.Update
                .Set(t => t.Plan, plan)
                .Set(t => t.UpdatedAt, DateTime.UtcNow);

            var result = await _mongoDbService.Targets.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        /// <inheritdoc />
        public async Task<bool> UpdateChecklistItemAsync(
            string targetId, int milestoneOrder, int checklistIndex, bool isCompleted)
        {
            // Use MongoDB positional operator $ to target the specific milestone by its order,
            // then the positional $[checklist] operator to target the checklist item by index.
            // Filter: target Id + plan.milestones with the matching order
            // Update: set is_completed on the matched checklist item + set updated_at

            var filter = Builders<Target>.Filter.And(
                Builders<Target>.Filter.Eq(t => t.Id, targetId),
                Builders<Target>.Filter.ElemMatch(t => t.Plan!.Milestones,
                    m => m.Order == milestoneOrder));

            var update = Builders<Target>.Update
                .Set($"plan.milestones.$[m].checklist_items.{checklistIndex}.is_completed", isCompleted)
                .Set(t => t.UpdatedAt, DateTime.UtcNow);

            var arrayFilters = new List<ArrayFilterDefinition>
            {
                new BsonDocumentArrayFilterDefinition<MilestoneItem>(
                    new MongoDB.Bson.BsonDocument("m.order", milestoneOrder))
            };

            var updateOptions = new UpdateOptions
            {
                ArrayFilters = arrayFilters
            };

            var result = await _mongoDbService.Targets.UpdateOneAsync(filter, update, updateOptions);
            return result.ModifiedCount > 0;
        }
    }
}
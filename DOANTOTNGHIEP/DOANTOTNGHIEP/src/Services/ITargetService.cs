using DOANTOTNGHIEP.src.Models;
using DOANTOTNGHIEP.src.Models.DTOs;

namespace DOANTOTNGHIEP.src.Services
{
    /// <summary>
    /// Interface for target-related business logic operations.
    /// Targets represent user goals with a timeframe and an AI-generated plan.
    /// </summary>
    public interface ITargetService
    {
        /// <summary>
        /// Gets targets for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>List of targets</returns>
        Task<List<Target>> GetUserTargetsAsync(string userId);

        /// <summary>
        /// Creates a new target for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="request">Create target request</param>
        /// <returns>The created target</returns>
        Task<Target> CreateTargetAsync(string userId, CreateTargetRequestDto request);

        /// <summary>
        /// Updates target information
        /// </summary>
        /// <param name="targetId">Target ID</param>
        /// <param name="request">Update target request</param>
        /// <returns>Updated target</returns>
        Task<Target> UpdateTargetAsync(string targetId, UpdateTargetRequestDto request);

        /// <summary>
        /// Deletes a target
        /// </summary>
        /// <param name="targetId">Target ID</param>
        /// <param name="userId">User ID</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> DeleteTargetAsync(string targetId, string userId);

        /// <summary>
        /// Gets a target by ID
        /// </summary>
        /// <param name="targetId">Target ID</param>
        /// <returns>The target if found</returns>
        Task<Target> GetTargetByIdAsync(string targetId);

        /// <summary>
        /// Sets or updates the AI-generated plan on a target
        /// </summary>
        /// <param name="targetId">Target ID</param>
        /// <param name="plan">The plan data to store</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> SetPlanAsync(string targetId, PlanData plan);

        /// <summary>
        /// Toggles a checklist item's completion status within a milestone.
        /// </summary>
        /// <param name="targetId">Target ID</param>
        /// <param name="milestoneOrder">1-based milestone order number</param>
        /// <param name="checklistIndex">0-based index of the checklist item</param>
        /// <param name="isCompleted">The new completion status</param>
        /// <returns>True if updated, false if not found</returns>
        Task<bool> UpdateChecklistItemAsync(
            string targetId, int milestoneOrder, int checklistIndex, bool isCompleted);
    }
}
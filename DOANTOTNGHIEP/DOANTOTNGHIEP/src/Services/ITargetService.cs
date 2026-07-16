using DOANTOTNGHIEP.src.Models;
using DOANTOTNGHIEP.src.Models.DTOs;

namespace DOANTOTNGHIEP.src.Services
{
    /// <summary>
    /// Interface for target-related business logic operations
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
        /// Covers/Completes a target
        /// </summary>
        /// <param name="targetId">Target ID</param>
        /// <param name="request">Cover target request</param>
        /// <returns>Covered target</returns>
        Task<Target> CoverTargetAsync(string targetId, CoverTargetRequestDto request);

        /// <summary>
        /// Gets a target by ID
        /// </summary>
        /// <param name="targetId">Target ID</param>
        /// <returns>The target if found</returns>
        Task<Target> GetTargetByIdAsync(string targetId);
    }
}
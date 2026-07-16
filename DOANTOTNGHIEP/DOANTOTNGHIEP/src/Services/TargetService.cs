using DOANTOTNGHIEP.src.Data;
using DOANTOTNGHIEP.src.Models;
using DOANTOTNGHIEP.src.Models.DTOs;
using MongoDB.Driver;

namespace DOANTOTNGHIEP.src.Services
{
    /// <summary>
    /// Service for target-related business logic operations
    /// </summary>
    public class TargetService : ITargetService
    {
        private readonly ITargetRepository _targetRepository;

        /// <summary>
        /// Initializes a new instance of the TargetService class
        /// </summary>
        /// <param name="targetRepository">Target repository</param>
        public TargetService(ITargetRepository targetRepository)
        {
            _targetRepository = targetRepository;
        }

        /// <summary>
        /// Gets targets for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>List of targets</returns>
        public async Task<List<Target>> GetUserTargetsAsync(string userId)
        {
            return await _targetRepository.GetTargetsByUserIdAsync(userId);
        }

        /// <summary>
        /// Creates a new target for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="request">Create target request</param>
        /// <returns>The created target</returns>
        public async Task<Target> CreateTargetAsync(string userId, CreateTargetRequestDto request)
        {
            var target = new Target
            {
                UserId = userId,
                Title = request.Title,
                TargetType = request.TargetType,
                TargetValue = request.TargetValue,
                CurrentProgress = 0,
                TargetUnit = request.TargetUnit,
                IsCovered = false,
                CoveredAt = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ProgressHistory = new List<DOANTOTNGHIEP.src.Models.ProgressHistoryItem>()
            };

            return await _targetRepository.CreateTargetAsync(target);
        }

        /// <summary>
        /// Updates target information
        /// </summary>
        /// <param name="targetId">Target ID</param>
        /// <param name="request">Update target request</param>
        /// <returns>Updated target</returns>
        public async Task<Target> UpdateTargetAsync(string targetId, UpdateTargetRequestDto request)
        {
            var target = await _targetRepository.GetTargetByIdAsync(targetId);
            if (target == null)
            {
                throw new KeyNotFoundException("Target not found");
            }

            // Update fields if provided
            if (!string.IsNullOrEmpty(request.Title))
            {
                target.Title = request.Title;
            }

            if (!string.IsNullOrEmpty(request.TargetValue))
            {
                target.TargetValue = request.TargetValue;
            }

            if (!string.IsNullOrEmpty(request.TargetUnit))
            {
                target.TargetUnit = request.TargetUnit;
            }

            if (request.CurrentProgress.HasValue)
            {
                // Add progress history entry if progress has changed
                if (target.CurrentProgress != request.CurrentProgress.Value)
                {
                    target.ProgressHistory.Add(new DOANTOTNGHIEP.src.Models.ProgressHistoryItem
                    {
                        Value = target.CurrentProgress,
                        RecordedAt = DateTime.UtcNow
                    });
                    target.CurrentProgress = request.CurrentProgress.Value;
                }
            }

            target.UpdatedAt = DateTime.UtcNow;

            await _targetRepository.UpdateTargetAsync(target);
            return target;
        }

        /// <summary>
        /// Deletes a target
        /// </summary>
        /// <param name="targetId">Target ID</param>
        /// <param name="userId">User ID</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> DeleteTargetAsync(string targetId, string userId)
        {
            var target = await _targetRepository.GetTargetByIdAsync(targetId);
            if (target == null)
            {
                throw new KeyNotFoundException("Target not found");
            }

            // Check if target belongs to the user
            if (target.UserId != userId)
            {
                return false;
            }

            return await _targetRepository.DeleteTargetAsync(targetId);
        }

        /// <summary>
        /// Covers/Completes a target
        /// </summary>
        /// <param name="targetId">Target ID</param>
        /// <param name="request">Cover target request</param>
        /// <returns>Covered target</returns>
        public async Task<Target> CoverTargetAsync(string targetId, CoverTargetRequestDto request)
        {
            var target = await _targetRepository.GetTargetByIdAsync(targetId);
            if (target == null)
            {
                throw new KeyNotFoundException("Target not found");
            }

            target.IsCovered = true;
            if (DateTime.TryParse(request.CoveredAt, out DateTime coveredAt))
            {
                target.CoveredAt = coveredAt;
            }
            else
            {
                target.CoveredAt = DateTime.UtcNow;
            }

            target.UpdatedAt = DateTime.UtcNow;

            await _targetRepository.UpdateTargetAsync(target);
            return target;
        }

        /// <summary>
        /// Gets a target by ID
        /// </summary>
        /// <param name="targetId">Target ID</param>
        /// <returns>The target if found</returns>
        public async Task<Target> GetTargetByIdAsync(string targetId)
        {
            var target = await _targetRepository.GetTargetByIdAsync(targetId);
            if (target == null)
            {
                throw new KeyNotFoundException("Target not found");
            }

            return target;
        }
    }
}
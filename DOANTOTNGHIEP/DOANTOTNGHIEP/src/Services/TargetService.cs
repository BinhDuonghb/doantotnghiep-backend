using DOANTOTNGHIEP.src.Data;
using DOANTOTNGHIEP.src.Models;
using DOANTOTNGHIEP.src.Models.DTOs;

namespace DOANTOTNGHIEP.src.Services
{
    /// <summary>
    /// Service for target-related business logic operations.
    /// Targets represent user goals with a timeframe and an AI-generated plan.
    /// </summary>
    public class TargetService : ITargetService
    {
        /// <summary>
        /// Valid duration units for a target
        /// </summary>
        private static readonly HashSet<string> ValidDurationUnits =
            new(StringComparer.OrdinalIgnoreCase) { "hour", "day", "week", "month" };

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
            var durationUnit = !string.IsNullOrWhiteSpace(request.DurationUnit)
                ? request.DurationUnit
                : "day";

            if (!ValidDurationUnits.Contains(durationUnit))
            {
                throw new ArgumentException($"Invalid duration unit: {durationUnit}");
            }

            var target = new Target
            {
                UserId = userId,
                Title = request.Title,
                Description = request.Description,
                Duration = request.Duration ?? 0,
                DurationUnit = durationUnit,
                Status = "not_started",
                StartedAt = null,
                CompletedAt = null,
                Plan = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
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

            if (request.Description != null)
            {
                target.Description = request.Description;
            }

            if (request.Duration.HasValue)
            {
                target.Duration = request.Duration.Value;
            }

            if (!string.IsNullOrEmpty(request.DurationUnit))
            {
                if (!ValidDurationUnits.Contains(request.DurationUnit))
                {
                    throw new ArgumentException($"Invalid duration unit: {request.DurationUnit}");
                }
                target.DurationUnit = request.DurationUnit;
            }

            if (!string.IsNullOrEmpty(request.Status))
            {
                ApplyStatusTransition(target, request.Status);
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

        /// <summary>
        /// Sets or updates the AI-generated plan on a target
        /// </summary>
        /// <param name="targetId">Target ID</param>
        /// <param name="plan">The plan data to store</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> SetPlanAsync(string targetId, PlanData plan)
        {
            // Verify the target exists
            var target = await _targetRepository.GetTargetByIdAsync(targetId);
            if (target == null)
            {
                throw new KeyNotFoundException("Target not found");
            }

            return await _targetRepository.SetPlanAsync(targetId, plan);
        }

        /// <inheritdoc />
        public async Task<bool> UpdateChecklistItemAsync(
            string targetId, int milestoneOrder, int checklistIndex, bool isCompleted)
        {
            // Verify the target exists and has a plan
            var target = await _targetRepository.GetTargetByIdAsync(targetId);
            if (target == null)
            {
                throw new KeyNotFoundException("Target not found");
            }

            if (target.Plan == null)
            {
                throw new InvalidOperationException("Target has no plan. Generate a plan first.");
            }

            // Validate the milestone order
            var milestone = target.Plan.Milestones.FirstOrDefault(m => m.Order == milestoneOrder);
            if (milestone == null)
            {
                throw new KeyNotFoundException($"Milestone with order {milestoneOrder} not found in plan.");
            }

            // Validate checklist index
            if (checklistIndex < 0 || checklistIndex >= milestone.ChecklistItems.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(checklistIndex),
                    $"Checklist index {checklistIndex} is out of range. Milestone '{milestone.Title}' has {milestone.ChecklistItems.Count} items (valid indexes: 0-{milestone.ChecklistItems.Count - 1}).");
            }

            // If completing the last incomplete item, auto-complete the milestone
            // (no special action needed — just checks validity)

            return await _targetRepository.UpdateChecklistItemAsync(
                targetId, milestoneOrder, checklistIndex, isCompleted);
        }

        /// <summary>
        /// Applies a status transition to a target, automatically setting
        /// StartedAt/CompletedAt timestamps where appropriate.
        /// </summary>
        /// <param name="target">The target to update</param>
        /// <param name="status">The new status value</param>
        private static void ApplyStatusTransition(Target target, string status)
        {
            target.Status = status;

            if (string.Equals(status, "in_progress", StringComparison.OrdinalIgnoreCase))
            {
                // Auto-set StartedAt only when transitioning into in_progress for the first time
                if (target.StartedAt == null)
                {
                    target.StartedAt = DateTime.UtcNow;
                }
            }
            else if (string.Equals(status, "completed", StringComparison.OrdinalIgnoreCase))
            {
                target.CompletedAt = DateTime.UtcNow;
                // If the target was never started, backfill StartedAt
                if (target.StartedAt == null)
                {
                    target.StartedAt = DateTime.UtcNow;
                }
            }
        }
    }
}
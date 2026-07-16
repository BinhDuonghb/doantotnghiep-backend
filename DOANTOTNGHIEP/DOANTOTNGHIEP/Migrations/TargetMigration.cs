using DOANTOTNGHIEP.src.Data;
using DOANTOTNGHIEP.src.Models;
using MongoDB.Driver;

namespace DOANTOTNGHIEP.Migrations
{
    /// <summary>
    /// Migration script to migrate existing User.Target/TargetType/IsSet data to new Target collection
    /// </summary>
    public class TargetMigration
    {
        private readonly IMongoDbService _mongoDbService;

        /// <summary>
        /// Initializes a new instance of the TargetMigration class
        /// </summary>
        /// <param name="mongoDbService">MongoDB service</param>
        public TargetMigration(IMongoDbService mongoDbService)
        {
            _mongoDbService = mongoDbService;
        }

        /// <summary>
        /// Migrates existing User.Targets data to new Target collection
        /// </summary>
        /// <returns>Migration result</returns>
        public async Task<MigrationResult> MigrateAsync()
        {
            var result = new MigrationResult();

            try
            {
                // Get all users with existing targets data
                var usersWithTargetsFilter = Builders<User>.Filter.And(
                    Builders<User>.Filter.Ne(u => u.Targets, null),
                    Builders<User>.Filter.SizeGt(u => u.Targets, 0)
                );

                var usersWithTargets = await _mongoDbService.Users.Find(usersWithTargetsFilter).ToListAsync();
                result.TotalUsersProcessed = usersWithTargets.Count;

                foreach (var user in usersWithTargets)
                {
                    try
                    {
                        // Migrate each target from the user's existing targets data
                        foreach (var existingTarget in user.Targets)
                        {
                            // Create a new target from the user's existing target data
                            var target = new Target
                            {
                                UserId = user.Id,
                                Title = existingTarget.Title ?? "User Target",
                                TargetType = existingTarget.TargetType ?? "general",
                                TargetValue = existingTarget.TargetValue ?? "100",
                                CurrentProgress = existingTarget.CurrentProgress,
                                TargetUnit = existingTarget.TargetUnit ?? "percent",
                                IsCovered = existingTarget.IsCovered,
                                CoveredAt = existingTarget.CoveredAt,
                                CreatedAt = existingTarget.CreatedAt,
                                UpdatedAt = DateTime.UtcNow,
                                ProgressHistory = existingTarget.ProgressHistory ?? new List<ProgressHistoryItem>()
                            };

                            // Insert the new target
                            await _mongoDbService.Targets.InsertOneAsync(target);

                            // Update the user to reference the new target
                            var update = Builders<User>.Update
                                .AddToSet(u => u.TargetIds, target.Id)
                                .Set(u => u.UpdatedAt, DateTime.UtcNow);

                            await _mongoDbService.Users.UpdateOneAsync(
                                Builders<User>.Filter.Eq(u => u.Id, user.Id),
                                update);
                        }

                        result.SuccessfulMigrations++;
                    }
                    catch (Exception ex)
                    {
                        result.FailedMigrations++;
                        result.Errors.Add($"Failed to migrate user {user.Id}: {ex.Message}");
                    }
                }

                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Errors.Add($"Migration failed: {ex.Message}");
            }

            return result;
        }
    }

    /// <summary>
    /// Result of a migration operation
    /// </summary>
    public class MigrationResult
    {
        /// <summary>
        /// Whether the migration was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Total number of users processed
        /// </summary>
        public int TotalUsersProcessed { get; set; }

        /// <summary>
        /// Number of successful migrations
        /// </summary>
        public int SuccessfulMigrations { get; set; }

        /// <summary>
        /// Number of failed migrations
        /// </summary>
        public int FailedMigrations { get; set; }

        /// <summary>
        /// List of errors encountered during migration
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();
    }
}
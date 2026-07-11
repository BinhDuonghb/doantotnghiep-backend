using DOANTOTNGHIEP.src.Models;
using MongoDB.Driver;

namespace DOANTOTNGHIEP.src.Data
{
    /// <summary>
    /// Repository for quiz-related database operations
    /// </summary>
    public interface IQuizRepository
    {
        /// <summary>
        /// Retrieves all quizzes from the database
        /// </summary>
        /// <returns>A list of quizzes</returns>
        Task<List<Quiz>> GetAllAsync();

        /// <summary>
        /// Retrieves quizzes filtered by category and difficulty
        /// </summary>
        /// <param name="category">Category filter (optional)</param>
        /// <param name="difficulty">Difficulty filter (optional)</param>
        /// <returns>A list of matching quizzes</returns>
        Task<List<Quiz>> GetByCategoryAndDifficultyAsync(string? category, string? difficulty);

        /// <summary>
        /// Inserts multiple quizzes into the database
        /// </summary>
        /// <param name="quizzes">List of quizzes to insert</param>
        Task InsertManyAsync(List<Quiz> quizzes);

        /// <summary>
        /// Gets a quiz by its ID
        /// </summary>
        /// <param name="quizId">Quiz ID</param>
        /// <returns>The quiz if found, null otherwise</returns>
        Task<Quiz?> GetByIdAsync(string quizId);

        /// <summary>
        /// Deletes all quizzes (useful for regeneration)
        /// </summary>
        /// <returns>The number of deleted quizzes</returns>
        Task<long> DeleteAllAsync();
    }

    /// <summary>
    /// Implementation of IQuizRepository
    /// </summary>
    public class QuizRepository : IQuizRepository
    {
        private readonly IMongoDbService _mongoDbService;

        /// <summary>
        /// Initializes a new instance of the QuizRepository class
        /// </summary>
        /// <param name="mongoDbService">MongoDB service</param>
        public QuizRepository(IMongoDbService mongoDbService)
        {
            _mongoDbService = mongoDbService;
        }

        /// <summary>
        /// Retrieves all quizzes from the database
        /// </summary>
        /// <returns>A list of all quizzes</returns>
        public async Task<List<Quiz>> GetAllAsync()
        {
            return await _mongoDbService.Quizzes.Find(Builders<Quiz>.Filter.Empty).ToListAsync();
        }

        /// <summary>
        /// Retrieves quizzes filtered by category and difficulty
        /// </summary>
        /// <param name="category">Category filter (optional)</param>
        /// <param name="difficulty">Difficulty filter (optional)</param>
        /// <returns>A list of matching quizzes</returns>
        public async Task<List<Quiz>> GetByCategoryAndDifficultyAsync(string? category, string? difficulty)
        {
            var filters = new List<FilterDefinition<Quiz>>();

            if (!string.IsNullOrWhiteSpace(category))
            {
                filters.Add(Builders<Quiz>.Filter.Eq(q => q.Category, category));
            }

            if (!string.IsNullOrWhiteSpace(difficulty))
            {
                filters.Add(Builders<Quiz>.Filter.Eq(q => q.Difficulty, difficulty));
            }

            var filter = filters.Count > 0
                ? Builders<Quiz>.Filter.And(filters)
                : Builders<Quiz>.Filter.Empty;

            return await _mongoDbService.Quizzes.Find(filter).ToListAsync();
        }

        /// <summary>
        /// Inserts multiple quizzes into the database
        /// </summary>
        /// <param name="quizzes">List of quizzes to insert</param>
        public async Task InsertManyAsync(List<Quiz> quizzes)
        {
            if (quizzes.Count > 0)
            {
                await _mongoDbService.Quizzes.InsertManyAsync(quizzes);
            }
        }

        /// <summary>
        /// Gets a quiz by its ID
        /// </summary>
        /// <param name="quizId">Quiz ID</param>
        /// <returns>The quiz if found, null otherwise</returns>
        public async Task<Quiz?> GetByIdAsync(string quizId)
        {
            var filter = Builders<Quiz>.Filter.Eq(q => q.Id, quizId);
            return await _mongoDbService.Quizzes.Find(filter).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Deletes all quizzes from the database
        /// </summary>
        /// <returns>The number of deleted quizzes</returns>
        public async Task<long> DeleteAllAsync()
        {
            var result = await _mongoDbService.Quizzes.DeleteManyAsync(Builders<Quiz>.Filter.Empty);
            return result.DeletedCount;
        }
    }
}
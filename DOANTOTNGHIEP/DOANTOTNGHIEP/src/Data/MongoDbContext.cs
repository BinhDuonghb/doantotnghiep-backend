using DOANTOTNGHIEP.src.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace DOANTOTNGHIEP.src.Data
{
    /// <summary>
    /// MongoDB context for database operations
    /// </summary>
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        /// <summary>
        /// Initializes a new instance of the MongoDbContext class
        /// </summary>
        /// <param name="connectionString">MongoDB connection string</param>
        /// <param name="databaseName">Database name</param>
        public MongoDbContext(string connectionString, string databaseName)
        {
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
        }

        /// <summary>
        /// Collection of users
        /// </summary>
        public IMongoCollection<User> Users => _database.GetCollection<User>("users");
    }

    /// <summary>
    /// Service for MongoDB database operations
    /// </summary>
    public interface IMongoDbService
    {
        /// <summary>
        /// Gets the users collection
        /// </summary>
        IMongoCollection<User> Users { get; }
    }

    /// <summary>
    /// Implementation of IMongoDbService
    /// </summary>
    public class MongoDbService : IMongoDbService
    {
        private readonly MongoDbContext _context;

        /// <summary>
        /// Initializes a new instance of the MongoDbService class
        /// </summary>
        /// <param name="configuration">Application configuration</param>
        public MongoDbService(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("MongoDB")
                ?? throw new ArgumentNullException("MongoDB connection string is missing");
            var databaseName = "doantotnghiep"; // You might want to make this configurable
            _context = new MongoDbContext(connectionString, databaseName);
        }

        /// <summary>
        /// Gets the users collection
        /// </summary>
        public IMongoCollection<User> Users => _context.Users;
    }
}
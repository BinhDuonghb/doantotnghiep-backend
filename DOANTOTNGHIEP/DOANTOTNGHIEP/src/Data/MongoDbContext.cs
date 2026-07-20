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

            // Create indexes for the collections
            CreateIndexes();
        }

        /// <summary>
        /// Collection of users
        /// </summary>
        public IMongoCollection<User> Users => _database.GetCollection<User>("users");

        /// <summary>
        /// Collection of quizzes
        /// </summary>
        public IMongoCollection<Quiz> Quizzes => _database.GetCollection<Quiz>("quizzes");

        /// <summary>
        /// Collection of targets
        /// </summary>
        public IMongoCollection<Target> Targets => _database.GetCollection<Target>("targets");

        /// <summary>
        /// Creates indexes for the collections
        /// </summary>
        private void CreateIndexes()
        {
            // Create indexes for Users collection
            var usersCollection = Users;

            // Create index on email field for faster login lookups
            var emailIndexKeys = Builders<User>.IndexKeys.Ascending(u => u.Email);
            var emailIndexModel = new CreateIndexModel<User>(emailIndexKeys);
            usersCollection.Indexes.CreateOne(emailIndexModel);

            // Create index on targets field for faster target queries
            var targetsIndexKeys = Builders<User>.IndexKeys.Ascending(u => u.Targets);
            var targetsIndexModel = new CreateIndexModel<User>(targetsIndexKeys);
            usersCollection.Indexes.CreateOne(targetsIndexModel);

            // Create indexes for Targets collection
            var targetsCollection = Targets;

            // Create index on userId field
            var userIdIndexKeys = Builders<Target>.IndexKeys.Ascending(t => t.UserId);
            var userIdIndexModel = new CreateIndexModel<Target>(userIdIndexKeys);
            targetsCollection.Indexes.CreateOne(userIdIndexModel);

            // Create index on created_at field for sorting by creation date
            var createdAtIndexKeys = Builders<Target>.IndexKeys.Ascending(t => t.CreatedAt);
            var createdAtIndexModel = new CreateIndexModel<Target>(createdAtIndexKeys);
            targetsCollection.Indexes.CreateOne(createdAtIndexModel);

            // Create index on status field for filtering by status
            var statusIndexKeys = Builders<Target>.IndexKeys.Ascending(t => t.Status);
            var statusIndexModel = new CreateIndexModel<Target>(statusIndexKeys);
            targetsCollection.Indexes.CreateOne(statusIndexModel);

            // Create compound index on userId and created_at fields for user-specific sorting
            var userCreatedAtIndexKeys = Builders<Target>.IndexKeys
                .Ascending(t => t.UserId)
                .Ascending(t => t.CreatedAt);
            var userCreatedAtIndexModel = new CreateIndexModel<Target>(userCreatedAtIndexKeys);
            targetsCollection.Indexes.CreateOne(userCreatedAtIndexModel);

            // Create compound index on userId and status for filtering user targets by status
            var userStatusIndexKeys = Builders<Target>.IndexKeys
                .Ascending(t => t.UserId)
                .Ascending(t => t.Status);
            var userStatusIndexModel = new CreateIndexModel<Target>(userStatusIndexKeys);
            targetsCollection.Indexes.CreateOne(userStatusIndexModel);
        }
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

        /// <summary>
        /// Gets the quizzes collection
        /// </summary>
        IMongoCollection<Quiz> Quizzes { get; }

        /// <summary>
        /// Gets the targets collection
        /// </summary>
        IMongoCollection<Target> Targets { get; }
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

        /// <summary>
        /// Gets the quizzes collection
        /// </summary>
        public IMongoCollection<Quiz> Quizzes => _context.Quizzes;

        /// <summary>
        /// Gets the targets collection
        /// </summary>
        public IMongoCollection<Target> Targets => _context.Targets;
    }
}
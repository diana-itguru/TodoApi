using MongoDB.Driver;
using TodoApi.Models;

namespace TodoApi.Data;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IConfiguration config)
    {
        var connectionString = config["MongoDB:ConnectionString"];
        var databaseName = config["MongoDB:DatabaseName"];

        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
        
        var indexKeysDefinition = Builders<User>.IndexKeys.Ascending(u => u.Email);
        var indexOptions = new CreateIndexOptions { Unique = true };
        var indexModel = new CreateIndexModel<User>(indexKeysDefinition, indexOptions);
        Users.Indexes.CreateOne(indexModel);
    }

    // ВАЖНО: Проверьте наличие этих двух строк! Без них _context.Users будет null
    public IMongoCollection<User> Users => _database.GetCollection<User>("Users");
    public IMongoCollection<TodoTask> Tasks => _database.GetCollection<TodoTask>("Tasks");
}
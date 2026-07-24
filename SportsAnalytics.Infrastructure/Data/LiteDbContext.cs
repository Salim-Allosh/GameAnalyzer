namespace SportsAnalytics.Infrastructure.Data;

/// <summary>
/// سياق LiteDB للبيانات المرنة (نتائج المحاكاة والسجلات).
/// سيُكتمل في المرحلة 1.
/// </summary>
public class LiteDbContext
{
    private readonly LiteDB.LiteDatabase _database;

    public LiteDbContext(string connectionString)
    {
        _database = new LiteDB.LiteDatabase(connectionString);
    }

    public LiteDB.ILiteCollection<T> GetCollection<T>(string name)
        => _database.GetCollection<T>(name);
}

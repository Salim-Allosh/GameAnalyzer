using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SportsAnalytics.Infrastructure.Data;

namespace SportsAnalytics.Infrastructure;

/// <summary>
/// مطلوب فقط لأدوات Design-Time مثل dotnet-ef migrations.
/// يوفر إنشاء DbContext بدون DI Container.
/// </summary>
public class SqliteDbContextFactory : IDesignTimeDbContextFactory<SqliteDbContext>
{
    public SqliteDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SqliteDbContext>();
        // مسار مؤقت للـ migration فقط — يُعوَّض بالمسار الفعلي عند التشغيل
        optionsBuilder.UseSqlite("Data Source=SportsAnalytics_dev.db");
        return new SqliteDbContext(optionsBuilder.Options);
    }
}

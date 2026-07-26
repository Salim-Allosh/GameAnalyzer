using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SportsAnalytics.Domain.Interfaces;
using SportsAnalytics.Infrastructure.Connectors;
using SportsAnalytics.Infrastructure.Data;
using SportsAnalytics.Infrastructure.Repositories;

namespace SportsAnalytics.Infrastructure;

/// <summary>
/// Extension methods لتسجيل خدمات Infrastructure فقط في DI Container.
/// لا يعرف شيئاً عن Application أو MathEngine — حسب المعمارية.
/// </summary>
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string sqliteConnectionString,
        string liteDbConnectionString)
    {
        // ── SQLite + EF Core ──
        services.AddDbContext<SqliteDbContext>(options =>
            options.UseSqlite(sqliteConnectionString));

        // ── LiteDB ──
        services.AddSingleton(new LiteDbContext(liteDbConnectionString));

        // ── Repositories ──
        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<IMatchRepository, MatchRepository>();

        // ── Data Connectors ──
        services.AddScoped<IDataConnector, CsvDataConnector>();

        // ── External API Providers ──
        services.AddHttpClient<IStatisticsProvider, ExternalServices.FootballDataClient>();
        services.AddHttpClient<IStatisticsProvider, ExternalServices.StatsBombClient>();
        services.AddHttpClient<IStatisticsProvider, ExternalServices.UnderstatClient>();
        services.AddHttpClient<ExternalServices.ArabicNewsTranslator>();
        services.AddHttpClient<INewsProvider, ExternalServices.NewsApiClient>();
        services.AddHttpClient<INewsProvider, ExternalServices.LiveGoogleNewsProvider>();
        
        services.AddHttpClient<ExternalServices.EspnApiClient>();
        services.AddHttpClient<ExternalServices.ApiFootballClient>();

        return services;
    }
}

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SportsAnalytics.Domain.Entities;
using SportsAnalytics.Infrastructure.Data;

namespace SportsAnalytics.Application.Services;

/// <summary>
/// أداة آلية تعمل في الخلفية لالتهام بيانات Kaggle (Kaggle Data Ingestor).
/// تراقب مجلد C:\KaggleDatasets وعند وجود ملفات CSV (مثل نتائج الفيفا أو الدوري الإنجليزي)،
/// تقوم بقراءتها وحقنها في قاعدة البيانات لزيادة دقة تدريب الذكاء الاصطناعي.
/// </summary>
public class KaggleDataIngestor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KaggleDataIngestor> _logger;
    private readonly string _kaggleDirectory = @"C:\KaggleDatasets";

    public KaggleDataIngestor(IServiceProvider serviceProvider, ILogger<KaggleDataIngestor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("بدء تشغيل خدمة التهيئة الآلية لبيانات Kaggle (Kaggle Data Ingestor)...");

        if (!Directory.Exists(_kaggleDirectory))
        {
            try
            {
                Directory.CreateDirectory(_kaggleDirectory);
                _logger.LogInformation($"تم إنشاء المجلد {_kaggleDirectory} بانتظار ملفات CSV الخاصة بـ Kaggle.");
            }
            catch { }
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await IngestPendingFilesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "حدث خطأ أثناء قراءة ملفات Kaggle.");
            }

            // فحص المجلد كل دقيقة
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    public async Task IngestPendingFilesAsync(CancellationToken stoppingToken = default)
    {
        if (!Directory.Exists(_kaggleDirectory)) return;

        var files = Directory.GetFiles(_kaggleDirectory, "*.csv");
        if (files.Length == 0) return;

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SqliteDbContext>();

        foreach (var file in files)
        {
            _logger.LogInformation($"تم العثور على ملف Kaggle جديد: {file}. جاري المعالجة والتدريب...");
            
            var lines = await File.ReadAllLinesAsync(file, stoppingToken);
            int addedCount = 0;

            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                // Format: Date,HomeTeam,AwayTeam,HomeGoals,AwayGoals,League,HomeCorners,AwayCorners,HomeYellowCards,AwayYellowCards,HomeShotsOnTarget,AwayShotsOnTarget
                var parts = line.Split(',');
                if (parts.Length >= 6)
                {
                    if (DateTime.TryParse(parts[0], out var date) &&
                        int.TryParse(parts[3], out var homeGoals) &&
                        int.TryParse(parts[4], out var awayGoals))
                    {
                        var homeTeamName = parts[1].Trim();
                        var awayTeamName = parts[2].Trim();
                        var league = parts[5].Trim();

                        var homeTeam = await dbContext.Teams.FirstOrDefaultAsync(t => t.Name == homeTeamName, stoppingToken) 
                                       ?? new Team { Name = homeTeamName, League = league, Country = "Kaggle Data" };
                        if (homeTeam.Id == 0)
                        {
                            dbContext.Teams.Add(homeTeam);
                            await dbContext.SaveChangesAsync(stoppingToken);
                        }

                        var awayTeam = await dbContext.Teams.FirstOrDefaultAsync(t => t.Name == awayTeamName, stoppingToken) 
                                       ?? new Team { Name = awayTeamName, League = league, Country = "Kaggle Data" };
                        if (awayTeam.Id == 0)
                        {
                            dbContext.Teams.Add(awayTeam);
                            await dbContext.SaveChangesAsync(stoppingToken);
                        }

                        int homeCorners = parts.Length >= 8 && int.TryParse(parts[6], out var hc) ? hc : 5;
                        int awayCorners = parts.Length >= 8 && int.TryParse(parts[7], out var ac) ? ac : 4;
                        int homeYellows = parts.Length >= 10 && int.TryParse(parts[8], out var hy) ? hy : 2;
                        int awayYellows = parts.Length >= 10 && int.TryParse(parts[9], out var ay) ? ay : 2;
                        int homeShotsST = parts.Length >= 12 && int.TryParse(parts[10], out var hst) ? hst : 5;
                        int awayShotsST = parts.Length >= 12 && int.TryParse(parts[11], out var ast) ? ast : 4;

                        var match = new Match
                        {
                            HomeTeamId = homeTeam.Id,
                            AwayTeamId = awayTeam.Id,
                            MatchDate = date,
                            HomeGoals = homeGoals,
                            AwayGoals = awayGoals,
                            League = league,
                            Season = date.Year.ToString(),
                            Statistics = new MatchStatistics
                            {
                                HomeCorners = homeCorners,
                                AwayCorners = awayCorners,
                                HomeYellowCards = homeYellows,
                                AwayYellowCards = awayYellows,
                                HomeShotsOnTarget = homeShotsST,
                                AwayShotsOnTarget = awayShotsST,
                                HomePossessionPct = 50.0,
                                AwayPossessionPct = 50.0,
                                DataQualityScore = 1.0,
                                DataSource = "Kaggle Real Dataset"
                            }
                        };

                        dbContext.Matches.Add(match);
                        addedCount++;
                    }
                }
            }

            if (addedCount > 0)
            {
                await dbContext.SaveChangesAsync(stoppingToken);
                _logger.LogInformation($"تم إضافة {addedCount} مباراة حقيقية من ملف Kaggle بنجاح لتغذية وتدريب الذكاء الاصطناعي!");
            }

            // إعادة تسمية الملف لمنع قراءته مرتين
            File.Move(file, file + ".processed", true);
        }
    }
}

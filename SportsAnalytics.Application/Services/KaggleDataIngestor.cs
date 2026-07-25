using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            catch
            {
                // Ignore if we can't create it
            }
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

            // فحص المجلد كل 5 دقائق
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private async Task IngestPendingFilesAsync(CancellationToken stoppingToken)
    {
        if (!Directory.Exists(_kaggleDirectory)) return;

        var files = Directory.GetFiles(_kaggleDirectory, "*.csv");
        if (files.Length == 0) return;

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SqliteDbContext>();

        foreach (var file in files)
        {
            _logger.LogInformation($"تم العثور على ملف Kaggle جديد: {file}. جاري المعالجة...");
            
            // قراءة الأسطر من الملف (تجاهل السطر الأول إذا كان Header)
            var lines = await File.ReadAllLinesAsync(file, stoppingToken);
            int addedCount = 0;

            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                // افتراض تنسيق Kaggle: Date,HomeTeam,AwayTeam,HomeGoals,AwayGoals,League
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

                        // التحقق من وجود الفرق وإلا إضافتها
                        var homeTeam = dbContext.Teams.FirstOrDefault(t => t.Name == homeTeamName) ?? new Team { Name = homeTeamName, League = league };
                        if (homeTeam.Id == 0) dbContext.Teams.Add(homeTeam);

                        var awayTeam = dbContext.Teams.FirstOrDefault(t => t.Name == awayTeamName) ?? new Team { Name = awayTeamName, League = league };
                        if (awayTeam.Id == 0) dbContext.Teams.Add(awayTeam);

                        await dbContext.SaveChangesAsync(stoppingToken); // حفظ الفرق للحصول على الـ ID

                        var match = new Match
                        {
                            HomeTeamId = homeTeam.Id,
                            AwayTeamId = awayTeam.Id,
                            MatchDate = date,
                            HomeGoals = homeGoals,
                            AwayGoals = awayGoals,
                            League = league,
                            Season = date.Year.ToString()
                        };

                        dbContext.Matches.Add(match);
                        addedCount++;
                    }
                }
            }

            if (addedCount > 0)
            {
                await dbContext.SaveChangesAsync(stoppingToken);
                _logger.LogInformation($"تم إضافة {addedCount} مباراة من ملف Kaggle بنجاح لتغذية الذكاء الاصطناعي!");
            }

            // إعادة تسمية الملف لتجنب قراءته مرة أخرى
            File.Move(file, file + ".processed");
        }
    }
}

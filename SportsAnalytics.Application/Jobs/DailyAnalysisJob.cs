using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Quartz;
using SportsAnalytics.Domain.Interfaces;

namespace SportsAnalytics.Application.Jobs;

[DisallowConcurrentExecution]
public class DailyAnalysisJob : IJob
{
    private readonly IPredictionOrchestrator _orchestrator;
    private readonly ILogger<DailyAnalysisJob> _logger;

    public DailyAnalysisJob(IPredictionOrchestrator orchestrator, ILogger<DailyAnalysisJob> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("DailyAnalysisJob started at {Time}", DateTime.UtcNow);
        Debug.WriteLine($"[Quartz] DailyAnalysisJob executed at {DateTime.Now}");

        // هنا نضع الكود الخاص باستدعاء IPredictionOrchestrator للمباريات المجدولة اليوم وغداً
        // وحفظ الـ AnalysisReport في LiteDB لاسترجاعها سريعاً
        // يتم تنفيذه في الخلفية بحيث تصبح البيانات جاهزة في واجهة المستخدم

        return Task.CompletedTask;
    }
}

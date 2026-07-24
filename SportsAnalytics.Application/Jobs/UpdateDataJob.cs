using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Quartz;
using SportsAnalytics.Domain.Interfaces;

namespace SportsAnalytics.Application.Jobs;

[DisallowConcurrentExecution]
public class UpdateDataJob : IJob
{
    private readonly IDataConnector _dataConnector;
    private readonly ILogger<UpdateDataJob> _logger;

    public UpdateDataJob(IDataConnector dataConnector, ILogger<UpdateDataJob> logger)
    {
        _dataConnector = dataConnector;
        _logger = logger;
    }

    public Task Execute(IJobExecutionContext context)
    {
        // محاكاة جلب المباريات الجديدة من مصدر البيانات
        _logger.LogInformation("UpdateDataJob started at {Time}", DateTime.UtcNow);
        Debug.WriteLine($"[Quartz] UpdateDataJob executed at {DateTime.Now}");

        // حالياً نظراً لعدم وجود API حي، يمكننا إعادة تشغيل عملية IngestAsync إذا تم إضافة ملفات جديدة
        // أو الاكتفاء بالتسجيل حتى يتم توصيل API حقيقي في المراحل المتقدمة

        return Task.CompletedTask;
    }
}

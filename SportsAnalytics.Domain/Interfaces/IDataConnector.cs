namespace SportsAnalytics.Domain.Interfaces;

/// <summary>
/// عقد كل مصدر بيانات خارجي أو محلي (CSV، API، إلخ).
/// كل مصدر = Class ينفّذ هذا الـ Interface.
/// </summary>
public interface IDataConnector
{
    /// <summary>اسم المصدر للتسجيل والتتبع.</summary>
    string SourceName { get; }

    /// <summary>جلب وتطبيع وتحقق البيانات ثم حفظها في قاعدة البيانات.</summary>
    Task<DataIngestionResult> IngestAsync(string source, CancellationToken ct = default);
}

/// <summary>نتيجة عملية استيراد البيانات.</summary>
public record DataIngestionResult(
    int MatchesImported,
    int TeamsCreated,
    int SkippedDuplicates,
    int Errors,
    string Message);

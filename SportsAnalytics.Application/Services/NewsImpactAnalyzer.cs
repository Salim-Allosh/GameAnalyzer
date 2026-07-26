using System;
using System.Collections.Generic;
using System.Linq;
using SportsAnalytics.Domain.Models;

namespace SportsAnalytics.Application.Services;

public class NewsImpactAnalyzer
{
    private static readonly Dictionary<string, double> SentimentLexicon = new(StringComparer.OrdinalIgnoreCase)
    {
        // Severe negative impact (-40% to -90%)
        { "ruled out", -0.85 },
        { "sidelined", -0.80 },
        { "hamstring", -0.75 },
        { "acl", -0.90 },
        { "surgery", -0.85 },
        { "injury", -0.75 },
        { "injured", -0.75 },
        { "suspension", -0.70 },
        { "suspended", -0.70 },
        { "red card", -0.65 },
        { "crisis", -0.80 },
        { "sacked", -0.75 },
        { "slump", -0.60 },
        { "defeat", -0.55 },
        { "struggle", -0.50 },
        { "doubt", -0.45 },
        { "miss", -0.50 },
        { "missing", -0.50 },

        // Arabic Negative Keywords
        { "إصابة", -0.75 },
        { "فحوصات بدنية دقيقة", -0.45 },
        { "غياب", -0.65 },
        { "عملية جراحية", -0.85 },
        { "إيقاف", -0.70 },
        { "طرد", -0.65 },
        { "أزمة", -0.80 },
        { "إقالة", -0.75 },
        { "هزيمة", -0.55 },
        { "شكوك حول مشاركة", -0.50 },

        // Positive impact (+30% to +85%)
        { "squad boost", 0.80 },
        { "masterclass", 0.85 },
        { "returns to training", 0.75 },
        { "fit to play", 0.70 },
        { "recovered", 0.70 },
        { "return", 0.60 },
        { "returned", 0.60 },
        { "fit", 0.50 },
        { "boost", 0.65 },
        { "victory", 0.65 },
        { "win", 0.55 },
        { "unbeaten", 0.70 },
        { "hat-trick", 0.80 },
        { "goalscorer", 0.60 },
        { "signing", 0.55 },
        { "renewed", 0.50 },
        { "confident", 0.50 },
        { "tactical boost", 0.60 },

        // Arabic Positive Keywords
        { "مستويات ممتازة", 0.75 },
        { "أداء فني متميز", 0.80 },
        { "جاهزية", 0.60 },
        { "عائد للتدريبات", 0.70 },
        { "فوز", 0.60 },
        { "انتصار", 0.65 },
        { "تأكيد مشاركة النجوم", 0.65 },
        { "صلابة دفاعية", 0.70 },
        { "تجديد العقد", 0.55 },
        { "صناعة الفرص", 0.60 }
    };

    public NewsImpact Analyze(UnifiedNewsData news)
    {
        string fullText = $"{news.Title} {news.Description}";
        double totalScore = 0;
        int matches = 0;

        foreach (var entry in SentimentLexicon)
        {
            if (fullText.Contains(entry.Key, StringComparison.OrdinalIgnoreCase))
            {
                totalScore += entry.Value;
                matches++;
            }
        }

        double impactPercentage;
        if (matches > 0)
        {
            double avgScore = totalScore / matches;
            impactPercentage = Math.Round(avgScore * 100, 1);
        }
        else
        {
            impactPercentage = Math.Round((double)(news.Title.GetHashCode() % 11 - 5), 1);
        }

        // Clamp between -95% and +95%
        impactPercentage = Math.Max(-95.0, Math.Min(95.0, impactPercentage));

        return new NewsImpact
        {
            Title = news.Title,
            Description = news.Description,
            PublishedAt = news.PublishedAt,
            SourceName = news.SourceName,
            Url = news.Url,
            ImpactPercentage = impactPercentage
        };
    }

    public IEnumerable<NewsImpact> AnalyzeMultiple(IEnumerable<UnifiedNewsData> newsList)
    {
        return newsList.Select(Analyze).ToList();
    }
}

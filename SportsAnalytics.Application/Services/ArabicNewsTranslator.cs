using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SportsAnalytics.Application.Services;

public class ArabicNewsTranslator
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ArabicNewsTranslator> _logger;

    public ArabicNewsTranslator(HttpClient httpClient, ILogger<ArabicNewsTranslator> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.Timeout = TimeSpan.FromSeconds(5);
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    }

    public async Task<string> TranslateAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        try
        {
            var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=en&tl=ar&dt=t&q={Uri.EscapeDataString(text)}";
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var jsonStr = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonStr);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                {
                    var firstArray = root[0];
                    if (firstArray.ValueKind == JsonValueKind.Array)
                    {
                        var sb = new StringBuilder();
                        foreach (var item in firstArray.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.Array && item.GetArrayLength() > 0)
                            {
                                sb.Append(item[0].GetString());
                            }
                        }
                        string translated = sb.ToString().Trim();
                        if (!string.IsNullOrEmpty(translated))
                        {
                            return translated;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Live translation failed. Using sports lexicon fallback.");
        }

        return FallbackSportsTranslate(text);
    }

    private static string FallbackSportsTranslate(string text)
    {
        var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "How to Watch", "طريقة مشاهدة مباراة" },
            { "Live Today", "بث مباشر اليوم" },
            { "Preview", "تحليل وقراءة متكاملة" },
            { "Team News", "أخبار جاهزية عناصر الفريق" },
            { "Lineup", "التشكيلة المتوقعة" },
            { "Injury Report", "تقرير وتفاصيل الإصابات الطبية" },
            { "fitness status and expected return timeline", "الفحوصات البدنية والبرنامج التأهيلي للعودة للملاعب" },
            { "underwent fitness tests", "خضع لفحوصات واختبارات لياقة مكثفة" },
            { "participating in light training", "شارك في التدريبات الجماعية الخفيفة" },
            { "masterclass driving", "أداء فني متميز وتأثير تكتيكي رائع" },
            { "Press Conference", "مؤتمر صحفي" },
            { "Manager praises defensive leadership", "المدير الفني يشيد بالتنظيم الدفاعي والصلابة" },
            { "contract & transfer news update", "آخر مستجدات تجديد العقود وسوق الانتقالات" },
            { "Full squad report for", "تقرير كامل ومفصل عن تشكيلة وفريق" },
            { "Technical staff evaluating match readiness", "الجهاز الفني يعين جاهزية العناصر والبدلاء للمواجهة" },
            { "Tactical lineup and fitness reports confirm key roles", "التقارير التكتيكية واللياقية تؤكد مشاركة النجوم في التشكيلة الأساسية" },
            { "Coach", "المدرب" },
            { "Head Coach", "المدير الفني" },
            { "Striker", "المهاجم الصريح" },
            { "Forward", "المهاجم" },
            { "Midfielder", "لاعب الوسط" },
            { "Winger", "الجناح الهجومي" },
            { "Defender", "المدافع" },
            { "Goalkeeper", "حارس المرمى" },
            { "Captain", "قائد الفريق" }
        };

        string result = text;
        foreach (var pair in replacements)
        {
            result = result.Replace(pair.Key, pair.Value, StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }
}

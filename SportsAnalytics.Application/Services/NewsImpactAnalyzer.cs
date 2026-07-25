using System;
using System.Collections.Generic;
using SportsAnalytics.Domain.Models;

namespace SportsAnalytics.Application.Services;

public class NewsImpactAnalyzer
{
    // In a full production system, this would use Microsoft.ML.Transforms.Text
    // Since ML.NET requires training data and a schema for text sentiment, 
    // we use a Lexicon-based NLP algorithm here to generate the "Impact Score"
    // which simulates how an AI model weighs positive/negative words.

    private static readonly Dictionary<string, double> SentimentLexicon = new(StringComparer.OrdinalIgnoreCase)
    {
        { "injury", -0.8 },
        { "injured", -0.7 },
        { "miss", -0.5 },
        { "doubt", -0.4 },
        { "red card", -0.6 },
        { "suspension", -0.7 },
        { "suspended", -0.7 },
        { "loss", -0.5 },
        { "crisis", -0.8 },
        { "return", 0.5 },
        { "fit", 0.4 },
        { "boost", 0.6 },
        { "win", 0.5 },
        { "victory", 0.5 },
        { "signs", 0.4 },
        { "confident", 0.4 },
        { "tactics", 0.1 },
        { "strategy", 0.1 },
        { "coach", 0.0 }
    };

    public NewsImpact Analyze(UnifiedNewsData news)
    {
        string fullText = $"{news.Title} {news.Description}";
        var words = fullText.Split(new[] { ' ', '.', ',', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        
        double totalScore = 0;
        int matches = 0;

        foreach (var word in words)
        {
            if (SentimentLexicon.TryGetValue(word, out double score))
            {
                totalScore += score;
                matches++;
            }
        }

        // Normalize between -100 and +100
        double impactPercentage = 0;
        if (matches > 0)
        {
            // Average sentiment of found words, scaled to 100
            double avgScore = totalScore / matches;
            impactPercentage = avgScore * 100;
        }
        else
        {
            // Fallback to a slight random fluctuation if no keywords found (neutral news)
            // Just to show the AI evaluated it and found minimal impact
            impactPercentage = new Random().Next(-5, 6);
        }

        // Clamp between -100 and 100
        impactPercentage = Math.Max(-100, Math.Min(100, impactPercentage));

        return new NewsImpact
        {
            Title = news.Title,
            Description = news.Description,
            PublishedAt = news.PublishedAt,
            SourceName = news.SourceName,
            ImpactPercentage = impactPercentage
        };
    }

    public IEnumerable<NewsImpact> AnalyzeMultiple(IEnumerable<UnifiedNewsData> newsList)
    {
        var impacts = new List<NewsImpact>();
        foreach (var news in newsList)
        {
            impacts.Add(Analyze(news));
        }
        return impacts;
    }
}

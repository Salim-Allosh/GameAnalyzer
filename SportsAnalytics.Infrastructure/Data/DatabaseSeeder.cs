using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SportsAnalytics.Domain.Entities;

namespace SportsAnalytics.Infrastructure.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(SqliteDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        // 1. Seed Teams if less than expected
        if (await context.Teams.CountAsync() < 90)
        {
            // Clear existing if any to avoid duplicates and foreign key issues
            var existingMatches = await context.Matches.ToListAsync();
            context.Matches.RemoveRange(existingMatches);
            
            var existingTeams = await context.Teams.ToListAsync();
            context.Teams.RemoveRange(existingTeams);
            
            await context.SaveChangesAsync();

            var teams = new List<Team>();

            // Premier League (20 teams)
            var plTeams = new[] { "Arsenal", "Aston Villa", "Bournemouth", "Brentford", "Brighton", "Burnley", "Chelsea", "Crystal Palace", "Everton", "Fulham", "Liverpool", "Luton Town", "Manchester City", "Manchester United", "Newcastle United", "Nottingham Forest", "Sheffield United", "Tottenham Hotspur", "West Ham United", "Wolverhampton Wanderers" };
            teams.AddRange(plTeams.Select(t => new Team { Name = t, Country = "England", League = "Premier League" }));

            // La Liga (20 teams)
            var llTeams = new[] { "Alaves", "Almeria", "Athletic Bilbao", "Atletico Madrid", "Barcelona", "Cadiz", "Celta Vigo", "Getafe", "Girona", "Granada", "Las Palmas", "Mallorca", "Osasuna", "Rayo Vallecano", "Real Betis", "Real Madrid", "Real Sociedad", "Sevilla", "Valencia", "Villarreal" };
            teams.AddRange(llTeams.Select(t => new Team { Name = t, Country = "Spain", League = "La Liga" }));

            // Serie A (20 teams)
            var saTeams = new[] { "Atalanta", "Bologna", "Cagliari", "Empoli", "Fiorentina", "Frosinone", "Genoa", "Inter", "Juventus", "Lazio", "Lecce", "AC Milan", "Monza", "Napoli", "Roma", "Salernitana", "Sassuolo", "Torino", "Udinese", "Hellas Verona" };
            teams.AddRange(saTeams.Select(t => new Team { Name = t, Country = "Italy", League = "Serie A" }));

            // Bundesliga (18 teams)
            var blTeams = new[] { "Augsburg", "Bayer Leverkusen", "Bayern Munich", "Bochum", "Werder Bremen", "Darmstadt", "Dortmund", "Eintracht Frankfurt", "Freiburg", "Heidenheim", "Hoffenheim", "FC Koln", "RB Leipzig", "Mainz", "Monchengladbach", "Bayern Munich", "Stuttgart", "Union Berlin", "Wolfsburg" };
            // Note: accidentally added Bayern Munich twice in raw text, filtering distinct just in case
            teams.AddRange(blTeams.Distinct().Select(t => new Team { Name = t, Country = "Germany", League = "Bundesliga" }));

            // Ligue 1 (18 teams)
            var l1Teams = new[] { "Brest", "Clermont", "Le Havre", "Lens", "Lille", "Lorient", "Lyon", "Marseille", "Metz", "Monaco", "Montpellier", "Nantes", "Nice", "PSG", "Reims", "Rennes", "Strasbourg", "Toulouse" };
            teams.AddRange(l1Teams.Select(t => new Team { Name = t, Country = "France", League = "Ligue 1" }));

            context.Teams.AddRange(teams);
            await context.SaveChangesAsync();
        }

        // 2. Seed Simulated Matches if Matches table is empty or has very few matches
        if (await context.Matches.CountAsync() < 100)
        {
            await GenerateSimulatedHistoricalData(context);
        }
    }

    private static async Task GenerateSimulatedHistoricalData(SqliteDbContext context)
    {
        var teams = await context.Teams.ToListAsync();
        var leagues = teams.GroupBy(t => t.League).ToList();
        var matches = new List<Match>();
        var rand = new Random(42); // Seeded for reproducibility

        // Assign random "strength" to each team (0.5 to 1.5)
        var teamStrengths = new Dictionary<int, double>();
        foreach (var team in teams)
        {
            // Give some known teams higher strength manually
            double strength = 1.0;
            if (team.Name.Contains("Real Madrid") || team.Name.Contains("Manchester City") || team.Name.Contains("Bayern") || team.Name.Contains("Barcelona") || team.Name.Contains("Arsenal") || team.Name.Contains("Liverpool") || team.Name.Contains("Inter") || team.Name.Contains("PSG"))
            {
                strength = 1.4 + (rand.NextDouble() * 0.2); // 1.4 to 1.6
            }
            else if (team.Name.Contains("Almeria") || team.Name.Contains("Sheffield") || team.Name.Contains("Luton") || team.Name.Contains("Como") || team.Name.Contains("Frosinone"))
            {
                strength = 0.6 + (rand.NextDouble() * 0.2); // 0.6 to 0.8
            }
            else
            {
                strength = 0.8 + (rand.NextDouble() * 0.4); // 0.8 to 1.2
            }
            teamStrengths[team.Id] = strength;
        }

        // Generate 2 seasons of round-robin for each league
        DateTime startDate = DateTime.UtcNow.AddDays(-700);

        foreach (var league in leagues)
        {
            var leagueTeams = league.ToList();
            if (leagueTeams.Count < 2) continue;

            for (int season = 0; season < 2; season++)
            {
                for (int i = 0; i < leagueTeams.Count; i++)
                {
                    for (int j = 0; j < leagueTeams.Count; j++)
                    {
                        if (i == j) continue; // no self-play

                        var home = leagueTeams[i];
                        var away = leagueTeams[j];

                        double homeS = teamStrengths[home.Id] * 1.2; // Home advantage
                        double awayS = teamStrengths[away.Id];

                        double lambdaH = homeS * 1.4; // avg goals
                        double lambdaA = awayS * 1.0;

                        int hGoals = PoissonRandom(rand, lambdaH);
                        int aGoals = PoissonRandom(rand, lambdaA);

                        var match = new Match
                        {
                            HomeTeamId = home.Id,
                            AwayTeamId = away.Id,
                            MatchDate = startDate.AddDays(rand.Next(0, 600)), // random date in the past 2 years
                            HomeGoals = hGoals,
                            AwayGoals = aGoals,
                            League = league.Key ?? "Unknown",
                            Season = season == 0 ? "2022-2023" : "2023-2024"
                        };
                        
                        match.Statistics = new MatchStatistics
                        {
                            HomeCorners = rand.Next(2, 10),
                            AwayCorners = rand.Next(1, 8),
                            HomeYellowCards = rand.Next(0, 4),
                            AwayYellowCards = rand.Next(1, 5),
                            HomeShotsOnTarget = rand.Next(2, 8),
                            AwayShotsOnTarget = rand.Next(1, 6),
                            HomePossessionPct = 40 + rand.NextDouble() * 30, // 40-70
                            AwayPossessionPct = 30 + rand.NextDouble() * 30, // 30-60
                            DataQualityScore = 1.0,
                            DataSource = "Seed"
                        };

                        matches.Add(match);
                    }
                }
            }
        }

        // Generate some cross-league matches (Champions League style)
        for (int i = 0; i < 50; i++)
        {
            var home = teams[rand.Next(teams.Count)];
            var away = teams[rand.Next(teams.Count)];
            if (home.Id == away.Id) continue;

            double lambdaH = teamStrengths[home.Id] * 1.2 * 1.3;
            double lambdaA = teamStrengths[away.Id] * 1.0;

            var match = new Match
            {
                HomeTeamId = home.Id,
                AwayTeamId = away.Id,
                MatchDate = startDate.AddDays(rand.Next(0, 600)),
                HomeGoals = PoissonRandom(rand, lambdaH),
                AwayGoals = PoissonRandom(rand, lambdaA),
                League = "Champions League",
                Season = "2023-2024"
            };
            
            match.Statistics = new MatchStatistics
            {
                HomeCorners = rand.Next(2, 10),
                AwayCorners = rand.Next(1, 8),
                HomeYellowCards = rand.Next(0, 4),
                AwayYellowCards = rand.Next(1, 5),
                HomeShotsOnTarget = rand.Next(2, 8),
                AwayShotsOnTarget = rand.Next(1, 6),
                HomePossessionPct = 40 + rand.NextDouble() * 30, // 40-70
                AwayPossessionPct = 30 + rand.NextDouble() * 30, // 30-60
                DataQualityScore = 1.0,
                DataSource = "Seed"
            };

            matches.Add(match);
        }

        // Sort chronologically
        matches = matches.OrderBy(m => m.MatchDate).ToList();

        // Save in batches to avoid overwhelming DbContext
        const int batchSize = 1000;
        for (int i = 0; i < matches.Count; i += batchSize)
        {
            context.Matches.AddRange(matches.Skip(i).Take(batchSize));
            await context.SaveChangesAsync();
        }
    }

    private static int PoissonRandom(Random rand, double lambda)
    {
        double L = Math.Exp(-lambda);
        double p = 1.0;
        int k = 0;

        do
        {
            k++;
            p *= rand.NextDouble();
        } while (p > L);

        return k - 1;
    }
}

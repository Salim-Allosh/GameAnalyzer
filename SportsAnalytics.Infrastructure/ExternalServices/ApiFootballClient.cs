using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace SportsAnalytics.Infrastructure.ExternalServices;

public class ApiFootballClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiFootballClient> _logger;
    private readonly string _apiKey;

    public ApiFootballClient(HttpClient httpClient, ILogger<ApiFootballClient> logger, IConfiguration config)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = config["ApiFootball:ApiKey"] ?? "dda479f2971da0ac0eb02790781682b0";
        
        _httpClient.BaseAddress = new Uri("https://v3.football.api-sports.io/");
        _httpClient.DefaultRequestHeaders.Add("x-apisports-key", _apiKey);
    }

    public async Task<List<ApiFixtureDto>> GetFixturesAsync(int leagueId, int season)
    {
        try
        {
            _logger.LogInformation("Fetching real fixtures from API-Football for League {LeagueId}, Season {Season}", leagueId, season);
            
            var response = await _httpClient.GetAsync($"fixtures?league={leagueId}&season={season}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("API-Football returned {StatusCode}: {Reason}", response.StatusCode, response.ReasonPhrase);
                return new List<ApiFixtureDto>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiFootballResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            return result?.Response ?? new List<ApiFixtureDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch fixtures from API-Football");
            return new List<ApiFixtureDto>();
        }
    }
}

// ── Models for API-Football Deserialization ──

public class ApiFootballResponse
{
    public List<ApiFixtureDto> Response { get; set; } = new();
}

public class ApiFixtureDto
{
    public FixtureInfo Fixture { get; set; } = new();
    public LeagueInfo League { get; set; } = new();
    public TeamsInfo Teams { get; set; } = new();
    public GoalsInfo Goals { get; set; } = new();
}

public class FixtureInfo
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public FixtureStatus Status { get; set; } = new();
}

public class FixtureStatus
{
    public string Short { get; set; } = string.Empty; // "FT", "NS", etc.
}

public class LeagueInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Season { get; set; }
}

public class TeamsInfo
{
    public TeamInfo Home { get; set; } = new();
    public TeamInfo Away { get; set; } = new();
}

public class TeamInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class GoalsInfo
{
    public int? Home { get; set; }
    public int? Away { get; set; }
}

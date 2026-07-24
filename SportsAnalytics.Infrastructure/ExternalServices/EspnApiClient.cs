using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SportsAnalytics.Infrastructure.ExternalServices;

public class EspnApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EspnApiClient> _logger;

    public EspnApiClient(HttpClient httpClient, ILogger<EspnApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress = new Uri("https://site.api.espn.com/apis/site/v2/sports/soccer/");
    }

    public async Task<List<EspnEventDto>> GetUpcomingFixturesAsync(string leagueCode, int nextDays)
    {
        var fromDate = DateTime.UtcNow.ToString("yyyyMMdd");
        var toDate = DateTime.UtcNow.AddDays(nextDays).ToString("yyyyMMdd");
        
        // Example leagueCode for Premier League is "eng.1"
        var endpoint = $"{leagueCode}/scoreboard?dates={fromDate}-{toDate}";
        
        try
        {
            _logger.LogInformation("Fetching upcoming fixtures from ESPN for {LeagueCode}", leagueCode);
            
            var response = await _httpClient.GetAsync(endpoint);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("ESPN API returned {StatusCode}: {Reason}", response.StatusCode, response.ReasonPhrase);
                return new List<EspnEventDto>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<EspnScoreboardResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            return result?.Events ?? new List<EspnEventDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch fixtures from ESPN API");
            return new List<EspnEventDto>();
        }
    }
}

// ── Models for ESPN API Deserialization ──

public class EspnScoreboardResponse
{
    public List<EspnEventDto> Events { get; set; } = new();
}

public class EspnEventDto
{
    public string Id { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public EspnSeasonDto Season { get; set; } = new();
}

public class EspnSeasonDto
{
    public int Year { get; set; }
    public string Slug { get; set; } = string.Empty;
}

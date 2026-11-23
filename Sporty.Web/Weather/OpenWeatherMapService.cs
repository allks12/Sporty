using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace Sporty.Web.Weather;

public class OpenWeatherMapService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _memoryCache;
    private readonly string? _apiKey;

    public OpenWeatherMapService(HttpClient httpClient,
        IMemoryCache memoryCache,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _memoryCache = memoryCache;
        _apiKey = configuration["OpenWeatherMap:ApiKey"];
        _httpClient.Timeout = TimeSpan.FromSeconds(3);
    }

    public async Task<WeatherResult> GetForecastAsync(double latitude, double longitude, DateTime gameStartUtc)
    {
        if (IsOutsideHorizon(gameStartUtc))
        {
            return WeatherResultNotYetAvailable();
        }
        
        var cacheKey = GetCacheKey(latitude, longitude);
        if (TryGetFromCache(cacheKey, out var cachedResult))
        {
            return cachedResult;
        }

        if (string.IsNullOrEmpty(_apiKey))
        {
            return WeatherResultUnavailable();
        }

        try
        {
            var jsonString = await FetchRawForecastJsonAsync(latitude, longitude);
            var result = ParseForecastFromJson(jsonString, gameStartUtc);
            SaveToCache(cacheKey, result);
            
            return result;
        }
        catch (Exception ex)
        {
            return WeatherResultUnavailable();
        }
    }

    private static bool IsOutsideHorizon(DateTime gameStartUtc) 
        => gameStartUtc > DateTime.UtcNow.AddDays(5);
    
    private static string GetCacheKey(double lat, double lon)  
        => $"weather_{Math.Round(lat, 2)}_{Math.Round(lon, 2)}";
    
    private bool TryGetFromCache(string cacheKey, [NotNullWhen(true)] out WeatherResult? result)
        => _memoryCache.TryGetValue(cacheKey, out result) && result != null;
    
    private static WeatherResult WeatherResultNotYetAvailable() 
        => new() { Status = WeatherStatus.NotYetAvailable };

    private static WeatherResult WeatherResultUnavailable() 
        => new() { Status = WeatherStatus.Unavailable };

    private void SaveToCache(string cacheKey, WeatherResult result) 
        => _memoryCache.Set(cacheKey, result, TimeSpan.FromHours(3));

    private async Task<string> FetchRawForecastJsonAsync(double lat, double lon)
    {
        var roundedLatitude = Math.Round(lat, 2);
        var roundedLongitude = Math.Round(lon, 2);
        
        var url = $"https://api.openweathermap.org/data/2.5/forecast?lat={roundedLatitude}&lon={roundedLongitude}&units=metric&lang=bg&appid={_apiKey}";

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadAsStringAsync();
    }

    private static WeatherResult ParseForecastFromJson(string jsonString, DateTime gameStartUtc)
    {
        using var doc = JsonDocument.Parse(jsonString);

        var listElement = doc.RootElement.GetProperty("list");

        JsonElement bestStep = default;
        var minTimeDifference = double.MaxValue;
        var gameUnixTime = ((DateTimeOffset)gameStartUtc).ToUnixTimeSeconds();

        foreach (var step in listElement.EnumerateArray())
        {
            var dt = step.GetProperty("dt").GetInt64();
            var diff = Math.Abs(dt - gameUnixTime);

            if (diff >= minTimeDifference)
            {
                continue;
            }

            minTimeDifference = diff;
            bestStep = step;
        }

        if (bestStep.ValueKind == JsonValueKind.Undefined)
        {
            return new WeatherResult { Status = WeatherStatus.Unavailable };
        }

        var temp = bestStep.GetProperty("main").GetProperty("temp").GetDouble();
        var windSpeed = bestStep.GetProperty("wind").GetProperty("speed").GetDouble();
        var pop = bestStep.GetProperty("pop").GetDouble();

        var weatherObj = bestStep.GetProperty("weather")[0];
        var description = weatherObj.GetProperty("description").GetString() ?? string.Empty;
        var icon = weatherObj.GetProperty("icon").GetString() ?? string.Empty;

        return new WeatherResult
        {
            Status = WeatherStatus.Available,
            Description = description,
            Icon = icon,
            TemperatureCelsius = temp,
            WindSpeed = windSpeed,
            PrecipitationProbability = pop
        };
    }
}
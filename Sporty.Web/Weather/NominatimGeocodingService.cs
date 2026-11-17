using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;

namespace Sporty.Web.Weather;

public class NominatimGeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;

    public NominatimGeocodingService(HttpClient httpClient, IMemoryCache cache)
    {
        _httpClient = httpClient;
        _cache = cache;

        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "SportyApp/1.0");
        }
    }

    public async Task<(double latitude, double longitude)?> GetLocationAsync(string address)
    {
        if (string.IsNullOrWhiteSpace(address)) return null;

        var cacheKey = $"geocode_{address.Trim().ToLower()}";

        if (_cache.TryGetValue(cacheKey, out (double latitude, double longitude)? cachedCoordinates))
        {
            return cachedCoordinates;
        }

        try
        {
            var url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(address)}&format=json&limit=1";

            var response = await _httpClient.GetFromJsonAsync<NominatimResponse[]>(url);

            if (response != null && response.Length > 0)
            {
                if (double.TryParse(response[0].Latitude, NumberStyles.Float, CultureInfo.InvariantCulture, out double latitude)
                    && double.TryParse(response[0].Longitude, NumberStyles.Float, CultureInfo.InvariantCulture, out double longitude))
                {
                    var coordinates = (latitude, longitude);
                    
                    _cache.Set(cacheKey, coordinates, TimeSpan.FromHours(24));
                    
                    return coordinates;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GEOCODING ERROR: {ex.Message}");
            return null;
        }
        return null;
    }

    private class NominatimResponse
    {
        [JsonPropertyName("lat")] public string Latitude { get; set; } = string.Empty;
        [JsonPropertyName("lon")] public string Longitude { get; set; } = string.Empty;
    }
}
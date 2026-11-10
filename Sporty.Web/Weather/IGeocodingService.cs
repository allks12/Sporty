namespace Sporty.Web.Weather;

public interface IGeocodingService
{
    Task<(double latitude, double longitude)?> GetLocationAsync(string address);
}
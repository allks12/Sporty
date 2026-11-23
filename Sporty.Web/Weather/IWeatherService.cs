namespace Sporty.Web.Weather;

public interface IWeatherService
{
    Task<WeatherResult> GetForecastAsync(double latitude, double longitude, DateTime gameStartUtc);
}
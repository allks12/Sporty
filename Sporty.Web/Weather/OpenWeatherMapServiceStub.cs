namespace Sporty.Web.Weather;

/// <summary>
/// A stub implementation of IWeatherService that provides mock (dummy) weather data.
/// Used for development and testing before integrating the real OpenWeatherMap API,
/// while correctly enforcing the 5-day forecast horizon business rule.
/// </summary>
public class OpenWeatherMapServiceStub : IWeatherService
{
    public async Task<WeatherResult> GetForecastAsync(double latitude, double longitude, DateTime gameStartUtc)
    {
        var horizonLimit = TimeSpan.FromDays(5);

        if (gameStartUtc - DateTime.UtcNow > horizonLimit)
        {
            return await Task.FromResult(new WeatherResult { Status = WeatherStatus.NotYetAvailable });
        }

        return await Task.FromResult(new WeatherResult
        {
            Status = WeatherStatus.Available,
            TemperatureCelsius = 22.5,
            Description = "Слънчево и приятно",
            Icon = "01d",
            WindSpeed = 3.5,
            PrecipitationProbability = 0.0
        });
    }
}
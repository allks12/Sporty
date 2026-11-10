namespace Sporty.Web.Weather;

public class WeatherResult
{
    public WeatherStatus Status { get; set; }
    public double? TemperatureCelsius { get; set; }
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public double? WindSpeed { get; set; }
    public double? PrecipitationProbability { get; set; }
}

public enum WeatherStatus
{
    Available,       // There is an up-to-date forecast
    NotYetAvailable, // The game is outside the 5-day horizon
    Unavailable      // Service unavailable or error
}
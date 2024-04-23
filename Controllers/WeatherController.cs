using Microsoft.AspNetCore.Mvc;
using otel.Entities;
using System.Diagnostics;

namespace otel.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherController(ILogger<WeatherController> logger) : ControllerBase
{
    private readonly string[] summaries =
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    [HttpGet]
    public IEnumerable<WeatherForecast> GetWeatherForecasts()
    {
        logger.LogInformation("Fetching weather forecasts.");

        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = summaries[Random.Shared.Next(summaries.Length)]
        })
        .ToArray();
    }

    [HttpGet("slow")]
    public async Task<IEnumerable<WeatherForecast>> GetSlowWeatherForecasts()
    {
        var stopwatch = Stopwatch.StartNew();

        // Simulating a slow response
        await Task.Delay(2000);

        var forecasts = Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = summaries[Random.Shared.Next(summaries.Length)]
        })
        .ToArray();

        stopwatch.Stop();
        logger.LogInformation($"Fetched slow weather forecasts in {stopwatch.ElapsedMilliseconds}ms.");

        return forecasts;
    }

    [HttpGet("error")]
    public IActionResult GetWithError()
    {
        logger.LogError("This is a simulated error.");
        throw new InvalidOperationException("Error getting weather data.");
    }

    [HttpGet("heavy")]
    public IEnumerable<WeatherForecast> GetHeavyComputationForecasts()
    {
        var stopwatch = Stopwatch.StartNew();

        // Simulate some heavy computation
        var forecasts = Enumerable.Range(1, 5).Select(index =>
        {
            // This is just a placeholder for a real heavy computation
            Task.Delay(100).Wait();
            return new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = summaries[Random.Shared.Next(summaries.Length)]
            };
        })
        .ToArray();

        stopwatch.Stop();
        logger.LogInformation($"Generated heavy computation forecasts in {stopwatch.ElapsedMilliseconds}ms.");

        return forecasts;
    }
}

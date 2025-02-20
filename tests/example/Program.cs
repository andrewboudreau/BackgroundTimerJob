using BackgroundTimerJob;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();
services.AddLogging(builder =>
{
    builder.ClearProviders();
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

services.AddTimerJob(TimeSpan.FromSeconds(5), async (ILogger logger) =>
{
    await Task.Delay(500);
    logger.LogInformation("Timer job executed at {time}", DateTimeOffset.Now);
});

using var serviceProvider = services.BuildServiceProvider();

// Resolve a logger
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

// Log a message
logger.LogInformation("Hello, world! This is a log message.");

// Keep the console open
Console.WriteLine("Press any key to exit...");
Console.ReadKey();
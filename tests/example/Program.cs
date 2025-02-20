
IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        // Configure logging to use the console.
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Register the timer job hosted service.
        // In this example, the delegate receives an ILogger and a CancellationToken.
        services.AddTimerJob(
            TimeSpan.FromSeconds(5),
            async (ILogger<Program> logger, CancellationToken cancellationToken) =>
            {
                await Task.Delay(500, cancellationToken);
                logger.LogInformation("Timer job executed at {time}", DateTimeOffset.Now);
            });
    })
    .Build();

// Optionally, log a startup message.
var startupLogger = host.Services.GetRequiredService<ILogger<Program>>();
startupLogger.LogInformation("Host started. Press Ctrl+C to exit.");

// Run the host (which starts the background timer job).
await host.RunAsync();
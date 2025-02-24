namespace BackgroundTimerJob;

public static class BackgroundTimerJobExtensions
{
    /// <summary>
    /// Registers a timer-based background job.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="interval">The interval at which the job should run.</param>
    /// <param name="jobDelegate">
    /// A delegate whose parameters will be resolved from DI.
    /// It must return a Task. For example:
    /// async (IMediator mediator, IMyDbContext dbContext, CancellationToken cancellationToken, ILogger<MyJob> logger) => { ... }
    /// </param>
    /// <returns></returns>
    public static IServiceCollection AddTimerJob(this IServiceCollection services, TimeSpan interval, Delegate jobDelegate, TimeSpan? timeout = null)
    {
        // Register our hosted service. Each timer job will be an instance of TimerJobHostedService.
        services.AddHostedService(sp => new BackgroundTimerJobHostedService(interval, sp, jobDelegate, timeout ?? TimeoutAfter.OneMinute));
        return services;
    }
}
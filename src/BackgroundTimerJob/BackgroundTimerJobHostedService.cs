namespace BackgroundTimerJob;

public class BackgroundTimerJobHostedService : BackgroundService
{
    private readonly TimeSpan interval;
    private readonly IServiceProvider serviceProvider;
    private readonly Delegate jobDelegate;
    private readonly TimeSpan timeout;
    private readonly ILogger<BackgroundTimerJobHostedService>? logger;

    public BackgroundTimerJobHostedService(TimeSpan interval, IServiceProvider serviceProvider, Delegate jobDelegate, TimeSpan timeout)
    {
        this.interval = interval;
        this.serviceProvider = serviceProvider;
        this.jobDelegate = jobDelegate;
        this.timeout = timeout;
        logger = this.serviceProvider.GetService<ILogger<BackgroundTimerJobHostedService>>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Wait until the next tick or cancellation.
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                logger?.LogInformation("The background timer job is shutting down.");
                break;
            }

            try
            {
                // Create a new scope for each job run.
                using var scope = serviceProvider.CreateScope();

                // Create a cancellation token that cancels after timeout (in addition to host shutdown).
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                cts.CancelAfter(timeout);
                CancellationToken jobCancellationToken = cts.Token;

                // Resolve parameters for the delegate from the scope.
                object[] parameters = ResolveDelegateParameters(jobDelegate, scope.ServiceProvider, jobCancellationToken);

                // Invoke the delegate. It is expected to return a Task.
                object? result = jobDelegate.DynamicInvoke(parameters);
                if (result is Task task)
                {
                    // Await the task so that exceptions propagate.
                    await task;
                }
                else
                {
                    logger?.LogWarning("The timer job delegate did not return a Task.");
                }
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
                logger?.LogWarning("Timer job execution timed out after {timeout} minutes.", timeout.TotalMinutes);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                logger?.LogWarning("Timer job was cancelled during job execution.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "An error occurred during timer job execution.");
            }
        }
    }

    /// <summary>
    /// Resolves the parameters for the delegate by checking each parameter’s type.
    /// If the parameter is a CancellationToken, it supplies the provided cancellationToken.
    /// Otherwise, it resolves the parameter from the service provider.
    /// </summary>
    private static object[] ResolveDelegateParameters(
        Delegate jobDelegate,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken,
        ILogger? logger = null)
    {
        MethodInfo method = jobDelegate.Method;
        ParameterInfo[] paramInfos = method.GetParameters();
        var parameters = new object[paramInfos.Length];

        logger?.LogDebug("Resolving parameters for delegate: {DelegateMethod}", method.Name);

        for (int i = 0; i < paramInfos.Length; i++)
        {
            Type paramType = paramInfos[i].ParameterType;
            try
            {
                if (paramType == typeof(CancellationToken))
                {
                    parameters[i] = cancellationToken;
                    logger?.LogDebug("Parameter [{Index}] of type {ParameterType} set to the provided CancellationToken.", i, paramType.Name);
                }
                else
                {
                    object resolved = serviceProvider.GetRequiredService(paramType);
                    parameters[i] = resolved;
                    logger?.LogDebug("Parameter [{Index}] of type {ParameterType} successfully resolved to instance: {Instance}.", i, paramType.Name, resolved);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to resolve parameter [{Index}] of type {ParameterType} for delegate {DelegateMethod}.", i, paramType.Name, method.Name);
                throw; // Re-throw the exception to allow upstream handling.
            }
        }

        return parameters;
    }
}

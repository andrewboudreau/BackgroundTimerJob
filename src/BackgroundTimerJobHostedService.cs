namespace BackgroundTimerJob;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

public class BackgroundTimerJobHostedService : BackgroundService
{
    private readonly TimeSpan interval;
    private readonly IServiceProvider serviceProvider;
    private readonly Delegate jobDelegate;
    private readonly ILogger<BackgroundTimerJobHostedService>? logger;

    // Use an int flag for concurrency control (0 = not running, 1 = running)

    private int isRunning = 0;

    public BackgroundTimerJobHostedService(TimeSpan interval, IServiceProvider serviceProvider, Delegate jobDelegate)
    {
        this.interval = interval;
        this.serviceProvider = serviceProvider;
        this.jobDelegate = jobDelegate;
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
                break;
            }

            // Prevent overlapping executions.
            if (Interlocked.CompareExchange(ref isRunning, 1, 0) == 1)
            {
                logger.LogInformation("Timer job is already running; skipping this interval.");
                continue;
            }

            try
            {
                // Create a new scope for each job run.
                using var scope = serviceProvider.CreateScope();

                // Create a cancellation token that cancels after 1 minute (in addition to host shutdown).
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                cts.CancelAfter(TimeSpan.FromMinutes(1));
                CancellationToken jobCancellationToken = cts.Token;

                // Resolve parameters for the delegate from the scope.
                object[] parameters = ResolveDelegateParameters(jobDelegate, scope.ServiceProvider, jobCancellationToken);

                // Invoke the delegate. It is expected to return a Task.
                object result = jobDelegate.DynamicInvoke(parameters);
                if (result is Task task)
                {
                    // Await the task so that exceptions propagate.
                    await task;
                }
                else
                {
                    logger.LogWarning("The timer job delegate did not return a Task.");
                }
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning("Timer job execution timed out after 1 minute.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during timer job execution.");
            }
            finally
            {
                Interlocked.Exchange(ref isRunning, 0);
            }
        }
    }

    /// <summary>
    /// Resolves the parameters for the delegate by checking each parameter’s type.
    /// If the parameter is a CancellationToken, it supplies the provided cancellationToken.
    /// Otherwise, it resolves the parameter from the service provider.
    /// </summary>
    private object[] ResolveDelegateParameters(Delegate jobDelegate, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        MethodInfo method = jobDelegate.Method;
        ParameterInfo[] paramInfos = method.GetParameters();
        var parameters = new object[paramInfos.Length];

        for (int i = 0; i < paramInfos.Length; i++)
        {
            Type paramType = paramInfos[i].ParameterType;
            if (paramType == typeof(CancellationToken))
            {
                parameters[i] = cancellationToken;
            }
            else
            {
                parameters[i] = serviceProvider.GetRequiredService(paramType);
            }
        }

        return parameters;
    }
}

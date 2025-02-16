# BackgroundTimerJobs

A simple and flexible timer job background service for ASP.NET Core. This package provides a way to create background service that runs a delegate on a configurable interval. It uses dependency injection to resolve parameters and supports a configurable timeout per job execution.

## Features

- **Easy registration:** Use the `AddTimerJob` extension method to register your timer job.
- **Configurable intervals:** Set the execution interval and timeout.
- **Delegate-based jobs:** Define your job as a delegate with dependencies resolved from DI.

## Installation

Install the package via NuGet:

```bash
dotnet add package BackgroundTimerJobs
```


## Example
```csharp

// Example: a timer job that runs every 5 minutes.
builder.Services.AddTimerJob(
    TimeSpan.FromMinutes(5),
    async (IMediator mediator, IMyDbContext dbContext, CancellationToken cancellationToken) =>
    {
        await mediator.Send(new MyBackgroundJobRequest(), cancellationToken);
        // Additional job logic...
    });
```

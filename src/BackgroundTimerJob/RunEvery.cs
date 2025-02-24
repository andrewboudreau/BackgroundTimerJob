namespace BackgroundTimerJob;

public static class RunEvery
{
    public static TimeSpan OneMinute => TimeSpan.FromMinutes(1);
    public static TimeSpan FiveMinutes => TimeSpan.FromMinutes(5);
    public static TimeSpan FifteenMinutes => TimeSpan.FromMinutes(15);
    public static TimeSpan ThirtyMinutes => TimeSpan.FromMinutes(30);
    public static TimeSpan OneHour => TimeSpan.FromHours(1);
    public static TimeSpan TwoHours => TimeSpan.FromHours(2);
    public static TimeSpan FourHours => TimeSpan.FromHours(4);
    public static TimeSpan EightHours => TimeSpan.FromHours(8);
    public static TimeSpan TwelveHours => TimeSpan.FromHours(12);
    public static TimeSpan OneDay => TimeSpan.FromDays(1);
}

public static class TimeoutAfter
{
    public static TimeSpan TenSeconds => TimeSpan.FromSeconds(10);
    public static TimeSpan ThirtySeconds => TimeSpan.FromSeconds(30);
    public static TimeSpan OneMinute => TimeSpan.FromMinutes(1);
}
namespace QbitGluetunPortSync.Service.Configs;

public class TimingsConfig
{
    public const string SectionName = "Timings";

    public int InitialDelaySeconds { get; set; } = 5;
    public int CheckIntervalSeconds { get; set; } = 60;
    public int ErrorIntervalSeconds { get; set; } = 5;
}

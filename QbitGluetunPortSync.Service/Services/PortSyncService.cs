using Microsoft.Extensions.Options;
using QbitGluetunPortSync.Service.Configs;
using QbitGluetunPortSync.Service.HttpClients;

namespace QbitGluetunPortSync.Service.Services;

public class PortSyncService : BackgroundService
{
    private readonly GluetunClient _gluetun;
    private readonly QbitClient _qbittorrent;
    private readonly TimingsConfig _timings;
    private readonly QbittorrentConfig _qbitConfig;
    private readonly GluetunConfig _gluetunConfig;
    private readonly ILogger<PortSyncService> _logger;

    public PortSyncService(
        GluetunClient gluetun,
        QbitClient qbittorrent,
        IOptions<TimingsConfig> timingsConfig,
        IOptions<QbittorrentConfig> qbittorrentConfig,
        IOptions<GluetunConfig> gluetunConfig,
        ILogger<PortSyncService> logger)
    {
        _gluetun = gluetun;
        _qbittorrent = qbittorrent;
        _timings = timingsConfig.Value;
        _qbitConfig = qbittorrentConfig.Value;
        _gluetunConfig = gluetunConfig.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Port sync service started.");
        _logger.LogInformation("qBittorrent Config: {Config}", _qbitConfig.LogConfig);
        _logger.LogInformation("Gluetun Config: {Config}", _gluetunConfig.LogConfig);
        if (_timings.InitialDelaySeconds > 0)
            await Task.Delay(TimeSpan.FromSeconds(_timings.InitialDelaySeconds), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var currentPort = await _qbittorrent.GetListenPortAsync(stoppingToken);
                var forwardedPort = await _gluetun.GetForwardedPortAsync(stoppingToken);

                if (forwardedPort <= 0)
                {
                    _logger.LogWarning("No forwarded port retrieved from Gluetun: {Port}", forwardedPort);
                    await Task.Delay(TimeSpan.FromSeconds(_timings.ErrorIntervalSeconds), stoppingToken);
                    continue;
                }

                if (currentPort is null)
                {
                    _logger.LogWarning("Unable to read current qBittorrent listen port.");
                    await Task.Delay(TimeSpan.FromSeconds(_timings.ErrorIntervalSeconds), stoppingToken);
                    continue;
                }

                if (forwardedPort != currentPort.Value)
                {
                    var ok = await _qbittorrent.UpdateListenPortAsync(forwardedPort, stoppingToken);
                    if (!ok)
                    {
                        _logger.LogWarning("Failed to update qBittorrent listen port to {Port}", forwardedPort);
                        await Task.Delay(TimeSpan.FromSeconds(_timings.ErrorIntervalSeconds), stoppingToken);
                        continue;
                    }

                    _logger.LogInformation("Updated qBittorrent listen port: {OldPort} -> {NewPort}", currentPort, forwardedPort);
                }

                await Task.Delay(TimeSpan.FromSeconds(_timings.CheckIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Port sync failed");
                await Task.Delay(TimeSpan.FromSeconds(_timings.ErrorIntervalSeconds), stoppingToken);
            }
        }
    }
}

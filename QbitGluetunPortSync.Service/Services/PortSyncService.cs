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

        int currentQbitPort;
        try
        {
            currentQbitPort = await _qbittorrent.GetListenPortAsync(stoppingToken)
                ?? throw new InvalidOperationException("Unable to read qBittorrent listen port at startup.");

            _logger.LogInformation("Initial qBittorrent listen port: {Port}", currentQbitPort);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogCritical(ex, "Startup failed: cannot determine qBittorrent listen port");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            bool portChanged = false;
            try
            {
                var forwardedPort = await _gluetun.GetForwardedPortAsync(stoppingToken);

                if (forwardedPort <= 0)
                {
                    _logger.LogWarning("Invalid Gluetun forwarded port: {Port}", forwardedPort);
                    await ErrorBackoff(stoppingToken);
                    continue;
                }

                if (forwardedPort != currentQbitPort)
                {
                    var ok = await _qbittorrent.UpdateListenPortAsync(forwardedPort, stoppingToken);
                    if (!ok)
                    {
                        _logger.LogWarning("Failed to update qBittorrent listen port to {Port}", forwardedPort);
                        await ErrorBackoff(stoppingToken);
                        continue;
                    }
                    _logger.LogInformation("Updated qBittorrent listen port: {OldPort} -> {NewPort}", currentQbitPort, forwardedPort);
                    portChanged = true;
                    currentQbitPort = forwardedPort;
                }
                if (!portChanged)
                {
                    _logger.LogInformation("Forwarding port unchanged from current port: {Port}. Sleeping until next interval.", forwardedPort);
                }

                await NextInterval(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Port sync failed");
                await ErrorBackoff(stoppingToken);
            }
        }
    }

    private Task NextInterval(CancellationToken stoppingToken) =>
        Task.Delay(TimeSpan.FromSeconds(_timings.CheckIntervalSeconds), stoppingToken);

    private Task ErrorBackoff(CancellationToken stoppingToken) =>
        Task.Delay(TimeSpan.FromSeconds(_timings.ErrorIntervalSeconds), stoppingToken);
}

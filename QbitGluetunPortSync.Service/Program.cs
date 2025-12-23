using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using QbitGluetunPortSync.Service.Configs;
using QbitGluetunPortSync.Service.HttpClients;
using QbitGluetunPortSync.Service.Services;

var builder = Host.CreateApplicationBuilder(args);
var services = builder.Services;
var config = builder.Configuration;

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss.fff] ";
    options.UseUtcTimestamp = true;
});

services.AddOptions<GluetunConfig>().Bind(config.GetSection(GluetunConfig.SectionName));
services.AddOptions<QbittorrentConfig>().Bind(config.GetSection(QbittorrentConfig.SectionName));
services.AddOptions<TimingsConfig>().Bind(config.GetSection(TimingsConfig.SectionName));

services.ConfigureHttpClientDefaults(builder =>
{
    builder.AddPolicyHandler(HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                )
            );
});
services.AddHttpClient<GluetunClient>(
    (services, gluetunClient) =>
    {
        var gluetunConfig = services.GetRequiredService<IOptions<GluetunConfig>>().Value;

        gluetunClient.BaseAddress = gluetunConfig.BaseUri;
    });

services.AddHttpClient<QbitClient>(
    (services, qbitClient) =>
    {
        var qbitConfig = services.GetRequiredService<IOptions<QbittorrentConfig>>().Value;

        qbitClient.BaseAddress = qbitConfig.BaseUri;
    }).ConfigurePrimaryHttpMessageHandler(() =>
    {
        return new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = new System.Net.CookieContainer(),
            AllowAutoRedirect = true,
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
        };
    });

builder.Services.AddHostedService<PortSyncService>();
var host = builder.Build();
host.Run();

using Microsoft.Extensions.Options;
using QbitGluetunPortSync.Service.Configs;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QbitGluetunPortSync.Service.HttpClients;

public class QbitClient
{
    private readonly HttpClient _client;
    private readonly QbittorrentConfig _qbitConfig;

    public QbitClient(HttpClient client, IOptions<QbittorrentConfig> qbitConfig)
    {
        _client = client;
        _qbitConfig = qbitConfig.Value;
    }

    public async Task<bool> UpdateListenPortAsync(int newPort, CancellationToken ct)
    {
        if (!await LoginAsync(ct))
            return false;

        var setPrefs = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>(
                "json", $@"{{""listen_port"":{newPort}}}")
        });

        var setResp = await _client.PostAsync("/api/v2/app/setPreferences", setPrefs, ct);
        if (!setResp.IsSuccessStatusCode)
            return false;

        var currentPort = await GetListenPortInternalAsync(ct);

        await LogoutAsync(ct);

        return currentPort == newPort;
    }

    public async Task<int?> GetListenPortAsync(CancellationToken ct)
    {
        if (!await LoginAsync(ct))
            return null;

        var port = await GetListenPortInternalAsync(ct);

        await LogoutAsync(ct);

        return port;
    }

    private async Task<bool> LoginAsync(CancellationToken ct)
    {
        var login = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("username", _qbitConfig.Username),
            new KeyValuePair<string,string>("password", _qbitConfig.Password)
        });

        var resp = await _client.PostAsync("/api/v2/auth/login", login, ct);
        return resp.IsSuccessStatusCode;
    }

    private async Task LogoutAsync(CancellationToken ct)
    {
        await _client.PostAsync("/api/v2/auth/logout", null, ct);
    }

    private async Task<int?> GetListenPortInternalAsync(CancellationToken ct)
    {
        var prefResp = await _client.GetAsync("/api/v2/app/preferences", ct);
        if (!prefResp.IsSuccessStatusCode)
            return null;

        var prefs = await JsonSerializer.DeserializeAsync<Prefs>(
            await prefResp.Content.ReadAsStreamAsync(ct),
            cancellationToken: ct);

        return prefs?.ListenPort;
    }

    private sealed class Prefs
    {
        [JsonPropertyName("listen_port")]
        public int ListenPort { get; set; }
    }
}

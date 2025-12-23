using Microsoft.Extensions.Options;
using QbitGluetunPortSync.Service.Configs;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QbitGluetunPortSync.Service.HttpClients;

public class GluetunClient
{
    private readonly HttpClient _client;
    private readonly GluetunConfig _config;

    public GluetunClient(HttpClient client, IOptions<GluetunConfig> gluetunConfig)
    {
        _client = client;
        _config = gluetunConfig.Value;

        ConfigureAuthentication();
    }

    public async Task<int> GetForwardedPortAsync(CancellationToken ct)
    {
        using var response = await _client.GetAsync("/v1/portforward", ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        var data = await JsonSerializer.DeserializeAsync<GluetunPortResponse>(
            stream,
            cancellationToken: ct);

        return data?.Port ?? 0;
    }

    private void ConfigureAuthentication()
    {
        // Prefer API key if present
        if (!string.IsNullOrWhiteSpace(_config.ApiKey))
        {
            _client.DefaultRequestHeaders.Remove("X-API-Key");
            _client.DefaultRequestHeaders.Add("X-API-Key", _config.ApiKey);
            return;
        }

        // Fall back to basic auth
        if (!string.IsNullOrWhiteSpace(_config.Username) &&
            !string.IsNullOrWhiteSpace(_config.Password))
        {
            var credentials = $"{_config.Username}:{_config.Password}";
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));

            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", base64);
        }
    }

    private sealed class GluetunPortResponse
    {
        [JsonPropertyName("port")]
        public int Port { get; set; }
    }
}

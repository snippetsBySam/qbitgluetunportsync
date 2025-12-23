namespace QbitGluetunPortSync.Service.Configs;

public class QbittorrentConfig
{
    public const string SectionName = "Qbittorrent";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 8080;
    public bool UseHttps { get; set; } = false;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public Uri BaseUri => new UriBuilder
    {
        Scheme = UseHttps ? Uri.UriSchemeHttps : Uri.UriSchemeHttp,
        Host = Host,
        Port = Port
    }.Uri;
    public string LogConfig =>
        $"Host={Host}, Port={Port}, UseHttps={UseHttps}, Username={Username}";
}

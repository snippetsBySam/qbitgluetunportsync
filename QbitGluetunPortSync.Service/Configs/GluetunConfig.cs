namespace QbitGluetunPortSync.Service.Configs;

public class GluetunConfig
{
    public const string SectionName = "Gluetun";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 8000;
    public bool UseHttps { get; set; } = false;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public Uri BaseUri => new UriBuilder
    {
        Scheme = UseHttps ? Uri.UriSchemeHttps : Uri.UriSchemeHttp,
        Host = Host,
        Port = Port
    }.Uri;
    public string LogConfig =>
        $"Host={Host}, Port={Port}, UseHttps={UseHttps}, Username={Username}";
}

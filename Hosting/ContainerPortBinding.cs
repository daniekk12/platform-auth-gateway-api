namespace Platform.Auth.Gateway.Api.Hosting;

internal static class ContainerPortBinding
{
    internal static void ApplyIfConfigured()
    {
        var port = Environment.GetEnvironmentVariable("PORT");
        if (string.IsNullOrWhiteSpace(port))
        {
            return;
        }

        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", $"http://0.0.0.0:{port}");
    }
}

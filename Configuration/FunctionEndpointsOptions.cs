namespace Platform.Auth.Gateway.Api.Configuration;

public sealed class FunctionEndpointsOptions
{
    public const string SectionName = "FunctionEndpoints";

    public string SignupUrl { get; set; } = string.Empty;

    public string LoginUrl { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 30;
}

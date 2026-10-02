using Microsoft.AspNetCore.Mvc.Testing;

namespace Platform.Auth.Gateway.Api.Tests;

public sealed class GatewayWebTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public GatewayWebTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("FunctionInvocation:ApiKey", "test-internal-key");
            builder.UseSetting("FunctionEndpoints:SignupUrl", "https://localhost:5001");
            builder.UseSetting("FunctionEndpoints:LoginUrl", "https://localhost:5002");
        }).CreateClient();
    }

    [Fact]
    public async Task Health_returns_ok()
    {
        var response = await _client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
    }
}

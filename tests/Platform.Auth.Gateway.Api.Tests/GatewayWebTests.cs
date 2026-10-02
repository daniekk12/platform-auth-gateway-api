using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Platform.Auth.Gateway.Api.Tests;

public sealed class GatewayWebTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public GatewayWebTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(AuthControllerValidationTests.ConfigureRequiredSettings).CreateClient();
    }

    [Fact]
    public async Task Health_returns_ok()
    {
        var response = await _client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
    }
}

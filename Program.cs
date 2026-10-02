using Platform.Auth.Gateway.Api.Configuration;
using Platform.Auth.Gateway.Api.Extensions;
using Platform.Auth.Gateway.Api.Filters;
using Platform.Auth.Gateway.Api.Infrastructure;
using Platform.Auth.Gateway.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGatewayConfiguration(builder.Configuration);
builder.Services.AddGatewayServices();
builder.Services.AddGatewayCors(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddScoped<AuthResponseCacheFilter>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks();

var openApiEnabled = builder.Configuration.GetSection(GatewayOpenApiOptions.SectionName).Get<GatewayOpenApiOptions>()?.Enabled ?? false;
if (openApiEnabled)
{
    builder.Services.AddOpenApi();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler();
    app.UseHsts();
}

app.UseMiddleware<SecurityHeadersMiddleware>();

var corsOrigins = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>()?.AllowedOrigins ?? [];
if (corsOrigins.Length > 0)
{
    app.UseCors(CorsOptions.SectionName);
}

if (openApiEnabled)
{
    app.MapOpenApi();
}

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;

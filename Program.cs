using Platform.Auth.Gateway.Api.Configuration;
using Platform.Auth.Gateway.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGatewayConfiguration(builder.Configuration);
builder.Services.AddGatewayServices();
builder.Services.AddGatewayCors(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddHealthChecks();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var corsOrigins = builder.Configuration.GetSection(CorsOptions.SectionName).Get<string[]>() ?? [];
if (corsOrigins.Length > 0)
{
    app.UseCors(CorsOptions.SectionName);
}

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;

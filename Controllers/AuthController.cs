using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Platform.Auth.Gateway.Api.Contracts;
using Platform.Auth.Gateway.Api.Filters;
using Platform.Auth.Gateway.Api.Services;

namespace Platform.Auth.Gateway.Api.Controllers;

[ApiController]
[Route("auth")]
[ServiceFilter(typeof(AuthResponseCacheFilter))]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthFunctionClient _authFunctionClient;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthFunctionClient authFunctionClient, ILogger<AuthController> logger)
    {
        _authFunctionClient = authFunctionClient;
        _logger = logger;
    }

    [HttpPost("signup")]
    [ProducesResponseType(typeof(SignupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public async Task<IActionResult> SignupAsync(
        [FromBody] SignupRequest? request,
        CancellationToken cancellationToken)
    {
        if (!TryValidateRequest(request, out var validationProblem))
        {
            return validationProblem!;
        }

        _logger.LogInformation("Signup request received for correlation {CorrelationId}", HttpContext.TraceIdentifier);

        var result = await _authFunctionClient.SignupAsync(request!, cancellationToken);
        return FunctionCallResultMapper.ToActionResult(result);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public async Task<IActionResult> LoginAsync(
        [FromBody] LoginRequest? request,
        CancellationToken cancellationToken)
    {
        if (!TryValidateRequest(request, out var validationProblem))
        {
            return validationProblem!;
        }

        _logger.LogInformation("Login request received for correlation {CorrelationId}", HttpContext.TraceIdentifier);

        var result = await _authFunctionClient.LoginAsync(request!, cancellationToken);
        return FunctionCallResultMapper.ToActionResult(result);
    }

    private bool TryValidateRequest<T>(T? request, out IActionResult? problem)
        where T : class
    {
        problem = null;

        if (request is null)
        {
            problem = ValidationProblem(CreateValidationProblem("Request body is required."));
            return false;
        }

        var validationContext = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, validationContext, validationResults, validateAllProperties: true))
        {
            problem = ValidationProblem(CreateValidationProblemFromResults(validationResults));
            return false;
        }

        return true;
    }

    private static ValidationProblemDetails CreateValidationProblem(string detail) =>
        new(new Dictionary<string, string[]>
        {
            [""] = [detail]
        })
        {
            Title = "Validation failed",
            Status = StatusCodes.Status400BadRequest,
            Detail = detail
        };

    private static ValidationProblemDetails CreateValidationProblemFromResults(IEnumerable<ValidationResult> results)
    {
        var errors = new Dictionary<string, string[]>();
        foreach (var result in results)
        {
            var key = result.MemberNames.FirstOrDefault() ?? string.Empty;
            errors[key] = [result.ErrorMessage ?? "Validation failed."];
        }

        return new ValidationProblemDetails(errors)
        {
            Title = "Validation failed",
            Status = StatusCodes.Status400BadRequest
        };
    }
}

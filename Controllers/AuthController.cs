using Microsoft.AspNetCore.Mvc;
using Platform.Auth.Gateway.Api.Contracts;
using Platform.Auth.Gateway.Api.Services;

namespace Platform.Auth.Gateway.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthFunctionClient _authFunctionClient;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthFunctionClient authFunctionClient, ILogger<AuthController> logger)
    {
        _authFunctionClient = authFunctionClient;
        _logger = logger;
    }

    /// <summary>Proxies signup to the signup function.</summary>
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
        if (!TryValidateAuthRequest(request, out var validationProblem))
        {
            return validationProblem!;
        }

        _logger.LogInformation("Signup request received");

        var result = await _authFunctionClient.SignupAsync(request!, cancellationToken);
        return FunctionCallResultMapper.ToActionResult(result);
    }

    /// <summary>Proxies login to the login function.</summary>
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
        if (!TryValidateAuthRequest(request, out var validationProblem))
        {
            return validationProblem!;
        }

        _logger.LogInformation("Login request received");

        var result = await _authFunctionClient.LoginAsync(request!, cancellationToken);
        return FunctionCallResultMapper.ToActionResult(result);
    }

    private static bool TryValidateAuthRequest<T>(T? request, out IActionResult? problem)
        where T : class
    {
        problem = null;

        if (request is null)
        {
            problem = new BadRequestObjectResult(CreateValidationProblem("Request body is required."));
            return false;
        }

        string? email = request switch
        {
            SignupRequest signup => signup.Email,
            LoginRequest login => login.Email,
            _ => null
        };

        string? password = request switch
        {
            SignupRequest signup => signup.Password,
            LoginRequest login => login.Password,
            _ => null
        };

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            problem = new BadRequestObjectResult(CreateValidationProblem("Email and password are required."));
            return false;
        }

        return true;
    }

    private static object CreateValidationProblem(string detail) =>
        new
        {
            title = "Validation failed",
            status = StatusCodes.Status400BadRequest,
            detail
        };
}

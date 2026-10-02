using Platform.Auth.Gateway.Api.Contracts;

namespace Platform.Auth.Gateway.Api.Services;

public interface IAuthFunctionClient
{
    Task<FunctionCallResult<SignupResponse>> SignupAsync(
        SignupRequest request,
        CancellationToken cancellationToken);

    Task<FunctionCallResult<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken);
}

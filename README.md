# platform-auth-gateway-api

Public HTTPS entry point for Platform Auth. Routes authentication requests to Signup and Login **Functions** through `IAuthFunctionClient` (HTTPS in local development; endpoint URLs are configuration-only so a future Function Host can replace the target without architectural changes).

The Gateway does **not** contain signup or login business logic and does **not** reference function projects.

## Architecture

```text
Client
  ↓
Gateway (this repo)
  ↓
Signup Function / Login Function
```

Future:

```text
Client → Gateway → Function Host → Functions
```

## Routes

**Clients must call signup and login only through this gateway** (`POST /auth/signup` and `POST /auth/login`). Do not call the function ports directly from browsers or apps.

| Method | Path | Description |
|--------|------|-------------|
| POST | `/auth/signup` | Forwards to signup function (`POST {SignupUrl}/signup`) |
| POST | `/auth/login` | Forwards to login function (`POST {LoginUrl}/login`) |
| GET | `/health` | Gateway liveness |

## Configuration

Function endpoints use the **Options pattern** (`FunctionEndpointsOptions`). Environment variables override `appsettings` (ASP.NET Core default precedence).

| Setting | Environment variable (alias) | Nested configuration key |
|---------|------------------------------|---------------------------|
| Signup base URL | `SIGNUP_FUNCTION_URL` | `FunctionEndpoints__SignupUrl` |
| Login base URL | `LOGIN_FUNCTION_URL` | `FunctionEndpoints__LoginUrl` |
| HTTP timeout (seconds) | — | `FunctionEndpoints__TimeoutSeconds` |
| Internal invocation key (gateway → functions) | — | `FunctionInvocation__ApiKey` |

The gateway sends `X-Platform-Auth-Internal-Key` on every function call. The same value must be configured on signup and login functions (`FunctionInvocation:ApiKey`). Requests to `/signup` or `/login` without that header receive **403 Forbidden**.

`appsettings.json` ships with empty URLs. Local values belong in `appsettings.Development.json` (not for production) or in environment variables.

Example (PowerShell, session-scoped):

```powershell
$env:FunctionEndpoints__SignupUrl = "https://localhost:5001"
$env:FunctionEndpoints__LoginUrl = "https://localhost:5002"
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --launch-profile https
```

Trust the ASP.NET Core HTTPS development certificate once per machine (required for the gateway to call function URLs over HTTPS):

```powershell
dotnet dev-certs https --trust
```

Optional CORS (Development example in `appsettings.Development.json`):

```text
Cors__AllowedOrigins__0=http://localhost:4200
```

Do not commit `.env` / `.env.local` files (see `.gitignore`).

## Run locally

Start Signup and Login functions first, then:

```bash
dotnet run --launch-profile https
```

Gateway listens on **https://localhost:5000** (see `Properties/launchSettings.json`). HTTP is not enabled in the default launch profile.

## Tests

```bash
dotnet test tests/Platform.Auth.Gateway.Api.Tests/Platform.Auth.Gateway.Api.Tests.csproj
```

## Build

```bash
dotnet build
```

Standalone repository: no `.sln`, no project references to other Platform Auth components.

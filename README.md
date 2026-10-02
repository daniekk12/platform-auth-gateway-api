# platform-auth-gateway-api

Public HTTP entry point for Platform Auth. Routes authentication requests to Signup and Login **Functions** through `IAuthFunctionClient` (HTTP today; endpoint URLs are configuration-only so a future Function Host can replace the target without architectural changes).

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

| Method | Path | Description |
|--------|------|-------------|
| POST | `/auth/signup` | Forwards to signup function (`POST {SignupUrl}/signup`) |
| POST | `/auth/login` | Forwards to login function (`POST {LoginUrl}/login`) |
| GET | `/health` | Gateway liveness |

Swagger UI is enabled in the Development environment.

## Configuration

Function endpoints use the **Options pattern** (`FunctionEndpointsOptions`). Environment variables override `appsettings` (ASP.NET Core default precedence).

| Setting | Environment variable (alias) | Nested configuration key |
|---------|------------------------------|---------------------------|
| Signup base URL | `SIGNUP_FUNCTION_URL` | `FunctionEndpoints__SignupUrl` |
| Login base URL | `LOGIN_FUNCTION_URL` | `FunctionEndpoints__LoginUrl` |
| HTTP timeout (seconds) | — | `FunctionEndpoints__TimeoutSeconds` |

`appsettings.json` ships with empty URLs. Local values belong in `appsettings.Development.json` (not for production) or in environment variables.

Example (PowerShell, session-scoped):

```powershell
$env:FunctionEndpoints__SignupUrl = "http://localhost:5001"
$env:FunctionEndpoints__LoginUrl = "http://localhost:5002"
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --launch-profile http
```

Optional CORS (Development example in `appsettings.Development.json`):

```text
Cors__AllowedOrigins__0=http://localhost:4200
```

Do not commit `.env` / `.env.local` files (see `.gitignore`).

## Run locally

Start Signup and Login functions first, then:

```bash
dotnet run --launch-profile http
```

Gateway listens on port **5000** (see `Properties/launchSettings.json`).

## Tests

```bash
dotnet test tests/Platform.Auth.Gateway.Api.Tests/Platform.Auth.Gateway.Api.Tests.csproj
```

## Build

```bash
dotnet build
```

Standalone repository: no `.sln`, no project references to other Platform Auth components.

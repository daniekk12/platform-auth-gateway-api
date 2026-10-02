# platform-auth-gateway-api

Public HTTPS entry point for Platform Auth. Routes authentication requests to Signup and Login **Functions** through `IAuthFunctionClient`. Endpoint URLs and secrets are supplied only through configuration (no hardcoded production values in code).

The Gateway does **not** contain signup or login business logic and does **not** reference function projects.

## Architecture

```text
Client
  ↓
Gateway (this repo)
  ↓  X-Internal-Api-Key
Signup Function / Login Function
```

## Routes

**Clients must call signup and login only through this gateway** (`POST /auth/signup` and `POST /auth/login`).

| Method | Path | Description |
|--------|------|-------------|
| POST | `/auth/signup` | Forwards to signup function |
| POST | `/auth/login` | Forwards to login function |
| GET | `/health` | Gateway liveness (no downstream dependency) |

## Configuration

Configuration uses the ASP.NET Core options pattern. **Environment variables override** `appsettings` files. Use the `__` convention for nested keys.

Committed JSON contains **placeholders only** for secrets and production URLs. Missing required settings cause **startup failure** (`ValidateOnStart`).

### Required (production)

| Purpose | Environment variable | Nested key |
|---------|---------------------|------------|
| Signup function base URL (HTTPS in Production) | `SIGNUP_FUNCTION_URL` (alias) or `FunctionEndpoints__SignupUrl` | `FunctionEndpoints:SignupUrl` |
| Login function base URL (HTTPS in Production) | `LOGIN_FUNCTION_URL` (alias) or `FunctionEndpoints__LoginUrl` | `FunctionEndpoints:LoginUrl` |
| Internal API key (gateway → functions) | `FunctionInvocation__ApiKey` | `FunctionInvocation:ApiKey` |

The gateway sends header **`X-Internal-Api-Key`**. The same secret must be configured on signup and login functions.

### Optional

| Purpose | Default | Environment variable / key |
|---------|---------|---------------------------|
| Outbound HTTP timeout (seconds) | `30` | `FunctionEndpoints__TimeoutSeconds` |
| CORS allowed origins | none (CORS disabled) | `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, … |
| OpenAPI document (`/openapi/v1.json`) | `false` (Development only when `OpenApi:Enabled`) | `OpenApi__Enabled` |

### Secrets

- **`FunctionInvocation:ApiKey`** — treat as a secret. Never commit, log, or return in API responses.
- Passwords from client requests are validated but **never logged**.

### Local development

1. Trust the dev certificate (once): `dotnet dev-certs https --trust`
2. Configure the internal API key with **User Secrets** (not committed):

```bash
dotnet user-secrets set "FunctionInvocation:ApiKey" "<your-local-internal-api-key>"
```

3. Non-secret dev defaults (function HTTPS URLs, CORS, OpenAPI) live in `appsettings.Development.json`.
4. Run: `dotnet run --launch-profile https` (listens on `https://localhost:5000`).

Example overrides (PowerShell):

```powershell
$env:FunctionInvocation__ApiKey = "<your-local-internal-api-key>"
$env:FunctionEndpoints__SignupUrl = "https://localhost:5001"
$env:FunctionEndpoints__LoginUrl = "https://localhost:5002"
dotnet run --launch-profile https
```

### Production deployment

Set required variables in the hosting environment (examples):

```text
FunctionInvocation__ApiKey=<secret>
FunctionEndpoints__SignupUrl=https://<internal-signup-host>
FunctionEndpoints__LoginUrl=https://<internal-login-host>
Cors__AllowedOrigins__0=https://<your-frontend-origin>
OpenApi__Enabled=false
AllowedHosts__0=<your-public-hostname>
ASPNETCORE_ENVIRONMENT=Production
```

Do not commit `.env` / `.env.local` files (see `.gitignore`).

## Tests

```bash
dotnet test tests/Platform.Auth.Gateway.Api.Tests/Platform.Auth.Gateway.Api.Tests.csproj
```

## Build & publish

```bash
dotnet build
dotnet publish -c Release
```

Standalone repository: no `.sln`, no project references to other Platform Auth components.

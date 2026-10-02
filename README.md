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

## Deploy to Render (free tier — learning / demo)

This is a **free-tier learning deployment**, not a production reliability guarantee. Render free Web Services **sleep after inactivity**, have **cold starts**, and **limited CPU/RAM**.

### Recommended approach

**GitHub Actions is the source of truth** for environment variables. See [`.github/RENDER_GITHUB_CONFIG.md`](.github/RENDER_GITHUB_CONFIG.md) for required Secrets/Variables and one-time Render service setup.

The deploy workflow syncs env vars via Render API (`PUT /v1/services/{serviceId}/env-vars`), triggers deploy (`POST …/deploys`), and verifies `PUBLIC_HEALTH_URL`.

### Deployment order (all three services)

1. Deploy **Signup Function** (this repo’s siblings: `platform-auth-signup-func`).
2. Deploy **Login Function** (`platform-auth-login-func`).
3. Note each service’s public **`https://…onrender.com`** base URL (Render assigns these).
4. Deploy **Gateway** with function URLs and the shared internal API key.
5. Test: `GET https://<gateway-host>/health`, then `POST /auth/signup` and `/auth/login` through the gateway only.

### Render Web Service settings (Gateway)

| Setting | Value |
|---------|--------|
| Environment | Docker |
| Dockerfile path | `./Dockerfile` |
| Health check path | `/health` |
| Instance type | Free |

### Gateway environment variables (GitHub Actions)

Configure these in GitHub (**Settings → Secrets and variables → Actions**), not in the Render dashboard. Full names and setup: [`.github/RENDER_GITHUB_CONFIG.md`](.github/RENDER_GITHUB_CONFIG.md).

**Secrets:**

| Key | Description |
|-----|-------------|
| `RENDER_API_KEY` | Authenticates Render API calls from the deploy workflow |
| `FunctionInvocation__ApiKey` | Shared secret; same value on signup and login services |

**Variables:**

| Key | Example shape |
|-----|----------------|
| `RENDER_SERVICE_ID` | `srv-…` (this gateway service) |
| `PUBLIC_HEALTH_URL` | `https://<your-gateway-service>/health` |
| `FunctionEndpoints__SignupUrl` | `https://<your-signup-service>.onrender.com` |
| `FunctionEndpoints__LoginUrl` | `https://<your-login-service>.onrender.com` |
| `FunctionEndpoints__TimeoutSeconds` | `30` |
| `Cors__AllowedOrigins__0` | Your frontend origin (optional) |
| `AllowedHosts__0` | `<your-gateway-service>.onrender.com` |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `OpenApi__Enabled` | `false` |

Aliases `SIGNUP_FUNCTION_URL` / `LOGIN_FUNCTION_URL` are also supported for the function base URLs.

Outbound calls use **HTTPS** to the configured function URLs. The gateway sends **`X-Internal-Api-Key`**. Free-tier function URLs are **public**; the API key is required but is **not** a substitute for private networking.

Render sets **`PORT`**; the container binds **`0.0.0.0`** via `Hosting/ContainerPortBinding.cs`.

### Docker (local)

```bash
docker build -t platform-auth-gateway .
docker run --rm -p 8080:8080 \
  -e PORT=8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e FunctionInvocation__ApiKey="<your-local-internal-api-key>" \
  -e FunctionEndpoints__SignupUrl="https://localhost:5001" \
  -e FunctionEndpoints__LoginUrl="https://localhost:5002" \
  platform-auth-gateway
curl http://localhost:8080/health
```

### Verify deployment

```bash
curl -fsS "https://<your-gateway-host>/health"
```

### GitHub Actions

| Workflow | Purpose |
|----------|---------|
| `.github/workflows/ci.yml` | Restore, Release build, tests on PRs and pushes to `main` |
| `.github/workflows/render-deploy.yml` | After successful CI on `main`: sync env vars to Render API, deploy, verify health |

Create GitHub Environment **`production`** (required). Restrict it to the `main` branch; add reviewers if desired. Deploy does not run from pull requests.

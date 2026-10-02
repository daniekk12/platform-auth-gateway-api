# GitHub → Render configuration (source of truth)

Environment variables for each Render Web Service are defined in **GitHub Actions** (Secrets + Variables) and applied by `.github/workflows/render-deploy.yml` using the [Render Public API](https://api-docs.render.com/reference/introduction):

1. `GET /v1/services/{serviceId}/env-vars` — read existing vars (preserves `PORT` and `RENDER_*`)
2. `PUT /v1/services/{serviceId}/env-vars` — replace with merged set
3. `POST /v1/services/{serviceId}/deploys` — deploy after sync
4. Poll deploy until `live`, then `GET` `PUBLIC_HEALTH_URL`

Auth: `Authorization: Bearer {RENDER_API_KEY}` ([BearerAuth](https://api-docs.render.com/reference/update-env-vars-for-service)).

## One-time: create Render services

GitHub Actions **cannot** create a service or discover its ID until the service exists.

1. In [Render Dashboard](https://dashboard.render.com), create a **Web Service** (Docker, `./Dockerfile`, health `/health`, free tier).
2. Connect the matching GitHub repo and branch `main` (auto-deploy may stay on; this workflow still triggers explicit deploys after env sync).
3. Copy the **Service ID** (`srv-…`) from **Settings → General**.

Repeat for signup, login, and gateway (deploy signup → login → gateway).

Do **not** rely on the Render dashboard for application env vars after this setup—configure them in GitHub below.

## GitHub Environment

Create environment **`production`** (Settings → Environments). Restrict deployment to `main` and require reviewers if desired.

Deploy runs only when:

- CI on **`main`** succeeds (`workflow_run`), or
- Manual **`workflow_dispatch`** (use only on trusted `main` checkout).

Pull request CI does **not** deploy (CI runs on the PR branch, not `main`).

## Secrets (this repository — gateway)

| Name | Purpose |
|------|---------|
| `RENDER_API_KEY` | Render API key ([Account Settings → API Keys](https://dashboard.render.com/u/settings#api-keys)) |
| `FunctionInvocation__ApiKey` | Shared internal key sent to signup/login (`X-Internal-Api-Key`) |

## Variables (this repository — gateway)

| Name | Purpose | Example shape |
|------|---------|----------------|
| `RENDER_SERVICE_ID` | This gateway’s Render service ID | `srv-…` |
| `PUBLIC_HEALTH_URL` | Full health URL for post-deploy check | `https://<gateway-host>/health` |
| `ASPNETCORE_ENVIRONMENT` | Host environment | `Production` |
| `FunctionEndpoints__SignupUrl` | Signup function base URL (HTTPS) | `https://<signup-host>` |
| `FunctionEndpoints__LoginUrl` | Login function base URL (HTTPS) | `https://<login-host>` |
| `FunctionEndpoints__TimeoutSeconds` | Outbound timeout | `30` |
| `AllowedHosts__0` | Allowed host header | `<gateway-host>.onrender.com` |
| `OpenApi__Enabled` | OpenAPI (dev-only in app code) | `false` |
| `Cors__AllowedOrigins__0` | Optional CORS origin | omit or set frontend URL |

Aliases `SIGNUP_FUNCTION_URL` / `LOGIN_FUNCTION_URL` are supported by the **application** but the sync script uses `FunctionEndpoints__*` keys above.

## Signup / login repositories

Same pattern with fewer variables—see `.github/RENDER_GITHUB_CONFIG.md` in each function repo.

## Shared internal API key

Use the **same** `FunctionInvocation__ApiKey` secret value in all three GitHub repositories (signup, login, gateway).

## Free tier

Services sleep when idle, cold starts are slow, and resources are limited. This is a **demo/learning** setup, not a production SLA.

#!/usr/bin/env python3
"""Sync GitHub-managed env vars to Render, trigger deploy, verify health.

Render API (official):
  GET  /v1/services/{serviceId}/env-vars
  PUT  /v1/services/{serviceId}/env-vars  (replaces full set; merge existing first)
  POST /v1/services/{serviceId}/deploys
  GET  /v1/services/{serviceId}/deploys/{deployId}
"""

from __future__ import annotations

import json
import os
import sys
import time
import urllib.error
import urllib.parse
import urllib.request

API_BASE = "https://api.render.com/v1"

# Keys GitHub Actions supplies for this repository (complete intended application set).
MANAGED_KEYS = [
    "ASPNETCORE_ENVIRONMENT",
    "FunctionInvocation__ApiKey",
    "FunctionEndpoints__SignupUrl",
    "FunctionEndpoints__LoginUrl",
    "FunctionEndpoints__TimeoutSeconds",
    "AllowedHosts__0",
    "OpenApi__Enabled",
    "Cors__AllowedOrigins__0",
]

OPTIONAL_KEYS = frozenset({"Cors__AllowedOrigins__0"})

# Preserve Render/platform keys when merging before PUT.
PRESERVE_PREFIXES = ("PORT", "RENDER_")


def _request(
    method: str,
    path: str,
    api_key: str,
    body: object | None = None,
) -> tuple[int, str]:
    url = f"{API_BASE}{path}"
    data = None
    headers = {
        "Authorization": f"Bearer {api_key}",
        "Accept": "application/json",
    }
    if body is not None:
        data = json.dumps(body).encode("utf-8")
        headers["Content-Type"] = "application/json"

    req = urllib.request.Request(url, data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(req, timeout=120) as resp:
            return resp.status, resp.read().decode("utf-8")
    except urllib.error.HTTPError as exc:
        payload = exc.read().decode("utf-8", errors="replace")
        return exc.code, payload


def _list_env_vars(service_id: str, api_key: str) -> dict[str, str]:
    merged: dict[str, str] = {}
    cursor: str | None = None
    while True:
        query = "?limit=100"
        if cursor:
            query += f"&cursor={urllib.parse.quote(cursor)}"
        status, text = _request("GET", f"/services/{service_id}/env-vars{query}", api_key)
        if status != 200:
            print(f"Failed to list env vars (HTTP {status}). Response logged without secret values.")
            print(text[:2000])
            sys.exit(1)
        page = json.loads(text)
        if not page:
            break
        for item in page:
            env = item.get("envVar") or {}
            key = env.get("key")
            value = env.get("value")
            if key:
                merged[key] = value or ""
            cursor = item.get("cursor")
        if not cursor or len(page) < 100:
            break
    return merged


def _build_managed_env() -> dict[str, str]:
    result: dict[str, str] = {}
    for key in MANAGED_KEYS:
        value = os.environ.get(key)
        if value is None or value == "":
            if key in OPTIONAL_KEYS:
                continue
            print(f"Missing required configuration environment variable: {key}")
            sys.exit(1)
        result[key] = value
    return result


def _merge_env(existing: dict[str, str], managed: dict[str, str]) -> dict[str, str]:
    preserved = {
        k: v
        for k, v in existing.items()
        if k not in managed and (k.startswith(PRESERVE_PREFIXES) or k not in MANAGED_KEYS)
    }
    merged = {**preserved, **managed}
    return merged


def _put_env_vars(service_id: str, api_key: str, env: dict[str, str]) -> None:
    body = [{"key": k, "value": v} for k, v in sorted(env.items())]
    status, text = _request("PUT", f"/services/{service_id}/env-vars", api_key, body)
    if status != 200:
        print(f"Failed to update env vars (HTTP {status}).")
        print(text[:2000])
        sys.exit(1)
    print(f"Synchronized {len(body)} environment variables to Render.")


def _trigger_deploy(service_id: str, api_key: str) -> str:
    status, text = _request(
        "POST",
        f"/services/{service_id}/deploys",
        api_key,
        {"clearCache": "do_not_clear"},
    )
    if status not in (200, 201, 202):
        print(f"Failed to trigger deploy (HTTP {status}).")
        print(text[:2000])
        sys.exit(1)
    payload = json.loads(text)
    deploy_id = payload.get("id")
    if not deploy_id:
        deploy = payload.get("deploy") or {}
        deploy_id = deploy.get("id")
    if not deploy_id:
        print("Deploy triggered but deploy ID was not returned.")
        sys.exit(1)
    print(f"Deploy triggered (id={deploy_id}).")
    return deploy_id


def _wait_for_deploy(service_id: str, deploy_id: str, api_key: str, timeout_sec: int = 900) -> None:
    deadline = time.time() + timeout_sec
    while time.time() < deadline:
        status, text = _request("GET", f"/services/{service_id}/deploys/{deploy_id}", api_key)
        if status != 200:
            print(f"Failed to poll deploy (HTTP {status}).")
            print(text[:2000])
            sys.exit(1)
        deploy = json.loads(text)
        state = (deploy.get("status") or deploy.get("deploy", {}).get("status") or "").lower()
        if state in {"live", "deactivated"}:
            print(f"Deploy reached status: {state}")
            return
        if state in {"build_failed", "update_failed", "canceled", "cancelled"}:
            print(f"Deploy failed with status: {state}")
            sys.exit(1)
        time.sleep(15)
    print("Timed out waiting for deploy to complete.")
    sys.exit(1)


def _verify_health(health_url: str) -> None:
    if not health_url:
        print("PUBLIC_HEALTH_URL is not set; skipping HTTP health verification.")
        return
    req = urllib.request.Request(health_url, method="GET")
    with urllib.request.urlopen(req, timeout=60) as resp:
        if resp.status != 200:
            print(f"Health check returned HTTP {resp.status}.")
            sys.exit(1)
    print("Health endpoint returned HTTP 200.")


def main() -> None:
    api_key = os.environ.get("RENDER_API_KEY")
    service_id = os.environ.get("RENDER_SERVICE_ID")
    if not api_key or not service_id:
        print("RENDER_API_KEY and RENDER_SERVICE_ID must be set.")
        sys.exit(1)

    existing = _list_env_vars(service_id, api_key)
    managed = _build_managed_env()
    merged = _merge_env(existing, managed)
    _put_env_vars(service_id, api_key, merged)

    deploy_id = _trigger_deploy(service_id, api_key)
    _wait_for_deploy(service_id, deploy_id, api_key)

    health_url = os.environ.get("PUBLIC_HEALTH_URL", "").strip()
    _verify_health(health_url)


if __name__ == "__main__":
    main()

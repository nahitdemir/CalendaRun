import "server-only";
import { cookies } from "next/headers";
import {
  ACCESS_EXPIRES_COOKIE,
  ACCESS_TOKEN_COOKIE,
  REFRESH_TOKEN_COOKIE,
  clearAuthCookies,
  isAccessTokenExpired,
  setAuthCookies,
} from "@/lib/auth/cookies";
import { refreshWithToken } from "@/lib/auth/keycloak";
import type { NextResponse } from "next/server";

export async function fetchWithAuth(
  input: RequestInfo | URL,
  init: RequestInit = {}
) {
  const store = cookies();
  const accessToken = store.get(ACCESS_TOKEN_COOKIE)?.value || null;
  const refreshToken = store.get(REFRESH_TOKEN_COOKIE)?.value || null;
  const accessExpiresAtRaw = store.get(ACCESS_EXPIRES_COOKIE)?.value || null;
  const accessExpiresAt = accessExpiresAtRaw ? Number(accessExpiresAtRaw) : null;

  let token = accessToken;
  let refreshedTokens: Awaited<ReturnType<typeof refreshWithToken>> | null = null;

  if ((!token || isAccessTokenExpired(accessExpiresAt)) && refreshToken) {
    try {
      refreshedTokens = await refreshWithToken(refreshToken);
      token = refreshedTokens.access_token;
    } catch {
      token = null;
    }
  }

  const headers = new Headers(init.headers || {});
  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  const response = await fetch(input, {
    ...init,
    headers,
    cache: "no-store",
  });

  return { response, refreshedTokens };
}

export function applyAuthCookies(
  nextResponse: NextResponse,
  refreshedTokens: Awaited<ReturnType<typeof refreshWithToken>> | null
) {
  if (refreshedTokens) {
    setAuthCookies(nextResponse, refreshedTokens);
  }
}

export function handleAuthError(nextResponse: NextResponse) {
  clearAuthCookies(nextResponse);
}

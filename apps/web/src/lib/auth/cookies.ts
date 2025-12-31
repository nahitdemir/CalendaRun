import type { NextRequest, NextResponse } from "next/server";
import { APP_BASE_URL } from "@/lib/auth/config";

export const ACCESS_TOKEN_COOKIE = "calendarun_access_token";
export const REFRESH_TOKEN_COOKIE = "calendarun_refresh_token";
// Stored to build optional OIDC end-session URLs (id_token_hint) without exposing it to the client.
export const ID_TOKEN_COOKIE = "calendarun_id_token";
export const ACCESS_EXPIRES_COOKIE = "calendarun_access_expires_at";

const isSecure =
  APP_BASE_URL.startsWith("https://") || process.env.NODE_ENV === "production";

const BASE_COOKIE_OPTIONS = {
  httpOnly: true,
  sameSite: "lax" as const,
  secure: isSecure,
  path: "/",
};

export type KeycloakTokenResponse = {
  access_token: string;
  refresh_token: string;
  id_token?: string;
  expires_in: number;
  refresh_expires_in?: number;
  token_type?: string;
  scope?: string;
};

export function setAuthCookies(
  response: NextResponse,
  tokens: KeycloakTokenResponse
) {
  const now = Date.now();
  const accessExpiresAt = now + tokens.expires_in * 1000;
  const refreshMaxAge = tokens.refresh_expires_in ?? 60 * 60 * 24 * 7;

  response.cookies.set(ACCESS_TOKEN_COOKIE, tokens.access_token, {
    ...BASE_COOKIE_OPTIONS,
    maxAge: tokens.expires_in,
  });
  response.cookies.set(REFRESH_TOKEN_COOKIE, tokens.refresh_token, {
    ...BASE_COOKIE_OPTIONS,
    maxAge: refreshMaxAge,
  });
  if (tokens.id_token) {
    response.cookies.set(ID_TOKEN_COOKIE, tokens.id_token, {
      ...BASE_COOKIE_OPTIONS,
      maxAge: refreshMaxAge,
    });
  }
  response.cookies.set(ACCESS_EXPIRES_COOKIE, String(accessExpiresAt), {
    ...BASE_COOKIE_OPTIONS,
    maxAge: refreshMaxAge,
  });
}

export function clearAuthCookies(response: NextResponse) {
  const expires = new Date(0);
  [ACCESS_TOKEN_COOKIE, REFRESH_TOKEN_COOKIE, ID_TOKEN_COOKIE, ACCESS_EXPIRES_COOKIE].forEach(
    (name) => {
      response.cookies.set(name, "", {
        ...BASE_COOKIE_OPTIONS,
        expires,
        maxAge: 0,
      });
    }
  );
}

export function readAuthCookies(request: NextRequest) {
  const accessToken = request.cookies.get(ACCESS_TOKEN_COOKIE)?.value || null;
  const refreshToken = request.cookies.get(REFRESH_TOKEN_COOKIE)?.value || null;
  const idToken = request.cookies.get(ID_TOKEN_COOKIE)?.value || null;
  const accessExpiresAtRaw =
    request.cookies.get(ACCESS_EXPIRES_COOKIE)?.value || null;
  const accessExpiresAt = accessExpiresAtRaw ? Number(accessExpiresAtRaw) : null;

  return { accessToken, refreshToken, idToken, accessExpiresAt };
}

export function isAccessTokenExpired(accessExpiresAt: number | null) {
  if (!accessExpiresAt) return true;
  return Date.now() > accessExpiresAt - 10_000;
}

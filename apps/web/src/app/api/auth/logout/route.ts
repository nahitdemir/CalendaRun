import { NextResponse, type NextRequest } from "next/server";
import { clearAuthCookies, readAuthCookies } from "@/lib/auth/cookies";
import { APP_BASE_URL, buildKeycloakLogoutUrl } from "@/lib/auth/config";
import { logoutWithRefreshToken } from "@/lib/auth/keycloak";
import { enforceSameOrigin } from "@/lib/auth/security";

export async function POST(request: NextRequest) {
  const originError = enforceSameOrigin(request);
  if (originError) return originError;

  const { refreshToken, idToken } = readAuthCookies(request);
  let keycloakLogoutUrl: string | null = null;

  if (idToken) {
    const postLogoutRedirectUri = new URL("/login", APP_BASE_URL).toString();
    keycloakLogoutUrl = buildKeycloakLogoutUrl({ idToken, postLogoutRedirectUri });
  }

  if (refreshToken) {
    try {
      await logoutWithRefreshToken(refreshToken);
    } catch (error) {
      console.warn("Failed to revoke refresh token:", error);
    }
  }

  // Best-effort end-session call using id_token_hint without a client redirect.
  if (keycloakLogoutUrl) {
    try {
      await fetch(keycloakLogoutUrl, { method: "GET", redirect: "manual", cache: "no-store" });
    } catch (error) {
      console.warn("Failed to call end-session endpoint:", error);
    }
  }

  const response = NextResponse.json({ ok: true });
  response.headers.set("Cache-Control", "no-store");
  clearAuthCookies(response);
  return response;
}

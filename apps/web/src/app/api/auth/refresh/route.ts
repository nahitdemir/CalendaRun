import { NextResponse, type NextRequest } from "next/server";
import { clearAuthCookies, readAuthCookies, setAuthCookies } from "@/lib/auth/cookies";
import { refreshWithToken } from "@/lib/auth/keycloak";
import { enforceRateLimit, enforceSameOrigin } from "@/lib/auth/security";

export async function POST(request: NextRequest) {
  const originError = enforceSameOrigin(request);
  if (originError) return originError;

  const rateLimitError = enforceRateLimit(request, "auth:refresh", {
    limit: 30,
    windowMs: 60_000,
  });
  if (rateLimitError) return rateLimitError;

  const { refreshToken } = readAuthCookies(request);
  if (!refreshToken) {
    const response = NextResponse.json(
      { error: "Not authenticated." },
      { status: 401 }
    );
    clearAuthCookies(response);
    return response;
  }

  try {
    const tokens = await refreshWithToken(refreshToken);
    const response = NextResponse.json({ ok: true });
    response.headers.set("Cache-Control", "no-store");
    setAuthCookies(response, tokens);
    return response;
  } catch (error) {
    const response = NextResponse.json(
      { error: "Session expired." },
      { status: 401 }
    );
    clearAuthCookies(response);
    return response;
  }
}

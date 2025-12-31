import { NextResponse, type NextRequest } from "next/server";
import { setAuthCookies } from "@/lib/auth/cookies";
import { loginWithPassword } from "@/lib/auth/keycloak";
import { fetchUserProfile } from "@/lib/auth/profile";
import { enforceRateLimit, enforceSameOrigin } from "@/lib/auth/security";

export async function POST(request: NextRequest) {
  const originError = enforceSameOrigin(request);
  if (originError) return originError;

  const rateLimitError = enforceRateLimit(request, "auth:login", {
    limit: 10,
    windowMs: 60_000,
  });
  if (rateLimitError) return rateLimitError;

  const body = await request.json().catch(() => null);
  const email = typeof body?.email === "string" ? body.email.trim() : "";
  const password = typeof body?.password === "string" ? body.password : "";

  if (!email || !password) {
    return NextResponse.json(
      { error: "Email and password are required." },
      { status: 400 }
    );
  }

  try {
    const tokens = await loginWithPassword(email, password);
    const user = await fetchUserProfile(tokens.access_token);
    const response = NextResponse.json({ ok: true, user });
    response.headers.set("Cache-Control", "no-store");
    setAuthCookies(response, tokens);
    return response;
  } catch (error) {
    return NextResponse.json(
      { error: "Invalid email or password." },
      { status: 401 }
    );
  }
}

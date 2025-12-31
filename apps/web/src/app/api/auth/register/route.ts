import { NextResponse, type NextRequest } from "next/server";
import { setAuthCookies } from "@/lib/auth/cookies";
import { createUser, loginWithPassword } from "@/lib/auth/keycloak";
import { fetchUserProfile } from "@/lib/auth/profile";
import { enforceRateLimit, enforceSameOrigin } from "@/lib/auth/security";

export async function POST(request: NextRequest) {
  const originError = enforceSameOrigin(request);
  if (originError) return originError;

  const rateLimitError = enforceRateLimit(request, "auth:register", {
    limit: 5,
    windowMs: 60_000,
  });
  if (rateLimitError) return rateLimitError;

  const body = await request.json().catch(() => null);
  const firstName = typeof body?.firstName === "string" ? body.firstName.trim() : "";
  const lastName = typeof body?.lastName === "string" ? body.lastName.trim() : "";
  const name = `${firstName} ${lastName}`.trim();
  const email = typeof body?.email === "string" ? body.email.trim() : "";
  const password = typeof body?.password === "string" ? body.password : "";

  if (!firstName || !lastName || !email || !password) {
    return NextResponse.json(
      { error: "First name, last name, email, and password are required." },
      { status: 400 }
    );
  }

  if (!email.includes("@")) {
    return NextResponse.json({ error: "Invalid email address." }, { status: 400 });
  }

  if (password.length < 8) {
    return NextResponse.json(
      { error: "Password must be at least 8 characters." },
      { status: 400 }
    );
  }

  try {
    await createUser({ email, name, password });
  } catch (error) {
    const status = (error as Error & { status?: number }).status;
    if (status === 409) {
      return NextResponse.json(
        { error: "An account with this email already exists." },
        { status: 409 }
      );
    }
    return NextResponse.json(
      { error: "Failed to create account." },
      { status: 500 }
    );
  }

  try {
    const tokens = await loginWithPassword(email, password);
    const user = await fetchUserProfile(tokens.access_token);
    const response = NextResponse.json({ ok: true, loggedIn: true, user });
    response.headers.set("Cache-Control", "no-store");
    setAuthCookies(response, tokens);
    return response;
  } catch {
    return NextResponse.json({ ok: true, loggedIn: false });
  }
}

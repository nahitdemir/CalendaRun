import { NextResponse, type NextRequest } from "next/server";
import crypto from "crypto";
import { enforceRateLimit, enforceSameOrigin } from "@/lib/auth/security";
import { findUserByEmail, setUserPassword, updateUserAttributes } from "@/lib/auth/keycloak";

export async function POST(request: NextRequest) {
  const originError = enforceSameOrigin(request);
  if (originError) return originError;

  const rateLimitError = enforceRateLimit(request, "auth:reset-password", {
    limit: 5,
    windowMs: 60_000,
  });
  if (rateLimitError) return rateLimitError;

  const body = await request.json().catch(() => null);
  const email = typeof body?.email === "string" ? body.email.trim() : "";
  const token = typeof body?.token === "string" ? body.token : "";
  const password = typeof body?.password === "string" ? body.password : "";

  if (!email || !token || !password) {
    return NextResponse.json(
      { error: "Email, token, and password are required." },
      { status: 400 }
    );
  }

  if (password.length < 8) {
    return NextResponse.json(
      { error: "Password must be at least 8 characters." },
      { status: 400 }
    );
  }

  const user = await findUserByEmail(email);
  if (!user) {
    return NextResponse.json({ error: "Invalid reset token." }, { status: 400 });
  }

  const storedHash = user.attributes?.calendarun_reset_token_hash?.[0] || "";
  const storedExpires = Number(
    user.attributes?.calendarun_reset_token_expires_at?.[0] || 0
  );
  const providedHash = crypto.createHash("sha256").update(token).digest("hex");

  const hashMatches =
    storedHash &&
    storedHash.length === providedHash.length &&
    crypto.timingSafeEqual(Buffer.from(storedHash), Buffer.from(providedHash));

  if (!hashMatches || !storedExpires || Date.now() > storedExpires) {
    return NextResponse.json({ error: "Invalid reset token." }, { status: 400 });
  }

  try {
    await setUserPassword(user.id, password);
    const nextAttributes = { ...(user.attributes || {}) };
    delete nextAttributes.calendarun_reset_token_hash;
    delete nextAttributes.calendarun_reset_token_expires_at;
    await updateUserAttributes(user.id, nextAttributes);
  } catch (error) {
    console.error("Failed to reset password:", error);
    return NextResponse.json(
      { error: "Unable to reset password." },
      { status: 500 }
    );
  }

  return NextResponse.json({ ok: true });
}

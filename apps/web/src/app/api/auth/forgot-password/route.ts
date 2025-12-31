import { NextResponse, type NextRequest } from "next/server";
import crypto from "crypto";
import { APP_BASE_URL } from "@/lib/auth/config";
import { enforceRateLimit, enforceSameOrigin } from "@/lib/auth/security";
import { findUserByEmail, updateUserAttributes } from "@/lib/auth/keycloak";
import { sendPasswordResetEmail } from "@/lib/auth/mailer";

const RESET_TOKEN_TTL_MINUTES = Number(process.env.RESET_TOKEN_TTL_MINUTES || 30);

export async function POST(request: NextRequest) {
  const originError = enforceSameOrigin(request);
  if (originError) return originError;

  const rateLimitError = enforceRateLimit(request, "auth:forgot-password", {
    limit: 5,
    windowMs: 60_000,
  });
  if (rateLimitError) return rateLimitError;

  const body = await request.json().catch(() => null);
  const email = typeof body?.email === "string" ? body.email.trim() : "";

  if (!email || !email.includes("@")) {
    return NextResponse.json({ ok: true });
  }

  const user = await findUserByEmail(email);
  if (!user) {
    return NextResponse.json({ ok: true });
  }

  const token = crypto.randomBytes(32).toString("hex");
  const tokenHash = crypto.createHash("sha256").update(token).digest("hex");
  const expiresAt = Date.now() + RESET_TOKEN_TTL_MINUTES * 60 * 1000;

  const nextAttributes = {
    ...(user.attributes || {}),
    calendarun_reset_token_hash: [tokenHash],
    calendarun_reset_token_expires_at: [String(expiresAt)],
  };

  try {
    await updateUserAttributes(user.id, nextAttributes);
    const resetUrl = new URL("/reset-password", APP_BASE_URL);
    resetUrl.searchParams.set("email", email);
    resetUrl.searchParams.set("token", token);
    await sendPasswordResetEmail({ to: email, resetUrl: resetUrl.toString() });
  } catch (error) {
    console.error("Failed to process password reset:", error);
  }

  return NextResponse.json({ ok: true });
}

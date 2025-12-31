import type { NextRequest } from "next/server";
import { NextResponse } from "next/server";
import { APP_BASE_URL } from "@/lib/auth/config";

const origin = new URL(APP_BASE_URL).origin;

type RateLimitEntry = {
  count: number;
  resetAt: number;
};

const rateLimits = new Map<string, RateLimitEntry>();

function getClientIp(request: NextRequest) {
  const forwarded = request.headers.get("x-forwarded-for");
  if (forwarded) {
    return forwarded.split(",")[0]?.trim() || "unknown";
  }
  return request.ip || "unknown";
}

export function enforceSameOrigin(request: NextRequest) {
  const headerOrigin = request.headers.get("origin");
  const referer = request.headers.get("referer");
  const requestOrigin = headerOrigin || (referer ? new URL(referer).origin : "");

  if (requestOrigin && requestOrigin !== origin) {
    return NextResponse.json({ error: "Invalid origin." }, { status: 403 });
  }

  return null;
}

export function enforceRateLimit(
  request: NextRequest,
  key: string,
  { limit, windowMs }: { limit: number; windowMs: number }
) {
  const ip = getClientIp(request);
  const now = Date.now();
  const bucketKey = `${key}:${ip}`;
  const entry = rateLimits.get(bucketKey);

  if (!entry || entry.resetAt <= now) {
    rateLimits.set(bucketKey, { count: 1, resetAt: now + windowMs });
    return null;
  }

  if (entry.count >= limit) {
    const retryAfter = Math.ceil((entry.resetAt - now) / 1000);
    return NextResponse.json(
      { error: "Too many requests. Please try again later." },
      {
        status: 429,
        headers: { "Retry-After": String(retryAfter) },
      }
    );
  }

  entry.count += 1;
  rateLimits.set(bucketKey, entry);
  return null;
}

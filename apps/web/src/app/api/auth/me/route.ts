import { NextResponse, type NextRequest } from "next/server";
import { fetchWithAuth, applyAuthCookies, handleAuthError } from "@/lib/auth/fetchWithAuth";
import { enforceSameOrigin } from "@/lib/auth/security";

const API_BASE =
  process.env.INTERNAL_API_URL ||
  process.env.NEXT_PUBLIC_API_URL ||
  "http://localhost:8080";

export async function GET(request: NextRequest) {
  const originError = enforceSameOrigin(request);
  if (originError) return originError;

  const { response, refreshedTokens } = await fetchWithAuth(
    `${API_BASE.replace(/\/$/, "")}/api/me`
  );

  if (!response.ok) {
    const nextResponse = NextResponse.json(
      { error: "Unauthorized" },
      { status: response.status }
    );
    handleAuthError(nextResponse);
    return nextResponse;
  }

  const data = await response.json();
  const nextResponse = NextResponse.json(data);
  nextResponse.headers.set("Cache-Control", "no-store");
  applyAuthCookies(nextResponse, refreshedTokens);
  return nextResponse;
}

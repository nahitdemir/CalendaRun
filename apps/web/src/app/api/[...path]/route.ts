import { NextResponse, type NextRequest } from "next/server";
import {
  clearAuthCookies,
  isAccessTokenExpired,
  readAuthCookies,
  setAuthCookies,
} from "@/lib/auth/cookies";
import { refreshWithToken } from "@/lib/auth/keycloak";

const API_BASE =
  process.env.INTERNAL_API_URL ||
  process.env.NEXT_PUBLIC_API_URL ||
  "http://localhost:8080";

async function proxyRequest(request: NextRequest, path: string) {
  const url = new URL(request.url);
  const targetUrl = `${API_BASE.replace(/\/$/, "")}/api/${path}${url.search}`;

  const body =
    request.method === "GET" || request.method === "HEAD"
      ? undefined
      : await request.arrayBuffer();

  const headers = new Headers(request.headers);
  headers.delete("cookie");
  headers.delete("host");
  if (body === undefined) {
    headers.delete("content-length");
  }

  const { accessToken, refreshToken, accessExpiresAt } = readAuthCookies(request);
  let token = accessToken;
  let refreshedTokens = null as Awaited<ReturnType<typeof refreshWithToken>> | null;

  if ((!token || isAccessTokenExpired(accessExpiresAt)) && refreshToken) {
    try {
      refreshedTokens = await refreshWithToken(refreshToken);
      token = refreshedTokens.access_token;
    } catch {
      token = null;
    }
  }

  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  } else {
    headers.delete("authorization");
  }

  const makeRequest = () =>
    fetch(targetUrl, {
      method: request.method,
      headers,
      body,
      redirect: "manual",
    });

  let upstream = await makeRequest();

  if (upstream.status === 401 && refreshToken && !refreshedTokens) {
    try {
      refreshedTokens = await refreshWithToken(refreshToken);
      headers.set("Authorization", `Bearer ${refreshedTokens.access_token}`);
      upstream = await makeRequest();
    } catch {
      const response = NextResponse.json(
        { error: "Unauthorized" },
        { status: 401 }
      );
      clearAuthCookies(response);
      return response;
    }
  }

  const responseHeaders = new Headers(upstream.headers);
  const response = new NextResponse(upstream.body, {
    status: upstream.status,
    headers: responseHeaders,
  });

  if (refreshedTokens) {
    setAuthCookies(response, refreshedTokens);
  }

  if (upstream.status === 401) {
    clearAuthCookies(response);
  }

  return response;
}

export async function GET(
  request: NextRequest,
  context: { params: { path?: string[] } }
) {
  const path = context.params.path?.join("/") || "";
  return proxyRequest(request, path);
}

export async function POST(
  request: NextRequest,
  context: { params: { path?: string[] } }
) {
  const path = context.params.path?.join("/") || "";
  return proxyRequest(request, path);
}

export async function PUT(
  request: NextRequest,
  context: { params: { path?: string[] } }
) {
  const path = context.params.path?.join("/") || "";
  return proxyRequest(request, path);
}

export async function PATCH(
  request: NextRequest,
  context: { params: { path?: string[] } }
) {
  const path = context.params.path?.join("/") || "";
  return proxyRequest(request, path);
}

export async function DELETE(
  request: NextRequest,
  context: { params: { path?: string[] } }
) {
  const path = context.params.path?.join("/") || "";
  return proxyRequest(request, path);
}

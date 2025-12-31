import { NextResponse, type NextRequest } from "next/server";
import { readAuthCookies } from "@/lib/auth/cookies";

const PROTECTED_PREFIXES = ["/plan", "/settings", "/admin", "/super-admin"];

const PUBLIC_FILE = /\.(.*)$/;

export function middleware(request: NextRequest) {
  const { pathname, search } = request.nextUrl;

  if (
    pathname.startsWith("/api") ||
    pathname.startsWith("/_next") ||
    pathname.startsWith("/public") ||
    PUBLIC_FILE.test(pathname)
  ) {
    return NextResponse.next();
  }

  const isProtected = PROTECTED_PREFIXES.some((prefix) => pathname.startsWith(prefix));
  if (!isProtected) {
    return NextResponse.next();
  }

  const { accessToken, refreshToken } = readAuthCookies(request);
  if (!accessToken && !refreshToken) {
    const url = request.nextUrl.clone();
    url.pathname = "/login";
    const callbackUrl = `${pathname}${search}`;
    url.searchParams.set("callbackUrl", callbackUrl || "/");
    return NextResponse.redirect(url);
  }

  return NextResponse.next();
}

export const config = {
  matcher: ["/:path*"],
};

import "server-only";
import { cookies } from "next/headers";
import {
  ACCESS_EXPIRES_COOKIE,
  ACCESS_TOKEN_COOKIE,
  ID_TOKEN_COOKIE,
  REFRESH_TOKEN_COOKIE,
} from "@/lib/auth/cookies";

export type AuthSession = {
  accessToken: string | null;
  refreshToken: string | null;
  idToken: string | null;
  accessExpiresAt: number | null;
  isAuthenticated: boolean;
  user: {
    id?: string;
    email?: string;
    name?: string;
    roles?: string[];
  } | null;
};

function decodeJwtPayload(token: string) {
  const payload = token.split(".")[1];
  if (!payload) return null;
  const normalized = payload.replace(/-/g, "+").replace(/_/g, "/");
  const padded = normalized.padEnd(Math.ceil(normalized.length / 4) * 4, "=");
  try {
    const decoded = Buffer.from(padded, "base64").toString("utf8");
    return JSON.parse(decoded) as Record<string, unknown>;
  } catch {
    return null;
  }
}

export function getSession(): AuthSession {
  const store = cookies();
  const accessToken = store.get(ACCESS_TOKEN_COOKIE)?.value || null;
  const refreshToken = store.get(REFRESH_TOKEN_COOKIE)?.value || null;
  const idToken = store.get(ID_TOKEN_COOKIE)?.value || null;
  const accessExpiresAtRaw = store.get(ACCESS_EXPIRES_COOKIE)?.value || null;
  const accessExpiresAt = accessExpiresAtRaw ? Number(accessExpiresAtRaw) : null;

  const payload = idToken ? decodeJwtPayload(idToken) : accessToken ? decodeJwtPayload(accessToken) : null;

  const user = payload
    ? {
        id: typeof payload.sub === "string" ? payload.sub : undefined,
        email: typeof payload.email === "string" ? payload.email : undefined,
        name: typeof payload.name === "string" ? payload.name : undefined,
        roles:
          payload.realm_access && typeof payload.realm_access === "object"
            ? ((payload.realm_access as { roles?: string[] }).roles || [])
            : undefined,
      }
    : null;

  return {
    accessToken,
    refreshToken,
    idToken,
    accessExpiresAt,
    isAuthenticated: Boolean(accessToken || refreshToken),
    user,
  };
}

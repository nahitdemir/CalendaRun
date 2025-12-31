import "server-only";
import { getSession } from "@/lib/auth/session";

type RoleData = {
  realm_access?: { roles?: string[] };
  resource_access?: Record<string, { roles?: string[] }>;
};

function decodeJwtPayload(token: string) {
  const payload = token.split(".")[1];
  if (!payload) return null;
  const normalized = payload.replace(/-/g, "+").replace(/_/g, "/");
  const padded = normalized.padEnd(Math.ceil(normalized.length / 4) * 4, "=");
  try {
    const decoded = Buffer.from(padded, "base64").toString("utf8");
    return JSON.parse(decoded) as RoleData;
  } catch {
    return null;
  }
}

export function getTokenRoles(token: string | null) {
  if (!token) return [];
  const payload = decodeJwtPayload(token);
  const roles = new Set<string>();

  payload?.realm_access?.roles?.forEach((role) => roles.add(role));
  if (payload?.resource_access) {
    Object.values(payload.resource_access).forEach((resource) => {
      resource.roles?.forEach((role) => roles.add(role));
    });
  }

  return Array.from(roles);
}

export function requireAuth() {
  const session = getSession();
  if (!session.isAuthenticated) {
    throw new Error("Unauthorized");
  }
  return session;
}

export function requireRole(role: string) {
  const session = requireAuth();
  const tokenRoles = getTokenRoles(session.accessToken || session.idToken);
  if (!tokenRoles.includes(role)) {
    throw new Error("Forbidden");
  }
  return session;
}

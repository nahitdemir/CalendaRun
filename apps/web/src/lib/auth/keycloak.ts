import "server-only";
import {
  KEYCLOAK_ADMIN_CLIENT_ID,
  KEYCLOAK_ADMIN_CLIENT_SECRET,
  KEYCLOAK_ADMIN_REALM,
  KEYCLOAK_CLIENT_ID,
  KEYCLOAK_CLIENT_SECRET,
  KEYCLOAK_ISSUER,
} from "@/lib/auth/config";
import type { KeycloakTokenResponse } from "@/lib/auth/cookies";

type KeycloakError = {
  error?: string;
  error_description?: string;
  message?: string;
};

type KeycloakUser = {
  id: string;
  username: string;
  email?: string;
  firstName?: string;
  lastName?: string;
  attributes?: Record<string, string[]>;
};

const TOKEN_ENDPOINT = `${KEYCLOAK_ISSUER.replace(/\/$/, "")}/protocol/openid-connect/token`;
const LOGOUT_ENDPOINT = `${KEYCLOAK_ISSUER.replace(/\/$/, "")}/protocol/openid-connect/logout`;
const ADMIN_BASE = `${KEYCLOAK_ISSUER.replace(/\/$/, "")}/admin/realms/${KEYCLOAK_ADMIN_REALM}`;

async function parseKeycloakError(response: Response) {
  try {
    return (await response.json()) as KeycloakError;
  } catch {
    return {};
  }
}

async function requestToken(params: URLSearchParams): Promise<KeycloakTokenResponse> {
  const response = await fetch(TOKEN_ENDPOINT, {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body: params,
    cache: "no-store",
  });

  if (!response.ok) {
    const error = await parseKeycloakError(response);
    const message = error.error_description || error.error || "Token request failed";
    throw new Error(message);
  }

  return (await response.json()) as KeycloakTokenResponse;
}

export async function loginWithPassword(username: string, password: string) {
  const params = new URLSearchParams({
    grant_type: "password",
    client_id: KEYCLOAK_CLIENT_ID,
    username,
    password,
    scope: "openid profile email",
  });

  if (KEYCLOAK_CLIENT_SECRET) {
    params.set("client_secret", KEYCLOAK_CLIENT_SECRET);
  }

  return requestToken(params);
}

export async function refreshWithToken(refreshToken: string) {
  const params = new URLSearchParams({
    grant_type: "refresh_token",
    client_id: KEYCLOAK_CLIENT_ID,
    refresh_token: refreshToken,
  });

  if (KEYCLOAK_CLIENT_SECRET) {
    params.set("client_secret", KEYCLOAK_CLIENT_SECRET);
  }

  return requestToken(params);
}

export async function logoutWithRefreshToken(refreshToken: string) {
  const params = new URLSearchParams({
    client_id: KEYCLOAK_CLIENT_ID,
    refresh_token: refreshToken,
  });

  if (KEYCLOAK_CLIENT_SECRET) {
    params.set("client_secret", KEYCLOAK_CLIENT_SECRET);
  }

  const response = await fetch(LOGOUT_ENDPOINT, {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body: params,
    cache: "no-store",
  });

  if (!response.ok) {
    const error = await parseKeycloakError(response);
    const message = error.error_description || error.error || "Logout failed";
    throw new Error(message);
  }
}

async function getAdminToken() {
  const params = new URLSearchParams({
    grant_type: "client_credentials",
    client_id: KEYCLOAK_ADMIN_CLIENT_ID,
  });

  if (KEYCLOAK_ADMIN_CLIENT_SECRET) {
    params.set("client_secret", KEYCLOAK_ADMIN_CLIENT_SECRET);
  }

  return requestToken(params);
}

async function adminFetch(path: string, options: RequestInit = {}) {
  const token = await getAdminToken();
  const response = await fetch(`${ADMIN_BASE}${path}`, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${token.access_token}`,
      ...(options.headers || {}),
    },
    cache: "no-store",
  });

  if (!response.ok) {
    const error = await parseKeycloakError(response);
    const message =
      error.error_description || error.message || error.error || "Admin request failed";
    const err = new Error(message);
    (err as Error & { status?: number }).status = response.status;
    throw err;
  }

  return response;
}

export async function findUserByEmail(email: string) {
  const params = new URLSearchParams({ email, exact: "true" });
  const response = await adminFetch(`/users?${params.toString()}`, {
    method: "GET",
  });
  const users = (await response.json()) as KeycloakUser[];
  return users[0] || null;
}

export async function createUser({
  email,
  name,
  password,
}: {
  email: string;
  name: string;
  password: string;
}) {
  const [firstName, ...rest] = name.trim().split(" ");
  const lastName = rest.join(" ");

  const response = await adminFetch("/users", {
    method: "POST",
    body: JSON.stringify({
      username: email,
      email,
      enabled: true,
      emailVerified: false,
      firstName,
      lastName,
      credentials: [
        {
          type: "password",
          value: password,
          temporary: false,
        },
      ],
    }),
  });

  const location = response.headers.get("Location");
  const id = location?.split("/").pop();
  return id || null;
}

export async function updateUserAttributes(userId: string, attributes: Record<string, string[]>) {
  await adminFetch(`/users/${userId}`, {
    method: "PUT",
    body: JSON.stringify({ attributes }),
  });
}

export async function setUserPassword(userId: string, password: string) {
  await adminFetch(`/users/${userId}/reset-password`, {
    method: "PUT",
    body: JSON.stringify({
      type: "password",
      value: password,
      temporary: false,
    }),
  });
}

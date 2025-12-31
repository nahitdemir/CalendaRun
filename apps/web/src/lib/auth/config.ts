const DEFAULT_ISSUER = "http://localhost:8180/realms/calendarun";

const explicitBase =
  process.env.KEYCLOAK_BASE_URL ||
  process.env.NEXT_PUBLIC_KEYCLOAK_BASE_URL ||
  "";
const explicitRealm =
  process.env.KEYCLOAK_REALM ||
  process.env.NEXT_PUBLIC_KEYCLOAK_REALM ||
  "";

const issuer =
  process.env.KEYCLOAK_ISSUER ||
  process.env.NEXT_PUBLIC_KEYCLOAK_ISSUER ||
  (explicitBase && explicitRealm
    ? `${explicitBase.replace(/\/$/, "")}/realms/${explicitRealm}`
    : DEFAULT_ISSUER);

const realmMatch = issuer.match(/\/realms\/([^/]+)\/?$/);

export const KEYCLOAK_BASE_URL =
  explicitBase ||
  (realmMatch
    ? issuer.replace(/\/realms\/[^/]+\/?$/, "")
    : issuer.replace(/\/$/, ""));
export const KEYCLOAK_REALM = explicitRealm || realmMatch?.[1] || "";
export const KEYCLOAK_CLIENT_ID =
  process.env.KEYCLOAK_CLIENT_ID ||
  process.env.NEXT_PUBLIC_KEYCLOAK_CLIENT_ID ||
  "calendarun-web";
export const KEYCLOAK_CLIENT_SECRET =
  process.env.KEYCLOAK_CLIENT_SECRET || "";
export const KEYCLOAK_ISSUER = issuer;
export const KEYCLOAK_ADMIN_CLIENT_ID =
  process.env.KEYCLOAK_ADMIN_CLIENT_ID || "calendarun-admin";
export const KEYCLOAK_ADMIN_CLIENT_SECRET =
  process.env.KEYCLOAK_ADMIN_CLIENT_SECRET || "";
export const KEYCLOAK_ADMIN_REALM =
  process.env.KEYCLOAK_ADMIN_REALM || KEYCLOAK_REALM;
export const APP_BASE_URL =
  process.env.APP_BASE_URL ||
  process.env.NEXT_PUBLIC_APP_URL ||
  process.env.NEXTAUTH_URL ||
  "http://localhost:3000";

export function buildKeycloakLogoutUrl({
  idToken,
  postLogoutRedirectUri,
  clientId = KEYCLOAK_CLIENT_ID,
}: {
  idToken: string;
  postLogoutRedirectUri: string;
  clientId?: string;
}) {
  const base = KEYCLOAK_REALM
    ? `${KEYCLOAK_BASE_URL}/realms/${KEYCLOAK_REALM}/protocol/openid-connect/logout`
    : `${KEYCLOAK_ISSUER.replace(/\/$/, "")}/protocol/openid-connect/logout`;

  const params = new URLSearchParams({
    id_token_hint: idToken,
    post_logout_redirect_uri: postLogoutRedirectUri,
  });

  if (clientId) {
    params.set("client_id", clientId);
  }

  return `${base}?${params.toString()}`;
}

import { NextAuthOptions } from "next-auth";
import KeycloakProvider from "next-auth/providers/keycloak";

declare module "next-auth" {
  interface Session {
    accessToken?: string;
    error?: string;
    user: {
      id: string;
      name?: string | null;
      email?: string | null;
      image?: string | null;
      roles?: string[];
    };
  }
}

declare module "next-auth/jwt" {
  interface JWT {
    accessToken?: string;
    refreshToken?: string;
    accessTokenExpires?: number;
    error?: string;
    roles?: string[];
    sub?: string;
  }
}

// Validate Keycloak configuration
const keycloakIssuer = process.env.KEYCLOAK_ISSUER || "http://localhost:8180/realms/calendarun";
const keycloakClientId = process.env.KEYCLOAK_CLIENT_ID || "calendarun-web";
const keycloakClientSecret = process.env.KEYCLOAK_CLIENT_SECRET || "";

if (!keycloakIssuer || !keycloakClientId) {
  console.warn("Keycloak configuration may be incomplete. Please check KEYCLOAK_ISSUER and KEYCLOAK_CLIENT_ID environment variables.");
}

export const authOptions: NextAuthOptions = {
  providers: [
    KeycloakProvider({
      clientId: keycloakClientId,
      clientSecret: keycloakClientSecret,
      issuer: keycloakIssuer,
    }),
  ],
  callbacks: {
    async jwt({ token, account, profile }) {
      // Initial sign in
      if (account && profile) {
        token.accessToken = account.access_token;
        token.refreshToken = account.refresh_token;
        // account.expires_at is in seconds (Unix timestamp), convert to milliseconds
        // If expires_at is not provided, calculate from expires_in (default 5 minutes)
        if (account.expires_at) {
          token.accessTokenExpires = account.expires_at * 1000;
        } else if (account.expires_in && typeof account.expires_in === 'number') {
          token.accessTokenExpires = Date.now() + account.expires_in * 1000;
        } else {
          // Default to 5 minutes if neither is provided
          token.accessTokenExpires = Date.now() + 5 * 60 * 1000;
        }
        token.sub = (profile as any).sub;
        
        // Extract realm roles from Keycloak token
        const realmAccess = (profile as any).realm_access;
        token.roles = realmAccess?.roles || [];
        
        // Return immediately after initial sign in
        return token;
      }

      // If no access token expiration is set, return token as-is
      if (!token.accessTokenExpires || token.accessTokenExpires === 0) {
        return token;
      }

      // Check if token has an error from previous refresh attempts
      if (token.error === "RefreshAccessTokenError") {
        // Don't retry immediately, wait a bit to avoid infinite loops
        // The session callback will handle the error
        return token;
      }

      // Return previous token if the access token has not expired yet
      // Refresh proactively 5 minutes before expiration to avoid race conditions
      const FIVE_MINUTES = 5 * 60 * 1000;
      const now = Date.now();
      
      if (token.accessTokenExpires && now < token.accessTokenExpires - FIVE_MINUTES) {
        return token;
      }

      // Access token is about to expire or has expired, try to refresh it
      // But only if we have a refresh token
      if (!token.refreshToken) {
        console.error("No refresh token available, cannot refresh access token");
        return {
          ...token,
          error: "RefreshAccessTokenError",
        };
      }

      return refreshAccessToken(token);
    },
    async session({ session, token }) {
      session.accessToken = token.accessToken;
      session.error = token.error;
      
      if (session.user) {
        session.user.id = token.sub || "";
        session.user.roles = token.roles || [];
      }
      
      return session;
    },
  },
  pages: {
    signIn: "/auth/signin",
    error: "/auth/error",
  },
  session: {
    strategy: "jwt",
  },
};

async function refreshAccessToken(token: any) {
  try {
    // Check if refresh token exists
    if (!token.refreshToken) {
      console.error("Cannot refresh: no refresh token available");
      return {
        ...token,
        error: "RefreshAccessTokenError",
      };
    }

    // Validate environment variables
    const issuer = process.env.KEYCLOAK_ISSUER || "http://localhost:8180/realms/calendarun";
    const clientId = process.env.KEYCLOAK_CLIENT_ID || "calendarun-web";
    const clientSecret = process.env.KEYCLOAK_CLIENT_SECRET || "";

    if (!issuer || !clientId) {
      console.error("Keycloak configuration missing: issuer or clientId");
      return {
        ...token,
        error: "RefreshAccessTokenError",
      };
    }

    const url = `${issuer}/protocol/openid-connect/token`;
    
    const response = await fetch(url, {
      headers: { "Content-Type": "application/x-www-form-urlencoded" },
      method: "POST",
      body: new URLSearchParams({
        client_id: clientId,
        client_secret: clientSecret,
        grant_type: "refresh_token",
        refresh_token: token.refreshToken,
      }),
    });

    // Check if response is ok before parsing JSON
    if (!response.ok) {
      let errorMessage = `Token refresh failed with status ${response.status}`;
      try {
        const errorData = await response.json();
        errorMessage = errorData.error_description || errorData.error || errorMessage;
        console.error("Token refresh failed:", errorData);
      } catch (e) {
        const text = await response.text();
        console.error("Token refresh failed (non-JSON response):", text);
      }
      throw new Error(errorMessage);
    }

    const refreshedTokens = await response.json();

    // Validate response structure
    if (!refreshedTokens.access_token) {
      console.error("Token refresh response missing access_token:", refreshedTokens);
      throw new Error("Invalid token refresh response: missing access_token");
    }

    // Calculate expiration time
    let expiresIn = refreshedTokens.expires_in;
    if (typeof expiresIn !== 'number' || expiresIn <= 0) {
      // Default to 5 minutes if expires_in is invalid
      expiresIn = 300;
      console.warn("Token refresh response has invalid expires_in, defaulting to 5 minutes");
    }

    // Use new refresh token if provided, otherwise keep the old one
    // Keycloak may not always return a new refresh_token
    const newRefreshToken = refreshedTokens.refresh_token || token.refreshToken;

    return {
      ...token,
      accessToken: refreshedTokens.access_token,
      accessTokenExpires: Date.now() + expiresIn * 1000,
      refreshToken: newRefreshToken,
      error: undefined, // Clear any previous errors
    };
  } catch (error) {
    console.error("Error refreshing access token:", error);
    // Only set error if it's a real error, not just a network issue
    // This allows retry on next request
    return {
      ...token,
      error: "RefreshAccessTokenError",
    };
  }
}


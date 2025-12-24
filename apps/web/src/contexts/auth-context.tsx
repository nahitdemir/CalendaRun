"use client";

import {
  createContext,
  useContext,
  useState,
  useEffect,
  useCallback,
  ReactNode,
} from "react";
import { useSession, signIn, signOut } from "next-auth/react";
import {
  getStoredToken,
  setStoredToken,
  authApi,
  UserProfile,
  TenantMembership,
  ApiError,
  getStoredTenantId,
  setStoredTenantId,
} from "@/lib/api-client";

interface AuthContextType {
  // User state
  user: UserProfile | null;
  isLoading: boolean;
  isAuthenticated: boolean;

  // Roles
  isSuperAdmin: boolean;
  isTenantAdmin: boolean;

  // Tenants
  tenants: TenantMembership[];
  selectedTenant: TenantMembership | null;
  setSelectedTenant: (tenant: TenantMembership | null) => void;

  // Actions
  login: () => void;
  logout: () => void;

  // Dev mode
  devToken: string | null;
  setDevToken: (token: string | null) => void;

  // Refresh
  refreshProfile: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const { data: session, status } = useSession();

  const [user, setUser] = useState<UserProfile | null>(null);
  const [tenants, setTenants] = useState<TenantMembership[]>([]);
  const [selectedTenant, setSelectedTenantState] = useState<TenantMembership | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [devToken, setDevTokenState] = useState<string | null>(null);

  // Initialize dev token from storage
  useEffect(() => {
    const storedToken = getStoredToken();
    if (storedToken) {
      setDevTokenState(storedToken);
    }
  }, []);

  // Get effective access token (session token or dev token)
  const getAccessToken = useCallback((): string | null => {
    // Prefer session token from next-auth
    if (session?.accessToken) {
      return session.accessToken as string;
    }
    // Fallback to dev token
    return devToken;
  }, [session, devToken]);

  // Set dev token
  const setDevToken = useCallback((token: string | null) => {
    setDevTokenState(token);
    setStoredToken(token);
  }, []);

  // Set selected tenant
  const setSelectedTenant = useCallback((tenant: TenantMembership | null) => {
    setSelectedTenantState(tenant);
    setStoredTenantId(tenant?.tenantId || null);
  }, []);

  // Load user profile and tenants
  const refreshProfile = useCallback(async () => {
    const token = getAccessToken();
    if (!token) {
      setUser(null);
      setTenants([]);
      setIsLoading(false);
      return;
    }

    // Temporarily set token for API calls
    setStoredToken(token);

    try {
      // Fetch profile and tenants in parallel
      // Silently catch 401 errors (user not authenticated)
      const [profile, userTenants] = await Promise.all([
        authApi.getMe().catch((err) => {
          // Only log non-401 errors
          if (err instanceof ApiError && err.status !== 401) {
            console.error("Failed to fetch profile:", err);
          }
          return null;
        }),
        authApi.getMyTenants().catch((err) => {
          // Only log non-401 errors
          if (err instanceof ApiError && err.status !== 401) {
            console.error("Failed to fetch tenants:", err);
          }
          return [];
        }),
      ]);

      if (profile) {
        setUser(profile);
      } else {
        // If /api/me fails, construct from session
        if (session?.user) {
          setUser({
            id: session.user.id || "",
            email: session.user.email || "",
            name: session.user.name || undefined,
            roles: (session.user as { roles?: string[] }).roles || [],
            isSuperAdmin: (session.user as { roles?: string[] }).roles?.includes("super_admin") || false,
          });
        }
      }

      setTenants(userTenants);

      // Restore or auto-select tenant
      const storedTenantId = getStoredTenantId();
      if (storedTenantId) {
        const found = userTenants.find((t) => t.tenantId === storedTenantId);
        if (found) {
          setSelectedTenantState(found);
        } else if (userTenants.length > 0) {
          setSelectedTenant(userTenants[0]);
        }
      } else if (userTenants.length > 0) {
        setSelectedTenant(userTenants[0]);
      }
    } catch (err) {
      console.error("Failed to load profile:", err);
      if (err instanceof ApiError && err.status === 401) {
        // Token expired, clear it
        setDevToken(null);
      }
    } finally {
      setIsLoading(false);
    }
  }, [getAccessToken, session, setDevToken, setSelectedTenant]);

  // Load profile on auth change
  useEffect(() => {
    if (status === "loading") return;

    if (status === "authenticated" || devToken) {
      refreshProfile();
    } else {
      setUser(null);
      setTenants([]);
      setSelectedTenantState(null);
      setIsLoading(false);
    }
  }, [status, devToken, refreshProfile]);

  // Sync access token to storage for API client
  useEffect(() => {
    const token = getAccessToken();
    if (token) {
      setStoredToken(token);
    }
  }, [getAccessToken]);

  // Listen for unauthorized events from API client
  useEffect(() => {
    const handleUnauthorized = () => {
      // Silently clear auth state on 401 (user not logged in or token expired)
      setUser(null);
      setTenants([]);
      setSelectedTenantState(null);
      setDevTokenState(null);
      // Don't call signOut here to avoid redirect loops
      // Just clear local state and let user see login UI
    };

    window.addEventListener("auth:unauthorized", handleUnauthorized);
    return () => {
      window.removeEventListener("auth:unauthorized", handleUnauthorized);
    };
  }, []);

  const isAuthenticated = !!user;
  const isSuperAdmin = user?.isSuperAdmin || user?.roles?.includes("super_admin") || false;
  const isTenantAdmin =
    selectedTenant?.role === "TenantAdmin" || isSuperAdmin;

  const login = () => signIn("keycloak");
  const logout = () => {
    setDevToken(null);
    setSelectedTenant(null);
    signOut();
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        isLoading,
        isAuthenticated,
        isSuperAdmin,
        isTenantAdmin,
        tenants,
        selectedTenant,
        setSelectedTenant,
        login,
        logout,
        devToken,
        setDevToken,
        refreshProfile,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}


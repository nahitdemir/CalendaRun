"use client";

import {
  createContext,
  useContext,
  useState,
  useEffect,
  useCallback,
  ReactNode,
} from "react";
import { useRouter } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import {
  authApi,
  UserProfile,
  TenantMembership,
  ApiError,
  getStoredTenantId,
  setStoredTenantId,
} from "@/lib/api-client";

interface AuthContextType {
  user: UserProfile | null;
  isLoading: boolean;
  isAuthenticated: boolean;

  isSuperAdmin: boolean;
  isTenantAdmin: boolean;

  tenants: TenantMembership[];
  selectedTenant: TenantMembership | null;
  setSelectedTenant: (tenant: TenantMembership | null) => void;

  login: () => void;
  logout: () => void;

  refreshProfile: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const router = useRouter();
  const queryClient = useQueryClient();

  const [user, setUser] = useState<UserProfile | null>(null);
  const [tenants, setTenants] = useState<TenantMembership[]>([]);
  const [selectedTenant, setSelectedTenantState] = useState<TenantMembership | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isLoggingOut, setIsLoggingOut] = useState(false);

  const clearAuthState = useCallback(() => {
    setUser(null);
    setTenants([]);
    setSelectedTenantState(null);
    setStoredTenantId(null);
    if (typeof window !== "undefined") {
      [
        "calendarun_access_token",
        "calendarun_refresh_token",
        "calendarun_id_token",
        "calendarun-access-token",
      ].forEach((key) => {
        localStorage.removeItem(key);
        sessionStorage.removeItem(key);
      });
    }
    setIsLoading(false);
    queryClient.clear();
  }, [queryClient]);

  const setSelectedTenant = useCallback((tenant: TenantMembership | null) => {
    setSelectedTenantState(tenant);
    setStoredTenantId(tenant?.tenantId || null);
  }, []);

  const refreshProfile = useCallback(async () => {
    if (isLoggingOut) return;

    setIsLoading(true);
    try {
      const profile = await authApi.getMe();
      setUser(profile);

      const userTenants = await authApi.getMyTenants().catch(() => []);
      setTenants(userTenants);

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
    } catch (error) {
      if (error instanceof ApiError && error.status !== 401) {
        console.error("Failed to load profile:", error);
      }
      clearAuthState();
    } finally {
      setIsLoading(false);
    }
  }, [isLoggingOut, clearAuthState, setSelectedTenant]);

  useEffect(() => {
    refreshProfile();
  }, [refreshProfile]);

  const login = useCallback(() => {
    if (typeof window === "undefined") return;
    const callbackUrl = `${window.location.pathname}${window.location.search}`;
    const safeCallbackUrl = callbackUrl.startsWith("/login")
      ? "/"
      : callbackUrl;
    router.push(`/login?callbackUrl=${encodeURIComponent(safeCallbackUrl || "/")}`);
  }, [router]);

  const logout = useCallback(() => {
    const run = async () => {
      setIsLoggingOut(true);
      clearAuthState();

      try {
        await fetch("/api/auth/logout", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        });
      } catch (error) {
        console.error("Failed to logout:", error);
      }

      router.replace("/login");
      setIsLoggingOut(false);
    };

    void run();
  }, [clearAuthState, router]);

  useEffect(() => {
    const handleUnauthorized = () => {
      if (isLoggingOut) return;
      if (!user) {
        clearAuthState();
        return;
      }
      logout();
    };

    window.addEventListener("auth:unauthorized", handleUnauthorized);
    return () => {
      window.removeEventListener("auth:unauthorized", handleUnauthorized);
    };
  }, [logout, isLoggingOut, user, clearAuthState]);

  const isAuthenticated = !!user;
  const isSuperAdmin = user?.isSuperAdmin || user?.roles?.includes("super_admin") || false;
  const isTenantAdmin = selectedTenant?.role === "TenantAdmin" || isSuperAdmin;

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

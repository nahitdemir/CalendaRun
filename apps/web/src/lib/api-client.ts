/**
 * Typed API Client with ProblemDetails handling
 * Centralizes all API calls with proper error handling and tenant/auth headers
 */

const API_BASE = process.env.NEXT_PUBLIC_API_URL || "http://localhost:8080";

// ============ Types ============

export interface ProblemDetails {
  type?: string;
  title: string;
  status: number;
  detail?: string;
  instance?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}

export class ApiError extends Error {
  constructor(
    message: string,
    public status: number,
    public problemDetails?: ProblemDetails
  ) {
    super(message);
    this.name = "ApiError";
  }

  get title(): string {
    return this.problemDetails?.title || this.message;
  }

  get detail(): string | undefined {
    return this.problemDetails?.detail;
  }

  get validationErrors(): Record<string, string[]> | undefined {
    return this.problemDetails?.errors;
  }

  get traceId(): string | undefined {
    return this.problemDetails?.traceId;
  }
}

// ============ Request Configuration ============

interface RequestConfig {
  method?: "GET" | "POST" | "PUT" | "PATCH" | "DELETE";
  body?: unknown;
  params?: Record<string, string | number | boolean | undefined>;
  skipTenant?: boolean;
  skipAuth?: boolean;
}

// ============ Storage Keys ============

const STORAGE_KEYS = {
  accessToken: "calendarun-access-token",
  tenantId: "calendarun-selected-tenant-id",
  locale: "calendarun-locale",
} as const;

// ============ Token/Tenant Management ============

export function getStoredToken(): string | null {
  if (typeof window === "undefined") return null;
  return localStorage.getItem(STORAGE_KEYS.accessToken);
}

export function setStoredToken(token: string | null): void {
  if (typeof window === "undefined") return;
  if (token) {
    localStorage.setItem(STORAGE_KEYS.accessToken, token);
  } else {
    localStorage.removeItem(STORAGE_KEYS.accessToken);
  }
}

export function getStoredTenantId(): string | null {
  if (typeof window === "undefined") return null;
  return localStorage.getItem(STORAGE_KEYS.tenantId);
}

export function setStoredTenantId(tenantId: string | null): void {
  if (typeof window === "undefined") return;
  if (tenantId) {
    localStorage.setItem(STORAGE_KEYS.tenantId, tenantId);
  } else {
    localStorage.removeItem(STORAGE_KEYS.tenantId);
  }
}

export function getStoredLocale(): string {
  if (typeof window === "undefined") return "tr";
  return localStorage.getItem(STORAGE_KEYS.locale) || "tr";
}

// ============ Core Request Function ============

async function request<T>(
  endpoint: string,
  config: RequestConfig = {}
): Promise<T> {
  const { method = "GET", body, params, skipTenant, skipAuth } = config;

  // Build URL with query params
  let url = `${API_BASE}${endpoint}`;
  if (params) {
    const searchParams = new URLSearchParams();
    Object.entries(params).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== "") {
        searchParams.set(key, String(value));
      }
    });
    const queryString = searchParams.toString();
    if (queryString) {
      url += `?${queryString}`;
    }
  }

  // Build headers
  const headers: Record<string, string> = {
    "Content-Type": "application/json",
    Accept: "application/json",
    "Accept-Language": getStoredLocale(),
  };

  // Add auth token
  if (!skipAuth) {
    const token = getStoredToken();
    if (token) {
      headers["Authorization"] = `Bearer ${token}`;
    }
  }

  // Add tenant header
  if (!skipTenant) {
    const tenantId = getStoredTenantId();
    if (tenantId) {
      headers["X-Tenant-Id"] = tenantId;
    }
  }

  // Make request
  const response = await fetch(url, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  });

  // Handle errors
  if (!response.ok) {
    const contentType = response.headers.get("Content-Type") || "";

    if (contentType.includes("application/problem+json") || contentType.includes("application/json")) {
      try {
        const problemDetails = await response.json() as ProblemDetails;
        throw new ApiError(
          problemDetails.detail || problemDetails.title || `HTTP ${response.status}`,
          response.status,
          problemDetails
        );
      } catch (e) {
        if (e instanceof ApiError) throw e;
        throw new ApiError(`HTTP ${response.status}`, response.status);
      }
    }

    throw new ApiError(`HTTP ${response.status}`, response.status);
  }

  // Handle empty responses
  if (response.status === 204 || response.headers.get("Content-Length") === "0") {
    return undefined as T;
  }

  return response.json();
}

// ============ HTTP Method Helpers ============

export const api = {
  get: <T>(endpoint: string, params?: Record<string, string | number | boolean | undefined>) =>
    request<T>(endpoint, { method: "GET", params }),

  post: <T>(endpoint: string, body?: unknown) =>
    request<T>(endpoint, { method: "POST", body }),

  put: <T>(endpoint: string, body?: unknown) =>
    request<T>(endpoint, { method: "PUT", body }),

  patch: <T>(endpoint: string, body?: unknown) =>
    request<T>(endpoint, { method: "PATCH", body }),

  delete: <T>(endpoint: string) =>
    request<T>(endpoint, { method: "DELETE" }),
};

// ============ Entity Types ============

export interface Tenant {
  id: string;
  name: string;
  slug: string;
  description?: string;
  defaultLanguage: string;
  defaultCurrency: string;
  status: string;
  createdAt: string;
  memberCount?: number;
}

export interface TenantMembership {
  tenantId: string;
  tenantName: string;
  tenantSlug: string;
  role: "TenantAdmin" | "TenantUser";
  createdAt: string;
}

export interface UserProfile {
  id: string;
  email: string;
  name?: string;
  roles: string[];
  isSuperAdmin: boolean;
}

export interface Event {
  id: string;
  tenantId: string | null;
  title: string;
  description: string | null;
  startAt: string;
  endAt?: string;
  city: string;
  countryCode: string;
  registrationUrl: string | null;
  organizerName?: string;
  organizerUrl?: string;
  distances?: string[];
  createdAt: string;
  createdBy: string | null;
  updatedAt: string | null;
  isGlobal?: boolean;
  isPublished?: boolean;
}

export interface EventMilestone {
  id: string;
  eventId: string;
  type: "REG_OPEN" | "REG_CLOSE" | "EVENT_START" | "EVENT_END" | "CUSTOM";
  label: string;
  date: string;
  description?: string;
}

export interface PlanItem {
  id: string;
  eventId: string;
  userId: string;
  tenantId: string;
  state: "Active" | "Registered" | "Completed" | "Cancelled";
  timezone: string;
  createdAt: string;
  event?: Event;
}

export interface EventFilters {
  city?: string;
  dateFrom?: string;
  dateTo?: string;
  distances?: string;
  page?: number;
  pageSize?: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// ============ API Functions ============

// --- Auth / Profile ---

export const authApi = {
  getMe: () => api.get<UserProfile>("/api/me"),
  getMyTenants: () => api.get<TenantMembership[]>("/api/me/tenants"),
};

// --- Events (Public) ---

export const eventsApi = {
  list: (filters?: EventFilters) =>
    api.get<Event[]>("/api/events", filters as Record<string, string | number | boolean | undefined>),

  getById: (id: string) => api.get<Event>(`/api/events/${id}`),

  getMilestones: (id: string) => api.get<EventMilestone[]>(`/api/events/${id}/milestones`),

  getIcsUrl: (id: string) => `${API_BASE}/api/events/${id}/ics`,

  getCities: () => api.get<string[]>("/api/events/cities"),
};

// --- Plans ---

export const plansApi = {
  list: () => api.get<PlanItem[]>("/api/plan"),

  create: (eventId: string) => api.post<PlanItem>("/api/plan", { eventId }),

  updateState: (id: string, state: "Registered" | "Completed") =>
    api.patch<PlanItem>(`/api/plan/${id}`, { state }),

  delete: (id: string) => api.delete<void>(`/api/plan/${id}`),
};

// --- Admin Events ---

export const adminEventsApi = {
  list: () => api.get<Event[]>("/api/admin/events"),

  create: (data: {
    title: string;
    description?: string;
    startAt: string;
    city: string;
    countryCode: string;
    registrationUrl?: string;
    distances?: string[];
    isGlobal?: boolean;
  }) => api.post<Event>("/api/events", data),

  update: (id: string, data: Partial<Event>) =>
    api.put<Event>(`/api/events/${id}`, data),

  delete: (id: string) => api.delete<void>(`/api/events/${id}`),

  publish: (id: string) => api.patch<Event>(`/api/events/${id}/publish`, {}),

  unpublish: (id: string) => api.patch<Event>(`/api/events/${id}/unpublish`, {}),
};

// --- Admin Users ---

export const adminUsersApi = {
  list: () => api.get<{ id: string; userId: string; userEmail: string; role: string; createdAt: string }[]>("/api/admin/users"),

  invite: (email: string, role: "TenantAdmin" | "TenantUser") =>
    api.post<{ id: string; token: string }>("/api/admin/invites", { email, role }),
};

// --- Super Admin ---

export const superAdminApi = {
  listTenants: () => api.get<Tenant[]>("/api/super-admin/tenants"),

  createTenant: (data: {
    name: string;
    slug?: string;
    description?: string;
    defaultLanguage?: string;
    defaultCurrency?: string;
  }) => api.post<Tenant>("/api/super-admin/tenants", data),

  listAllEvents: (params?: { tenantId?: string; page?: number; pageSize?: number }) =>
    api.get<PagedResult<Event>>("/api/super-admin/events", params as Record<string, string | number | boolean | undefined>),
};


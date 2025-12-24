const API_BASE = process.env.NEXT_PUBLIC_API_URL || "http://localhost:8080";

interface FetchOptions extends RequestInit {
  accessToken?: string;
  tenantId?: string;
}

export async function apiClient<T>(
  endpoint: string,
  options: FetchOptions = {}
): Promise<T> {
  const { accessToken, tenantId, ...fetchOptions } = options;

  const headers: HeadersInit = {
    "Content-Type": "application/json",
    ...(options.headers || {}),
  };

  if (accessToken) {
    (headers as Record<string, string>)["Authorization"] = `Bearer ${accessToken}`;
  }

  if (tenantId) {
    (headers as Record<string, string>)["X-Tenant-Id"] = tenantId;
  }

  const response = await fetch(`${API_BASE}${endpoint}`, {
    ...fetchOptions,
    headers,
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({ error: "Unknown error" }));
    throw new Error(error.error || `HTTP ${response.status}`);
  }

  return response.json();
}

// Tenant types
export interface Tenant {
  id: string;
  name: string;
  slug: string;
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
  role: string;
  createdAt: string;
}

// Event types
export interface Event {
  id: string;
  tenantId: string | null;
  title: string;
  description: string | null;
  startAt: string;
  city: string;
  countryCode: string;
  registrationUrl: string;
  createdAt: string;
  createdBy: string | null;
  updatedAt: string | null;
  updatedBy: string | null;
  isGlobal?: boolean;
}

// User types
export interface User {
  id: string;
  userId: string;
  userEmail: string;
  role: string;
  createdAt: string;
  acceptedAt: string | null;
}

// API functions

// Tenants
export const getTenants = (accessToken: string) =>
  apiClient<Tenant[]>("/super-admin/tenants", { accessToken });

export const createTenant = (
  accessToken: string,
  data: { name: string; slug?: string; description?: string; defaultLanguage?: string; defaultCurrency?: string }
) =>
  apiClient<Tenant>("/super-admin/tenants", {
    method: "POST",
    accessToken,
    body: JSON.stringify(data),
  });

export const getMyTenants = (accessToken: string) =>
  apiClient<TenantMembership[]>("/me/tenants", { accessToken });

// Events
export const getAdminEvents = (accessToken: string, tenantId?: string) =>
  apiClient<Event[]>("/admin/events", { accessToken, tenantId });

export const getEvents = (accessToken: string, tenantId?: string) =>
  apiClient<Event[]>("/events", { accessToken, tenantId });

export const createEvent = (
  accessToken: string,
  tenantId: string,
  data: {
    title: string;
    description?: string;
    startAt: string;
    city: string;
    countryCode: string;
    registrationUrl?: string;
    isGlobal?: boolean;
  }
) =>
  apiClient<Event>("/events", {
    method: "POST",
    accessToken,
    tenantId,
    body: JSON.stringify(data),
  });

export const updateEvent = (
  accessToken: string,
  tenantId: string,
  eventId: string,
  data: Partial<Event>
) =>
  apiClient<Event>(`/events/${eventId}`, {
    method: "PUT",
    accessToken,
    tenantId,
    body: JSON.stringify(data),
  });

export const deleteEvent = (accessToken: string, tenantId: string, eventId: string) =>
  apiClient<void>(`/events/${eventId}`, {
    method: "DELETE",
    accessToken,
    tenantId,
  });

// Users
export const getTenantUsers = (accessToken: string, tenantId: string) =>
  apiClient<User[]>("/admin/users", { accessToken, tenantId });

// Audit Log types
export interface AuditLog {
  id: string;
  tenantId: string | null;
  actorUserId: string;
  actorEmail: string | null;
  action: string;
  entityType: string;
  entityId: string | null;
  beforeJson: string | null;
  afterJson: string | null;
  traceId: string | null;
  timestamp: string;
}

export interface AuditLogPagedResult {
  items: AuditLog[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface AuditLogFilter {
  entityType?: string;
  action?: string;
  actorUserId?: string;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
  tenantId?: string;
}

// Audit Logs
export const getAuditLogs = (
  accessToken: string,
  tenantId: string | undefined,
  filters: AuditLogFilter = {}
) => {
  const params = new URLSearchParams();
  if (filters.entityType) params.set("entityType", filters.entityType);
  if (filters.action) params.set("action", filters.action);
  if (filters.actorUserId) params.set("actorUserId", filters.actorUserId);
  if (filters.from) params.set("from", filters.from);
  if (filters.to) params.set("to", filters.to);
  if (filters.page) params.set("page", filters.page.toString());
  if (filters.pageSize) params.set("pageSize", filters.pageSize.toString());
  
  const queryString = params.toString();
  const endpoint = `/admin/audit${queryString ? `?${queryString}` : ""}`;
  
  return apiClient<AuditLogPagedResult>(endpoint, { accessToken, tenantId });
};

export const getSuperAdminAuditLogs = (
  accessToken: string,
  filters: AuditLogFilter = {}
) => {
  const params = new URLSearchParams();
  if (filters.tenantId) params.set("tenantId", filters.tenantId);
  if (filters.entityType) params.set("entityType", filters.entityType);
  if (filters.action) params.set("action", filters.action);
  if (filters.actorUserId) params.set("actorUserId", filters.actorUserId);
  if (filters.from) params.set("from", filters.from);
  if (filters.to) params.set("to", filters.to);
  if (filters.page) params.set("page", filters.page.toString());
  if (filters.pageSize) params.set("pageSize", filters.pageSize.toString());
  
  const queryString = params.toString();
  const endpoint = `/super-admin/audit${queryString ? `?${queryString}` : ""}`;
  
  return apiClient<AuditLogPagedResult>(endpoint, { accessToken });
};


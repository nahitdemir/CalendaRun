"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "@/contexts/locale-context";
import { SuperAdminGuard } from "@/components/route-guards";
import { PageHeader } from "@/components/page-header";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { api, superAdminApi } from "@/lib/api-client";
import { ChevronLeft, ChevronRight } from "lucide-react";

const ACTIONS = [
  "TenantCreated",
  "InviteCreated",
  "InviteAccepted",
  "MembershipRoleChanged",
  "EventCreated",
  "EventUpdated",
  "EventDeleted",
  "PlanCreated",
  "PlanDeleted",
];

const ENTITY_TYPES = ["Tenant", "Membership", "Invite", "Event", "UserPlanItem"];

interface AuditLog {
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

interface AuditLogPagedResult {
  items: AuditLog[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

function JsonPreview({ json, title }: { json: string | null; title: string }) {
  if (!json) return null;

  try {
    const parsed = JSON.parse(json);
    return (
      <details className="mt-2">
        <summary className="cursor-pointer text-xs text-muted-foreground hover:text-foreground">
          {title}
        </summary>
        <pre className="mt-1 p-2 bg-muted rounded-lg text-xs overflow-auto max-h-40">
          {JSON.stringify(parsed, null, 2)}
        </pre>
      </details>
    );
  } catch {
    return null;
  }
}

function SuperAdminAuditContent() {
  const { t, locale } = useTranslation();

  const [selectedTenantId, setSelectedTenantId] = useState<string>("");
  const [selectedAction, setSelectedAction] = useState<string>("");
  const [selectedEntityType, setSelectedEntityType] = useState<string>("");
  const [currentPage, setCurrentPage] = useState(1);
  const pageSize = 20;

  // Format date
  const formatDate = (dateStr: string) => {
    return new Intl.DateTimeFormat(locale === "tr" ? "tr-TR" : "en-US", {
      dateStyle: "short",
      timeStyle: "medium",
    }).format(new Date(dateStr));
  };

  // Fetch tenants for filter
  const { data: tenants } = useQuery({
    queryKey: ["super-admin-tenants-filter"],
    queryFn: () => superAdminApi.listTenants(),
  });

  // Fetch audit logs
  const { data: auditLogs, isLoading } = useQuery({
    queryKey: [
      "super-admin-audit",
      selectedTenantId,
      selectedAction,
      selectedEntityType,
      currentPage,
    ],
    queryFn: () => {
      const params: Record<string, string | number> = {
        page: currentPage,
        pageSize,
      };
      if (selectedTenantId) params.tenantId = selectedTenantId;
      if (selectedAction) params.action = selectedAction;
      if (selectedEntityType) params.entityType = selectedEntityType;

      return api.get<AuditLogPagedResult>("/api/super-admin/audit", params);
    },
  });

  const getActionBadgeVariant = (action: string) => {
    if (action.includes("Created")) return "success";
    if (action.includes("Updated")) return "accent";
    if (action.includes("Deleted")) return "destructive";
    if (action.includes("Accepted")) return "secondary";
    return "muted";
  };

  if (isLoading) {
    return (
      <div className="container-app space-y-6">
        <Skeleton className="h-10 w-48" />
        <Skeleton className="h-16 w-full rounded-2xl" />
        <div className="rounded-2xl border">
          <div className="p-4">
            {[1, 2, 3, 4, 5].map((i) => (
              <div key={i} className="flex gap-4 py-4 border-b last:border-0">
                <Skeleton className="h-4 w-32" />
                <Skeleton className="h-4 w-24" />
                <Skeleton className="h-4 w-48" />
              </div>
            ))}
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="container-app space-y-6">
      <PageHeader title={t("super.audit.title")} />

      {/* Filters */}
      <div className="rounded-2xl border bg-card p-4">
        <div className="flex flex-wrap gap-4">
          <div className="flex-1 min-w-[200px]">
            <label className="block text-sm text-muted-foreground mb-1">
              Tenant
            </label>
            <select
              value={selectedTenantId}
              onChange={(e) => {
                setSelectedTenantId(e.target.value);
                setCurrentPage(1);
              }}
              className="w-full h-10 px-3 bg-background border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-ring"
            >
              <option value="">
                {locale === "tr" ? "Tüm Tenant'lar" : "All Tenants"}
              </option>
              {tenants?.map((tenant) => (
                <option key={tenant.id} value={tenant.id}>
                  {tenant.name}
                </option>
              ))}
            </select>
          </div>

          <div className="flex-1 min-w-[200px]">
            <label className="block text-sm text-muted-foreground mb-1">
              {locale === "tr" ? "Aksiyon" : "Action"}
            </label>
            <select
              value={selectedAction}
              onChange={(e) => {
                setSelectedAction(e.target.value);
                setCurrentPage(1);
              }}
              className="w-full h-10 px-3 bg-background border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-ring"
            >
              <option value="">
                {locale === "tr" ? "Tüm Aksiyonlar" : "All Actions"}
              </option>
              {ACTIONS.map((action) => (
                <option key={action} value={action}>
                  {action}
                </option>
              ))}
            </select>
          </div>

          <div className="flex-1 min-w-[200px]">
            <label className="block text-sm text-muted-foreground mb-1">
              {locale === "tr" ? "Entity Tipi" : "Entity Type"}
            </label>
            <select
              value={selectedEntityType}
              onChange={(e) => {
                setSelectedEntityType(e.target.value);
                setCurrentPage(1);
              }}
              className="w-full h-10 px-3 bg-background border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-ring"
            >
              <option value="">
                {locale === "tr" ? "Tüm Entity'ler" : "All Entities"}
              </option>
              {ENTITY_TYPES.map((type) => (
                <option key={type} value={type}>
                  {type}
                </option>
              ))}
            </select>
          </div>
        </div>
      </div>

      {/* Table */}
      <div className="rounded-2xl border bg-card overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr className="border-b bg-muted/50">
                <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">
                  {locale === "tr" ? "Zaman" : "Time"}
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">
                  {locale === "tr" ? "Aksiyon" : "Action"}
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">
                  Entity
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">
                  {locale === "tr" ? "Kullanıcı" : "User"}
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">
                  {locale === "tr" ? "Detay" : "Detail"}
                </th>
              </tr>
            </thead>
            <tbody className="divide-y">
              {auditLogs?.items.map((log) => (
                <tr key={log.id} className="hover:bg-muted/30 transition-colors">
                  <td className="px-4 py-3 whitespace-nowrap">
                    <span className="text-sm">{formatDate(log.timestamp)}</span>
                  </td>
                  <td className="px-4 py-3 whitespace-nowrap">
                    <Badge variant={getActionBadgeVariant(log.action) as "success" | "accent" | "destructive" | "secondary" | "muted"}>
                      {log.action}
                    </Badge>
                  </td>
                  <td className="px-4 py-3 whitespace-nowrap">
                    <div className="text-sm">{log.entityType}</div>
                    {log.entityId && (
                      <code className="text-xs text-muted-foreground">
                        {log.entityId.substring(0, 8)}...
                      </code>
                    )}
                  </td>
                  <td className="px-4 py-3 whitespace-nowrap">
                    <div className="text-sm">{log.actorEmail || "—"}</div>
                    <code className="text-xs text-muted-foreground">
                      {log.actorUserId.substring(0, 8)}...
                    </code>
                  </td>
                  <td className="px-4 py-3">
                    <JsonPreview
                      json={log.beforeJson}
                      title={locale === "tr" ? "📄 Önceki durum" : "📄 Before"}
                    />
                    <JsonPreview
                      json={log.afterJson}
                      title={locale === "tr" ? "📝 Sonraki durum" : "📝 After"}
                    />
                    {log.traceId && (
                      <div className="mt-1 text-xs text-muted-foreground">
                        TraceId: {log.traceId.substring(0, 12)}...
                      </div>
                    )}
                  </td>
                </tr>
              ))}
              {auditLogs?.items.length === 0 && (
                <tr>
                  <td colSpan={5} className="px-4 py-8 text-center text-muted-foreground">
                    {t("empty.noResultsTitle")}
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Pagination */}
      {auditLogs && auditLogs.totalPages > 1 && (
        <div className="flex items-center justify-between">
          <div className="text-sm text-muted-foreground">
            {locale === "tr"
              ? `Toplam ${auditLogs.totalCount} kayıt (${auditLogs.totalPages} sayfa)`
              : `Total ${auditLogs.totalCount} records (${auditLogs.totalPages} pages)`}
          </div>
          <div className="flex gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
              disabled={currentPage === 1}
            >
              <ChevronLeft className="h-4 w-4" />
            </Button>
            <span className="flex items-center px-3 text-sm">
              {currentPage} / {auditLogs.totalPages}
            </span>
            <Button
              variant="outline"
              size="sm"
              onClick={() =>
                setCurrentPage((p) => Math.min(auditLogs.totalPages, p + 1))
              }
              disabled={currentPage === auditLogs.totalPages}
            >
              <ChevronRight className="h-4 w-4" />
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}

export default function SuperAdminAuditPage() {
  return (
    <SuperAdminGuard>
      <SuperAdminAuditContent />
    </SuperAdminGuard>
  );
}

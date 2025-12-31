"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useAuth } from "@/contexts/auth-context";
import { useTranslation } from "@/contexts/locale-context";
import { TenantAdminGuard } from "@/components/route-guards";
import { PageHeader } from "@/components/page-header";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { api } from "@/lib/api-client";
import { ListPagination, ListToolbar, PageShell, ResultsHeader } from "@/components/listing";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

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

function AdminAuditContent() {
  const { selectedTenant } = useAuth();
  const { t, locale } = useTranslation();

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

  // Fetch audit logs
  const { data: auditLogs, isLoading } = useQuery({
    queryKey: [
      "admin-audit",
      selectedTenant?.tenantId,
      selectedAction,
      selectedEntityType,
      currentPage,
    ],
    queryFn: () => {
      const params: Record<string, string | number> = {
        page: currentPage,
        pageSize,
      };
      if (selectedAction) params.action = selectedAction;
      if (selectedEntityType) params.entityType = selectedEntityType;

      return api.get<AuditLogPagedResult>("/api/admin/audit", params);
    },
    enabled: !!selectedTenant?.tenantId,
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
      <PageShell>
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
      </PageShell>
    );
  }

  const hasActiveFilters = Boolean(selectedAction || selectedEntityType);

  return (
    <PageShell>
      <PageHeader
        title={t("admin.audit.title")}
        subtitle={selectedTenant?.tenantName}
      />

      <ListToolbar
        left={
          <div className="flex flex-wrap items-center gap-4">
            <div className="min-w-[200px]">
              <label className="mb-1 block text-sm text-muted-foreground">
                {locale === "tr" ? "Aksiyon" : "Action"}
              </label>
              <Select
                value={selectedAction || "all"}
                onValueChange={(value) => {
                  setSelectedAction(value === "all" ? "" : value);
                  setCurrentPage(1);
                }}
              >
                <SelectTrigger className="h-9 w-full">
                  <SelectValue placeholder={locale === "tr" ? "Tüm Aksiyonlar" : "All Actions"} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">
                    {locale === "tr" ? "Tüm Aksiyonlar" : "All Actions"}
                  </SelectItem>
                  {ACTIONS.map((action) => (
                    <SelectItem key={action} value={action}>
                      {action}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="min-w-[200px]">
              <label className="mb-1 block text-sm text-muted-foreground">
                {locale === "tr" ? "Entity Tipi" : "Entity Type"}
              </label>
              <Select
                value={selectedEntityType || "all"}
                onValueChange={(value) => {
                  setSelectedEntityType(value === "all" ? "" : value);
                  setCurrentPage(1);
                }}
              >
                <SelectTrigger className="h-9 w-full">
                  <SelectValue placeholder={locale === "tr" ? "Tüm Entity'ler" : "All Entities"} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">
                    {locale === "tr" ? "Tüm Entity'ler" : "All Entities"}
                  </SelectItem>
                  {ENTITY_TYPES.map((type) => (
                    <SelectItem key={type} value={type}>
                      {type}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>
        }
        showClear={hasActiveFilters}
        clearLabel={t("filters.clear")}
        onClear={() => {
          setSelectedAction("");
          setSelectedEntityType("");
          setCurrentPage(1);
        }}
      />

      <ResultsHeader count={auditLogs?.totalCount ?? 0} />

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

      <ListPagination
        page={currentPage}
        pageSize={pageSize}
        total={auditLogs?.totalCount}
        onPageChange={(nextPage) => {
          setCurrentPage(nextPage);
          window.scrollTo({ top: 0, behavior: "smooth" });
        }}
      />
    </PageShell>
  );
}

export default function AdminAuditPage() {
  return (
    <TenantAdminGuard>
      <AdminAuditContent />
    </TenantAdminGuard>
  );
}

"use client";

import { useQuery } from "@tanstack/react-query";
import { useAuth } from "@/contexts/auth-context";
import { useTranslation } from "@/contexts/locale-context";
import { adminUsersApi } from "@/lib/api-client";
import { TenantAdminGuard } from "@/components/route-guards";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { PageHeader } from "@/components/page-header";
import { EmptyState } from "@/components/empty-state";
import { Skeleton } from "@/components/ui/skeleton";
import { Badge } from "@/components/ui/badge";
import { Users, Mail, Shield, UserCheck } from "lucide-react";

function AdminUsersContent() {
  const { selectedTenant } = useAuth();
  const { t, locale } = useTranslation();

  // Format date
  const formatDate = (dateStr: string | null) => {
    if (!dateStr) return "-";
    return new Intl.DateTimeFormat(locale === "tr" ? "tr-TR" : "en-US", {
      dateStyle: "medium",
      timeStyle: "short",
    }).format(new Date(dateStr));
  };

  // Fetch users
  const { data: users, isLoading } = useQuery({
    queryKey: ["admin-users", selectedTenant?.tenantId],
    queryFn: () => adminUsersApi.list(),
    enabled: !!selectedTenant?.tenantId,
  });

  if (isLoading) {
    return (
      <div className="container-app space-y-6">
        <Skeleton className="h-10 w-48" />
        <div className="rounded-2xl border">
          <div className="p-4">
            {[1, 2, 3].map((i) => (
              <div key={i} className="flex items-center gap-4 py-4 border-b last:border-0">
                <Skeleton className="h-10 w-10 rounded-full" />
                <div className="flex-1">
                  <Skeleton className="h-4 w-48 mb-2" />
                  <Skeleton className="h-3 w-32" />
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="container-app space-y-6">
      <PageHeader
        title={t("admin.users.title")}
        subtitle={selectedTenant?.tenantName}
      />

      {users && users.length === 0 ? (
        <EmptyState
          variant="generic"
          icon={Users}
          title={locale === "tr" ? "Henüz kullanıcı yok" : "No users yet"}
          description={
            locale === "tr"
              ? "Kullanıcılar davetleri kabul ettiğinde burada görünecekler"
              : "Users will appear here when they accept invitations"
          }
        />
      ) : (
        <>
          <div className="rounded-2xl border bg-card overflow-hidden">
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="border-b bg-muted/50">
                    <th className="px-4 py-3 text-left text-sm font-medium text-muted-foreground">
                      {locale === "tr" ? "Kullanıcı" : "User"}
                    </th>
                    <th className="px-4 py-3 text-left text-sm font-medium text-muted-foreground">
                      {t("admin.users.role")}
                    </th>
                    <th className="px-4 py-3 text-left text-sm font-medium text-muted-foreground">
                      {locale === "tr" ? "Katılma" : "Joined"}
                    </th>
                    <th className="px-4 py-3 text-left text-sm font-medium text-muted-foreground">
                      {locale === "tr" ? "Durum" : "Status"}
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {users?.map((user) => (
                    <tr
                      key={user.id}
                      className="border-b last:border-0 hover:bg-muted/30 transition-colors"
                    >
                      <td className="px-4 py-3">
                        <div className="flex items-center gap-3">
                          <div className="flex h-9 w-9 items-center justify-center rounded-full bg-primary/10">
                            <span className="text-sm font-medium text-primary">
                              {user.userEmail.charAt(0).toUpperCase()}
                            </span>
                          </div>
                          <div>
                            <div className="flex items-center gap-2">
                              <Mail className="h-3.5 w-3.5 text-muted-foreground" />
                              <span className="text-sm font-medium">
                                {user.userEmail}
                              </span>
                            </div>
                            <code className="text-xs text-muted-foreground">
                              {user.userId.slice(0, 8)}...
                            </code>
                          </div>
                        </div>
                      </td>
                      <td className="px-4 py-3">
                        <Badge
                          variant={user.role === "TenantAdmin" ? "accent" : "secondary"}
                          className="gap-1"
                        >
                          {user.role === "TenantAdmin" && (
                            <Shield className="h-3 w-3" />
                          )}
                          {user.role}
                        </Badge>
                      </td>
                      <td className="px-4 py-3">
                        <span className="text-sm text-muted-foreground">
                          {formatDate(user.createdAt)}
                        </span>
                      </td>
                      <td className="px-4 py-3">
                        <span className="inline-flex items-center gap-1 text-sm text-success">
                          <UserCheck className="h-4 w-4" />
                          {locale === "tr" ? "Aktif" : "Active"}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          {/* Stats Cards */}
          {users && users.length > 0 && (
            <div className="grid gap-4 md:grid-cols-3">
              <Card>
                <CardHeader className="pb-2">
                  <CardDescription>
                    {locale === "tr" ? "Toplam Kullanıcı" : "Total Users"}
                  </CardDescription>
                  <CardTitle className="text-3xl">{users.length}</CardTitle>
                </CardHeader>
              </Card>
              <Card>
                <CardHeader className="pb-2">
                  <CardDescription>
                    {locale === "tr" ? "Admin" : "Admins"}
                  </CardDescription>
                  <CardTitle className="text-3xl">
                    {users.filter((u) => u.role === "TenantAdmin").length}
                  </CardTitle>
                </CardHeader>
              </Card>
              <Card>
                <CardHeader className="pb-2">
                  <CardDescription>
                    {locale === "tr" ? "Normal Kullanıcı" : "Regular Users"}
                  </CardDescription>
                  <CardTitle className="text-3xl">
                    {users.filter((u) => u.role === "TenantUser").length}
                  </CardTitle>
                </CardHeader>
              </Card>
            </div>
          )}
        </>
      )}
    </div>
  );
}

export default function AdminUsersPage() {
  return (
    <TenantAdminGuard>
      <AdminUsersContent />
    </TenantAdminGuard>
  );
}

"use client";

import { useAuth } from "@/contexts/auth-context";
import { useTranslation } from "@/contexts/locale-context";
import { AuthGuard } from "@/components/route-guards";
import { PageHeader } from "@/components/page-header";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { User, Shield, Building2, Globe } from "lucide-react";

function SettingsContent() {
  const { user, tenants, isSuperAdmin } = useAuth();
  const { t, locale } = useTranslation();

  return (
    <div className="container-app max-w-2xl space-y-6">
      <PageHeader title={t("nav.settings")} />

      {/* Profile */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <User className="h-5 w-5" />
            {t("nav.profile")}
          </CardTitle>
          <CardDescription>
            {locale === "tr" ? "Hesap bilgileriniz" : "Your account information"}
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center justify-between">
            <span className="text-sm text-muted-foreground">Email</span>
            <span className="font-medium">{user?.email}</span>
          </div>
          {user?.name && (
            <div className="flex items-center justify-between">
              <span className="text-sm text-muted-foreground">
                {locale === "tr" ? "İsim" : "Name"}
              </span>
              <span className="font-medium">{user.name}</span>
            </div>
          )}
          <div className="flex items-center justify-between">
            <span className="text-sm text-muted-foreground">
              {locale === "tr" ? "Rol" : "Role"}
            </span>
            <div className="flex gap-2">
              {isSuperAdmin && (
                <Badge variant="warning" className="gap-1">
                  <Shield className="h-3 w-3" />
                  Super Admin
                </Badge>
              )}
              {user?.roles?.map((role) => (
                <Badge key={role} variant="secondary">
                  {role}
                </Badge>
              ))}
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Tenants */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Building2 className="h-5 w-5" />
            {locale === "tr" ? "Firmalarım" : "My Organizations"}
          </CardTitle>
          <CardDescription>
            {locale === "tr"
              ? "Üye olduğunuz firmalar"
              : "Organizations you belong to"}
          </CardDescription>
        </CardHeader>
        <CardContent>
          {tenants.length === 0 ? (
            <p className="text-sm text-muted-foreground">
              {locale === "tr"
                ? "Henüz bir firmaya üye değilsiniz."
                : "You are not a member of any organization yet."}
            </p>
          ) : (
            <ul className="space-y-3">
              {tenants.map((tenant) => (
                <li
                  key={tenant.tenantId}
                  className="flex items-center justify-between rounded-lg border p-3"
                >
                  <div className="flex items-center gap-3">
                    <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-primary/10">
                      <Building2 className="h-4 w-4 text-primary" />
                    </div>
                    <div>
                      <p className="font-medium">{tenant.tenantName}</p>
                      <code className="text-xs text-muted-foreground">
                        {tenant.tenantSlug}
                      </code>
                    </div>
                  </div>
                  <Badge
                    variant={tenant.role === "TenantAdmin" ? "accent" : "secondary"}
                  >
                    {tenant.role}
                  </Badge>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>

      {/* Language */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Globe className="h-5 w-5" />
            {locale === "tr" ? "Dil" : "Language"}
          </CardTitle>
          <CardDescription>
            {locale === "tr"
              ? "Uygulama dili sağ üst köşeden değiştirilebilir"
              : "Application language can be changed from the top right corner"}
          </CardDescription>
        </CardHeader>
        <CardContent>
          <p className="text-sm">
            {locale === "tr"
              ? "Mevcut dil: Türkçe 🇹🇷"
              : "Current language: English 🇬🇧"}
          </p>
        </CardContent>
      </Card>

    </div>
  );
}

export default function SettingsPage() {
  return (
    <AuthGuard>
      <SettingsContent />
    </AuthGuard>
  );
}

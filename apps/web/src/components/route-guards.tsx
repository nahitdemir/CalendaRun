"use client";

import { ReactNode } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/contexts/auth-context";
import { useTranslation } from "@/contexts/locale-context";
import { EmptyState } from "@/components/empty-state";
import { Skeleton } from "@/components/ui/skeleton";

interface GuardProps {
  children: ReactNode;
}

function LoadingState() {
  return (
    <div className="container-app space-y-6">
      <Skeleton className="h-10 w-48" />
      <div className="space-y-4">
        {[1, 2, 3].map((i) => (
          <Skeleton key={i} className="h-24 w-full rounded-2xl" />
        ))}
      </div>
    </div>
  );
}

export function AuthGuard({ children }: GuardProps) {
  const { isAuthenticated, isLoading, login } = useAuth();
  const { t } = useTranslation();

  if (isLoading) {
    return <LoadingState />;
  }

  if (!isAuthenticated) {
    return (
      <div className="container-app">
        <EmptyState
          variant="generic"
          title={t("nav.signIn")}
          description="Bu sayfayı görüntülemek için giriş yapmalısınız."
          action={{
            label: t("nav.signIn"),
            onClick: login,
          }}
        />
      </div>
    );
  }

  return <>{children}</>;
}

export function TenantRequiredGuard({ children }: GuardProps) {
  const { isAuthenticated, isLoading, selectedTenant, tenants, login } = useAuth();
  const { t } = useTranslation();
  const router = useRouter();

  if (isLoading) {
    return <LoadingState />;
  }

  if (!isAuthenticated) {
    return (
      <div className="container-app">
        <EmptyState
          variant="generic"
          title={t("nav.signIn")}
          description="Bu sayfayı görüntülemek için giriş yapmalısınız."
          action={{
            label: t("nav.signIn"),
            onClick: login,
          }}
        />
      </div>
    );
  }

  if (!selectedTenant && tenants.length === 0) {
    return (
      <div className="container-app">
        <EmptyState
          variant="tenant"
          title={t("empty.selectTenantTitle")}
          description="Henüz bir firmaya üye değilsiniz."
        />
      </div>
    );
  }

  if (!selectedTenant) {
    return (
      <div className="container-app">
        <EmptyState
          variant="tenant"
          title={t("empty.selectTenantTitle")}
          description={t("empty.selectTenantDesc")}
          action={{
            label: t("nav.selectTenant"),
            onClick: () => {
              // This will open tenant selector - for now just refresh
              router.refresh();
            },
          }}
        />
      </div>
    );
  }

  return <>{children}</>;
}

export function TenantAdminGuard({ children }: GuardProps) {
  const { isAuthenticated, isLoading, isTenantAdmin, selectedTenant, login } = useAuth();
  const { t } = useTranslation();
  const router = useRouter();

  if (isLoading) {
    return <LoadingState />;
  }

  if (!isAuthenticated) {
    return (
      <div className="container-app">
        <EmptyState
          variant="generic"
          title={t("nav.signIn")}
          description="Bu sayfayı görüntülemek için giriş yapmalısınız."
          action={{
            label: t("nav.signIn"),
            onClick: login,
          }}
        />
      </div>
    );
  }

  if (!selectedTenant) {
    return (
      <div className="container-app">
        <EmptyState
          variant="tenant"
          title={t("empty.selectTenantTitle")}
          description={t("empty.selectTenantDesc")}
        />
      </div>
    );
  }

  if (!isTenantAdmin) {
    return (
      <div className="container-app">
        <EmptyState
          variant="generic"
          title="Yetki Hatası"
          description="Bu sayfaya erişim yetkiniz yok."
          action={{
            label: t("nav.backToExplore"),
            onClick: () => router.push("/"),
          }}
        />
      </div>
    );
  }

  return <>{children}</>;
}

export function SuperAdminGuard({ children }: GuardProps) {
  const { isAuthenticated, isLoading, isSuperAdmin, login } = useAuth();
  const { t } = useTranslation();
  const router = useRouter();

  if (isLoading) {
    return <LoadingState />;
  }

  if (!isAuthenticated) {
    return (
      <div className="container-app">
        <EmptyState
          variant="generic"
          title={t("nav.signIn")}
          description="Bu sayfayı görüntülemek için giriş yapmalısınız."
          action={{
            label: t("nav.signIn"),
            onClick: login,
          }}
        />
      </div>
    );
  }

  if (!isSuperAdmin) {
    return (
      <div className="container-app">
        <EmptyState
          variant="generic"
          title="Yetki Hatası"
          description="Bu sayfa sadece Super Admin kullanıcıları içindir."
          action={{
            label: t("nav.backToExplore"),
            onClick: () => router.push("/"),
          }}
        />
      </div>
    );
  }

  return <>{children}</>;
}

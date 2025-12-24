"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "@/contexts/locale-context";
import { useToast } from "@/components/ui/toast";
import { useApiMutation } from "@/hooks/use-api-error";
import { superAdminApi } from "@/lib/api-client";
import { SuperAdminGuard } from "@/components/route-guards";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { PageHeader } from "@/components/page-header";
import { EmptyState } from "@/components/empty-state";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Building2, Plus, Users, Globe, Loader2 } from "lucide-react";

function TenantsPageContent() {
  const { t, locale } = useTranslation();
  const toast = useToast();
  const { onError } = useApiMutation();
  const queryClient = useQueryClient();

  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [formData, setFormData] = useState({
    name: "",
    slug: "",
    description: "",
    defaultLanguage: "tr",
    defaultCurrency: "TRY",
  });

  // Fetch tenants
  const { data: tenants, isLoading } = useQuery({
    queryKey: ["super-admin-tenants"],
    queryFn: () => superAdminApi.listTenants(),
  });

  // Create mutation
  const createMutation = useMutation({
    mutationFn: (data: typeof formData) => superAdminApi.createTenant(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["super-admin-tenants"] });
      setIsCreateOpen(false);
      setFormData({
        name: "",
        slug: "",
        description: "",
        defaultLanguage: "tr",
        defaultCurrency: "TRY",
      });
      toast.success(t("toast.created"));
    },
    onError: (err) => onError(err),
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    createMutation.mutate(formData);
  };

  if (isLoading) {
    return (
      <div className="container-app space-y-6">
        <div className="flex items-center justify-between">
          <Skeleton className="h-10 w-32" />
          <Skeleton className="h-10 w-32" />
        </div>
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {[1, 2, 3].map((i) => (
            <div key={i} className="rounded-2xl border bg-card p-5">
              <Skeleton className="mb-3 h-10 w-10 rounded-xl" />
              <Skeleton className="mb-2 h-6 w-2/3" />
              <Skeleton className="h-4 w-1/2" />
            </div>
          ))}
        </div>
      </div>
    );
  }

  return (
    <div className="container-app space-y-6">
      <PageHeader
        title={t("super.tenants.title")}
        actions={
          <Dialog open={isCreateOpen} onOpenChange={setIsCreateOpen}>
            <DialogTrigger asChild>
              <Button variant="accent" className="gap-2">
                <Plus className="h-4 w-4" />
                {t("super.tenants.create")}
              </Button>
            </DialogTrigger>
            <DialogContent>
              <form onSubmit={handleSubmit}>
                <DialogHeader>
                  <DialogTitle>{t("super.tenants.create")}</DialogTitle>
                  <DialogDescription>
                    {locale === "tr"
                      ? "Sisteme yeni bir firma ekleyin"
                      : "Create a new organization in the system"}
                  </DialogDescription>
                </DialogHeader>
                <div className="grid gap-4 py-4">
                  <div className="grid gap-2">
                    <Label htmlFor="name">{t("super.tenants.name")}</Label>
                    <Input
                      id="name"
                      value={formData.name}
                      onChange={(e) =>
                        setFormData({ ...formData, name: e.target.value })
                      }
                      placeholder={locale === "tr" ? "Koşu Kulübü" : "Running Club"}
                      required
                    />
                  </div>
                  <div className="grid gap-2">
                    <Label htmlFor="slug">{t("super.tenants.slug")}</Label>
                    <Input
                      id="slug"
                      value={formData.slug}
                      onChange={(e) =>
                        setFormData({ ...formData, slug: e.target.value })
                      }
                      placeholder="kosu-kulubu"
                    />
                    <p className="text-xs text-muted-foreground">
                      {locale === "tr"
                        ? "URL-uyumlu tanımlayıcı. Boş bırakılırsa otomatik oluşturulur."
                        : "URL-friendly identifier. Auto-generated if empty."}
                    </p>
                  </div>
                  <div className="grid grid-cols-2 gap-4">
                    <div className="grid gap-2">
                      <Label htmlFor="language">
                        {t("super.tenants.defaultLanguage")}
                      </Label>
                      <Input
                        id="language"
                        value={formData.defaultLanguage}
                        onChange={(e) =>
                          setFormData({
                            ...formData,
                            defaultLanguage: e.target.value,
                          })
                        }
                        placeholder="tr"
                      />
                    </div>
                    <div className="grid gap-2">
                      <Label htmlFor="currency">
                        {t("super.tenants.defaultCurrency")}
                      </Label>
                      <Input
                        id="currency"
                        value={formData.defaultCurrency}
                        onChange={(e) =>
                          setFormData({
                            ...formData,
                            defaultCurrency: e.target.value,
                          })
                        }
                        placeholder="TRY"
                      />
                    </div>
                  </div>
                </div>
                <DialogFooter>
                  <Button
                    type="button"
                    variant="outline"
                    onClick={() => setIsCreateOpen(false)}
                  >
                    {t("common.cancel")}
                  </Button>
                  <Button
                    type="submit"
                    variant="accent"
                    disabled={createMutation.isPending}
                  >
                    {createMutation.isPending && (
                      <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    )}
                    {t("super.tenants.create")}
                  </Button>
                </DialogFooter>
              </form>
            </DialogContent>
          </Dialog>
        }
      />

      {tenants && tenants.length === 0 ? (
        <EmptyState
          variant="tenant"
          title={locale === "tr" ? "Henüz firma yok" : "No tenants yet"}
          description={
            locale === "tr"
              ? "İlk firmanızı oluşturarak başlayın"
              : "Create your first tenant to get started"
          }
          action={{
            label: t("super.tenants.create"),
            onClick: () => setIsCreateOpen(true),
          }}
        />
      ) : (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {tenants?.map((tenant) => (
            <article
              key={tenant.id}
              className="group rounded-2xl border bg-card p-5 shadow-sm transition-all hover:shadow-md"
            >
              <div className="mb-4 flex items-start justify-between">
                <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-primary/10">
                  <Building2 className="h-6 w-6 text-primary" />
                </div>
                <Badge variant={tenant.status === "Active" ? "success" : "muted"}>
                  {tenant.status}
                </Badge>
              </div>

              <h3 className="mb-1 text-lg font-semibold">{tenant.name}</h3>
              <code className="text-sm text-muted-foreground">{tenant.slug}</code>

              <div className="mt-4 space-y-2 text-sm">
                <div className="flex items-center gap-2 text-muted-foreground">
                  <Users className="h-4 w-4" />
                  <span>
                    {tenant.memberCount || 0} {t("super.tenants.members")}
                  </span>
                </div>
                <div className="flex items-center gap-2 text-muted-foreground">
                  <Globe className="h-4 w-4" />
                  <span>
                    {tenant.defaultLanguage} / {tenant.defaultCurrency}
                  </span>
                </div>
              </div>
            </article>
          ))}
        </div>
      )}
    </div>
  );
}

export default function TenantsPage() {
  return (
    <SuperAdminGuard>
      <TenantsPageContent />
    </SuperAdminGuard>
  );
}

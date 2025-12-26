"use client";

import { useState, useMemo, useEffect } from "react";
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
  ActiveFiltersBar,
  ListPagination,
  ListToolbar,
  PageShell,
  ResultsHeader,
} from "@/components/listing";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Building2, Plus, Users, Globe, Loader2 } from "lucide-react";
import { useListQueryParams } from "@/hooks/use-list-query-params";

function TenantsPageContent() {
  const { t, locale } = useTranslation();
  const toast = useToast();
  const { onError } = useApiMutation();
  const queryClient = useQueryClient();
  const {
    getParam,
    getNumberParam,
    updateParams,
    clearParams,
    hasActiveFilters,
  } = useListQueryParams({
    filterKeys: ["search", "sort"],
    defaults: { sort: "createdAt", pageSize: 20 },
    debounceMs: 400,
  });

  const searchFilter = getParam("search");
  const sortFilter = getParam("sort", "createdAt");
  const sortBy = sortFilter === "name" ? "name" : "createdAt";
  const page = getNumberParam("page", 1);
  const pageSize = getNumberParam("pageSize", 20);

  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [formData, setFormData] = useState({
    name: "",
    slug: "",
    description: "",
    defaultLanguage: "tr",
    defaultCurrency: "TRY",
  });

  // Fetch tenants
  const {
    data: tenants,
    isLoading,
    error,
  } = useQuery({
    queryKey: ["super-admin-tenants"],
    queryFn: () => superAdminApi.listTenants(),
  });

  useEffect(() => {
    if (error) {
      onError(error);
    }
  }, [error, onError]);

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

  const filteredTenants = useMemo(() => {
    if (!tenants) return [];
    const term = searchFilter.trim().toLowerCase();
    if (!term) return tenants;
    return tenants.filter((tenant) =>
      tenant.name.toLowerCase().includes(term)
    );
  }, [searchFilter, tenants]);

  const sortedTenants = useMemo(() => {
    const list = [...filteredTenants];
    if (sortBy === "name") {
      list.sort((a, b) => a.name.localeCompare(b.name));
      return list;
    }
    list.sort(
      (a, b) =>
        new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
    );
    return list;
  }, [filteredTenants, sortBy]);

  const paginatedTenants = useMemo(() => {
    const start = (page - 1) * pageSize;
    return sortedTenants.slice(start, start + pageSize);
  }, [page, pageSize, sortedTenants]);

  const activeFilters = useMemo(() => {
    const filters: { key: string; label: string; onRemove: () => void }[] = [];

    if (searchFilter) {
      filters.push({
        key: "search",
        label: `${t("common.search")}: ${searchFilter}`,
        onRemove: () => updateParams({ search: "" }),
      });
    }

    if (sortBy !== "createdAt") {
      filters.push({
        key: "sort",
        label: t("super.tenants.sort.name"),
        onRemove: () => updateParams({ sort: "createdAt" }),
      });
    }

    return filters;
  }, [searchFilter, sortBy, t, updateParams]);

  const sortLabel =
    sortBy === "name" ? t("super.tenants.sort.name") : t("super.tenants.sort.createdAt");

  if (isLoading) {
    return (
      <PageShell>
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
      </PageShell>
    );
  }

  return (
    <PageShell>
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
      <ListToolbar
        left={
          <Input
            value={searchFilter}
            onChange={(e) => updateParams({ search: e.target.value })}
            placeholder={t("common.search")}
            className="h-9 w-[220px]"
          />
        }
        right={
          <Select value={sortBy} onValueChange={(value) => updateParams({ sort: value })}>
            <SelectTrigger className="h-9 w-[180px]">
              <SelectValue placeholder={t("super.tenants.sort.label")} />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="createdAt">{t("super.tenants.sort.createdAt")}</SelectItem>
              <SelectItem value="name">{t("super.tenants.sort.name")}</SelectItem>
            </SelectContent>
          </Select>
        }
        showClear={hasActiveFilters}
        clearLabel={t("filters.clear")}
        onClear={() => clearParams()}
      />

      <ActiveFiltersBar
        filters={activeFilters}
        onClearAll={() => clearParams()}
        clearLabel={t("filters.clear")}
      />

      <ResultsHeader count={filteredTenants.length} sortLabel={sortBy !== "createdAt" ? sortLabel : undefined} />

      {filteredTenants.length === 0 ? (
        <EmptyState
          variant="tenant"
          title={
            hasActiveFilters
              ? t("empty.noResultsTitle")
              : locale === "tr"
              ? "Henüz firma yok"
              : "No tenants yet"
          }
          description={
            hasActiveFilters
              ? t("empty.noResultsDesc")
              : locale === "tr"
              ? "İlk firmanızı oluşturarak başlayın"
              : "Create your first tenant to get started"
          }
          action={
            hasActiveFilters
              ? {
                  label: t("filters.clear"),
                  onClick: () => clearParams(),
                }
              : {
                  label: t("super.tenants.create"),
                  onClick: () => setIsCreateOpen(true),
                }
          }
        />
      ) : (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {paginatedTenants.map((tenant) => (
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
      <ListPagination
        page={page}
        pageSize={pageSize}
        total={filteredTenants.length}
        onPageChange={(nextPage) => updateParams({ page: nextPage }, { resetPage: false })}
      />
    </PageShell>
  );
}

export default function TenantsPage() {
  return (
    <SuperAdminGuard>
      <TenantsPageContent />
    </SuperAdminGuard>
  );
}

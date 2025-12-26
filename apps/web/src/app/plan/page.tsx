"use client";

import { useMemo, useEffect, useCallback } from "react";
import { useRouter } from "next/navigation";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "@/contexts/locale-context";
import { useToast } from "@/components/ui/toast";
import { useApiMutation } from "@/hooks/use-api-error";
import { plansApi, PlanItem, eventsApi, Event } from "@/lib/api-client";
import { normalizePlanState } from "@/lib/normalizers/plan";
import { PageHeader } from "@/components/page-header";
import { EmptyState } from "@/components/empty-state";
import {
  ActiveFiltersBar,
  ListPagination,
  ListToolbar,
  PageShell,
  ResultsHeader,
} from "@/components/listing";
import { PlanItemCard } from "@/components/plan-item-card";
import { PlanPageSkeleton } from "@/components/plan-page-skeleton";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Flag } from "lucide-react";
import { AuthGuard } from "@/components/route-guards";
import { useListQueryParams } from "@/hooks/use-list-query-params";

type TabValue = "upcoming" | "completed" | "all";
type SortValue = "date" | "milestone";

function PlanPageContent() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const { t, locale } = useTranslation();
  const toast = useToast();
  const { onError } = useApiMutation();
  const { getParam, getNumberParam, updateParams, clearParams, hasActiveFilters } =
    useListQueryParams({
    filterKeys: ["tab", "sort"],
    defaults: { tab: "upcoming", sort: "date", pageSize: 20 },
    debounceMs: 400,
  });

  const tabParam = getParam("tab", "upcoming");
  const activeTab: TabValue =
    tabParam === "completed" || tabParam === "all" ? tabParam : "upcoming";

  const sortParam = getParam("sort", "date");
  const sortBy: SortValue = sortParam === "milestone" ? "milestone" : "date";

  const page = getNumberParam("page", 1);
  const pageSize = getNumberParam("pageSize", 20);

  // Fetch plans
  const {
    data: plansRaw,
    isLoading,
    error,
  } = useQuery({
    queryKey: ["my-plans"],
    queryFn: () => plansApi.list(),
  });

  useEffect(() => {
    if (error) {
      onError(error);
    }
  }, [error, onError]);

  // Fetch events for each plan item
  const eventIds = plansRaw?.map((p) => p.eventId) || [];
  const { data: events } = useQuery({
    queryKey: ["events", eventIds],
    queryFn: async () => {
      const eventPromises = eventIds.map((id) =>
        eventsApi.getById(id).catch(() => null)
      );
      const results = await Promise.all(eventPromises);
      return results.filter((e): e is Event => e !== null);
    },
    enabled: eventIds.length > 0,
  });

  // Merge plans with events and normalize state
  const plans = useMemo(() => {
    if (!plansRaw) return undefined;

    const eventMap = new Map(events?.map((e) => [e.id, e]) || []);

    return plansRaw.map((plan) => ({
      ...plan,
      state: normalizePlanState(plan.state),
      event: eventMap.get(plan.eventId),
    }));
  }, [plansRaw, events]);

  // Update state mutation
  const updateStateMutation = useMutation({
    mutationFn: ({ id, state }: { id: string; state: "Registered" | "Completed" }) =>
      plansApi.updateState(id, state),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["my-plans"] });
      toast.success(t("toast.updated"));
    },
    onError: (err) => onError(err),
  });

  // Delete mutation
  const deleteMutation = useMutation({
    mutationFn: (id: string) => plansApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["my-plans"] });
      toast.success(t("toast.removedFromPlan"));
    },
    onError: (err) => onError(err),
  });

  // Handle state update
  const handleUpdateState = async (id: string, state: "Registered" | "Completed") => {
    try {
      await updateStateMutation.mutateAsync({ id, state });
    } catch (err) {
      // Error handled by onError
    }
  };

  // Handle delete
  const handleDelete = async (id: string) => {
    const confirmed = window.confirm(
      locale === "tr"
        ? "Bu etkinliği planından kaldırmak istediğinden emin misin?"
        : "Are you sure you want to remove this event from your plan?"
    );
    if (!confirmed) return;

    try {
      await deleteMutation.mutateAsync(id);
    } catch (err) {
      // Error handled by onError
    }
  };

  // Filter and sort plans
  const filteredAndSortedPlans = useMemo(() => {
    if (!plans) return [];

    let filtered: PlanItem[] = [];

    // Filter by tab
    switch (activeTab) {
      case "upcoming":
        filtered = plans.filter(
          (p) => (p.state === "Active" || p.state === "Registered") && p.event
        );
        break;
      case "completed":
        filtered = plans.filter((p) => p.state === "Completed" && p.event);
        break;
      case "all":
        filtered = plans.filter((p) => p.event); // Only show plans with events
        break;
    }

    // Sort
    const sorted = [...filtered].sort((a, b) => {
      if (!a.event || !b.event) return 0;

      if (sortBy === "date") {
        return new Date(a.event.startAt).getTime() - new Date(b.event.startAt).getTime();
      } else {
        // Sort by milestone (nearest upcoming milestone first)
        // For now, fallback to date sorting
        return new Date(a.event.startAt).getTime() - new Date(b.event.startAt).getTime();
      }
    });

    return sorted;
  }, [plans, activeTab, sortBy]);

  const paginatedPlans = useMemo(() => {
    const start = (page - 1) * pageSize;
    return filteredAndSortedPlans.slice(start, start + pageSize);
  }, [filteredAndSortedPlans, page, pageSize]);

  // Group by month
  const groupedPlans = useMemo(() => {
    const groups: Record<string, PlanItem[]> = {};

    paginatedPlans.forEach((plan) => {
      if (!plan.event) return;

      const date = new Date(plan.event.startAt);
      const monthKey = new Intl.DateTimeFormat(locale === "tr" ? "tr-TR" : "en-US", {
        month: "long",
        year: "numeric",
      }).format(date);

      if (!groups[monthKey]) {
        groups[monthKey] = [];
      }
      groups[monthKey].push(plan);
    });

    return groups;
  }, [paginatedPlans, locale]);

  // Calculate stats for subtitle
  const stats = useMemo(() => {
    if (!plans) return { upcoming: 0, pending: 0 };

    const upcoming = plans.filter(
      (p) => (p.state === "Active" || p.state === "Registered") && p.event
    ).length;
    const pending = plans.filter((p) => p.state === "Active" && p.event).length;

    return { upcoming, pending };
  }, [plans]);

  // Generate subtitle
  const subtitle = useMemo(() => {
    if (stats.upcoming === 0 && stats.pending === 0) {
      return t("plan.subtitleNone");
    }
    if (stats.pending === 0) {
      return t("plan.subtitleNoPending", { upcoming: stats.upcoming });
    }
    if (stats.upcoming === stats.pending) {
      return t("plan.subtitleOnlyPending", { pending: stats.pending });
    }
    return t("plan.subtitle", {
      upcoming: stats.upcoming,
      pending: stats.pending,
    });
  }, [stats, t]);

  const handleTabChange = useCallback(
    (value: string) => {
      const nextTab: TabValue =
        value === "completed" || value === "all" ? value : "upcoming";
      updateParams({ tab: nextTab });
    },
    [updateParams]
  );

  const handleSortChange = useCallback(
    (value: string) => {
      const nextSort: SortValue = value === "milestone" ? "milestone" : "date";
      updateParams({ sort: nextSort });
    },
    [updateParams]
  );

  const activeFilters = useMemo(() => {
    const filters: { key: string; label: string; onRemove: () => void }[] = [];

    if (activeTab !== "upcoming") {
      filters.push({
        key: "tab",
        label: t(`plan.tabs.${activeTab}`),
        onRemove: () => updateParams({ tab: "upcoming" }),
      });
    }

    if (sortBy !== "date") {
      filters.push({
        key: "sort",
        label: t("plan.sort.byMilestone"),
        onRemove: () => updateParams({ sort: "date" }),
      });
    }

    return filters;
  }, [activeTab, sortBy, t, updateParams]);

  const sortLabel =
    sortBy === "milestone" ? t("plan.sort.byMilestone") : t("plan.sort.byDate");

  // Loading state
  if (isLoading) {
    return <PlanPageSkeleton />;
  }

  // Empty state
  if (!plans || plans.length === 0) {
    return (
      <PageShell>
        <PageHeader title={t("plan.title")} subtitle={subtitle} />
        <EmptyState
          icon={Flag}
          title={t("plan.emptyTitle")}
          description={t("plan.emptyDesc")}
          action={{
            label: t("nav.explore"),
            onClick: () => router.push("/"),
          }}
        />
      </PageShell>
    );
  }

  return (
    <PageShell>
      <PageHeader title={t("plan.title")} subtitle={subtitle} />

      <ListToolbar
        left={
          <Tabs value={activeTab} onValueChange={handleTabChange}>
            <TabsList>
              <TabsTrigger value="upcoming">{t("plan.tabs.upcoming")}</TabsTrigger>
              <TabsTrigger value="completed">{t("plan.tabs.completed")}</TabsTrigger>
              <TabsTrigger value="all">{t("plan.tabs.all")}</TabsTrigger>
            </TabsList>
          </Tabs>
        }
        right={
          filteredAndSortedPlans.length > 0 ? (
            <Select value={sortBy} onValueChange={handleSortChange}>
              <SelectTrigger className="w-[180px]">
                <SelectValue placeholder={t("plan.sort.label")} />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="date">{t("plan.sort.byDate")}</SelectItem>
                <SelectItem value="milestone">{t("plan.sort.byMilestone")}</SelectItem>
              </SelectContent>
            </Select>
          ) : null
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

      <ResultsHeader
        count={filteredAndSortedPlans.length}
        sortLabel={sortBy !== "date" ? sortLabel : undefined}
      />

      {filteredAndSortedPlans.length === 0 ? (
        <EmptyState
          icon={Flag}
          title={
            activeTab === "completed"
              ? locale === "tr"
                ? "Henüz tamamlanan etkinlik yok"
                : "No completed events yet"
              : locale === "tr"
              ? "Yaklaşan etkinlik bulunmuyor"
              : "No upcoming events"
          }
          description={
            activeTab === "completed"
              ? locale === "tr"
                ? "Tamamladığın yarışlar burada görünecek."
                : "Completed races will appear here."
              : locale === "tr"
              ? "Yaklaşan etkinlikler burada görünecek."
              : "Upcoming events will appear here."
          }
          action={{
            label: t("nav.explore"),
            onClick: () => router.push("/"),
          }}
        />
      ) : (
        <>
          <div className="space-y-8">
            {Object.entries(groupedPlans)
              .sort(([a], [b]) => {
                const dateA = new Date(a);
                const dateB = new Date(b);
                return dateA.getTime() - dateB.getTime();
              })
              .map(([month, monthPlans]) => (
                <div key={month}>
                  <h2 className="mb-4 font-display text-xl font-semibold capitalize">
                    {month}
                  </h2>
                  <div className="space-y-4">
                    {monthPlans.map((plan) => (
                      <PlanItemCard
                        key={plan.id}
                        plan={plan}
                        isLoading={
                          updateStateMutation.isPending || deleteMutation.isPending
                        }
                        onUpdateState={handleUpdateState}
                        onDelete={handleDelete}
                      />
                    ))}
                  </div>
                </div>
              ))}
          </div>

          <ListPagination
            page={page}
            pageSize={pageSize}
            total={filteredAndSortedPlans.length}
            onPageChange={(nextPage) =>
              updateParams({ page: nextPage }, { resetPage: false })
            }
          />
        </>
      )}
    </PageShell>
  );
}

export default function PlanPage() {
  return (
    <AuthGuard>
      <PlanPageContent />
    </AuthGuard>
  );
}

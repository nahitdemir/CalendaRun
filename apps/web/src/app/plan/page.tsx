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
import { Button } from "@/components/ui/button";
import {
  EventSortSelect,
  ListPagination,
  ListToolbar,
  PageShell,
  ResultsHeader,
} from "@/components/listing";
import { EventCard } from "@/components/event-card";
import { PlanPageSkeleton } from "@/components/plan-page-skeleton";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { CheckCircle2, Flag, Loader2, Trophy } from "lucide-react";
import { AuthGuard } from "@/components/route-guards";
import { useListQueryParams } from "@/hooks/use-list-query-params";
import { useDistances } from "@/hooks/use-distances";
import { mapEventToCard } from "@/lib/normalizers/event";
import type { Distance } from "@/components/distance-badge";
import type { EventSortValue } from "@/components/listing/event-sort-select";

type TabValue = "upcoming" | "completed" | "all";

function PlanPageContent() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const { t, locale } = useTranslation();
  const toast = useToast();
  const { onError } = useApiMutation();
  const { distances: availableDistances, kmToDistance } = useDistances();
  const { getParam, getNumberParam, updateParams } =
    useListQueryParams({
    filterKeys: ["tab", "sort"],
    defaults: { tab: "upcoming", sort: "date_asc", pageSize: 20 },
    debounceMs: 400,
  });

  const tabParam = getParam("tab", "upcoming");
  const activeTab: TabValue =
    tabParam === "completed" || tabParam === "all" ? tabParam : "upcoming";

  const sortParam = getParam("sort", "date_asc");
  const sortBy: EventSortValue =
    sortParam === "date_desc" ? "date_desc" : "date_asc";

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

    let filtered = plans;

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

      const timeA = new Date(a.event.startAt).getTime();
      const timeB = new Date(b.event.startAt).getTime();
      return sortBy === "date_desc" ? timeB - timeA : timeA - timeB;
    });

    return sorted;
  }, [plans, activeTab, sortBy]);

  const paginatedPlans = useMemo(() => {
    const start = (page - 1) * pageSize;
    return filteredAndSortedPlans.slice(start, start + pageSize);
  }, [filteredAndSortedPlans, page, pageSize]);

  const defaultDistance = useMemo<Distance>(() => {
    return availableDistances[0] || ("21K" as Distance);
  }, [availableDistances]);

  const planCards = useMemo(() => {
    if (!kmToDistance || Object.keys(kmToDistance).length === 0) return [];
    return paginatedPlans
      .map((plan) => {
        if (!plan.event) return null;
        return {
          plan,
          card: mapEventToCard(plan.event, kmToDistance, defaultDistance),
        };
      })
      .filter((item): item is { plan: PlanItem; card: ReturnType<typeof mapEventToCard> } => item !== null);
  }, [paginatedPlans, kmToDistance, defaultDistance]);

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
    (value: EventSortValue) => {
      updateParams({ sort: value });
    },
    [updateParams]
  );

  const handlePageChange = useCallback(
    (nextPage: number) => {
      updateParams({ page: nextPage }, { resetPage: false });
      window.scrollTo({ top: 0, behavior: "smooth" });
    },
    [updateParams]
  );

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
          <div className="flex flex-wrap items-center gap-3">
            <Tabs value={activeTab} onValueChange={handleTabChange}>
              <TabsList>
                <TabsTrigger value="upcoming">{t("plan.tabs.upcoming")}</TabsTrigger>
                <TabsTrigger value="completed">{t("plan.tabs.completed")}</TabsTrigger>
                <TabsTrigger value="all">{t("plan.tabs.all")}</TabsTrigger>
              </TabsList>
            </Tabs>
            <ResultsHeader count={filteredAndSortedPlans.length} className="w-auto" />
          </div>
        }
        right={
          <EventSortSelect value={sortBy} onChange={handleSortChange} />
        }
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
          <div className="space-y-4">
            {planCards.map(({ plan, card }) => {
              const planState = normalizePlanState(plan.state);

              return (
                <EventCard
                  key={plan.id}
                  event={card}
                  planState={planState}
                  isInPlan
                  onRemoveFromPlan={() => handleDelete(plan.id)}
                  onView={() => router.push(`/events/${plan.eventId}`)}
                  isPlanActionLoading={
                    updateStateMutation.isPending || deleteMutation.isPending
                  }
                  extraActions={
                    planState === "Active" ? (
                      <Button
                        variant="accent"
                        onClick={() => handleUpdateState(plan.id, "Registered")}
                        disabled={updateStateMutation.isPending || deleteMutation.isPending}
                        className="gap-2"
                      >
                        {updateStateMutation.isPending || deleteMutation.isPending ? (
                          <Loader2 className="h-4 w-4 animate-spin" />
                        ) : (
                          <CheckCircle2 className="h-4 w-4" />
                        )}
                        {t("plan.markRegistered")}
                      </Button>
                    ) : planState === "Registered" ? (
                      <Button
                        variant="accent"
                        onClick={() => handleUpdateState(plan.id, "Completed")}
                        disabled={updateStateMutation.isPending || deleteMutation.isPending}
                        className="gap-2"
                      >
                        {updateStateMutation.isPending || deleteMutation.isPending ? (
                          <Loader2 className="h-4 w-4 animate-spin" />
                        ) : (
                          <Trophy className="h-4 w-4" />
                        )}
                        {t("plan.markCompleted")}
                      </Button>
                    ) : null
                  }
                />
              );
            })}
          </div>

          <ListPagination
            page={page}
            pageSize={pageSize}
            total={filteredAndSortedPlans.length}
            onPageChange={handlePageChange}
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

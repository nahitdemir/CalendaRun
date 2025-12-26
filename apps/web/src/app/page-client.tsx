"use client";

import { useState, useMemo, useEffect, useCallback, useRef } from "react";
import { useRouter, useSearchParams, usePathname } from "next/navigation";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useAuth } from "@/contexts/auth-context";
import { useTranslation } from "@/contexts/locale-context";
import { useToast } from "@/components/ui/toast";
import { useApiMutation } from "@/hooks/use-api-error";
import { eventsApi, plansApi, Event, PagedResult, PlanItem, ApiError } from "@/lib/api-client";
import { Button } from "@/components/ui/button";
import { PageHeader } from "@/components/page-header";
import { FilterBar, FilterValues } from "@/components/filter-bar";
import { EventCard } from "@/components/event-card";
import { EventCardSkeletonList } from "@/components/event-card-skeleton";
import { EmptyState } from "@/components/empty-state";
import type { Distance } from "@/components/distance-badge";
import { useDistances } from "@/hooks/use-distances";
import { mapEventToCard } from "@/lib/normalizers/event";
import { Flag, ArrowRight, Calendar, MapPin, Bell, ChevronLeft, ChevronRight } from "lucide-react";

// Parse distance string (comma-separated KM) to Distance array (uses distances from Settings)
function parseDistances(distStr: string | null, kmToDistance: Record<number, Distance>): Distance[] {
  if (!distStr) return [];
  return distStr
    .split(",")
    .map((d) => d.trim())
    .map((d) => {
      const km = parseInt(d, 10);
      return kmToDistance[km];
    })
    .filter((d): d is Distance => d !== undefined);
}

// Convert Distance array to comma-separated KM string for API (uses distances from Settings)
function distancesToString(distances: Distance[], distanceToKm: Record<Distance, number>): string {
  return distances.map((d) => distanceToKm[d]).join(",");
}

function setQueryParam(params: URLSearchParams, key: string, value: string) {
  if (value) {
    params.set(key, value);
  } else {
    params.delete(key);
  }
}

function removeQueryParam(params: URLSearchParams, key: string) {
  params.delete(key);
}

export default function ExplorePage() {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const { isAuthenticated, isLoading: authLoading, login } = useAuth();
  const { t, locale } = useTranslation();
  const toast = useToast();
  const { onError } = useApiMutation();
  const { distances: availableDistances, kmToDistance, distanceToKm } = useDistances();
  const queryClient = useQueryClient();
  const [pendingPlanActions, setPendingPlanActions] = useState<Record<string, boolean>>({});

  const { data: planItems } = useQuery({
    queryKey: ["my-plans"],
    queryFn: () => plansApi.list(),
    enabled: isAuthenticated,
    staleTime: 30 * 1000,
  });

  const planIdByEventId = useMemo(() => {
    const map = new Map<string, string>();
    planItems?.forEach((plan) => {
      map.set(plan.eventId, plan.id);
    });
    return map;
  }, [planItems]);

  const setPlanActionPending = useCallback((eventId: string, isPending: boolean) => {
    setPendingPlanActions((prev) => {
      const next = { ...prev };
      if (isPending) {
        next[eventId] = true;
      } else {
        delete next[eventId];
      }
      return next;
    });
  }, []);

  const addPlanToCache = useCallback(
    (plan: PlanItem) => {
      queryClient.setQueryData<PlanItem[]>(["my-plans"], (prev) => {
        if (!prev) return [plan];
        if (prev.some((item) => item.id === plan.id || item.eventId === plan.eventId)) {
          return prev;
        }
        return [...prev, plan];
      });
    },
    [queryClient]
  );

  const removePlanFromCache = useCallback(
    (planId: string) => {
      queryClient.setQueryData<PlanItem[]>(["my-plans"], (prev) => {
        if (!prev) return prev;
        return prev.filter((item) => item.id !== planId);
      });
    },
    [queryClient]
  );

  const removePlanItem = useCallback(
    async (planId: string, options?: { showToast?: boolean }) => {
      try {
        await plansApi.delete(planId);
        removePlanFromCache(planId);
        if (options?.showToast !== false) {
          toast.success(t("toast.removedFromPlan"));
        }
      } catch (err) {
        onError(err);
      }
    },
    [removePlanFromCache, toast, t, onError]
  );

  // Read filters from URL query params (single source of truth)
  const filtersFromUrl = useMemo<FilterValues>(() => {
    return {
      city: searchParams.get("city") || "",
      dateFrom: searchParams.get("from") || "",
      dateTo: searchParams.get("to") || "",
      distances: parseDistances(searchParams.get("distanceKm"), kmToDistance),
    };
  }, [searchParams, kmToDistance]);

  // Pagination from URL
  const page = useMemo(() => {
    const pageParam = searchParams.get("page");
    return pageParam ? parseInt(pageParam, 10) : 1;
  }, [searchParams]);

  const pageSize = useMemo(() => {
    const pageSizeParam = searchParams.get("pageSize");
    return pageSizeParam ? parseInt(pageSizeParam, 10) : 20;
  }, [searchParams]);

  // Update URL query params (without page reload)
  const updateQueryParams = useCallback(
    (updates: {
      city?: string;
      from?: string;
      to?: string;
      distanceKm?: string;
      page?: number;
      pageSize?: number;
    }) => {
      const params = new URLSearchParams(searchParams.toString());

      // Update or remove params
      if (updates.city !== undefined) {
        setQueryParam(params, "city", updates.city);
      }
      if (updates.from !== undefined) {
        setQueryParam(params, "from", updates.from);
      }
      if (updates.to !== undefined) {
        setQueryParam(params, "to", updates.to);
      }
      if (updates.distanceKm !== undefined) {
        setQueryParam(params, "distanceKm", updates.distanceKm);
      }
      if (updates.page !== undefined) {
        if (updates.page > 1) params.set("page", updates.page.toString());
        else removeQueryParam(params, "page");
      }
      if (updates.pageSize !== undefined) {
        if (updates.pageSize !== 20) params.set("pageSize", updates.pageSize.toString());
        else removeQueryParam(params, "pageSize");
      }

      // Reset to page 1 when filters change (except when explicitly setting page)
      if (updates.page === undefined && (updates.city !== undefined || updates.from !== undefined || updates.to !== undefined || updates.distanceKm !== undefined)) {
        params.delete("page");
      }

      if (process.env.NODE_ENV === "development") {
        if (updates.from === "" && params.get("from") !== null) {
          console.warn("[Explore] Expected 'from' to be removed from URL.");
        }
        if (updates.to === "" && params.get("to") !== null) {
          console.warn("[Explore] Expected 'to' to be removed from URL.");
        }
      }

      // Use replace instead of push to avoid adding to history stack when clearing
      const currentPathname = pathname || "/";
      const newUrl = params.toString() ? `${currentPathname}?${params.toString()}` : currentPathname;
      router.replace(newUrl, { scroll: false });
      
      // Debug log (dev only)
      if (process.env.NODE_ENV === "development") {
        console.log("[Explore] URL updated:", newUrl);
        console.log("[Explore] Query params:", Object.fromEntries(params));
      }
    },
    [router, pathname, searchParams]
  );

  // Handle filter changes with debounce (desktop) or immediate (mobile with Apply button)
  const [pendingFilters, setPendingFilters] = useState<FilterValues>(filtersFromUrl);
  const [debounceTimer, setDebounceTimer] = useState<NodeJS.Timeout | null>(null);

  // Sync pendingFilters with URL when URL changes
  useEffect(() => {
    setPendingFilters(filtersFromUrl);
  }, [filtersFromUrl]);

  // Clear all filters and reset URL - single source of truth
  const clearFilters = useCallback(() => {
    // Clear pending filters state
    const emptyFilters: FilterValues = {
      city: "",
      dateFrom: "",
      dateTo: "",
      distances: [],
    };
    setPendingFilters(emptyFilters);

    // Clear debounce timer if exists
    if (debounceTimer) {
      clearTimeout(debounceTimer);
      setDebounceTimer(null);
    }

    // Build clean URL - remove all filter params
    const params = new URLSearchParams();
    // Keep only pageSize if it's not default (20)
    const currentPageSize = parseInt(searchParams.get("pageSize") || "20", 10);
    if (currentPageSize !== 20) {
      params.set("pageSize", currentPageSize.toString());
    }

    // Use pathname to ensure correct route (/, /explore, etc.)
    const cleanUrl = params.toString() ? `${pathname}?${params.toString()}` : pathname;
    router.replace(cleanUrl, { scroll: false });

    // Debug log (dev only)
    if (process.env.NODE_ENV === "development") {
      console.log("[Explore] Filters cleared. URL:", cleanUrl);
      console.log("[Explore] Query params after clear:", Object.fromEntries(params));
    }
  }, [router, pathname, searchParams, debounceTimer]);

  // Apply filters to URL (with debounce for desktop)
  const applyFilters = useCallback(
    (newFilters: FilterValues, immediate = false) => {
      setPendingFilters(newFilters);
      const distanceKmValue = newFilters.distances.length > 0 ? distancesToString(newFilters.distances, distanceToKm) : "";

      if (immediate) {
        if (debounceTimer) {
          clearTimeout(debounceTimer);
          setDebounceTimer(null);
        }
        updateQueryParams({
          city: newFilters.city,
          from: newFilters.dateFrom,
          to: newFilters.dateTo,
          distanceKm: distanceKmValue,
        });
      } else {
        // Desktop: debounce
        if (debounceTimer) clearTimeout(debounceTimer);
        const timer = setTimeout(() => {
          updateQueryParams({
            city: newFilters.city,
            from: newFilters.dateFrom,
            to: newFilters.dateTo,
            distanceKm: distanceKmValue,
          });
        }, 400);
        setDebounceTimer(timer);
      }
    },
    [updateQueryParams, debounceTimer, distanceToKm]
  );

  // Validate date range
  const dateRangeError = useMemo(() => {
    if (pendingFilters.dateFrom && pendingFilters.dateTo) {
      const from = new Date(pendingFilters.dateFrom);
      const to = new Date(pendingFilters.dateTo);
      if (from > to) {
        return locale === "tr" ? "Başlangıç tarihi bitiş tarihinden sonra olamaz" : "Start date cannot be after end date";
      }
    }
    return null;
  }, [pendingFilters.dateFrom, pendingFilters.dateTo, locale]);

  // Convert distances to KM for API
  const distanceKmParam = useMemo(() => {
    if (filtersFromUrl.distances.length === 0) return undefined;
    return distancesToString(filtersFromUrl.distances, distanceToKm);
  }, [filtersFromUrl.distances, distanceToKm]);

  // Fetch events with filters from URL
  const {
    data: pagedEvents,
    isLoading: eventsLoading,
    error: eventsError,
  } = useQuery({
    queryKey: ["events", filtersFromUrl.city, filtersFromUrl.dateFrom, filtersFromUrl.dateTo, distanceKmParam, page, pageSize],
    queryFn: async (): Promise<PagedResult<Event>> => {
      // Don't fetch if date range is invalid
      if (dateRangeError) {
        return { items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0 };
      }
      const result = await eventsApi.list({
        city: filtersFromUrl.city || undefined,
        dateFrom: filtersFromUrl.dateFrom || undefined,
        dateTo: filtersFromUrl.dateTo || undefined,
        distances: distanceKmParam,
        page,
        pageSize,
      });
      
      // Debug log (dev only)
      if (process.env.NODE_ENV === "development") {
        console.log("[Explore] Fetched events:", {
          url: `/api/events?city=${filtersFromUrl.city || ""}&from=${filtersFromUrl.dateFrom || ""}&to=${filtersFromUrl.dateTo || ""}&page=${page}&pageSize=${pageSize}`,
          resultType: Array.isArray(result) ? "array" : "object",
          itemsCount: Array.isArray(result) ? result.length : result.items?.length || 0,
          totalCount: Array.isArray(result) ? result.length : result.totalCount || 0,
        });
      }
      
      return result;
    },
    enabled: !dateRangeError, // Don't fetch if validation fails
    staleTime: 5 * 60 * 1000, // 5 minutes
  });

  // Extract events array from paged result (support both array and PagedResult)
  const events = useMemo(() => {
    if (!pagedEvents) return [];
    // If pagedEvents is an array (legacy response), return as-is
    if (Array.isArray(pagedEvents)) return pagedEvents;
    // If pagedEvents is PagedResult, extract items
    return pagedEvents.items || [];
  }, [pagedEvents]);

  // Fetch cities for filter
  const { data: cities } = useQuery({
    queryKey: ["event-cities"],
    queryFn: () => eventsApi.getCities(),
    staleTime: 30 * 60 * 1000, // 30 minutes
  });

  // Add to plan mutation
  const handleAddToPlan = useCallback(
    async (eventId: string) => {
      if (!isAuthenticated) {
        login();
        return;
      }

      setPlanActionPending(eventId, true);
      try {
        const created = await plansApi.create(eventId);
        addPlanToCache(created);
        toast.success(t("toast.addedToPlan"), undefined, {
          label: t("common.undo"),
          onClick: () => {
            void removePlanItem(created.id, { showToast: false });
          },
        });
      } catch (err) {
        if (err instanceof ApiError && err.status === 409) {
          queryClient.invalidateQueries({ queryKey: ["my-plans"] });
          toast.info(t("toast.alreadyInPlan"));
          return;
        }
        onError(err);
      } finally {
        setPlanActionPending(eventId, false);
      }
    },
    [isAuthenticated, login, setPlanActionPending, addPlanToCache, toast, t, removePlanItem, queryClient, onError]
  );

  // Map events to card data
  const eventCards = useMemo(() => {
    if (!events || !kmToDistance || Object.keys(kmToDistance).length === 0) return [];
    const defaultDistance = availableDistances[0] || ("21K" as Distance);
    return events.map((event) => mapEventToCard(event, kmToDistance, defaultDistance));
  }, [events, kmToDistance, availableDistances]);

  // Extract unique cities from events for filter fallback
  const availableCities = useMemo(() => {
    if (cities && cities.length > 0) return cities;
    if (!events) return [];
    return Array.from(new Set(events.map((e) => e.city))).sort();
  }, [events, cities]);

  // Show error only once when it changes
  const onErrorRef = useRef(onError);
  useEffect(() => {
    onErrorRef.current = onError;
  }, [onError]);

  useEffect(() => {
    if (eventsError) {
      onErrorRef.current(eventsError);
    }
  }, [eventsError]);

  // Clear all filters
  const handleClearFilters = useCallback(() => {
    clearFilters();
  }, [clearFilters]);

  const handleRemoveFromPlan = useCallback(
    async (eventId: string) => {
      const planId = planIdByEventId.get(eventId);
      if (!planId) {
        queryClient.invalidateQueries({ queryKey: ["my-plans"] });
        return;
      }
      setPlanActionPending(eventId, true);
      try {
        await removePlanItem(planId);
      } finally {
        setPlanActionPending(eventId, false);
      }
    },
    [planIdByEventId, queryClient, removePlanItem, setPlanActionPending]
  );

  // Handle pagination
  const handlePageChange = useCallback(
    (newPage: number) => {
      updateQueryParams({ page: newPage });
      window.scrollTo({ top: 0, behavior: "smooth" });
    },
    [updateQueryParams]
  );

  // Landing page for unauthenticated users
  if (!authLoading && !isAuthenticated) {
    return (
      <div className="min-h-[calc(100vh-4rem)]">
        {/* Hero section */}
        <section className="container-app">
          <div className="relative overflow-hidden rounded-3xl bg-gradient-to-br from-primary/5 via-background to-accent/5 px-6 py-16 md:px-12 md:py-24">
            {/* Decorative elements */}
            <div className="absolute -right-20 -top-20 h-64 w-64 rounded-full bg-accent/10 blur-3xl" />
            <div className="absolute -bottom-20 -left-20 h-64 w-64 rounded-full bg-primary/10 blur-3xl" />

            <div className="relative z-10 mx-auto max-w-2xl text-center">
              <div className="mb-6 inline-flex items-center gap-2 rounded-full bg-accent/10 px-4 py-2 text-sm font-medium text-accent">
                <Flag className="h-4 w-4" />
                {locale === "tr" ? "Yarışlarını planla" : "Plan your races"}
              </div>

              <h1 className="mb-6 font-display text-4xl font-bold tracking-tight md:text-5xl lg:text-6xl">
                {t("explore.title")}
              </h1>

              <p className="mb-8 text-lg text-muted-foreground md:text-xl">
                {t("explore.subtitle")}
              </p>

              <Button
                variant="accent"
                size="xl"
                onClick={login}
                className="gap-2"
              >
                {t("nav.signIn")}
                <ArrowRight className="h-4 w-4" />
              </Button>
            </div>
          </div>
        </section>

        {/* Features */}
        <section className="container-app">
          <div className="grid gap-6 md:grid-cols-3">
            <div className="group rounded-2xl border bg-card p-6 transition-all hover:shadow-md">
              <div className="mb-4 inline-flex h-12 w-12 items-center justify-center rounded-xl bg-accent/10">
                <Calendar className="h-6 w-6 text-accent" />
              </div>
              <h3 className="mb-2 font-semibold">
                {locale === "tr" ? "Etkinlik Takvimi" : "Event Calendar"}
              </h3>
              <p className="text-sm text-muted-foreground">
                {locale === "tr"
                  ? "Türkiye ve dünya genelindeki koşu etkinliklerini keşfet"
                  : "Discover running events around the world"}
              </p>
            </div>

            <div className="group rounded-2xl border bg-card p-6 transition-all hover:shadow-md">
              <div className="mb-4 inline-flex h-12 w-12 items-center justify-center rounded-xl bg-primary/10">
                <MapPin className="h-6 w-6 text-primary" />
              </div>
              <h3 className="mb-2 font-semibold">
                {locale === "tr" ? "Planla" : "Plan"}
              </h3>
              <p className="text-sm text-muted-foreground">
                {locale === "tr"
                  ? "Katılmak istediğin yarışları planına ekle, takip et"
                  : "Add races to your plan and track them"}
              </p>
            </div>

            <div className="group rounded-2xl border bg-card p-6 transition-all hover:shadow-md">
              <div className="mb-4 inline-flex h-12 w-12 items-center justify-center rounded-xl bg-success/10">
                <Bell className="h-6 w-6 text-success" />
              </div>
              <h3 className="mb-2 font-semibold">
                {locale === "tr" ? "Hatırlatma Al" : "Get Reminders"}
              </h3>
              <p className="text-sm text-muted-foreground">
                {locale === "tr"
                  ? "Kayıt tarihlerini kaçırma, e-posta ile hatırlatma al"
                  : "Never miss registration, get email reminders"}
              </p>
            </div>
          </div>
        </section>

        {/* Preview events */}
        <section className="container-app">
          <div className="mb-6 flex items-center justify-between">
            <h2 className="font-display text-2xl font-bold">
              {locale === "tr" ? "Yaklaşan Yarışlar" : "Upcoming Races"}
            </h2>
            <Button variant="ghost" onClick={login} className="gap-1">
              {locale === "tr" ? "Tümünü Gör" : "View All"}
              <ArrowRight className="h-4 w-4" />
            </Button>
          </div>

          {eventsLoading ? (
            <EventCardSkeletonList count={3} />
          ) : (
            <div className="space-y-4">
              {eventCards.slice(0, 3).map((event) => (
                <EventCard key={event.id} event={event} showActions={false} />
              ))}
            </div>
          )}
        </section>
      </div>
    );
  }

  // Loading state
  if (authLoading) {
    return (
      <div className="container-app">
        <div className="mb-8 h-12 w-64 animate-pulse rounded-lg bg-muted" />
        <EventCardSkeletonList count={6} />
      </div>
    );
  }

  // Authenticated user - Explore page
  return (
    <div className="container-app space-y-6">
      <PageHeader
        title={t("explore.title")}
        subtitle={t("explore.subtitle")}
        size="large"
      />

      <FilterBar
        values={pendingFilters}
        onChange={(newFilters) => applyFilters(newFilters, false)}
        onApply={() => applyFilters(pendingFilters, true)}
        onClear={clearFilters}
        cities={availableCities}
        dateRangeError={dateRangeError}
      />

      {/* Debug: Show current query string (dev only) */}
      {process.env.NODE_ENV === "development" && (
        <div className="rounded-lg border border-dashed border-muted-foreground/20 bg-muted/30 p-2 text-xs text-muted-foreground">
          <strong>Debug:</strong> Query params:{" "}
          {searchParams.toString() || "(none)"}
        </div>
      )}

      {eventsLoading ? (
        <EventCardSkeletonList count={6} />
      ) : dateRangeError ? (
        <div className="rounded-lg border border-destructive/50 bg-destructive/10 p-4 text-sm text-destructive">
          {dateRangeError}
        </div>
      ) : eventCards.length === 0 ? (
        <EmptyState
          variant="events"
          title={t("empty.noEventsTitle")}
          description={t("empty.noEventsDesc")}
          action={{
            label: t("filters.clear"),
            onClick: handleClearFilters,
          }}
        />
      ) : (
        <>
          <div className="space-y-4">
            {eventCards.map((event) => (
              <EventCard
                key={event.id}
                event={event}
                onView={() => router.push(`/events/${event.id}`)}
                onAddToPlan={() => handleAddToPlan(event.id)}
                onRemoveFromPlan={() => handleRemoveFromPlan(event.id)}
                isInPlan={planIdByEventId.has(event.id)}
                planItemId={planIdByEventId.get(event.id)}
                isPlanActionLoading={!!pendingPlanActions[event.id]}
              />
            ))}
          </div>

          {/* Simple pagination */}
          <div className="flex items-center justify-between border-t pt-4">
            <div className="text-sm text-muted-foreground">
              {locale === "tr" ? "Sayfa" : "Page"} {page}
            </div>
            <div className="flex gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={() => handlePageChange(page - 1)}
                disabled={page <= 1 || eventsLoading}
                className="gap-1"
              >
                <ChevronLeft className="h-4 w-4" />
                {locale === "tr" ? "Önceki" : "Previous"}
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={() => handlePageChange(page + 1)}
                disabled={eventCards.length < pageSize || eventsLoading}
                className="gap-1"
              >
                {locale === "tr" ? "Sonraki" : "Next"}
                <ChevronRight className="h-4 w-4" />
              </Button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}

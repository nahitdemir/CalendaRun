"use client";

import { useState, useMemo, useEffect } from "react";
import { useRouter } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { useAuth } from "@/contexts/auth-context";
import { useTranslation } from "@/contexts/locale-context";
import { useToast } from "@/components/ui/toast";
import { useApiMutation } from "@/hooks/use-api-error";
import { eventsApi, plansApi, Event } from "@/lib/api-client";
import { Button } from "@/components/ui/button";
import { PageHeader } from "@/components/page-header";
import { FilterBar, FilterValues } from "@/components/filter-bar";
import { EventCard, EventCardData } from "@/components/event-card";
import { EventCardSkeletonList } from "@/components/event-card-skeleton";
import { EmptyState } from "@/components/empty-state";
import { Distance } from "@/components/distance-badge";
import { Flag, ArrowRight, Calendar, MapPin, Bell } from "lucide-react";

// Map API event to EventCardData
function mapEventToCard(event: Event): EventCardData {
  const now = new Date();
  const eventDate = new Date(event.startAt);

  // Determine registration status based on event date
  let registrationStatus: "open" | "closed" | "upcoming" = "open";
  if (eventDate < now) {
    registrationStatus = "closed";
  } else if (!event.registrationUrl) {
    registrationStatus = "upcoming";
  }

  // Parse distances from event if available
  const distances: Distance[] = (event.distances || []).filter((d): d is Distance =>
    ["5K", "10K", "21K", "42K", "ultra"].includes(d)
  );

  return {
    id: event.id,
    title: event.title,
    city: event.city,
    countryCode: event.countryCode,
    date: event.startAt,
    distances: distances.length > 0 ? distances : ["21K"], // Default to 21K if no distances
    registrationStatus,
    registrationUrl: event.registrationUrl || undefined,
  };
}

export default function ExplorePage() {
  const router = useRouter();
  const { isAuthenticated, isLoading: authLoading, login } = useAuth();
  const { t, locale } = useTranslation();
  const toast = useToast();
  const { onError } = useApiMutation();

  const [filters, setFilters] = useState<FilterValues>({
    city: "",
    dateFrom: "",
    dateTo: "",
    distances: [],
  });

  // Fetch events
  const {
    data: events,
    isLoading: eventsLoading,
    error: eventsError,
  } = useQuery({
    queryKey: ["events", filters.city, filters.dateFrom, filters.dateTo, filters.distances],
    queryFn: () =>
      eventsApi.list({
        city: filters.city || undefined,
        dateFrom: filters.dateFrom || undefined,
        dateTo: filters.dateTo || undefined,
        distances: filters.distances.length > 0 ? filters.distances.join(",") : undefined,
      }),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });

  // Fetch cities for filter
  const { data: cities } = useQuery({
    queryKey: ["event-cities"],
    queryFn: () => eventsApi.getCities(),
    staleTime: 30 * 60 * 1000, // 30 minutes
  });

  // Add to plan mutation
  const handleAddToPlan = async (eventId: string) => {
    if (!isAuthenticated) {
      login();
      return;
    }

    try {
      await plansApi.create(eventId);
      toast.success(t("toast.addedToPlan"));
    } catch (err) {
      onError(err);
    }
  };

  // Map events to card data
  const eventCards = useMemo(() => {
    if (!events) return [];
    return events.map(mapEventToCard);
  }, [events]);

  // Extract unique cities from events for filter fallback
  const availableCities = useMemo(() => {
    if (cities && cities.length > 0) return cities;
    if (!events) return [];
    return Array.from(new Set(events.map((e) => e.city))).sort();
  }, [events, cities]);

  // Show error only once when it changes
  useEffect(() => {
    if (eventsError) {
      onError(eventsError);
    }
  }, [eventsError]); // eslint-disable-line react-hooks/exhaustive-deps

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
        values={filters}
        onChange={setFilters}
        cities={availableCities}
      />

      {eventsLoading ? (
        <EventCardSkeletonList count={6} />
      ) : eventCards.length === 0 ? (
        <EmptyState
          variant="events"
          title={t("empty.noEventsTitle")}
          description={t("empty.noEventsDesc")}
          action={{
            label: t("filters.clear"),
            onClick: () =>
              setFilters({ city: "", dateFrom: "", dateTo: "", distances: [] }),
          }}
        />
      ) : (
        <div className="space-y-4">
          {eventCards.map((event) => (
            <EventCard
              key={event.id}
              event={event}
              onView={() => router.push(`/events/${event.id}`)}
              onAddToPlan={() => handleAddToPlan(event.id)}
            />
          ))}
        </div>
      )}
    </div>
  );
}

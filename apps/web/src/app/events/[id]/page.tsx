"use client";

import { useMemo, useCallback, useState } from "react";
import { useRouter } from "next/navigation";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useAuth } from "@/contexts/auth-context";
import { useTranslation } from "@/contexts/locale-context";
import { useToast } from "@/components/ui/toast";
import { useApiMutation } from "@/hooks/use-api-error";
import {
  eventsApi,
  plansApi,
  EventMilestone,
  ApiError,
  PlanItem,
  getStoredLocale,
  getStoredTenantId,
  getStoredToken,
} from "@/lib/api-client";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { DistanceBadge } from "@/components/distance-badge";
import type { Distance } from "@/components/distance-badge";
import { useDistances } from "@/hooks/use-distances";
import { normalizeEventDistances } from "@/lib/normalizers/event";
import {
  MILESTONE_TYPES,
  isRegistrationMilestone,
} from "@/lib/constants/milestones";
import { MilestoneTimeline, Milestone } from "@/components/milestone-timeline";
import { EmptyState } from "@/components/empty-state";
import { EventDetailSkeleton } from "@/components/event-detail-skeleton";
import { PageShell } from "@/components/listing";
import {
  Calendar,
  MapPin,
  ExternalLink,
  ArrowLeft,
  Plus,
  Trash2,
  Loader2,
  CalendarX,
  SearchX,
} from "lucide-react";

type RegistrationStatus = "Open" | "OpenSoon" | "Closed";

function determineRegistrationStatus(
  milestones: EventMilestone[] | undefined,
  registrationUrl: string | null
): RegistrationStatus {
  if (!milestones || milestones.length === 0) {
    return registrationUrl ? "Open" : "Closed";
  }

  const now = new Date();
  const regOpen = milestones.find((m) => m.type === MILESTONE_TYPES.regOpen);
  const regClose = milestones.find((m) => m.type === MILESTONE_TYPES.regClose);

  if (regClose && new Date(regClose.date) < now) {
    return "Closed";
  }

  if (regOpen) {
    const openDate = new Date(regOpen.date);
    if (openDate < now) {
      return registrationUrl ? "Open" : "Closed";
    }
    // Registration opens in the future
    const daysUntilOpen = (openDate.getTime() - now.getTime()) / (1000 * 60 * 60 * 24);
    if (daysUntilOpen <= 7) {
      return "OpenSoon";
    }
  }

  return registrationUrl ? "Open" : "Closed";
}

export default function EventDetailPage({
  params,
}: {
  params: { id: string };
}) {
  const { id } = params;
  const router = useRouter();
  const { isAuthenticated, login, selectedTenant } = useAuth();
  const { t, locale } = useTranslation();
  const toast = useToast();
  const { onError } = useApiMutation();
  const { kmToDistance } = useDistances();
  const queryClient = useQueryClient();
  const [isPlanActionLoading, setIsPlanActionLoading] = useState(false);
  const [isCalendarLoading, setIsCalendarLoading] = useState(false);

  // Fetch event
  const {
    data: event,
    isLoading,
    error,
  } = useQuery({
    queryKey: ["event", id],
    queryFn: () => eventsApi.getById(id),
    enabled: !!id,
    retry: (failureCount, error) => {
      // Don't retry on 404 or 403
      if (error instanceof ApiError && (error.status === 404 || error.status === 403)) {
        return false;
      }
      return failureCount < 2;
    },
  });

  // Fetch milestones
  const { data: milestones } = useQuery({
    queryKey: ["event-milestones", id],
    queryFn: () => eventsApi.getMilestones(id),
    enabled: !!id && !!event,
  });

  // Fetch user plans to determine if event is already added
  const { data: planItems } = useQuery({
    queryKey: ["my-plans"],
    queryFn: () => plansApi.list(),
    enabled: isAuthenticated,
    staleTime: 30 * 1000,
  });

  const planItem = useMemo(
    () => planItems?.find((plan) => plan.eventId === id),
    [planItems, id]
  );

  const isInPlan = !!planItem;

  // Parse distances from event (string or string[])
  const distances: Distance[] = useMemo(() => {
    return normalizeEventDistances(event?.distances, kmToDistance);
  }, [event?.distances, kmToDistance]);

  // Format date
  const formatDate = (dateStr: string, options?: Intl.DateTimeFormatOptions) => {
    return new Intl.DateTimeFormat(locale === "tr" ? "tr-TR" : "en-US", {
      dateStyle: "medium",
      ...options,
    }).format(new Date(dateStr));
  };

  const dateLabel = useMemo(() => {
    if (!event?.startAt) return "";
    if (event.endAt) {
      const start = formatDate(event.startAt);
      const end = formatDate(event.endAt);
      if (start !== end) {
        return `${start} - ${end}`;
      }
    }
    return formatDate(event.startAt);
  }, [event?.startAt, event?.endAt, locale]);

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

  // Add to plan
  const handleAddToPlan = useCallback(async () => {
    if (!isAuthenticated) {
      login();
      return;
    }

    setIsPlanActionLoading(true);
    try {
      const created = await plansApi.create(id);
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
      setIsPlanActionLoading(false);
    }
  }, [isAuthenticated, login, plansApi, id, addPlanToCache, toast, t, removePlanItem, queryClient, onError]);

  const handleRemoveFromPlan = useCallback(async () => {
    if (!planItem) {
      queryClient.invalidateQueries({ queryKey: ["my-plans"] });
      return;
    }
    setIsPlanActionLoading(true);
    try {
      await removePlanItem(planItem.id);
    } finally {
      setIsPlanActionLoading(false);
    }
  }, [planItem, queryClient, removePlanItem]);

  // Add to calendar (download ICS)
  const handleAddToCalendar = useCallback(async () => {
    setIsCalendarLoading(true);
    try {
      const icsUrl = eventsApi.getIcsUrl(id);
      const headers: Record<string, string> = {
        Accept: "text/calendar",
        "Accept-Language": getStoredLocale(),
      };
      const token = getStoredToken();
      if (token) {
        headers.Authorization = `Bearer ${token}`;
      }
      const tenantId = getStoredTenantId();
      if (tenantId) {
        headers["X-Tenant-Id"] = tenantId;
      }

      const response = await fetch(icsUrl, { headers });
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }

      const blob = await response.blob();
      const downloadUrl = window.URL.createObjectURL(blob);
      const link = document.createElement("a");
      const safeTitle = (event?.title || "event")
        .toLowerCase()
        .replace(/[^a-z0-9]+/g, "-")
        .replace(/(^-|-$)/g, "");
      link.href = downloadUrl;
      link.download = `${safeTitle || "event"}.ics`;
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(downloadUrl);

      toast.success(t("toast.addedToCalendar"));
    } catch {
      toast.error(t("toast.calendarFailed"));
    } finally {
      setIsCalendarLoading(false);
    }
  }, [event?.title, id, t, toast]);

  // Determine registration status
  const registrationStatus = useMemo(() => {
    return determineRegistrationStatus(milestones, event?.registrationUrl ?? null);
  }, [milestones, event?.registrationUrl]);

  // Convert milestones to timeline format (only REG_OPEN and REG_CLOSE)
  const timelineMilestones = useMemo<Milestone[]>(() => {
    return (milestones || [])
      .filter((m) => isRegistrationMilestone(m.type))
      .map((m) => {
        const now = new Date();
        const milestoneDate = new Date(m.date);
        return {
          id: m.id,
          type: m.type,
          label: m.label,
          date: m.date,
          description: m.description,
          status:
            milestoneDate < now
              ? "completed"
              : milestoneDate.toDateString() === now.toDateString()
              ? "current"
              : "upcoming",
        };
      });
  }, [milestones]);

  const registrationStatusConfig = {
    Open: {
      label: t("event.registrationOpen"),
      variant: "success" as const,
    },
    OpenSoon: {
      label: t("event.registrationOpenSoon"),
      variant: "secondary" as const,
    },
    Closed: {
      label: t("event.registrationClosed"),
      variant: "muted" as const,
    },
  };

  const regStatus = registrationStatusConfig[registrationStatus];

  const registrationAction = useMemo(() => {
    const isOpen = registrationStatus === "Open";
    const label = isOpen ? t("event.registerNow") : t("event.eventWebsite");
    const href = isOpen ? event?.registrationUrl : event?.organizerUrl;
    const hint = isOpen
      ? t("event.registrationLinkMissing")
      : t("event.websiteLinkMissing");

    return {
      label,
      href: href || undefined,
      hint: href ? undefined : hint,
      disabled: !href,
    };
  }, [event?.organizerUrl, event?.registrationUrl, registrationStatus, t]);

  const registrationInfo = useMemo(() => {
    if (registrationStatus === "Open") {
      return t("event.registrationInfoOpen");
    }
    if (registrationStatus === "OpenSoon") {
      return t("event.registrationInfoOpenSoon");
    }
    return t("event.registrationInfoClosed");
  }, [registrationStatus, t]);

  // Loading state
  if (isLoading) {
    return <EventDetailSkeleton />;
  }

  // Error state - 404
  if (error instanceof ApiError && error.status === 404) {
    return (
      <PageShell>
        <EmptyState
          icon={CalendarX}
          title={t("event.notFoundTitle")}
          description={t("event.notFoundDesc")}
          action={{
            label: t("nav.backToExplore"),
            onClick: () => router.push("/"),
          }}
        />
      </PageShell>
    );
  }

  // Error state - 403 Forbidden
  if (error instanceof ApiError && error.status === 403) {
    return (
      <PageShell>
        <EmptyState
          icon={SearchX}
          title={t("event.forbiddenTitle")}
          description={t("event.forbiddenDesc")}
          action={
            selectedTenant
              ? {
                  label: t("nav.selectTenant"),
                  onClick: () => {
                    // Trigger tenant selector (could be a modal or navigation)
                    router.push("/");
                  },
                }
              : undefined
          }
        />
      </PageShell>
    );
  }

  // Error state - other errors
  if (error || !event) {
    return (
      <PageShell>
        <EmptyState
          icon={CalendarX}
          title={t("event.notFoundTitle")}
          description={t("event.notFoundDesc")}
          action={{
            label: t("nav.backToExplore"),
            onClick: () => router.push("/"),
          }}
        />
      </PageShell>
    );
  }

  return (
    <PageShell>
      {/* Back link */}
      <button
        onClick={() => router.push("/")}
        className="inline-flex items-center gap-1.5 text-sm text-muted-foreground transition-colors hover:text-foreground"
      >
        <ArrowLeft className="h-4 w-4" />
        {t("nav.backToExplore")}
      </button>

      <section className="card-sports p-6 md:p-8">
        <div className="flex flex-col gap-6 lg:flex-row lg:items-center lg:justify-between">
          <div className="space-y-3">
            <div className="flex flex-wrap items-center gap-2">
              <Badge variant={regStatus.variant} className="gap-1">
                {regStatus.label}
              </Badge>
              {distances.map((distance) => (
                <DistanceBadge key={distance} distance={distance} size="sm" />
              ))}
            </div>

            <h1 className="font-display text-3xl font-bold tracking-tight md:text-4xl">
              {event.title}
            </h1>

            <div className="flex flex-wrap items-center gap-x-4 gap-y-2 text-sm text-muted-foreground">
              <span className="inline-flex items-center gap-2">
                <MapPin className="h-4 w-4" />
                {event.city}, {event.countryCode}
              </span>
              <span className="inline-flex items-center gap-2">
                <Calendar className="h-4 w-4" />
                {dateLabel}
              </span>
            </div>
          </div>

          <div className="flex w-full flex-col gap-2 sm:flex-row sm:flex-wrap sm:items-center sm:justify-end lg:w-auto">
            <div
              data-state={isInPlan ? "in" : "out"}
              className="transition-all duration-200 data-[state=in]:animate-in data-[state=in]:fade-in-0 data-[state=in]:zoom-in-95"
            >
              {isInPlan ? (
                <Button
                  variant="outline"
                  onClick={handleRemoveFromPlan}
                  disabled={isPlanActionLoading}
                  className="gap-2 border-destructive/40 text-destructive hover:bg-destructive/10"
                >
                  {isPlanActionLoading ? (
                    <Loader2 className="h-4 w-4 animate-spin" />
                  ) : (
                    <Trash2 className="h-4 w-4" />
                  )}
                  {t("event.removeFromPlan")}
                </Button>
              ) : (
                <Button
                  variant="accent"
                  onClick={handleAddToPlan}
                  className="gap-2"
                  disabled={isPlanActionLoading}
                >
                  {isPlanActionLoading ? (
                    <Loader2 className="h-4 w-4 animate-spin" />
                  ) : (
                    <Plus className="h-4 w-4" />
                  )}
                  {t("event.addToPlan")}
                </Button>
              )}
            </div>

            <div className="flex flex-col gap-1">
              {registrationAction.href ? (
                <Button
                  variant={registrationStatus === "Open" ? "accent" : "outline"}
                  asChild
                  className="gap-2"
                >
                  <a
                    href={registrationAction.href}
                    target="_blank"
                    rel="noopener noreferrer"
                  >
                    {registrationAction.label}
                    <ExternalLink className="h-4 w-4" />
                  </a>
                </Button>
              ) : (
                <Button
                  variant={registrationStatus === "Open" ? "accent" : "outline"}
                  disabled
                  className="gap-2"
                  title={registrationAction.hint}
                >
                  {registrationAction.label}
                  <ExternalLink className="h-4 w-4" />
                </Button>
              )}
              {registrationAction.hint && (
                <span className="text-xs text-muted-foreground">
                  {registrationAction.hint}
                </span>
              )}
            </div>

            <Button
              variant="ghost"
              onClick={handleAddToCalendar}
              className="gap-2"
              disabled={isCalendarLoading}
            >
              {isCalendarLoading ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                <Calendar className="h-4 w-4" />
              )}
              {t("event.addToCalendar")}
            </Button>
          </div>
        </div>
      </section>

      <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_320px]">
        <div className="space-y-6">
          <Card className="rounded-2xl border shadow-sm">
            <CardHeader>
              <CardTitle>{t("event.description")}</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              {event.description ? (
                <p className="text-muted-foreground whitespace-pre-wrap leading-relaxed">
                  {event.description}
                </p>
              ) : (
                <p className="text-muted-foreground italic">
                  {locale === "tr"
                    ? "Bu etkinlik için açıklama bulunmuyor."
                    : "No description available for this event."}
                </p>
              )}
            </CardContent>
          </Card>
        </div>

        <div className="space-y-6">
          <Card className="rounded-2xl border shadow-sm">
            <CardHeader>
              <CardTitle>{t("event.registration")}</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <Badge variant={regStatus.variant} className="w-fit">
                {regStatus.label}
              </Badge>
              <p className="text-sm text-muted-foreground">{registrationInfo}</p>
              {timelineMilestones.length > 0 && (
                <div className="pt-2">
                  <MilestoneTimeline milestones={timelineMilestones} />
                </div>
              )}
            </CardContent>
          </Card>

          {(event.organizerName || event.organizerUrl) && (
            <Card className="rounded-2xl border shadow-sm">
              <CardHeader>
                <CardTitle>{t("event.organizer")}</CardTitle>
              </CardHeader>
              <CardContent className="space-y-2">
                {event.organizerName && (
                  <p className="text-sm font-medium text-foreground">
                    {event.organizerName}
                  </p>
                )}
                {event.organizerUrl && (
                  <a
                    href={event.organizerUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="inline-flex items-center gap-1 text-sm text-accent hover:underline"
                  >
                    {t("event.visitWebsite")}
                    <ExternalLink className="h-4 w-4" />
                  </a>
                )}
              </CardContent>
            </Card>
          )}
        </div>
      </div>
    </PageShell>
  );
}

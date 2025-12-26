"use client";

import { useMemo, useCallback, useState } from "react";
import { useRouter } from "next/navigation";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useAuth } from "@/contexts/auth-context";
import { useTranslation } from "@/contexts/locale-context";
import { useToast } from "@/components/ui/toast";
import { useApiMutation } from "@/hooks/use-api-error";
import { eventsApi, plansApi, Event, EventMilestone, ApiError, PlanItem } from "@/lib/api-client";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import {
  Dialog,
  DialogTrigger,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
  DialogClose,
} from "@/components/ui/dialog";
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
import {
  Calendar,
  MapPin,
  ExternalLink,
  ArrowLeft,
  Flag,
  Download,
  Plus,
  Check,
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

  const handleRemoveFromPlan = useCallback(() => {
    if (!planItem) {
      queryClient.invalidateQueries({ queryKey: ["my-plans"] });
      return;
    }
    setIsPlanActionLoading(true);
    void removePlanItem(planItem.id).finally(() => {
      setIsPlanActionLoading(false);
    });
  }, [planItem, queryClient, removePlanItem]);

  // Add to calendar (download ICS)
  const handleAddToCalendar = () => {
    const icsUrl = eventsApi.getIcsUrl(id);
    window.open(icsUrl, "_blank");
  };

  // Loading state
  if (isLoading) {
    return <EventDetailSkeleton />;
  }

  // Error state - 404
  if (error instanceof ApiError && error.status === 404) {
    return (
      <div className="container-app max-w-6xl">
        <EmptyState
          icon={CalendarX}
          title={t("event.notFoundTitle")}
          description={t("event.notFoundDesc")}
          action={{
            label: t("nav.backToExplore"),
            onClick: () => router.push("/"),
          }}
        />
      </div>
    );
  }

  // Error state - 403 Forbidden
  if (error instanceof ApiError && error.status === 403) {
    return (
      <div className="container-app max-w-6xl">
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
      </div>
    );
  }

  // Error state - other errors
  if (error || !event) {
    return (
      <div className="container-app max-w-6xl">
        <EmptyState
          icon={CalendarX}
          title={t("event.notFoundTitle")}
          description={t("event.notFoundDesc")}
          action={{
            label: t("nav.backToExplore"),
            onClick: () => router.push("/"),
          }}
        />
      </div>
    );
  }

  // Determine registration status
  const registrationStatus = determineRegistrationStatus(milestones, event.registrationUrl);

  // Convert milestones to timeline format (only REG_OPEN and REG_CLOSE)
  const timelineMilestones: Milestone[] = (milestones || [])
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

  // Registration status badge config
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

  return (
    <div className="container-app max-w-6xl">
      {/* Back link */}
      <button
        onClick={() => router.push("/")}
        className="mb-6 inline-flex items-center gap-1.5 text-sm text-muted-foreground transition-colors hover:text-foreground"
      >
        <ArrowLeft className="h-4 w-4" />
        {t("nav.backToExplore")}
      </button>

      {/* Header section */}
      <div className="mb-8">
        {/* Title */}
        <h1 className="mb-4 font-display text-3xl font-bold tracking-tight md:text-4xl lg:text-5xl">
          {event.title}
        </h1>

        {/* Meta row */}
        <div className="flex flex-wrap items-center gap-x-6 gap-y-2 text-muted-foreground">
          <span className="inline-flex items-center gap-2">
            <MapPin className="h-5 w-5" />
            {event.city}, {event.countryCode}
          </span>
          <span className="inline-flex items-center gap-2">
            <Calendar className="h-5 w-5" />
            {formatDate(event.startAt)}
          </span>
          {distances.length > 0 && (
            <div className="inline-flex items-center gap-2">
              <Flag className="h-5 w-5" />
              <div className="flex flex-wrap gap-1.5">
                {distances.map((d) => (
                  <DistanceBadge key={d} distance={d} />
                ))}
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Actions (desktop right / mobile stacked) */}
      <div className="mb-8 flex flex-col gap-3 sm:flex-row">
        {isInPlan ? (
          <Dialog>
            <DialogTrigger asChild>
              <Button variant="outline" size="lg" className="gap-2" disabled={isPlanActionLoading}>
                {isPlanActionLoading ? (
                  <Loader2 className="h-4 w-4 animate-spin" />
                ) : (
                  <Check className="h-4 w-4" />
                )}
                {t("event.addedToPlan")}
              </Button>
            </DialogTrigger>
            <DialogContent className="max-w-sm">
              <DialogHeader>
                <DialogTitle>{t("event.removeFromPlanTitle")}</DialogTitle>
                <DialogDescription>{t("event.removeFromPlanDesc")}</DialogDescription>
              </DialogHeader>
              <DialogFooter>
                <DialogClose asChild>
                  <Button variant="ghost">{t("common.cancel")}</Button>
                </DialogClose>
                <DialogClose asChild>
                  <Button variant="destructive" onClick={handleRemoveFromPlan} disabled={isPlanActionLoading}>
                    {t("event.removeFromPlan")}
                  </Button>
                </DialogClose>
              </DialogFooter>
            </DialogContent>
          </Dialog>
        ) : (
          <Button
            variant="accent"
            size="lg"
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

        <Button variant="outline" size="lg" onClick={handleAddToCalendar} className="gap-2">
          <Download className="h-4 w-4" />
          {t("event.addToCalendar")}
        </Button>
      </div>

      {/* Sections */}
      <div className="space-y-6">
        {/* Registration card */}
        <Card className="rounded-2xl border shadow-sm">
          <CardHeader>
            <CardTitle>{t("event.registration")}</CardTitle>
          </CardHeader>
          <CardContent className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
            <Badge variant={regStatus.variant} className="w-fit">
              {regStatus.label}
            </Badge>
            {event.registrationUrl && registrationStatus !== "Closed" && (
              <Button variant="outline" size="lg" asChild className="gap-2">
                <a
                  href={event.registrationUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                >
                  {t("event.registration")}
                  <ExternalLink className="h-4 w-4" />
                </a>
              </Button>
            )}
          </CardContent>
        </Card>

        {/* Milestones timeline card */}
        {timelineMilestones.length > 0 && (
          <Card className="rounded-2xl border shadow-sm">
            <CardHeader>
              <CardTitle>{t("event.milestones")}</CardTitle>
            </CardHeader>
            <CardContent>
              <MilestoneTimeline milestones={timelineMilestones} />
            </CardContent>
          </Card>
        )}

        {/* Event info card */}
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

            {/* Organizer info */}
            {(event.organizerName || event.organizerUrl) && (
              <div className="pt-4 border-t">
                <h3 className="mb-2 text-sm font-semibold">{t("event.organizer")}</h3>
                <div className="flex items-center gap-2">
                  {event.organizerName && (
                    <span className="text-sm text-foreground">{event.organizerName}</span>
                  )}
                  {event.organizerUrl && (
                    <a
                      href={event.organizerUrl}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="inline-flex items-center gap-1 text-sm text-accent hover:underline"
                    >
                      {t("event.visitWebsite")}
                      <ExternalLink className="h-3 w-3" />
                    </a>
                  )}
                </div>
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

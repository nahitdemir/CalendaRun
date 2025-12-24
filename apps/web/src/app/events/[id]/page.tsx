"use client";

import { useRouter } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { useAuth } from "@/contexts/auth-context";
import { useTranslation } from "@/contexts/locale-context";
import { useToast } from "@/components/ui/toast";
import { useApiMutation } from "@/hooks/use-api-error";
import { eventsApi, plansApi, Event, EventMilestone, ApiError } from "@/lib/api-client";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { DistanceBadge, Distance } from "@/components/distance-badge";
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
  const regOpen = milestones.find((m) => m.type === "REG_OPEN");
  const regClose = milestones.find((m) => m.type === "REG_CLOSE");

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

  // Format date
  const formatDate = (dateStr: string, options?: Intl.DateTimeFormatOptions) => {
    return new Intl.DateTimeFormat(locale === "tr" ? "tr-TR" : "en-US", {
      dateStyle: "medium",
      ...options,
    }).format(new Date(dateStr));
  };

  // Add to plan
  const handleAddToPlan = async () => {
    if (!isAuthenticated) {
      login();
      return;
    }

    try {
      await plansApi.create(id);
      toast.success(t("toast.addedToPlan"));
    } catch (err) {
      if (err instanceof ApiError && err.status === 409) {
        toast.error(t("toast.alreadyInPlan"));
      } else {
        onError(err);
      }
    }
  };

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

  // Parse distances
  const distances: Distance[] = (event.distances || []).filter((d): d is Distance =>
    ["5K", "10K", "21K", "42K", "ultra"].includes(d)
  );

  // Determine registration status
  const registrationStatus = determineRegistrationStatus(milestones, event.registrationUrl);

  // Convert milestones to timeline format (only REG_OPEN and REG_CLOSE)
  const timelineMilestones: Milestone[] = (milestones || [])
    .filter((m) => m.type === "REG_OPEN" || m.type === "REG_CLOSE")
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
        <Button variant="accent" size="lg" onClick={handleAddToPlan} className="gap-2">
          <Plus className="h-4 w-4" />
          {t("event.addToPlan")}
        </Button>

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

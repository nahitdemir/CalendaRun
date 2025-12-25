"use client";

import { useRouter } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { PlanItem, eventsApi } from "@/lib/api-client";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import { DistanceBadge } from "@/components/distance-badge";
import type { Distance } from "@/components/distance-badge";
import { useDistances } from "@/hooks/use-distances";
import { normalizeEventDistances } from "@/lib/normalizers/event";
import { normalizePlanState } from "@/lib/normalizers/plan";
import { isRegistrationMilestone } from "@/lib/constants/milestones";
import {
  Calendar,
  MapPin,
  ExternalLink,
  CheckCircle2,
  Trophy,
  Trash2,
  Flag,
  Clock,
  Loader2,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { useTranslation } from "@/contexts/locale-context";

interface PlanItemCardProps {
  plan: PlanItem;
  isLoading?: boolean;
  onUpdateState: (id: string, state: "Registered" | "Completed") => void;
  onDelete?: (id: string) => void;
}

export function PlanItemCard({
  plan,
  isLoading = false,
  onUpdateState,
  onDelete,
}: PlanItemCardProps) {
  const router = useRouter();
  const { t, locale } = useTranslation();
  const { kmToDistance } = useDistances();

  // Fetch milestones to find next important date
  const { data: milestones } = useQuery({
    queryKey: ["event-milestones", plan.eventId],
    queryFn: () => eventsApi.getMilestones(plan.eventId),
    enabled: !!plan.eventId,
  });

  const event = plan.event;
  if (!event) return null;

  // Parse distances from event (string or string[])
  const distances: Distance[] = normalizeEventDistances(event.distances, kmToDistance);

  // Format date
  const formatDate = (dateStr: string) => {
    return new Intl.DateTimeFormat(locale === "tr" ? "tr-TR" : "en-US", {
      day: "numeric",
      month: "short",
      year: "numeric",
    }).format(new Date(dateStr));
  };

  // Calculate countdown
  const getCountdown = (): number | null => {
    if (!event.startAt) return null;
    const now = new Date();
    const eventDate = new Date(event.startAt);
    const diffTime = eventDate.getTime() - now.getTime();
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
    return diffDays > 0 ? diffDays : null;
  };

  const countdown = getCountdown();

  // Find next important milestone
  const getNextImportantDate = (): { date: string; label: string } | null => {
    if (!milestones || milestones.length === 0) return null;

    const now = new Date();
    const upcoming = milestones
      .filter((m) => {
        const milestoneDate = new Date(m.date);
        return milestoneDate > now && isRegistrationMilestone(m.type);
      })
      .sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime());

    if (upcoming.length === 0) return null;

    const next = upcoming[0];
    return {
      date: next.date,
      label: next.label,
    };
  };

  const nextImportant = getNextImportantDate();

  // State config
  const stateConfig = {
    Active: {
      label: t("plan.status.planned"),
      variant: "muted" as const,
      icon: Clock,
    },
    Registered: {
      label: t("plan.status.registered"),
      variant: "secondary" as const,
      icon: CheckCircle2,
    },
    Completed: {
      label: t("plan.status.completed"),
      variant: "muted" as const,
      icon: Trophy,
    },
    Cancelled: {
      label: t("plan.status.planned"),
      variant: "muted" as const,
      icon: Clock,
    },
  };

  // Normalize state to string
  const stateStr = normalizePlanState(plan.state);

  const config = stateConfig[stateStr] || stateConfig.Active;
  const StateIcon = config.icon;

  // Format next important date
  const formatNextDate = (dateStr: string) => {
    return new Intl.DateTimeFormat(locale === "tr" ? "tr-TR" : "en-US", {
      day: "numeric",
      month: "short",
    }).format(new Date(dateStr));
  };

  return (
    <Card className="rounded-2xl border bg-card p-4 shadow-sm transition-colors hover:bg-muted/40 md:p-5">
      <div className="flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
        {/* Left: Content */}
        <div className="flex-1 space-y-3">
          {/* State chip + Countdown */}
          <div className="flex flex-wrap items-center justify-between gap-2">
            <Badge variant={config.variant} className="gap-1.5">
              <StateIcon className="h-3 w-3" />
              {config.label}
            </Badge>
            {countdown !== null && countdown > 0 && (
              <span className="text-xs font-medium text-muted-foreground">
                {t("plan.countdown", { days: countdown })}
              </span>
            )}
          </div>

          {/* Title */}
          <h3 className="text-lg font-semibold leading-tight">{event.title}</h3>

          {/* Meta row */}
          <div className="flex flex-wrap items-center gap-x-4 gap-y-2 text-sm text-muted-foreground">
            <span className="inline-flex items-center gap-1.5">
              <MapPin className="h-4 w-4" />
              {event.city}, {event.countryCode}
            </span>
            <span className="inline-flex items-center gap-1.5">
              <Calendar className="h-4 w-4" />
              {formatDate(event.startAt)}
            </span>
          </div>

          {/* Distance badges */}
          {distances.length > 0 && (
            <div className="flex flex-wrap gap-1.5">
              {distances.map((d) => (
                <DistanceBadge key={d} distance={d} size="sm" />
              ))}
            </div>
          )}

          {/* Next important date */}
          {nextImportant ? (
            <div className="flex items-center gap-2 text-sm">
              <Flag className="h-4 w-4 text-accent" />
              <span className="text-muted-foreground">
                {nextImportant.label}: {formatNextDate(nextImportant.date)}
              </span>
            </div>
          ) : (
            <div className="text-sm text-muted-foreground">
              {t("plan.nextImportantDate")}
            </div>
          )}
        </div>

        {/* Right: Actions */}
        <div className="flex flex-wrap items-center gap-2">
          {/* Primary action based on state */}
          {stateStr === "Active" && (
            <Button
              variant="accent"
              size="sm"
              onClick={() => onUpdateState(plan.id, "Registered")}
              disabled={isLoading}
              className="gap-1.5"
            >
              {isLoading ? (
                <Loader2 className="h-3 w-3 animate-spin" />
              ) : (
                <CheckCircle2 className="h-3 w-3" />
              )}
              {t("plan.markRegistered")}
            </Button>
          )}

          {stateStr === "Registered" && (
            <Button
              variant="success"
              size="sm"
              onClick={() => onUpdateState(plan.id, "Completed")}
              disabled={isLoading}
              className="gap-1.5"
            >
              {isLoading ? (
                <Loader2 className="h-3 w-3 animate-spin" />
              ) : (
                <Trophy className="h-3 w-3" />
              )}
              {t("plan.markCompleted")}
            </Button>
          )}

          {/* Secondary actions */}
          <Button
            variant="outline"
            size="sm"
            onClick={() => router.push(`/events/${plan.eventId}`)}
            className="gap-1.5"
          >
            {t("plan.details")}
          </Button>

          {event.registrationUrl && stateStr !== "Completed" && (
            <Button variant="outline" size="sm" asChild className="gap-1.5">
              <a
                href={event.registrationUrl}
                target="_blank"
                rel="noopener noreferrer"
              >
                {t("plan.registration")}
                <ExternalLink className="h-3 w-3" />
              </a>
            </Button>
          )}

          {/* Delete button (if supported) */}
          {onDelete && (
            <Button
              variant="ghost"
              size="icon-sm"
              onClick={() => onDelete(plan.id)}
              disabled={isLoading}
              className="text-muted-foreground hover:text-destructive"
            >
              {isLoading ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                <Trash2 className="h-4 w-4" />
              )}
            </Button>
          )}
        </div>
      </div>
    </Card>
  );
}

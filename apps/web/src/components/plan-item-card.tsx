"use client";

import { useRouter } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { PlanItem, eventsApi } from "@/lib/api-client";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { DistanceBadge } from "@/components/distance-badge";
import type { Distance } from "@/components/distance-badge";
import { useDistances } from "@/hooks/use-distances";
import { normalizeEventDistances } from "@/lib/normalizers/event";
import { normalizePlanState } from "@/lib/normalizers/plan";
import { MILESTONE_TYPES } from "@/lib/constants/milestones";
import {
  Calendar,
  MapPin,
  ExternalLink,
  CheckCircle2,
  Trophy,
  Trash2,
  Clock,
  Loader2,
  MoreHorizontal,
  ArrowRight,
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
      variant: "secondary" as const,
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

  const formatMiniDate = (dateStr: string) => {
    return new Intl.DateTimeFormat(locale === "tr" ? "tr-TR" : "en-US", {
      day: "numeric",
      month: "short",
    }).format(new Date(dateStr));
  };

  const registrationOpen = milestones?.find((m) => m.type === MILESTONE_TYPES.regOpen);
  const registrationClose = milestones?.find((m) => m.type === MILESTONE_TYPES.regClose);
  const hasRegistrationMilestones = Boolean(registrationOpen || registrationClose);

  return (
    <Card className={cn("card-sports p-4 md:p-5", "hover-lift")}>
      <div className="flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
        {/* Left: Content */}
        <div className="flex-1 space-y-3">
          {/* State chip + Countdown */}
          <div className="flex flex-wrap items-center gap-2">
            <Badge variant={config.variant} className="gap-2">
              <StateIcon className="h-4 w-4" />
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

          <div className="rounded-xl border bg-muted/30 p-3">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <span className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                {t("plan.importantDates")}
              </span>
              {!hasRegistrationMilestones && (
                <Button
                  variant="ghost"
                  size="sm"
                  className="h-9 px-2"
                  onClick={() => {
                    const icsUrl = eventsApi.getIcsUrl(plan.eventId);
                    window.open(icsUrl, "_blank");
                  }}
                >
                  {t("plan.addReminder")}
                </Button>
              )}
            </div>
            <div className="mt-2 space-y-1 text-sm">
              <div className="flex items-center justify-between gap-3">
                <span className="text-muted-foreground">{t("plan.raceDay")}</span>
                <span className="font-medium">{formatMiniDate(event.startAt)}</span>
              </div>
              {registrationOpen && (
                <div className="flex items-center justify-between gap-3">
                  <span className="text-muted-foreground">
                    {t("plan.registrationOpen")}
                  </span>
                  <span className="font-medium">
                    {formatMiniDate(registrationOpen.date)}
                  </span>
                </div>
              )}
              {registrationClose && (
                <div className="flex items-center justify-between gap-3">
                  <span className="text-muted-foreground">
                    {t("plan.registrationClose")}
                  </span>
                  <span className="font-medium">
                    {formatMiniDate(registrationClose.date)}
                  </span>
                </div>
              )}
            </div>
            {!hasRegistrationMilestones && (
              <p className="mt-2 text-xs text-muted-foreground">
                {t("plan.nextImportantDate")}
              </p>
            )}
          </div>
        </div>

        {/* Right: Actions */}
        <div className="flex flex-wrap items-center gap-2 md:flex-col md:items-end">
          {stateStr === "Active" && (
            <Button
              variant="accent"
              onClick={() => onUpdateState(plan.id, "Registered")}
              disabled={isLoading}
              className="gap-2"
            >
              {isLoading ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                <CheckCircle2 className="h-4 w-4" />
              )}
              {t("plan.markRegistered")}
            </Button>
          )}

          <Button
            variant="ghost"
            size="sm"
            onClick={() => router.push(`/events/${plan.eventId}`)}
            className="h-9 gap-2"
          >
            {t("plan.details")}
            <ArrowRight className="h-4 w-4" />
          </Button>

          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                variant="ghost"
                size="sm"
                className="h-9 w-9 p-0"
                aria-label={t("common.actions")}
                disabled={isLoading}
              >
                <MoreHorizontal className="h-4 w-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              {event.registrationUrl && stateStr !== "Completed" && (
                <DropdownMenuItem
                  onSelect={(menuEvent) => {
                    menuEvent.preventDefault();
                    window.open(event.registrationUrl, "_blank");
                  }}
                  disabled={isLoading}
                >
                  <ExternalLink className="mr-2 h-4 w-4" />
                  {t("plan.registration")}
                </DropdownMenuItem>
              )}

              {stateStr === "Registered" && (
                <DropdownMenuItem
                  onSelect={() => onUpdateState(plan.id, "Completed")}
                  disabled={isLoading}
                >
                  <Trophy className="mr-2 h-4 w-4" />
                  {t("plan.markCompleted")}
                </DropdownMenuItem>
              )}

              {onDelete && (
                <>
                  {(event.registrationUrl || stateStr === "Registered") && (
                    <DropdownMenuSeparator />
                  )}
                  <DropdownMenuItem
                    onSelect={() => onDelete(plan.id)}
                    disabled={isLoading}
                    className="text-destructive focus:text-destructive"
                  >
                    <Trash2 className="mr-2 h-4 w-4" />
                    {t("plan.remove")}
                  </DropdownMenuItem>
                </>
              )}
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </div>
    </Card>
  );
}

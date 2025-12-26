"use client";

import { useRouter } from "next/navigation";
import { useTranslation } from "@/contexts/locale-context";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import {
  DropdownMenu,
  DropdownMenuTrigger,
  DropdownMenuContent,
  DropdownMenuItem,
} from "@/components/ui/dropdown-menu";
import { DistanceBadge, Distance } from "@/components/distance-badge";
import { eventsApi } from "@/lib/api-client";
import {
  Calendar,
  MapPin,
  ArrowRight,
  Flag,
  ExternalLink,
  ChevronDown,
  List,
  Trash2,
  Plus,
  Loader2,
} from "lucide-react";
import { cn } from "@/lib/utils";

export interface EventCardData {
  id: string;
  title: string;
  city: string;
  countryCode: string;
  date: string;
  distances: Distance[];
  registrationStatus: "open" | "closed" | "upcoming";
  registrationUrl?: string;
}

interface EventCardProps {
  event: EventCardData;
  onView?: () => void;
  onAddToPlan?: () => void;
  onRemoveFromPlan?: () => void;
  isInPlan?: boolean;
  planItemId?: string;
  isPlanActionLoading?: boolean;
  showActions?: boolean;
  className?: string;
}

export function EventCard({
  event,
  onView,
  onAddToPlan,
  onRemoveFromPlan,
  isInPlan = false,
  isPlanActionLoading = false,
  showActions = true,
  className,
}: EventCardProps) {
  const { t, locale } = useTranslation();
  const router = useRouter();

  const formattedDate = new Intl.DateTimeFormat(
    locale === "tr" ? "tr-TR" : "en-US",
    {
      weekday: "short",
      day: "numeric",
      month: "short",
      year: "numeric",
    }
  ).format(new Date(event.date));

  const statusConfig = {
    open: {
      label: t("event.registrationOpen"),
      variant: "success" as const,
      icon: Flag,
    },
    closed: {
      label: t("event.registrationClosed"),
      variant: "muted" as const,
      icon: null,
    },
    upcoming: {
      label: t("event.upcoming"),
      variant: "secondary" as const,
      icon: null,
    },
  };

  const status = statusConfig[event.registrationStatus];
  const StatusIcon = status.icon;

  const handleViewPlan = () => {
    router.push("/plan");
  };

  const handleAddToCalendar = () => {
    const icsUrl = eventsApi.getIcsUrl(event.id);
    window.open(icsUrl, "_blank");
  };

  const planAction = onAddToPlan ? (
    isInPlan ? (
      <div
        data-state="added"
        className={cn(
          "inline-flex items-center transition-all duration-200",
          "data-[state=added]:animate-in data-[state=added]:fade-in-0 data-[state=added]:zoom-in-95"
        )}
      >
        <Button
          variant="outline"
          size="default"
          onClick={onRemoveFromPlan}
          disabled={isPlanActionLoading || !onRemoveFromPlan}
          className={cn(
            "rounded-r-none border-destructive/40 text-destructive hover:bg-destructive/10",
            "gap-2"
          )}
        >
          {isPlanActionLoading ? (
            <Loader2 className="h-4 w-4 animate-spin" />
          ) : (
            <Trash2 className="h-4 w-4" />
          )}
          {t("event.removeFromPlan")}
        </Button>
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button
              variant="outline"
              size="default"
              disabled={isPlanActionLoading}
              className={cn(
                "-ml-px rounded-l-none border-destructive/40 text-destructive hover:bg-destructive/10",
                "px-2"
              )}
              aria-label={t("common.actions")}
            >
              <ChevronDown className="h-4 w-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem onSelect={handleViewPlan} disabled={isPlanActionLoading}>
              <List className="mr-2 h-4 w-4" />
              {t("event.viewPlan")}
            </DropdownMenuItem>
            <DropdownMenuItem onSelect={handleAddToCalendar} disabled={isPlanActionLoading}>
              <Calendar className="mr-2 h-4 w-4" />
              {t("event.addToCalendar")}
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    ) : (
      <Button
        variant="accent"
        size="default"
        onClick={onAddToPlan}
        disabled={isPlanActionLoading}
      >
        {isPlanActionLoading ? (
          <Loader2 className="h-4 w-4 animate-spin" />
        ) : (
          <Plus className="h-4 w-4" />
        )}
        {t("event.addToPlan")}
      </Button>
    )
  ) : null;

  return (
    <article
      className={cn(
        "group rounded-2xl border bg-card p-4 shadow-sm transition-all hover:shadow-md md:p-5",
        "md:grid md:grid-cols-[1fr_auto] md:items-center md:gap-6",
        className
      )}
    >
      {/* Left: Content */}
      <div className="space-y-3">
        {/* Badges */}
        <div className="flex flex-wrap items-center gap-2">
          {event.distances.map((d) => (
            <DistanceBadge key={d} distance={d} size="sm" />
          ))}
          <Badge variant={status.variant} className="gap-1">
            {StatusIcon && <StatusIcon className="h-4 w-4" />}
            {status.label}
          </Badge>
        </div>

        {/* Title */}
        <h3 className="text-lg font-semibold leading-tight md:text-xl">
          {event.title}
        </h3>

        {/* Meta */}
        <div className="flex flex-wrap items-center gap-x-4 gap-y-2 text-sm text-muted-foreground">
          <span className="inline-flex items-center gap-1.5">
            <MapPin className="h-4 w-4" />
            {event.city}, {event.countryCode}
          </span>
          <span className="inline-flex items-center gap-1.5">
            <Calendar className="h-4 w-4" />
            {formattedDate}
          </span>
        </div>
      </div>

      {/* Right: Actions */}
      {showActions && (
        <div className="mt-4 flex flex-wrap items-center gap-2 md:mt-0 md:flex-col md:items-end">
          {planAction}

          {event.registrationStatus === "open" && event.registrationUrl ? (
            <Button variant="accent" size="default" asChild>
              <a
                href={event.registrationUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="gap-2"
              >
                {t("event.registerNow")}
                <ExternalLink className="h-4 w-4" />
              </a>
            </Button>
          ) : null}

          {onView && (
            <Button variant="ghost" size="sm" onClick={onView} className="h-9 gap-2">
              {t("event.view")}
              <ArrowRight className="h-4 w-4" />
            </Button>
          )}
        </div>
      )}
    </article>
  );
}

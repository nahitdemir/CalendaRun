"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useAuth } from "@/contexts/auth-context";
import { useTranslation } from "@/contexts/locale-context";
import { useToast } from "@/components/ui/toast";
import { useApiMutation } from "@/hooks/use-api-error";
import { plansApi, PlanItem, eventsApi } from "@/lib/api-client";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { PageHeader } from "@/components/page-header";
import { EmptyState } from "@/components/empty-state";
import { MilestoneTimeline, Milestone } from "@/components/milestone-timeline";
import { DistanceBadge, Distance } from "@/components/distance-badge";
import { Skeleton } from "@/components/ui/skeleton";
import { AuthGuard } from "@/components/route-guards";
import {
  Calendar,
  MapPin,
  ExternalLink,
  Trash2,
  CheckCircle2,
  Clock,
  Flag,
  ChevronDown,
  ChevronUp,
  Loader2,
} from "lucide-react";
import { cn } from "@/lib/utils";

const statusConfig = {
  Active: {
    label: "plan.status.planned",
    icon: Clock,
    variant: "muted" as const,
  },
  Registered: {
    label: "plan.status.registered",
    icon: CheckCircle2,
    variant: "success" as const,
  },
  Completed: {
    label: "plan.status.completed",
    icon: Flag,
    variant: "accent" as const,
  },
  Cancelled: {
    label: "plan.status.planned",
    icon: Clock,
    variant: "muted" as const,
  },
};

function PlanPageContent() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const { t, locale } = useTranslation();
  const toast = useToast();
  const { onError } = useApiMutation();

  const [expandedId, setExpandedId] = useState<string | null>(null);
  const [loadingActions, setLoadingActions] = useState<Record<string, boolean>>({});

  // Fetch plans
  const {
    data: plans,
    isLoading,
    error,
  } = useQuery({
    queryKey: ["my-plans"],
    queryFn: () => plansApi.list(),
  });

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

  // Format date
  const formatDate = (dateStr: string) => {
    return new Intl.DateTimeFormat(locale === "tr" ? "tr-TR" : "en-US", {
      weekday: "short",
      day: "numeric",
      month: "short",
      year: "numeric",
    }).format(new Date(dateStr));
  };

  // Handle state update
  const handleUpdateState = async (id: string, state: "Registered" | "Completed") => {
    setLoadingActions((prev) => ({ ...prev, [id]: true }));
    try {
      await updateStateMutation.mutateAsync({ id, state });
    } finally {
      setLoadingActions((prev) => ({ ...prev, [id]: false }));
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

    setLoadingActions((prev) => ({ ...prev, [id]: true }));
    try {
      await deleteMutation.mutateAsync(id);
    } finally {
      setLoadingActions((prev) => ({ ...prev, [id]: false }));
    }
  };

  if (error) {
    onError(error);
  }

  if (isLoading) {
    return (
      <div className="container-app space-y-6">
        <Skeleton className="h-10 w-48" />
        <div className="space-y-4">
          {[1, 2, 3].map((i) => (
            <div key={i} className="rounded-2xl border bg-card p-5">
              <Skeleton className="mb-3 h-6 w-3/4" />
              <Skeleton className="mb-2 h-4 w-1/2" />
              <Skeleton className="h-4 w-1/3" />
            </div>
          ))}
        </div>
      </div>
    );
  }

  return (
    <div className="container-app space-y-6">
      <PageHeader title={t("plan.title")} />

      {!plans || plans.length === 0 ? (
        <EmptyState
          variant="plan"
          title={t("plan.emptyTitle")}
          description={t("plan.emptyDesc")}
          action={{
            label: t("nav.explore"),
            onClick: () => router.push("/"),
          }}
        />
      ) : (
        <div className="space-y-4">
          {plans.map((plan) => (
            <PlanItemCard
              key={plan.id}
              plan={plan}
              isExpanded={expandedId === plan.id}
              isLoading={loadingActions[plan.id] || false}
              onToggleExpand={() =>
                setExpandedId(expandedId === plan.id ? null : plan.id)
              }
              onUpdateState={handleUpdateState}
              onDelete={handleDelete}
              formatDate={formatDate}
              t={t}
              locale={locale}
            />
          ))}
        </div>
      )}
    </div>
  );
}

interface PlanItemCardProps {
  plan: PlanItem;
  isExpanded: boolean;
  isLoading: boolean;
  onToggleExpand: () => void;
  onUpdateState: (id: string, state: "Registered" | "Completed") => void;
  onDelete: (id: string) => void;
  formatDate: (date: string) => string;
  t: (key: string) => string;
  locale: string;
}

function PlanItemCard({
  plan,
  isExpanded,
  isLoading,
  onToggleExpand,
  onUpdateState,
  onDelete,
  formatDate,
  t,
  locale,
}: PlanItemCardProps) {
  const queryClient = useQueryClient();
  const config = statusConfig[plan.state] || statusConfig.Active;
  const StatusIcon = config.icon;

  // Fetch milestones when expanded
  const { data: milestones } = useQuery({
    queryKey: ["event-milestones", plan.eventId],
    queryFn: () => eventsApi.getMilestones(plan.eventId),
    enabled: isExpanded,
  });

  const timelineMilestones: Milestone[] = (milestones || []).map((m) => {
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

  // Get event info
  const event = plan.event;
  const distances: Distance[] = (event?.distances || []).filter((d): d is Distance =>
    ["5K", "10K", "21K", "42K", "ultra"].includes(d)
  );

  return (
    <article className="rounded-2xl border bg-card shadow-sm transition-all hover:shadow-md">
      <div className="p-4 md:p-5">
        <div className="flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
          {/* Left content */}
          <div className="flex-1 space-y-3">
            <div className="flex flex-wrap items-center gap-2">
              <Badge variant={config.variant} className="gap-1">
                <StatusIcon className="h-3 w-3" />
                {t(config.label)}
              </Badge>
              {distances.map((d) => (
                <DistanceBadge key={d} distance={d} size="sm" />
              ))}
            </div>

            <h3 className="text-lg font-semibold leading-tight">
              {event?.title || "Event"}
            </h3>

            <div className="flex flex-wrap items-center gap-x-4 gap-y-2 text-sm text-muted-foreground">
              {event && (
                <>
                  <span className="inline-flex items-center gap-1.5">
                    <MapPin className="h-4 w-4" />
                    {event.city}, {event.countryCode}
                  </span>
                  <span className="inline-flex items-center gap-1.5">
                    <Calendar className="h-4 w-4" />
                    {formatDate(event.startAt)}
                  </span>
                </>
              )}
            </div>
          </div>

          {/* Right content - actions */}
          <div className="flex flex-wrap items-center gap-2">
            {plan.state === "Active" && event?.registrationUrl && (
              <Button variant="accent" size="sm" asChild disabled={isLoading}>
                <a
                  href={event.registrationUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  onClick={(e) => {
                    e.stopPropagation();
                    // Also mark as registered when clicking
                    onUpdateState(plan.id, "Registered");
                  }}
                  className="gap-1.5"
                >
                  {t("plan.markRegistered")}
                  <ExternalLink className="h-3 w-3" />
                </a>
              </Button>
            )}

            {plan.state === "Registered" && (
              <Button
                variant="success"
                size="sm"
                onClick={() => onUpdateState(plan.id, "Completed")}
                disabled={isLoading}
              >
                {isLoading ? (
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                ) : (
                  <CheckCircle2 className="mr-2 h-4 w-4" />
                )}
                {t("plan.markCompleted")}
              </Button>
            )}

            <Button
              variant="ghost"
              size="sm"
              onClick={onToggleExpand}
            >
              {t("event.milestones")}
              {isExpanded ? (
                <ChevronUp className="ml-1 h-4 w-4" />
              ) : (
                <ChevronDown className="ml-1 h-4 w-4" />
              )}
            </Button>

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
          </div>
        </div>
      </div>

      {/* Expandable milestones */}
      <div
        className={cn(
          "overflow-hidden border-t transition-all duration-300",
          isExpanded ? "max-h-96" : "max-h-0 border-transparent"
        )}
      >
        <div className="p-4 md:p-5">
          {timelineMilestones.length > 0 ? (
            <MilestoneTimeline milestones={timelineMilestones} />
          ) : (
            <p className="text-sm text-muted-foreground">
              {locale === "tr"
                ? "Bu etkinlik için henüz milestone eklenmemiş."
                : "No milestones added for this event yet."}
            </p>
          )}
        </div>
      </div>
    </article>
  );
}

export default function PlanPage() {
  return (
    <AuthGuard>
      <PlanPageContent />
    </AuthGuard>
  );
}

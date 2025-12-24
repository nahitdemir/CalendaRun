"use client";

import { cn } from "@/lib/utils";
import { useTranslation } from "@/contexts/locale-context";
import { CheckCircle2, Circle, Flag, Play } from "lucide-react";

export type MilestoneType =
  | "REG_OPEN"
  | "REG_CLOSE"
  | "EVENT_START"
  | "EVENT_END"
  | "CUSTOM";

export interface Milestone {
  id: string;
  type: MilestoneType;
  label: string;
  date: string;
  description?: string;
  status: "completed" | "current" | "upcoming";
}

interface MilestoneTimelineProps extends React.HTMLAttributes<HTMLDivElement> {
  milestones: Milestone[];
}

const milestoneIcons: Record<string, React.ElementType> = {
  completed: CheckCircle2,
  current: Circle,
  upcoming: Circle,
  REG_OPEN: Play,
  REG_CLOSE: Flag,
  EVENT_START: Flag,
  EVENT_END: Flag,
};

export function MilestoneTimeline({
  milestones,
  className,
  ...props
}: MilestoneTimelineProps) {
  const { locale } = useTranslation();

  // Sort by date
  const sorted = [...milestones].sort(
    (a, b) => new Date(a.date).getTime() - new Date(b.date).getTime()
  );

  const formatDate = (dateStr: string) => {
    return new Intl.DateTimeFormat(locale === "tr" ? "tr-TR" : "en-US", {
      day: "numeric",
      month: "short",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    }).format(new Date(dateStr));
  };

  return (
    <div className={cn("relative", className)} {...props}>
      {sorted.map((milestone, index) => {
        const isLast = index === sorted.length - 1;
        const Icon =
          milestone.status === "completed"
            ? CheckCircle2
            : milestoneIcons[milestone.type] || Circle;

        return (
          <div key={milestone.id} className="relative flex gap-4 pb-8 last:pb-0">
            {/* Line */}
            {!isLast && (
              <div
                className={cn(
                  "absolute left-3 top-8 w-0.5 h-full -ml-px",
                  milestone.status === "completed"
                    ? "bg-success"
                    : "bg-border"
                )}
              />
            )}

            {/* Icon */}
            <div
              className={cn(
                "relative z-10 flex h-6 w-6 shrink-0 items-center justify-center rounded-full",
                milestone.status === "completed"
                  ? "bg-success text-success-foreground"
                  : milestone.status === "current"
                  ? "bg-accent text-accent-foreground ring-4 ring-accent/20"
                  : "bg-muted text-muted-foreground"
              )}
            >
              <Icon className="h-3.5 w-3.5" />
            </div>

            {/* Content */}
            <div className="flex-1 pt-0.5">
              <h4
                className={cn(
                  "font-medium",
                  milestone.status === "completed"
                    ? "text-muted-foreground"
                    : milestone.status === "current"
                    ? "text-foreground"
                    : "text-foreground"
                )}
              >
                {milestone.label}
              </h4>
              <p className="text-sm text-muted-foreground">
                {formatDate(milestone.date)}
              </p>
              {milestone.description && (
                <p className="mt-1 text-sm text-muted-foreground">
                  {milestone.description}
                </p>
              )}
            </div>
          </div>
        );
      })}
    </div>
  );
}

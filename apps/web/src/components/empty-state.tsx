"use client";

import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import {
  Calendar,
  ClipboardList,
  Building2,
  Search,
  LucideIcon,
} from "lucide-react";

type EmptyStateVariant = "events" | "plan" | "tenant" | "generic";

interface EmptyStateAction {
  label: string;
  onClick: () => void;
}

interface EmptyStateProps extends React.HTMLAttributes<HTMLDivElement> {
  variant?: EmptyStateVariant;
  icon?: LucideIcon;
  title: string;
  description?: string;
  action?: EmptyStateAction;
}

const variantIcons: Record<EmptyStateVariant, LucideIcon> = {
  events: Calendar,
  plan: ClipboardList,
  tenant: Building2,
  generic: Search,
};

export function EmptyState({
  variant = "generic",
  icon,
  title,
  description,
  action,
  className,
  ...props
}: EmptyStateProps) {
  const Icon = icon || variantIcons[variant];

  return (
    <div
      className={cn(
        "flex flex-col items-center justify-center rounded-2xl border bg-card/50 px-6 py-16 text-center",
        className
      )}
      {...props}
    >
      <div className="mb-6 flex h-16 w-16 items-center justify-center rounded-full bg-muted">
        <Icon className="h-8 w-8 text-muted-foreground" />
      </div>

      <h3 className="mb-2 text-lg font-semibold">{title}</h3>

      {description && (
        <p className="mb-6 max-w-sm text-sm text-muted-foreground">
          {description}
        </p>
      )}

      {action && (
        <Button variant="accent" onClick={action.onClick}>
          {action.label}
        </Button>
      )}
    </div>
  );
}

import { ReactNode } from "react";
import { EmptyState } from "@/components/empty-state";
import { cn } from "@/lib/utils";

interface ListEmptyStateProps extends React.HTMLAttributes<HTMLDivElement> {
  title: string;
  description: string;
  icon?: ReactNode;
  variant?: "events" | "plan" | "tenant" | "generic";
  action?: {
    label: string;
    onClick: () => void;
  };
}

export function ListEmptyState({
  title,
  description,
  icon,
  variant = "generic",
  action,
  className,
  ...props
}: ListEmptyStateProps) {
  return (
    <div className={cn("rounded-2xl border bg-card p-6", className)} {...props}>
      <EmptyState
        title={title}
        description={description}
        action={action}
        variant={variant}
        icon={icon}
      />
    </div>
  );
}

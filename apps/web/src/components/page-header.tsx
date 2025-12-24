import { cn } from "@/lib/utils";
import { ReactNode } from "react";

interface PageHeaderProps extends React.HTMLAttributes<HTMLDivElement> {
  title: string;
  subtitle?: string;
  actions?: ReactNode;
  size?: "default" | "large";
}

export function PageHeader({
  title,
  subtitle,
  actions,
  size = "default",
  className,
  ...props
}: PageHeaderProps) {
  return (
    <div
      className={cn(
        "flex flex-col gap-4 md:flex-row md:items-center md:justify-between",
        size === "large" ? "mb-8" : "mb-6",
        className
      )}
      {...props}
    >
      <div>
        <h1
          className={cn(
            "font-display font-bold tracking-tight",
            size === "large"
              ? "text-3xl md:text-4xl"
              : "text-2xl md:text-3xl"
          )}
        >
          {title}
        </h1>
        {subtitle && (
          <p
            className={cn(
              "mt-1 text-muted-foreground",
              size === "large" ? "text-lg" : "text-base"
            )}
          >
            {subtitle}
          </p>
        )}
      </div>
      {actions && <div className="shrink-0">{actions}</div>}
    </div>
  );
}

import { X } from "lucide-react";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";

export interface ActiveFilter {
  key: string;
  label: string;
  onRemove: () => void;
}

interface ActiveFiltersBarProps extends React.HTMLAttributes<HTMLDivElement> {
  filters: ActiveFilter[];
  onClearAll?: () => void;
  clearLabel?: string;
}

export function ActiveFiltersBar({
  filters,
  onClearAll,
  clearLabel,
  className,
  ...props
}: ActiveFiltersBarProps) {
  if (filters.length === 0) return null;

  return (
    <div
      className={cn(
        "flex flex-wrap items-center gap-2 rounded-xl border bg-muted/30 px-3 py-2",
        className
      )}
      {...props}
    >
      {filters.map((filter) => (
        <button
          key={filter.key}
          type="button"
          onClick={filter.onRemove}
          className="inline-flex items-center gap-1 rounded-full border bg-background px-2.5 py-1 text-xs text-foreground transition hover:bg-muted"
        >
          {filter.label}
          <X className="h-3 w-3 text-muted-foreground" />
        </button>
      ))}
      {onClearAll && clearLabel && (
        <Button variant="ghost" size="sm" onClick={onClearAll} className="ml-auto">
          {clearLabel}
        </Button>
      )}
    </div>
  );
}

import { ReactNode } from "react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

interface ListToolbarProps extends React.HTMLAttributes<HTMLDivElement> {
  left?: ReactNode;
  right?: ReactNode;
  showClear?: boolean;
  clearLabel?: string;
  onClear?: () => void;
}

export function ListToolbar({
  left,
  right,
  showClear,
  clearLabel,
  onClear,
  className,
  ...props
}: ListToolbarProps) {
  return (
    <div
      className={cn(
        "flex flex-col gap-3 md:flex-row md:items-center md:justify-between",
        className
      )}
      {...props}
    >
      <div className="flex flex-1 flex-wrap items-center gap-3">{left}</div>
      <div className="flex items-center gap-2">
        {showClear && onClear && (
          <Button variant="ghost" size="sm" onClick={onClear}>
            {clearLabel}
          </Button>
        )}
        {right}
      </div>
    </div>
  );
}

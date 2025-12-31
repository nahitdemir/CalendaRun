import { useTranslation } from "@/contexts/locale-context";
import { cn } from "@/lib/utils";

interface ResultsHeaderProps extends React.HTMLAttributes<HTMLDivElement> {
  count: number;
  sortLabel?: string;
}

export function ResultsHeader({
  count,
  sortLabel,
  className,
  ...props
}: ResultsHeaderProps) {
  const { t } = useTranslation();

  return (
    <div
      className={cn(
        "flex flex-wrap items-center justify-between gap-2 text-sm text-muted-foreground",
        className
      )}
      {...props}
    >
      <span>{t("listing.results", { count })}</span>
      {sortLabel && <span>{t("listing.sortedBy", { sort: sortLabel })}</span>}
    </div>
  );
}

import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { useTranslation } from "@/contexts/locale-context";

interface ListPaginationProps extends React.HTMLAttributes<HTMLDivElement> {
  page: number;
  pageSize: number;
  total?: number;
  onPageChange: (page: number) => void;
}

export function ListPagination({
  page,
  pageSize,
  total,
  onPageChange,
  className,
  ...props
}: ListPaginationProps) {
  const { t } = useTranslation();

  if (!total || total <= pageSize) return null;

  const totalPages = Math.ceil(total / pageSize);
  if (totalPages <= 1) return null;

  const from = (page - 1) * pageSize + 1;
  const to = Math.min(page * pageSize, total);
  const canPrev = page > 1;
  const canNext = page < totalPages;

  return (
    <div
      className={cn("flex items-center justify-between border-t pt-4", className)}
      {...props}
    >
      <div className="text-sm text-muted-foreground">
        {from}–{to} / {total}
      </div>
      <div className="flex gap-2">
        <Button
          variant="outline"
          size="sm"
          onClick={() => onPageChange(page - 1)}
          disabled={!canPrev}
          className="gap-1"
        >
          <ChevronLeft className="h-4 w-4" />
          {t("listing.prev")}
        </Button>
        <Button
          variant="outline"
          size="sm"
          onClick={() => onPageChange(page + 1)}
          disabled={!canNext}
          className="gap-1"
        >
          {t("listing.next")}
          <ChevronRight className="h-4 w-4" />
        </Button>
      </div>
    </div>
  );
}

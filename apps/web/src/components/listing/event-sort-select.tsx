import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { useTranslation } from "@/contexts/locale-context";
import { cn } from "@/lib/utils";

export type EventSortValue = "date_asc" | "date_desc";

interface EventSortSelectProps {
  value: EventSortValue;
  onChange: (value: EventSortValue) => void;
  className?: string;
}

export function EventSortSelect({ value, onChange, className }: EventSortSelectProps) {
  const { t } = useTranslation();

  const handleChange = (nextValue: string) => {
    const nextSort = nextValue === "date_desc" ? "date_desc" : "date_asc";
    onChange(nextSort);
  };

  return (
    <Select value={value} onValueChange={handleChange}>
      <SelectTrigger
        className={cn("h-9 min-w-[160px] max-w-[220px]", className)}
        aria-label={t("filters.sort.label")}
      >
        <SelectValue placeholder={t("filters.sort.label")} />
      </SelectTrigger>
      <SelectContent>
        <SelectItem value="date_asc">{t("filters.sort.dateAsc")}</SelectItem>
        <SelectItem value="date_desc">{t("filters.sort.dateDesc")}</SelectItem>
      </SelectContent>
    </Select>
  );
}

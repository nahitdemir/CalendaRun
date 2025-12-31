"use client";

import { useTranslation } from "@/contexts/locale-context";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { DistanceBadge, Distance } from "@/components/distance-badge";
import { useDistances } from "@/hooks/use-distances";
import { RotateCcw } from "lucide-react";

export interface FilterValues {
  city: string;
  dateFrom: string;
  dateTo: string;
  distances: Distance[];
}

interface FilterBarProps {
  values: FilterValues;
  onChange: (values: FilterValues) => void;
  onClear?: () => void;
  clearLabel?: string;
  showClear?: boolean;
  cities?: string[];
  dateRangeError?: string | null;
}

export function FilterBar({
  values,
  onChange,
  onClear,
  clearLabel,
  showClear,
  cities = [],
  dateRangeError,
}: FilterBarProps) {
  const { t } = useTranslation();
  const { distances: availableDistances, isLoading: distancesLoading } = useDistances();

  const handleCityChange = (city: string) => {
    onChange({ ...values, city: city === "all" ? "" : city });
  };

  const handleDateFromChange = (date: string) => {
    onChange({ ...values, dateFrom: date });
  };

  const handleDateToChange = (date: string) => {
    onChange({ ...values, dateTo: date });
  };

  const handleDistanceToggle = (distance: Distance) => {
    const newDistances = values.distances.includes(distance)
      ? values.distances.filter((d) => d !== distance)
      : [...values.distances, distance];
    onChange({ ...values, distances: newDistances });
  };

  const showClearAction = Boolean(showClear && onClear);

  return (
    <div className="rounded-2xl border bg-card p-4 shadow-sm">
      <div className="flex items-center justify-between gap-2">
        <span className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
          {t("filters.title")}
        </span>
        {showClearAction && (
          <Button
            variant="ghost"
            size="sm"
            onClick={onClear}
            className="h-9 gap-1.5"
          >
            <RotateCcw className="h-4 w-4" />
            {clearLabel || t("filters.clear")}
          </Button>
        )}
      </div>

      <div className="mt-4 flex flex-wrap items-end gap-4">
        {/* City */}
        <div className="w-full min-w-[220px] flex-1 space-y-1.5">
          <label className="text-sm font-medium text-muted-foreground">
            {t("filters.city")}
          </label>
          <Select value={values.city || "all"} onValueChange={handleCityChange}>
            <SelectTrigger>
              <SelectValue placeholder={t("filters.allCities")} />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">{t("filters.allCities")}</SelectItem>
              {cities.map((city) => (
                <SelectItem key={city} value={city}>
                  {city}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        {/* Date range */}
        <div className="w-full min-w-[280px] flex-1 space-y-1.5">
          <label className="text-sm font-medium text-muted-foreground">
            {t("filters.dateRange")}
          </label>
          <div className="grid grid-cols-2 gap-2">
            <Input
              type="date"
              value={values.dateFrom}
              onChange={(e) => handleDateFromChange(e.target.value)}
              className={dateRangeError ? "border-destructive" : ""}
              placeholder={t("filters.from")}
              aria-label={t("filters.from")}
            />
            <Input
              type="date"
              value={values.dateTo}
              onChange={(e) => handleDateToChange(e.target.value)}
              className={dateRangeError ? "border-destructive" : ""}
              placeholder={t("filters.to")}
              aria-label={t("filters.to")}
            />
          </div>
        </div>

        {/* Distances */}
        <div className="w-full min-w-[240px] flex-1 space-y-1.5">
          <label className="text-sm font-medium text-muted-foreground">
            {t("filters.distance")}
          </label>
          <div className="flex flex-wrap gap-2">
            {distancesLoading ? (
              <span className="text-sm text-muted-foreground">
                {t("filters.loading") || "Yükleniyor..."}
              </span>
            ) : availableDistances.length > 0 ? (
              availableDistances.map((distance) => (
                <DistanceBadge
                  key={distance}
                  distance={distance}
                  selected={values.distances.includes(distance)}
                  onClick={() => handleDistanceToggle(distance)}
                  size="sm"
                />
              ))
            ) : (
              // Fallback: Show default distances if API fails
              (["5K", "10K", "21K", "42K", "ultra"] as Distance[]).map((distance) => (
                <DistanceBadge
                  key={distance}
                  distance={distance}
                  selected={values.distances.includes(distance)}
                  onClick={() => handleDistanceToggle(distance)}
                  size="sm"
                />
              ))
            )}
          </div>
        </div>
      </div>

      {/* Date range error */}
      {dateRangeError && (
        <div className="mt-3 rounded-lg border border-destructive/50 bg-destructive/10 p-2 text-sm text-destructive">
          {dateRangeError}
        </div>
      )}
    </div>
  );
}

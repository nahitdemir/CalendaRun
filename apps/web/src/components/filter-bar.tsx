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
import { Filter, X, Check } from "lucide-react";
import { useMediaQuery } from "@/hooks/use-media-query";

export interface FilterValues {
  city: string;
  dateFrom: string;
  dateTo: string;
  distances: Distance[];
}

interface FilterBarProps {
  values: FilterValues;
  onChange: (values: FilterValues) => void;
  onApply?: () => void; // For mobile: explicit Apply button
  onClear?: () => void; // Clear all filters (updates URL)
  cities?: string[];
  dateRangeError?: string | null;
  showActions?: boolean;
}

export function FilterBar({
  values,
  onChange,
  onApply,
  onClear,
  cities = [],
  dateRangeError,
  showActions = true,
}: FilterBarProps) {
  const { t } = useTranslation();
  const isMobile = useMediaQuery("(max-width: 768px)");
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

  const handleClear = () => {
    // If onClear is provided, use it (it will update URL)
    // Otherwise, fallback to local onChange (for backward compatibility)
    if (onClear) {
      onClear();
    } else {
      // Fallback: update local state and apply if onApply exists
      onChange({ city: "", dateFrom: "", dateTo: "", distances: [] });
      if (onApply) {
        setTimeout(() => onApply(), 0);
      }
    }
  };

  const hasFilters =
    values.city || values.dateFrom || values.dateTo || values.distances.length > 0;

  return (
    <div className="rounded-2xl border bg-card p-4 shadow-sm">
      <div className="grid grid-cols-1 gap-4 md:grid-cols-5">
        {/* City */}
        <div className="space-y-1.5">
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

        {/* From date */}
        <div className="space-y-1.5">
          <label className="text-sm font-medium text-muted-foreground">
            {t("filters.from")}
          </label>
          <Input
            type="date"
            value={values.dateFrom}
            onChange={(e) => handleDateFromChange(e.target.value)}
            className={dateRangeError ? "border-destructive" : ""}
          />
        </div>

        {/* To date */}
        <div className="space-y-1.5">
          <label className="text-sm font-medium text-muted-foreground">
            {t("filters.to")}
          </label>
          <Input
            type="date"
            value={values.dateTo}
            onChange={(e) => handleDateToChange(e.target.value)}
            className={dateRangeError ? "border-destructive" : ""}
          />
        </div>

        {/* Distances */}
        <div className="md:col-span-2 space-y-1.5">
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

      {showActions && (
        <div className="mt-4 flex items-center justify-between">
          {hasFilters && (
            <Button variant="ghost" size="sm" onClick={handleClear} className="gap-1.5">
              <X className="h-4 w-4" />
              {t("filters.clear")}
            </Button>
          )}
          {isMobile && onApply && (
            <Button
              variant="accent"
              size="sm"
              onClick={onApply}
              className="ml-auto gap-1.5"
              disabled={!!dateRangeError}
            >
              <Check className="h-4 w-4" />
              {t("filters.apply")}
            </Button>
          )}
        </div>
      )}
    </div>
  );
}

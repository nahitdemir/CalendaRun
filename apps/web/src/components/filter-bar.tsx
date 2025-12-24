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
import { Filter, X } from "lucide-react";

export interface FilterValues {
  city: string;
  dateFrom: string;
  dateTo: string;
  distances: Distance[];
}

interface FilterBarProps {
  values: FilterValues;
  onChange: (values: FilterValues) => void;
  cities?: string[];
}

const ALL_DISTANCES: Distance[] = ["5K", "10K", "21K", "42K", "ultra"];

export function FilterBar({ values, onChange, cities = [] }: FilterBarProps) {
  const { t } = useTranslation();

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
    onChange({ city: "", dateFrom: "", dateTo: "", distances: [] });
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
          />
        </div>

        {/* Distances */}
        <div className="md:col-span-2 space-y-1.5">
          <label className="text-sm font-medium text-muted-foreground">
            {t("filters.distance")}
          </label>
          <div className="flex flex-wrap gap-2">
            {ALL_DISTANCES.map((distance) => (
              <DistanceBadge
                key={distance}
                distance={distance}
                selected={values.distances.includes(distance)}
                onClick={() => handleDistanceToggle(distance)}
                size="sm"
              />
            ))}
          </div>
        </div>
      </div>

      {/* Clear button */}
      {hasFilters && (
        <div className="mt-4 flex justify-end">
          <Button variant="ghost" size="sm" onClick={handleClear} className="gap-1.5">
            <X className="h-4 w-4" />
            {t("filters.clear")}
          </Button>
        </div>
      )}
    </div>
  );
}

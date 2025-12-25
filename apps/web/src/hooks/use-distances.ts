"use client";

import { useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import { useAuth } from "@/contexts/auth-context";
import { settingsApi, DistanceOption } from "@/lib/api-client";
import type { Distance } from "@/components/distance-badge";
import {
  DEFAULT_DISTANCE_OPTIONS,
  DEFAULT_DISTANCES,
  DEFAULT_DISTANCE_TO_KM,
  DEFAULT_KM_TO_DISTANCE,
} from "@/lib/constants/distances";

/**
 * Hook to fetch and use available event distances from Settings Service
 * Returns distances as Distance[] for UI components
 */
export function useDistances() {
  const { selectedTenant } = useAuth();

  const { data: distanceOptions, isLoading, error } = useQuery<DistanceOption[]>({
    queryKey: ["distances", selectedTenant?.id],
    queryFn: async () => {
      try {
        const result = await settingsApi.getDistances(selectedTenant?.id);
        return result;
      } catch (err) {
        return [...DEFAULT_DISTANCE_OPTIONS];
      }
    },
    staleTime: 30 * 60 * 1000, // 30 minutes - distances don't change often
    retry: 2,
  });

  // Convert DistanceOption[] to Distance[] for UI components (memoized)
  const distances: Distance[] = useMemo(() => {
    if (!distanceOptions || distanceOptions.length === 0) {
      // Return fallback distances if API didn't return any
      return [...DEFAULT_DISTANCES];
    }
    const result = distanceOptions
      .sort((a, b) => a.displayOrder - b.displayOrder)
      .map((d) => d.label as Distance)
      .filter((d): d is Distance => DEFAULT_DISTANCES.includes(d as Distance));
    
    // If filtering removed all items, return fallback
    if (result.length === 0) {
      return [...DEFAULT_DISTANCES];
    }
    
    return result;
  }, [distanceOptions]);

  // Map KM to Distance for parsing (memoized)
  const kmToDistance: Record<number, Distance> = useMemo(() => {
    if (!distanceOptions) {
      return DEFAULT_KM_TO_DISTANCE;
    }
    return distanceOptions.reduce((acc, d) => {
      acc[d.km] = d.label as Distance;
      return acc;
    }, {} as Record<number, Distance>);
  }, [distanceOptions]);

  // Map Distance to KM for API calls (memoized)
  const distanceToKm: Record<Distance, number> = useMemo(() => {
    if (!distanceOptions) {
      return DEFAULT_DISTANCE_TO_KM;
    }
    return distanceOptions.reduce((acc, d) => {
      acc[d.label as Distance] = d.km;
      return acc;
    }, {} as Record<Distance, number>);
  }, [distanceOptions]);

  // Memoize distanceOptions to prevent unnecessary re-renders
  const memoizedDistanceOptions = useMemo(() => distanceOptions || [], [distanceOptions]);

  return {
    distances,
    distanceOptions: memoizedDistanceOptions,
    kmToDistance,
    distanceToKm,
    isLoading,
    error,
  };
}

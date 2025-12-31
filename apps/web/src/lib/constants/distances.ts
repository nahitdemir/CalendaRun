import type { Distance } from "@/components/distance-badge";

export const DEFAULT_DISTANCES = ["5K", "10K", "21K", "42K", "ultra"] as const;

export const DEFAULT_DISTANCE_OPTIONS = [
  { km: 5, label: "5K", displayOrder: 1 },
  { km: 10, label: "10K", displayOrder: 2 },
  { km: 21, label: "21K", displayOrder: 3 },
  { km: 42, label: "42K", displayOrder: 4 },
  { km: 100, label: "ultra", displayOrder: 5 },
] as const;

export const DEFAULT_KM_TO_DISTANCE: Record<number, Distance> = {
  5: "5K",
  10: "10K",
  21: "21K",
  42: "42K",
  100: "ultra",
};

export const DEFAULT_DISTANCE_TO_KM: Record<Distance, number> = {
  "5K": 5,
  "10K": 10,
  "21K": 21,
  "42K": 42,
  ultra: 100,
};

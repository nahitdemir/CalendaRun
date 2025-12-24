"use client";

import { cn } from "@/lib/utils";

export type Distance = "5K" | "10K" | "21K" | "42K" | "ultra";

interface DistanceBadgeProps extends React.HTMLAttributes<HTMLButtonElement> {
  distance: Distance;
  selected?: boolean;
  size?: "sm" | "md";
}

const distanceColors: Record<Distance, { bg: string; text: string; selectedBg: string }> = {
  "5K": {
    bg: "bg-emerald-500/10",
    text: "text-emerald-600 dark:text-emerald-400",
    selectedBg: "bg-emerald-500 text-white",
  },
  "10K": {
    bg: "bg-blue-500/10",
    text: "text-blue-600 dark:text-blue-400",
    selectedBg: "bg-blue-500 text-white",
  },
  "21K": {
    bg: "bg-purple-500/10",
    text: "text-purple-600 dark:text-purple-400",
    selectedBg: "bg-purple-500 text-white",
  },
  "42K": {
    bg: "bg-orange-500/10",
    text: "text-orange-600 dark:text-orange-400",
    selectedBg: "bg-orange-500 text-white",
  },
  ultra: {
    bg: "bg-red-500/10",
    text: "text-red-600 dark:text-red-400",
    selectedBg: "bg-red-500 text-white",
  },
};

const distanceLabels: Record<Distance, string> = {
  "5K": "5K",
  "10K": "10K",
  "21K": "21K",
  "42K": "42K",
  ultra: "Ultra",
};

export function DistanceBadge({
  distance,
  selected = false,
  size = "md",
  className,
  onClick,
  ...props
}: DistanceBadgeProps) {
  const colors = distanceColors[distance];

  return (
    <button
      type="button"
      onClick={onClick}
      className={cn(
        "inline-flex items-center justify-center rounded-full font-semibold transition-all",
        size === "sm" ? "px-2.5 py-0.5 text-xs" : "px-3 py-1 text-sm",
        selected
          ? colors.selectedBg
          : cn(colors.bg, colors.text, "hover:opacity-80"),
        onClick && "cursor-pointer",
        !onClick && "cursor-default",
        className
      )}
      disabled={!onClick}
      {...props}
    >
      {distanceLabels[distance]}
    </button>
  );
}

"use client";

import Image from "next/image";
import Link from "next/link";
import { BRAND } from "@/constants/brand";
import { cn } from "@/lib/utils";

type BrandLogoVariant = "icon" | "wordmark";
type BrandLogoSize = "sm" | "md" | "lg";

interface BrandLogoProps {
  variant?: BrandLogoVariant;
  size?: BrandLogoSize;
  className?: string;
  priority?: boolean;
}

const ICON_RENDER_PX: Record<BrandLogoSize, number> = {
  sm: 24,
  md: 28,
  lg: 32,
};

const ICON_SOURCES: Record<BrandLogoSize, string> = {
  sm: BRAND.assets.icon32,
  md: BRAND.assets.icon64,
  lg: BRAND.assets.icon128,
};

const TEXT_SIZES: Record<BrandLogoSize, string> = {
  sm: "text-sm",
  md: "text-base",
  lg: "text-lg",
};

const GAP_SIZES: Record<BrandLogoSize, string> = {
  sm: "gap-2",
  md: "gap-2.5",
  lg: "gap-3",
};

export function BrandLogo({
  variant = "wordmark",
  size = "md",
  className,
  priority = false,
}: BrandLogoProps) {
  const iconSize = ICON_RENDER_PX[size];

  return (
    <Link
      href="/"
      aria-label={`${BRAND.name} Home`}
      className={cn(
        "inline-flex items-center rounded-md transition-opacity hover:opacity-90 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2",
        GAP_SIZES[size],
        className
      )}
    >
      <Image
        src={ICON_SOURCES[size]}
        alt={`${BRAND.name} icon`}
        width={iconSize}
        height={iconSize}
        sizes={`${iconSize}px`}
        priority={priority}
      />
      {variant === "wordmark" && (
        <span
          className={cn(
            "font-brand font-semibold leading-none tracking-tight text-foreground",
            TEXT_SIZES[size]
          )}
        >
          <span>Calenda</span>
          <span className="font-bold text-accent">RUN</span>
        </span>
      )}
    </Link>
  );
}

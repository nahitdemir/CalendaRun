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

const WORDMARK_RATIO = 756 / 182;

const ICON_RENDER_PX: Record<BrandLogoSize, number> = {
  sm: 24,
  md: 28,
  lg: 32,
};

const WORDMARK_HEIGHT_PX: Record<BrandLogoSize, number> = {
  sm: 24,
  md: 28,
  lg: 32,
};

const ICON_SOURCES: Record<BrandLogoSize, string> = {
  sm: BRAND.assets.icon32,
  md: BRAND.assets.icon64,
  lg: BRAND.assets.icon128,
};

export function BrandLogo({
  variant = "wordmark",
  size = "md",
  className,
  priority = false,
}: BrandLogoProps) {
  const iconSize = ICON_RENDER_PX[size];
  const wordmarkHeight = WORDMARK_HEIGHT_PX[size];
  const wordmarkWidth = Math.round(wordmarkHeight * WORDMARK_RATIO);
  const source =
    variant === "wordmark" ? BRAND.assets.wordmark : ICON_SOURCES[size];
  const altText =
    variant === "wordmark"
      ? `${BRAND.name} wordmark`
      : `${BRAND.name} icon`;

  return (
    <Link
      href="/"
      aria-label={`${BRAND.name} Home`}
      className={cn(
        "inline-flex items-center gap-2 rounded-md transition-opacity hover:opacity-90 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/30",
        className
      )}
    >
      <Image
        src={source}
        alt={altText}
        width={variant === "wordmark" ? wordmarkWidth : iconSize}
        height={variant === "wordmark" ? wordmarkHeight : iconSize}
        sizes={`${variant === "wordmark" ? wordmarkWidth : iconSize}px`}
        className="block"
        priority={priority}
      />
    </Link>
  );
}

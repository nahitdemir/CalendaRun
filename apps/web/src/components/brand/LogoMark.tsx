"use client";

import { cn } from "@/lib/utils";

interface LogoMarkProps {
  size?: number;
  tone?: "primary" | "foreground";
  className?: string;
}

export function LogoMark({
  size = 36,
  tone = "primary",
  className,
}: LogoMarkProps) {
  const toneClass = tone === "primary" ? "text-primary" : "text-foreground";

  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      viewBox="0 0 64 64"
      width={size}
      height={size}
      className={cn(toneClass, className)}
      fill="none"
      role="img"
      aria-hidden="true"
    >
      <path
        d="M46 18 A20 20 0 1 0 46 46"
        stroke="currentColor"
        strokeWidth="6"
        strokeLinecap="round"
      />
      <path
        d="M43 23 A14 14 0 1 0 43 41"
        stroke="currentColor"
        strokeWidth="3"
        strokeLinecap="round"
        opacity="0.75"
      />
      <circle cx="46" cy="18" r="2.6" fill="currentColor" />
      <circle cx="46" cy="46" r="2.6" fill="currentColor" opacity="0.7" />
    </svg>
  );
}

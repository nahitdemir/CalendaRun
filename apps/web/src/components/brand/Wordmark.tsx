"use client";

import { cn } from "@/lib/utils";

interface WordmarkProps {
  className?: string;
}

export function Wordmark({ className }: WordmarkProps) {
  return (
    <span
      className={cn(
        "inline-flex items-baseline gap-1 text-lg font-semibold text-foreground",
        className
      )}
    >
      <span className="font-sans tracking-tight">Calenda</span>
      <span className="font-display text-base uppercase tracking-widest text-primary">
        RUN
      </span>
    </span>
  );
}

"use client";

import { ReactNode } from "react";
import { BrandLogo } from "@/components/Brand/BrandLogo";
import { Card, CardContent } from "@/components/ui/card";
import { cn } from "@/lib/utils";

interface AuthShellProps {
  title: string;
  subtitle: string;
  children: ReactNode;
  footer?: ReactNode;
  sideTitle?: string;
  sideDescription?: string;
  sideItems?: string[];
  className?: string;
}

export function AuthShell({
  title,
  subtitle,
  children,
  footer,
  sideTitle,
  sideDescription,
  sideItems = [],
  className,
}: AuthShellProps) {
  return (
    <div className={cn("relative min-h-[calc(100vh-4rem)] overflow-hidden", className)}>
      <div className="absolute inset-0 bg-[radial-gradient(circle_at_top,_hsl(var(--accent))_0%,_transparent_45%)] opacity-10" />
      <div className="absolute inset-0 bg-[linear-gradient(120deg,_transparent_0%,_hsl(var(--muted))_60%,_transparent_100%)] opacity-60" />
      <div className="container-app relative">
        <div className="grid gap-10 lg:grid-cols-[1.1fr_0.9fr] lg:items-center">
          <div className="space-y-6">
            <BrandLogo variant="wordmark" size="lg" />
            <div className="space-y-3">
              <p className="text-xs uppercase tracking-[0.3em] text-muted-foreground">CalendaRUN</p>
              <h1 className="heading-display text-3xl font-display text-foreground md:text-4xl">
                {sideTitle || "Race day prep, refined."}
              </h1>
              <p className="max-w-lg text-base text-muted-foreground">
                {sideDescription ||
                  "Plan, register, and manage every run in one place. Secure by design, polished for athletes."}
              </p>
            </div>
            {sideItems.length > 0 && (
              <div className="grid gap-3 sm:grid-cols-2">
                {sideItems.map((item) => (
                  <div
                    key={item}
                    className="rounded-2xl border bg-background/80 px-4 py-3 text-sm text-foreground shadow-sm"
                  >
                    {item}
                  </div>
                ))}
              </div>
            )}
          </div>
          <Card className="card-sports shadow-lg">
            <CardContent className="space-y-6 p-6 sm:p-8">
              <div className="space-y-2">
                <h2 className="text-2xl font-semibold text-foreground">{title}</h2>
                <p className="text-sm text-muted-foreground">{subtitle}</p>
              </div>
              {children}
              {footer && <div className="pt-2 text-sm text-muted-foreground">{footer}</div>}
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}

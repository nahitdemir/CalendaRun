"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useAuth } from "@/contexts/auth-context";
import { useTranslation } from "@/contexts/locale-context";
import { Compass, ClipboardList } from "lucide-react";
import { cn } from "@/lib/utils";

export function MobileNav() {
  const pathname = usePathname();
  const { isAuthenticated } = useAuth();
  const { t } = useTranslation();

  // Don't show on admin pages or when not authenticated
  if (
    !isAuthenticated ||
    pathname.startsWith("/admin") ||
    pathname.startsWith("/super-admin")
  ) {
    return null;
  }

  const links = [
    {
      href: "/",
      label: t("nav.explore"),
      icon: Compass,
      active: pathname === "/" || pathname === "/explore",
    },
    {
      href: "/plan",
      label: t("nav.plan"),
      icon: ClipboardList,
      active: pathname === "/plan",
    },
  ];

  return (
    <nav className="fixed bottom-0 left-0 right-0 z-50 border-t bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60 md:hidden">
      <div className="flex h-16 items-center justify-around">
        {links.map((link) => (
          <Link
            key={link.href}
            href={link.href}
            className={cn(
              "flex flex-1 flex-col items-center justify-center gap-1 py-2 transition-colors",
              link.active
                ? "text-accent"
                : "text-muted-foreground hover:text-foreground"
            )}
          >
            <link.icon className="h-5 w-5" />
            <span className="text-xs font-medium">{link.label}</span>
          </Link>
        ))}
      </div>
    </nav>
  );
}

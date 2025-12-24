"use client";

import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Globe, Check } from "lucide-react";
import { useTranslation } from "@/contexts/locale-context";
import { cn } from "@/lib/utils";

const locales = [
  { code: "tr" as const, label: "Türkçe", flag: "🇹🇷" },
  { code: "en" as const, label: "English", flag: "🇬🇧" },
];

export function LocaleSwitcher() {
  const { locale, setLocale } = useTranslation();

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="sm" className="gap-1.5">
          <Globe className="h-4 w-4" />
          <span className="hidden text-xs font-medium uppercase sm:inline">
            {locale}
          </span>
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        {locales.map((loc) => (
          <DropdownMenuItem
            key={loc.code}
            onClick={() => setLocale(loc.code)}
            className={cn(
              "flex items-center justify-between gap-4",
              locale === loc.code && "bg-muted"
            )}
          >
            <span className="flex items-center gap-2">
              <span>{loc.flag}</span>
              <span>{loc.label}</span>
            </span>
            {locale === loc.code && <Check className="h-4 w-4" />}
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

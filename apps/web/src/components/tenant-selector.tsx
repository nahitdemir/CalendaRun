"use client";

import { Building2, ChevronDown, Check } from "lucide-react";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { useTranslation } from "@/contexts/locale-context";

export interface TenantOption {
  id: string;
  name: string;
  role: string;
}

interface TenantSelectorProps {
  tenants: TenantOption[];
  selectedTenant: TenantOption | null;
  onSelect: (tenant: TenantOption) => void;
  className?: string;
}

export function TenantSelector({
  tenants,
  selectedTenant,
  onSelect,
  className,
}: TenantSelectorProps) {
  const { t } = useTranslation();

  if (tenants.length === 0) {
    return null;
  }

  // Don't show selector if only one tenant
  if (tenants.length === 1 && !selectedTenant) {
    return null;
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="outline"
          className={cn("gap-2 min-w-[180px] justify-between", className)}
        >
          <div className="flex items-center gap-2 truncate">
            <Building2 className="h-4 w-4 shrink-0 text-muted-foreground" />
            <span className="truncate">
              {selectedTenant?.name || t("nav.selectTenant")}
            </span>
          </div>
          <ChevronDown className="h-4 w-4 shrink-0 text-muted-foreground" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-[220px]">
        {tenants.map((tenant) => (
          <DropdownMenuItem
            key={tenant.id}
            onClick={() => onSelect(tenant)}
            className="flex items-center justify-between gap-2"
          >
            <div className="flex flex-col">
              <span className="font-medium">{tenant.name}</span>
              <span className="text-xs text-muted-foreground capitalize">
                {tenant.role.replace("Tenant", "").toLowerCase()}
              </span>
            </div>
            {selectedTenant?.id === tenant.id && (
              <Check className="h-4 w-4 text-accent" />
            )}
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

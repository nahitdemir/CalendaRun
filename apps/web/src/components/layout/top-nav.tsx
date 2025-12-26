"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useAuth } from "@/contexts/auth-context";
import { useTranslation } from "@/contexts/locale-context";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { TenantSelector } from "@/components/tenant-selector";
import { LocaleSwitcher } from "./locale-switcher";
import { ThemeToggle } from "./theme-toggle";
import {
  Flag,
  Compass,
  ClipboardList,
  Settings,
  Shield,
  ChevronDown,
  LogOut,
  User,
  ArrowLeft,
  Building2,
  Users,
  Calendar,
  FileText,
  Key,
} from "lucide-react";
import { cn } from "@/lib/utils";

export function TopNav() {
  const pathname = usePathname();
  const {
    user,
    isAuthenticated,
    isLoading,
    isSuperAdmin,
    isTenantAdmin,
    tenants,
    selectedTenant,
    setSelectedTenant,
    login,
    logout,
  } = useAuth();
  const { t } = useTranslation();

  const isAdminSection =
    pathname.startsWith("/admin") || pathname.startsWith("/super-admin");

  // Convert tenants to TenantOption format
  const tenantOptions = tenants.map((t) => ({
    id: t.tenantId,
    name: t.tenantName,
    role: t.role as "tenant_admin" | "tenant_user",
  }));

  const selectedTenantOption = selectedTenant
    ? {
        id: selectedTenant.tenantId,
        name: selectedTenant.tenantName,
        role: selectedTenant.role as "tenant_admin" | "tenant_user",
      }
    : null;

  const handleTenantSelect = (option: { id: string; name: string; role: string }) => {
    const tenant = tenants.find((t) => t.tenantId === option.id);
    if (tenant) setSelectedTenant(tenant);
  };

  const navLinks = [
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
    <header className="sticky top-0 z-50 w-full border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
      <div className="container-app !py-0">
        <div className="flex h-16 items-center gap-6">
          {/* Logo */}
          <Link
            href="/"
            className="flex items-center gap-2 font-display text-xl font-bold tracking-tight"
          >
            <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-accent">
              <Flag className="h-4 w-4 text-accent-foreground" />
            </div>
            <span className="hidden sm:inline">CalendaRUN</span>
          </Link>

          {/* Back to Explore - shown on admin pages */}
          {isAdminSection && (
            <Link
              href="/"
              className="hidden items-center gap-1.5 text-sm text-muted-foreground transition-colors hover:text-foreground md:flex"
            >
              <ArrowLeft className="h-4 w-4" />
              {t("nav.backToExplore")}
            </Link>
          )}

          {/* Main nav - hidden on admin pages */}
          {!isAdminSection && isAuthenticated && (
            <nav className="hidden items-center gap-1 md:flex">
              {navLinks.map((link) => (
                <Link
                  key={link.href}
                  href={link.href}
                  className={cn(
                    "flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-medium transition-colors",
                    link.active
                      ? "bg-muted text-foreground"
                      : "text-muted-foreground hover:bg-muted/50 hover:text-foreground"
                  )}
                >
                  <link.icon className="h-4 w-4" />
                  {link.label}
                </Link>
              ))}
            </nav>
          )}

          {/* Admin navigation */}
          {isAuthenticated && (
            <div className="hidden items-center gap-2 md:flex">
              {/* Tenant Admin dropdown */}
              {isTenantAdmin && selectedTenant && (
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button
                      variant={pathname.startsWith("/admin") ? "secondary" : "ghost"}
                      size="sm"
                      className="gap-1.5"
                    >
                      <Settings className="h-4 w-4" />
                      {t("nav.admin")}
                      <ChevronDown className="h-3 w-3" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="start" className="w-48">
                    <DropdownMenuLabel>{selectedTenant.tenantName}</DropdownMenuLabel>
                    <DropdownMenuSeparator />
                    <DropdownMenuItem asChild>
                      <Link href="/admin/events" className="flex items-center gap-2">
                        <Calendar className="h-4 w-4" />
                        {t("admin.events.title")}
                      </Link>
                    </DropdownMenuItem>
                    <DropdownMenuItem asChild>
                      <Link href="/admin/users" className="flex items-center gap-2">
                        <Users className="h-4 w-4" />
                        {t("admin.users.title")}
                      </Link>
                    </DropdownMenuItem>
                    <DropdownMenuItem asChild>
                      <Link href="/admin/audit" className="flex items-center gap-2">
                        <FileText className="h-4 w-4" />
                        {t("admin.audit.title")}
                      </Link>
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              )}

              {/* Super Admin dropdown */}
              {isSuperAdmin && (
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button
                      variant={pathname.startsWith("/super-admin") ? "secondary" : "ghost"}
                      size="sm"
                      className="gap-1.5"
                    >
                      <Shield className="h-4 w-4 text-amber-500" />
                      {t("nav.superAdmin")}
                      <ChevronDown className="h-3 w-3" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="start" className="w-48">
                    <DropdownMenuLabel>Super Admin</DropdownMenuLabel>
                    <DropdownMenuSeparator />
                    <DropdownMenuItem asChild>
                      <Link href="/super-admin/tenants" className="flex items-center gap-2">
                        <Building2 className="h-4 w-4" />
                        {t("super.tenants.title")}
                      </Link>
                    </DropdownMenuItem>
                    <DropdownMenuItem asChild>
                      <Link href="/super-admin/events" className="flex items-center gap-2">
                        <Calendar className="h-4 w-4" />
                        {t("super.events.title")}
                      </Link>
                    </DropdownMenuItem>
                    <DropdownMenuItem asChild>
                      <Link href="/super-admin/audit" className="flex items-center gap-2">
                        <FileText className="h-4 w-4" />
                        {t("super.audit.title")}
                      </Link>
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              )}
            </div>
          )}

          {/* Spacer */}
          <div className="flex-1" />

          {/* Right side */}
          <div className="flex items-center gap-3">
            {/* Tenant selector - only show if multiple tenants */}
            {isAuthenticated && tenantOptions.length > 1 && (
              <TenantSelector
                tenants={tenantOptions}
                selectedTenant={selectedTenantOption}
                onSelect={handleTenantSelect}
                className="hidden md:flex"
              />
            )}

            {/* Locale switcher */}
            <LocaleSwitcher />

            <ThemeToggle />

            {/* User menu */}
            {isAuthenticated ? (
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="ghost" size="sm" className="gap-2">
                    <div className="flex h-8 w-8 items-center justify-center rounded-full bg-primary/10">
                      <span className="text-sm font-semibold text-primary">
                        {user?.name?.charAt(0).toUpperCase() ||
                          user?.email?.charAt(0).toUpperCase() ||
                          "U"}
                      </span>
                    </div>
                    <span className="hidden text-sm font-medium lg:inline">
                      {user?.name || user?.email?.split("@")[0] || "User"}
                    </span>
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end" className="w-56">
                  <DropdownMenuLabel>
                    <div className="flex flex-col gap-1">
                      <span className="font-medium">{user?.name || "User"}</span>
                      <span className="text-xs font-normal text-muted-foreground">
                        {user?.email}
                      </span>
                      {isSuperAdmin && (
                        <span className="mt-1 inline-flex w-fit items-center rounded-full bg-amber-100 px-2 py-0.5 text-xs font-medium text-amber-800 dark:bg-amber-900/50 dark:text-amber-400">
                          Super Admin
                        </span>
                      )}
                    </div>
                  </DropdownMenuLabel>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem asChild>
                    <Link href="/settings" className="flex items-center gap-2">
                      <Settings className="h-4 w-4" />
                      {t("nav.settings")}
                    </Link>
                  </DropdownMenuItem>
                  <DropdownMenuItem asChild>
                    <Link href="/settings/token" className="flex items-center gap-2">
                      <Key className="h-4 w-4" />
                      Dev Token
                    </Link>
                  </DropdownMenuItem>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem
                    onClick={logout}
                    className="text-destructive focus:text-destructive"
                  >
                    <LogOut className="mr-2 h-4 w-4" />
                    {t("nav.signOut")}
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            ) : isLoading ? (
              <div className="h-8 w-24 animate-pulse rounded-lg bg-muted" />
            ) : (
              <Button variant="accent" onClick={login}>
                {t("nav.signIn")}
              </Button>
            )}
          </div>
        </div>
      </div>
    </header>
  );
}

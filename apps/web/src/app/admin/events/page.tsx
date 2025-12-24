"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useAuth } from "@/contexts/auth-context";
import { useTranslation } from "@/contexts/locale-context";
import { useToast } from "@/components/ui/toast";
import { useApiMutation } from "@/hooks/use-api-error";
import { adminEventsApi, Event } from "@/lib/api-client";
import { TenantAdminGuard } from "@/components/route-guards";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { PageHeader } from "@/components/page-header";
import { EmptyState } from "@/components/empty-state";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import {
  Calendar,
  Plus,
  MapPin,
  ExternalLink,
  Loader2,
  Pencil,
  Trash2,
  Globe,
} from "lucide-react";

function AdminEventsContent() {
  const { selectedTenant, isSuperAdmin } = useAuth();
  const { t, locale } = useTranslation();
  const toast = useToast();
  const { onError } = useApiMutation();
  const queryClient = useQueryClient();

  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [editingEvent, setEditingEvent] = useState<Event | null>(null);
  const [formData, setFormData] = useState({
    title: "",
    description: "",
    startAt: "",
    city: "",
    countryCode: "TR",
    registrationUrl: "",
    isGlobal: false,
  });

  // Format date for display
  const formatDate = (dateStr: string) => {
    return new Intl.DateTimeFormat(locale === "tr" ? "tr-TR" : "en-US", {
      dateStyle: "medium",
      timeStyle: "short",
    }).format(new Date(dateStr));
  };

  // Format date for input
  const formatDateForInput = (dateStr: string) => {
    try {
      const date = new Date(dateStr);
      return date.toISOString().slice(0, 16);
    } catch {
      return "";
    }
  };

  // Fetch events
  const { data: events, isLoading } = useQuery({
    queryKey: ["admin-events", selectedTenant?.tenantId],
    queryFn: () => adminEventsApi.list(),
    enabled: !!selectedTenant?.tenantId,
  });

  // Create mutation
  const createMutation = useMutation({
    mutationFn: (data: typeof formData) =>
      adminEventsApi.create({
        ...data,
        startAt: new Date(data.startAt).toISOString(),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["admin-events"] });
      setIsCreateOpen(false);
      resetForm();
      toast.success(t("toast.created"));
    },
    onError: (err) => onError(err),
  });

  // Update mutation
  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: Partial<Event> }) =>
      adminEventsApi.update(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["admin-events"] });
      setEditingEvent(null);
      resetForm();
      toast.success(t("toast.updated"));
    },
    onError: (err) => onError(err),
  });

  // Delete mutation
  const deleteMutation = useMutation({
    mutationFn: (id: string) => adminEventsApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["admin-events"] });
      toast.success(t("toast.deleted"));
    },
    onError: (err) => onError(err),
  });

  const resetForm = () => {
    setFormData({
      title: "",
      description: "",
      startAt: "",
      city: "",
      countryCode: "TR",
      registrationUrl: "",
      isGlobal: false,
    });
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (editingEvent) {
      updateMutation.mutate({
        id: editingEvent.id,
        data: {
          title: formData.title,
          description: formData.description || undefined,
          startAt: new Date(formData.startAt).toISOString(),
          city: formData.city,
          countryCode: formData.countryCode,
          registrationUrl: formData.registrationUrl || undefined,
        },
      });
    } else {
      createMutation.mutate(formData);
    }
  };

  const openEditDialog = (event: Event) => {
    setEditingEvent(event);
    setFormData({
      title: event.title,
      description: event.description || "",
      startAt: formatDateForInput(event.startAt),
      city: event.city,
      countryCode: event.countryCode,
      registrationUrl: event.registrationUrl || "",
      isGlobal: event.isGlobal || false,
    });
  };

  const handleDelete = (id: string) => {
    const confirmed = window.confirm(
      locale === "tr"
        ? "Bu etkinliği silmek istediğinizden emin misiniz?"
        : "Are you sure you want to delete this event?"
    );
    if (confirmed) {
      deleteMutation.mutate(id);
    }
  };

  if (isLoading) {
    return (
      <div className="container-app space-y-6">
        <div className="flex items-center justify-between">
          <Skeleton className="h-10 w-48" />
          <Skeleton className="h-10 w-32" />
        </div>
        <div className="space-y-4">
          {[1, 2, 3].map((i) => (
            <div key={i} className="rounded-2xl border bg-card p-5">
              <Skeleton className="mb-3 h-6 w-2/3" />
              <Skeleton className="h-4 w-1/2" />
            </div>
          ))}
        </div>
      </div>
    );
  }

  return (
    <div className="container-app space-y-6">
      <PageHeader
        title={t("admin.events.title")}
        subtitle={selectedTenant?.tenantName}
        actions={
          <Dialog
            open={isCreateOpen || !!editingEvent}
            onOpenChange={(open) => {
              if (!open) {
                setIsCreateOpen(false);
                setEditingEvent(null);
                resetForm();
              }
            }}
          >
            <DialogTrigger asChild>
              <Button
                variant="accent"
                className="gap-2"
                onClick={() => setIsCreateOpen(true)}
              >
                <Plus className="h-4 w-4" />
                {t("admin.events.create")}
              </Button>
            </DialogTrigger>
            <DialogContent className="max-w-md">
              <form onSubmit={handleSubmit}>
                <DialogHeader>
                  <DialogTitle>
                    {editingEvent ? t("admin.events.edit") : t("admin.events.create")}
                  </DialogTitle>
                  <DialogDescription>
                    {editingEvent
                      ? locale === "tr"
                        ? "Etkinlik detaylarını güncelleyin"
                        : "Update event details"
                      : locale === "tr"
                      ? "Yeni bir etkinlik ekleyin"
                      : "Add a new event"}
                  </DialogDescription>
                </DialogHeader>
                <div className="grid gap-4 py-4 max-h-[60vh] overflow-y-auto">
                  <div className="grid gap-2">
                    <Label htmlFor="title">
                      {locale === "tr" ? "Başlık" : "Title"} *
                    </Label>
                    <Input
                      id="title"
                      value={formData.title}
                      onChange={(e) =>
                        setFormData({ ...formData, title: e.target.value })
                      }
                      placeholder={
                        locale === "tr"
                          ? "İstanbul Maratonu 2025"
                          : "Istanbul Marathon 2025"
                      }
                      required
                    />
                  </div>
                  <div className="grid gap-2">
                    <Label htmlFor="startAt">{t("event.date")} *</Label>
                    <Input
                      id="startAt"
                      type="datetime-local"
                      value={formData.startAt}
                      onChange={(e) =>
                        setFormData({ ...formData, startAt: e.target.value })
                      }
                      required
                    />
                  </div>
                  <div className="grid grid-cols-2 gap-4">
                    <div className="grid gap-2">
                      <Label htmlFor="city">{t("filters.city")} *</Label>
                      <Input
                        id="city"
                        value={formData.city}
                        onChange={(e) =>
                          setFormData({ ...formData, city: e.target.value })
                        }
                        placeholder={locale === "tr" ? "İstanbul" : "Istanbul"}
                        required
                      />
                    </div>
                    <div className="grid gap-2">
                      <Label htmlFor="countryCode">
                        {locale === "tr" ? "Ülke Kodu" : "Country"} *
                      </Label>
                      <Input
                        id="countryCode"
                        value={formData.countryCode}
                        onChange={(e) =>
                          setFormData({ ...formData, countryCode: e.target.value })
                        }
                        placeholder="TR"
                        maxLength={2}
                        required
                      />
                    </div>
                  </div>
                  <div className="grid gap-2">
                    <Label htmlFor="description">
                      {locale === "tr" ? "Açıklama" : "Description"}
                    </Label>
                    <Input
                      id="description"
                      value={formData.description}
                      onChange={(e) =>
                        setFormData({ ...formData, description: e.target.value })
                      }
                      placeholder={
                        locale === "tr"
                          ? "44. İstanbul Maratonu..."
                          : "44th Istanbul Marathon..."
                      }
                    />
                  </div>
                  <div className="grid gap-2">
                    <Label htmlFor="registrationUrl">
                      {t("event.registration")} URL
                    </Label>
                    <Input
                      id="registrationUrl"
                      type="url"
                      value={formData.registrationUrl}
                      onChange={(e) =>
                        setFormData({ ...formData, registrationUrl: e.target.value })
                      }
                      placeholder="https://example.com/register"
                    />
                  </div>
                  {isSuperAdmin && !editingEvent && (
                    <div className="flex items-center gap-2">
                      <input
                        type="checkbox"
                        id="isGlobal"
                        checked={formData.isGlobal}
                        onChange={(e) =>
                          setFormData({ ...formData, isGlobal: e.target.checked })
                        }
                        className="h-4 w-4 rounded border-input"
                      />
                      <Label htmlFor="isGlobal" className="text-sm font-normal">
                        {locale === "tr"
                          ? "Global etkinlik (tüm firmalar görebilir)"
                          : "Global event (visible to all tenants)"}
                      </Label>
                    </div>
                  )}
                </div>
                <DialogFooter>
                  <Button
                    type="button"
                    variant="outline"
                    onClick={() => {
                      setIsCreateOpen(false);
                      setEditingEvent(null);
                      resetForm();
                    }}
                  >
                    {t("common.cancel")}
                  </Button>
                  <Button
                    type="submit"
                    variant="accent"
                    disabled={createMutation.isPending || updateMutation.isPending}
                  >
                    {(createMutation.isPending || updateMutation.isPending) && (
                      <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    )}
                    {editingEvent ? t("common.save") : t("admin.events.create")}
                  </Button>
                </DialogFooter>
              </form>
            </DialogContent>
          </Dialog>
        }
      />

      {events && events.length === 0 ? (
        <EmptyState
          variant="events"
          title={t("empty.noEventsTitle")}
          description={
            locale === "tr"
              ? "İlk etkinliğinizi oluşturarak başlayın"
              : "Create your first event to get started"
          }
          action={{
            label: t("admin.events.create"),
            onClick: () => setIsCreateOpen(true),
          }}
        />
      ) : (
        <div className="space-y-4">
          {events?.map((event) => (
            <article
              key={event.id}
              className="group rounded-2xl border bg-card p-4 shadow-sm transition-all hover:shadow-md md:p-5"
            >
              <div className="flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
                <div className="flex items-start gap-4">
                  <div
                    className={`flex h-12 w-12 shrink-0 items-center justify-center rounded-xl ${
                      event.isGlobal
                        ? "bg-amber-100 dark:bg-amber-900/30"
                        : "bg-primary/10"
                    }`}
                  >
                    {event.isGlobal ? (
                      <Globe className="h-6 w-6 text-amber-600 dark:text-amber-400" />
                    ) : (
                      <Calendar className="h-6 w-6 text-primary" />
                    )}
                  </div>
                  <div className="space-y-1">
                    <div className="flex flex-wrap items-center gap-2">
                      <h3 className="text-lg font-semibold">{event.title}</h3>
                      {event.isGlobal && <Badge variant="warning">Global</Badge>}
                    </div>
                    <p className="text-sm text-muted-foreground">
                      {formatDate(event.startAt)}
                    </p>
                    <div className="flex flex-wrap items-center gap-3 text-sm text-muted-foreground">
                      <span className="inline-flex items-center gap-1">
                        <MapPin className="h-4 w-4" />
                        {event.city}, {event.countryCode}
                      </span>
                      {event.registrationUrl && (
                        <a
                          href={event.registrationUrl}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="inline-flex items-center gap-1 text-accent hover:underline"
                        >
                          <ExternalLink className="h-4 w-4" />
                          {t("event.registration")}
                        </a>
                      )}
                    </div>
                    {event.description && (
                      <p className="mt-2 text-sm text-muted-foreground line-clamp-2">
                        {event.description}
                      </p>
                    )}
                  </div>
                </div>

                <div className="flex items-center gap-2">
                  <Button
                    variant="ghost"
                    size="icon-sm"
                    onClick={() => openEditDialog(event)}
                  >
                    <Pencil className="h-4 w-4" />
                  </Button>
                  <Button
                    variant="ghost"
                    size="icon-sm"
                    onClick={() => handleDelete(event.id)}
                    disabled={deleteMutation.isPending}
                    className="text-muted-foreground hover:text-destructive"
                  >
                    <Trash2 className="h-4 w-4" />
                  </Button>
                </div>
              </div>
            </article>
          ))}
        </div>
      )}
    </div>
  );
}

export default function AdminEventsPage() {
  return (
    <TenantAdminGuard>
      <AdminEventsContent />
    </TenantAdminGuard>
  );
}

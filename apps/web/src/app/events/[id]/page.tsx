"use client";

import { useRouter } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { useAuth } from "@/contexts/auth-context";
import { useTranslation } from "@/contexts/locale-context";
import { useToast } from "@/components/ui/toast";
import { useApiMutation } from "@/hooks/use-api-error";
import { eventsApi, plansApi } from "@/lib/api-client";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { DistanceBadge, Distance } from "@/components/distance-badge";
import { MilestoneTimeline, Milestone } from "@/components/milestone-timeline";
import { EmptyState } from "@/components/empty-state";
import {
  Calendar,
  MapPin,
  ExternalLink,
  ArrowLeft,
  Flag,
  User,
  Clock,
  Download,
  Plus,
} from "lucide-react";

export default function EventDetailPage({
  params,
}: {
  params: { id: string };
}) {
  const { id } = params;
  const router = useRouter();
  const { isAuthenticated, login } = useAuth();
  const { t, locale } = useTranslation();
  const toast = useToast();
  const { onError } = useApiMutation();

  // Fetch event
  const {
    data: event,
    isLoading,
    error,
  } = useQuery({
    queryKey: ["event", id],
    queryFn: () => eventsApi.getById(id),
    enabled: !!id,
  });

  // Fetch milestones
  const { data: milestones } = useQuery({
    queryKey: ["event-milestones", id],
    queryFn: () => eventsApi.getMilestones(id),
    enabled: !!id,
  });

  // Format date
  const formatDate = (dateStr: string, options?: Intl.DateTimeFormatOptions) => {
    return new Intl.DateTimeFormat(locale === "tr" ? "tr-TR" : "en-US", {
      dateStyle: "medium",
      ...options,
    }).format(new Date(dateStr));
  };

  // Add to plan
  const handleAddToPlan = async () => {
    if (!isAuthenticated) {
      login();
      return;
    }

    try {
      await plansApi.create(id);
      toast.success(t("toast.addedToPlan"));
    } catch (err) {
      onError(err);
    }
  };

  // Add to calendar (download ICS)
  const handleAddToCalendar = () => {
    const icsUrl = eventsApi.getIcsUrl(id);
    window.open(icsUrl, "_blank");
  };

  // Loading state
  if (isLoading) {
    return (
      <div className="container-app max-w-4xl">
        <Skeleton className="mb-4 h-8 w-32" />
        <Skeleton className="mb-6 h-12 w-3/4" />
        <div className="space-y-4">
          <Skeleton className="h-6 w-1/2" />
          <Skeleton className="h-6 w-1/3" />
          <Skeleton className="h-32 w-full rounded-2xl" />
        </div>
      </div>
    );
  }

  // Error state
  if (error || !event) {
    return (
      <div className="container-app">
        <EmptyState
          variant="events"
          title={locale === "tr" ? "Etkinlik bulunamadı" : "Event not found"}
          description={
            locale === "tr"
              ? "Aradığınız etkinlik mevcut değil veya silinmiş olabilir."
              : "The event you are looking for does not exist or may have been deleted."
          }
          action={{
            label: t("nav.backToExplore"),
            onClick: () => router.push("/"),
          }}
        />
      </div>
    );
  }

  // Parse distances
  const distances: Distance[] = (event.distances || []).filter((d): d is Distance =>
    ["5K", "10K", "21K", "42K", "ultra"].includes(d)
  );

  // Determine registration status
  const now = new Date();
  const eventDate = new Date(event.startAt);
  const isPast = eventDate < now;
  const hasRegistration = !!event.registrationUrl;

  // Convert milestones to timeline format
  const timelineMilestones: Milestone[] = (milestones || []).map((m) => ({
    id: m.id,
    type: m.type,
    label: m.label,
    date: m.date,
    description: m.description,
    status:
      new Date(m.date) < now
        ? "completed"
        : new Date(m.date).toDateString() === now.toDateString()
        ? "current"
        : "upcoming",
  }));

  return (
    <div className="container-app max-w-4xl">
      {/* Back button */}
      <button
        onClick={() => router.back()}
        className="mb-6 inline-flex items-center gap-1.5 text-sm text-muted-foreground transition-colors hover:text-foreground"
      >
        <ArrowLeft className="h-4 w-4" />
        {t("nav.backToExplore")}
      </button>

      {/* Header */}
      <div className="mb-8">
        {/* Distance badges */}
        <div className="mb-4 flex flex-wrap gap-2">
          {distances.map((d) => (
            <DistanceBadge key={d} distance={d} />
          ))}
          {isPast && (
            <Badge variant="muted">{t("event.past")}</Badge>
          )}
          {!isPast && hasRegistration && (
            <Badge variant="success" className="gap-1">
              <Flag className="h-3 w-3" />
              {t("event.registrationOpen")}
            </Badge>
          )}
        </div>

        {/* Title */}
        <h1 className="mb-4 font-display text-3xl font-bold tracking-tight md:text-4xl">
          {event.title}
        </h1>

        {/* Meta */}
        <div className="flex flex-wrap items-center gap-x-6 gap-y-2 text-muted-foreground">
          <span className="inline-flex items-center gap-2">
            <MapPin className="h-5 w-5" />
            {event.city}, {event.countryCode}
          </span>
          <span className="inline-flex items-center gap-2">
            <Calendar className="h-5 w-5" />
            {formatDate(event.startAt)}
          </span>
          {event.endAt && (
            <span className="inline-flex items-center gap-2">
              <Clock className="h-5 w-5" />
              {formatDate(event.endAt, { dateStyle: undefined, timeStyle: "short" })}
            </span>
          )}
        </div>
      </div>

      {/* Actions */}
      <div className="mb-8 flex flex-wrap gap-3">
        <Button variant="accent" size="lg" onClick={handleAddToPlan} className="gap-2">
          <Plus className="h-4 w-4" />
          {t("event.addToPlan")}
        </Button>

        <Button variant="outline" size="lg" onClick={handleAddToCalendar} className="gap-2">
          <Download className="h-4 w-4" />
          {t("event.addToCalendar")}
        </Button>

        {hasRegistration && !isPast && (
          <Button variant="outline" size="lg" asChild>
            <a
              href={event.registrationUrl!}
              target="_blank"
              rel="noopener noreferrer"
              className="gap-2"
            >
              <ExternalLink className="h-4 w-4" />
              {t("event.registration")}
            </a>
          </Button>
        )}
      </div>

      {/* Content grid */}
      <div className="grid gap-8 lg:grid-cols-3">
        {/* Main content */}
        <div className="lg:col-span-2 space-y-8">
          {/* Description */}
          {event.description && (
            <section>
              <h2 className="mb-4 text-lg font-semibold">
                {locale === "tr" ? "Açıklama" : "Description"}
              </h2>
              <p className="text-muted-foreground whitespace-pre-wrap">
                {event.description}
              </p>
            </section>
          )}

          {/* Milestones */}
          {timelineMilestones.length > 0 && (
            <section className="rounded-2xl border bg-card p-6">
              <h2 className="mb-6 text-lg font-semibold">{t("event.milestones")}</h2>
              <MilestoneTimeline milestones={timelineMilestones} />
            </section>
          )}
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          {/* Organizer */}
          {(event.organizerName || event.organizerUrl) && (
            <section className="rounded-2xl border bg-card p-6">
              <h3 className="mb-4 font-semibold">{t("event.organizer")}</h3>
              <div className="flex items-start gap-3">
                <div className="flex h-10 w-10 items-center justify-center rounded-full bg-primary/10">
                  <User className="h-5 w-5 text-primary" />
                </div>
                <div>
                  <p className="font-medium">{event.organizerName || "Organizer"}</p>
                  {event.organizerUrl && (
                    <a
                      href={event.organizerUrl}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="text-sm text-accent hover:underline"
                    >
                      {locale === "tr" ? "Web sitesini ziyaret et" : "Visit website"}
                    </a>
                  )}
                </div>
              </div>
            </section>
          )}

          {/* Quick info */}
          <section className="rounded-2xl border bg-card p-6">
            <h3 className="mb-4 font-semibold">
              {locale === "tr" ? "Hızlı Bilgi" : "Quick Info"}
            </h3>
            <dl className="space-y-3 text-sm">
              <div className="flex justify-between">
                <dt className="text-muted-foreground">{t("event.location")}</dt>
                <dd className="font-medium">
                  {event.city}, {event.countryCode}
                </dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-muted-foreground">{t("event.date")}</dt>
                <dd className="font-medium">{formatDate(event.startAt)}</dd>
              </div>
              {distances.length > 0 && (
                <div className="flex justify-between">
                  <dt className="text-muted-foreground">{t("event.distance")}</dt>
                  <dd className="font-medium">{distances.join(", ")}</dd>
                </div>
              )}
            </dl>
          </section>
        </div>
      </div>
    </div>
  );
}


"use client";

import { useCallback, useMemo } from "react";
import { useSession } from "next-auth/react";
import { useQuery } from "@tanstack/react-query";
import { SuperAdminGuard } from "@/components/route-guards";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { getAdminEvents, Event } from "@/lib/api";
import { CalendarDays, MapPin, Globe, ExternalLink, Loader2 } from "lucide-react";
import { format } from "date-fns";
import { tr } from "date-fns/locale";
import { PageHeader } from "@/components/page-header";
import { ListPagination, PageShell, ResultsHeader } from "@/components/listing";
import { useListQueryParams } from "@/hooks/use-list-query-params";

function formatDate(dateStr: string) {
  try {
    return format(new Date(dateStr), "d MMMM yyyy, HH:mm", { locale: tr });
  } catch {
    return dateStr;
  }
}

function EventsPageContent() {
  const { data: session } = useSession();
  const { getNumberParam, updateParams } = useListQueryParams({
    defaults: { pageSize: 20 },
  });
  const page = getNumberParam("page", 1);
  const pageSize = getNumberParam("pageSize", 20);

  const { data: events, isLoading } = useQuery({
    queryKey: ["super-admin-events"],
    queryFn: () => getAdminEvents(session?.accessToken!),
    enabled: !!session?.accessToken,
  });

  const paginatedEvents = useMemo(() => {
    if (!events) return [];
    const start = (page - 1) * pageSize;
    return events.slice(start, start + pageSize);
  }, [events, page, pageSize]);

  const handlePageChange = useCallback(
    (nextPage: number) => {
      updateParams({ page: nextPage }, { resetPage: false });
      window.scrollTo({ top: 0, behavior: "smooth" });
    },
    [updateParams]
  );

  if (isLoading) {
    return (
      <PageShell>
        <div className="flex items-center justify-center py-20">
          <Loader2 className="h-8 w-8 animate-spin text-primary" />
        </div>
      </PageShell>
    );
  }

  return (
    <PageShell>
      <PageHeader
        title="All Events"
        subtitle="View all events across all tenants (read-only)"
      />

      <ResultsHeader count={events?.length ?? 0} />

      {events && events.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-20">
            <CalendarDays className="h-12 w-12 text-muted-foreground mb-4" />
            <h3 className="text-lg font-medium">No events yet</h3>
            <p className="text-sm text-muted-foreground">
              Events will appear here when created by tenant admins
            </p>
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-4">
          {paginatedEvents.map((event) => (
            <Card key={event.id} className="hover:border-primary/30 transition-colors">
              <CardHeader className="pb-3">
                <div className="flex items-start justify-between">
                  <div className="flex items-center gap-3">
                    <div
                      className={`flex h-10 w-10 items-center justify-center rounded-lg ${
                        event.isGlobal ? "bg-amber-100" : "bg-primary/10"
                      }`}
                    >
                      {event.isGlobal ? (
                        <Globe className="h-5 w-5 text-amber-600" />
                      ) : (
                        <CalendarDays className="h-5 w-5 text-primary" />
                      )}
                    </div>
                    <div>
                      <CardTitle className="text-lg">{event.title}</CardTitle>
                      <CardDescription>
                        {formatDate(event.startAt)}
                      </CardDescription>
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    {event.isGlobal ? (
                      <span className="inline-flex items-center rounded-full bg-amber-100 px-2.5 py-0.5 text-xs font-medium text-amber-800">
                        Global
                      </span>
                    ) : (
                      <span className="inline-flex items-center rounded-full bg-blue-100 px-2.5 py-0.5 text-xs font-medium text-blue-800">
                        Tenant: {event.tenantId?.slice(0, 8)}...
                      </span>
                    )}
                  </div>
                </div>
              </CardHeader>
              <CardContent>
                <div className="flex flex-wrap gap-4 text-sm">
                  <div className="flex items-center gap-1.5 text-muted-foreground">
                    <MapPin className="h-4 w-4" />
                    <span>
                      {event.city}, {event.countryCode}
                    </span>
                  </div>
                  {event.registrationUrl && (
                    <a
                      href={event.registrationUrl}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="flex items-center gap-1.5 text-primary hover:underline"
                    >
                      <ExternalLink className="h-4 w-4" />
                      Registration
                    </a>
                  )}
                </div>
                {event.description && (
                  <p className="mt-3 text-sm text-muted-foreground line-clamp-2">
                    {event.description}
                  </p>
                )}
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      <ListPagination
        page={page}
        pageSize={pageSize}
        total={events?.length}
        onPageChange={handlePageChange}
      />
    </PageShell>
  );
}

export default function SuperAdminEventsPage() {
  return (
    <SuperAdminGuard>
      <EventsPageContent />
    </SuperAdminGuard>
  );
}

import { Skeleton } from "@/components/ui/skeleton";
import { PageShell } from "@/components/listing";
import { EventCardSkeletonList } from "@/components/event-card-skeleton";

export function PlanPageSkeleton() {
  return (
    <PageShell>
      {/* Header */}
      <div className="mb-6">
        <Skeleton className="mb-2 h-8 w-32" />
        <Skeleton className="h-5 w-64" />
      </div>

      {/* Toolbar */}
      <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
        <div className="flex gap-2">
          <Skeleton className="h-10 w-24 rounded-lg" />
          <Skeleton className="h-10 w-24 rounded-lg" />
          <Skeleton className="h-10 w-24 rounded-lg" />
        </div>
        <Skeleton className="h-9 w-36 rounded-lg" />
      </div>

      <Skeleton className="h-4 w-32" />

      <EventCardSkeletonList count={4} />
    </PageShell>
  );
}

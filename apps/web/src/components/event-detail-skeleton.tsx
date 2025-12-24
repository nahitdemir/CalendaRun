import { Skeleton } from "@/components/ui/skeleton";
import { Card } from "@/components/ui/card";

export function EventDetailSkeleton() {
  return (
    <div className="container-app max-w-6xl">
      {/* Back link */}
      <Skeleton className="mb-6 h-6 w-32" />

      {/* Title section */}
      <div className="mb-6">
        {/* Badges */}
        <div className="mb-4 flex flex-wrap gap-2">
          <Skeleton className="h-6 w-16 rounded-full" />
          <Skeleton className="h-6 w-20 rounded-full" />
        </div>

        {/* Title */}
        <Skeleton className="mb-4 h-10 w-3/4" />

        {/* Meta row */}
        <div className="flex flex-wrap items-center gap-x-6 gap-y-2">
          <Skeleton className="h-5 w-32" />
          <Skeleton className="h-5 w-28" />
          <Skeleton className="h-5 w-24" />
        </div>
      </div>

      {/* Actions */}
      <div className="mb-8 flex flex-wrap gap-3">
        <Skeleton className="h-11 w-40 rounded-lg" />
        <Skeleton className="h-11 w-36 rounded-lg" />
      </div>

      {/* Cards grid */}
      <div className="space-y-6">
        {/* Registration card */}
        <Card className="rounded-2xl border p-6">
          <Skeleton className="mb-4 h-6 w-32" />
          <Skeleton className="h-10 w-28 rounded-lg" />
        </Card>

        {/* Milestones card */}
        <Card className="rounded-2xl border p-6">
          <Skeleton className="mb-6 h-6 w-40" />
          <div className="space-y-4">
            <Skeleton className="h-20 w-full" />
            <Skeleton className="h-20 w-full" />
          </div>
        </Card>

        {/* Event info card */}
        <Card className="rounded-2xl border p-6">
          <Skeleton className="mb-4 h-6 w-32" />
          <div className="space-y-3">
            <Skeleton className="h-4 w-full" />
            <Skeleton className="h-4 w-5/6" />
            <Skeleton className="h-4 w-4/6" />
          </div>
        </Card>
      </div>
    </div>
  );
}


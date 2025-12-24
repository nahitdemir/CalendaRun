import { Skeleton } from "@/components/ui/skeleton";
import { cn } from "@/lib/utils";

interface EventCardSkeletonProps {
  className?: string;
}

export function EventCardSkeleton({ className }: EventCardSkeletonProps) {
  return (
    <div
      className={cn(
        "rounded-2xl border bg-card p-4 shadow-sm md:p-5",
        "md:grid md:grid-cols-[1fr_auto] md:items-center md:gap-6",
        className
      )}
    >
      {/* Left: Content */}
      <div className="space-y-3">
        {/* Badges */}
        <div className="flex flex-wrap items-center gap-2">
          <Skeleton className="h-5 w-12 rounded-full" />
          <Skeleton className="h-5 w-24 rounded-full" />
        </div>

        {/* Title */}
        <Skeleton className="h-6 w-3/4" />

        {/* Meta */}
        <div className="flex items-center gap-4">
          <Skeleton className="h-4 w-32" />
          <Skeleton className="h-4 w-24" />
        </div>
      </div>

      {/* Right: Actions */}
      <div className="mt-4 flex flex-wrap items-center gap-2 md:mt-0 md:flex-col md:items-end">
        <Skeleton className="h-9 w-24 rounded-lg" />
        <Skeleton className="h-9 w-20 rounded-lg" />
      </div>
    </div>
  );
}

interface EventCardSkeletonListProps {
  count?: number;
}

export function EventCardSkeletonList({ count = 6 }: EventCardSkeletonListProps) {
  return (
    <div className="space-y-4">
      {Array.from({ length: count }).map((_, i) => (
        <EventCardSkeleton key={i} />
      ))}
    </div>
  );
}

import { Skeleton } from "@/components/ui/skeleton";
import { Card } from "@/components/ui/card";

export function PlanPageSkeleton() {
  return (
    <div className="container-app max-w-6xl">
      {/* Header */}
      <div className="mb-6">
        <Skeleton className="mb-2 h-8 w-32" />
        <Skeleton className="h-5 w-64" />
      </div>

      {/* Tabs */}
      <div className="mb-6 flex gap-2">
        <Skeleton className="h-10 w-24 rounded-lg" />
        <Skeleton className="h-10 w-24 rounded-lg" />
        <Skeleton className="h-10 w-24 rounded-lg" />
      </div>

      {/* Sort bar */}
      <div className="mb-6 flex items-center justify-between">
        <Skeleton className="h-9 w-32" />
      </div>

      {/* Grouped cards */}
      <div className="space-y-8">
        {/* Month group */}
        <div>
          <Skeleton className="mb-4 h-6 w-32" />
          <div className="space-y-4">
            {[1, 2, 3, 4].map((i) => (
              <Card key={i} className="rounded-2xl border p-4 md:p-5">
                <div className="flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
                  <div className="flex-1 space-y-3">
                    <div className="flex flex-wrap items-center gap-2">
                      <Skeleton className="h-5 w-20 rounded-full" />
                      <Skeleton className="h-5 w-16 rounded-full" />
                    </div>
                    <Skeleton className="h-6 w-3/4" />
                    <div className="flex flex-wrap items-center gap-4">
                      <Skeleton className="h-4 w-32" />
                      <Skeleton className="h-4 w-28" />
                    </div>
                    <Skeleton className="h-4 w-40" />
                  </div>
                  <div className="flex flex-wrap items-center gap-2">
                    <Skeleton className="h-9 w-28 rounded-lg" />
                    <Skeleton className="h-9 w-24 rounded-lg" />
                  </div>
                </div>
              </Card>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}


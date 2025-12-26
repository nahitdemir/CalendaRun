import { Skeleton } from "@/components/ui/skeleton";
import { Card } from "@/components/ui/card";
import { PageShell } from "@/components/listing";

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

      {/* Grouped cards */}
      <div className="space-y-8">
        {/* Month group */}
        <div>
          <div className="mb-4 flex items-center gap-3">
            <Skeleton className="h-4 w-28" />
            <Skeleton className="h-px w-full" />
          </div>
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
                    <Skeleton className="h-10 w-28 rounded-lg" />
                    <Skeleton className="h-9 w-20 rounded-lg" />
                  </div>
                </div>
              </Card>
            ))}
          </div>
        </div>
      </div>
    </PageShell>
  );
}

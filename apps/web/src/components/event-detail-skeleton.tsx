import { Skeleton } from "@/components/ui/skeleton";
import { Card } from "@/components/ui/card";
import { PageShell } from "@/components/listing";

export function EventDetailSkeleton() {
  return (
    <PageShell>
      {/* Back link */}
      <Skeleton className="h-6 w-32" />

      <div className="card-sports p-6 md:p-8">
        <div className="flex flex-col gap-6 lg:flex-row lg:items-center lg:justify-between">
          <div className="space-y-4">
            <div className="flex flex-wrap gap-2">
              <Skeleton className="h-6 w-20 rounded-full" />
              <Skeleton className="h-6 w-24 rounded-full" />
            </div>
            <Skeleton className="h-10 w-3/4" />
            <div className="flex flex-wrap items-center gap-x-6 gap-y-2">
              <Skeleton className="h-5 w-32" />
              <Skeleton className="h-5 w-28" />
            </div>
          </div>

          <div className="flex flex-col gap-3 sm:flex-row sm:flex-wrap">
            <Skeleton className="h-10 w-40 rounded-lg" />
            <Skeleton className="h-10 w-36 rounded-lg" />
            <Skeleton className="h-10 w-32 rounded-lg" />
          </div>
        </div>
      </div>

      <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_320px]">
        <Card className="rounded-2xl border p-6">
          <Skeleton className="mb-4 h-6 w-32" />
          <div className="space-y-3">
            <Skeleton className="h-4 w-full" />
            <Skeleton className="h-4 w-5/6" />
            <Skeleton className="h-4 w-4/6" />
          </div>
        </Card>

        <div className="space-y-6">
          <Card className="rounded-2xl border p-6">
            <Skeleton className="mb-4 h-6 w-32" />
            <Skeleton className="h-6 w-24 rounded-full" />
            <Skeleton className="mt-3 h-4 w-4/5" />
            <Skeleton className="mt-4 h-16 w-full" />
          </Card>
          <Card className="rounded-2xl border p-6">
            <Skeleton className="mb-4 h-6 w-32" />
            <Skeleton className="h-4 w-2/3" />
            <Skeleton className="mt-2 h-4 w-1/2" />
          </Card>
        </div>
      </div>
    </PageShell>
  );
}

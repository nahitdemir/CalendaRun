import type { Distance } from "@/components/distance-badge";
import type { Event } from "@/lib/api-client";
import type { EventCardData } from "@/components/event-card";

export function normalizeEventDistances(
  distances: Event["distances"],
  kmToDistance: Record<number, Distance>
): Distance[] {
  if (!distances || Object.keys(kmToDistance).length === 0) return [];
  if (Array.isArray(distances)) {
    const validDistances = Object.values(kmToDistance);
    return (distances as string[]).filter((d): d is Distance =>
      validDistances.includes(d as Distance)
    );
  }
  if (typeof distances === "string") {
    return distances
      .split(",")
      .map((d) => d.trim())
      .map((d) => {
        const km = parseInt(d, 10);
        return kmToDistance[km];
      })
      .filter((d): d is Distance => d !== undefined);
  }
  return [];
}

export function mapEventToCard(
  event: Event,
  kmToDistance: Record<number, Distance>,
  defaultDistance: Distance
): EventCardData {
  const now = new Date();
  const eventDate = new Date(event.startAt);

  let registrationStatus: "open" | "closed" | "upcoming" = "open";
  if (eventDate < now) {
    registrationStatus = "closed";
  } else if (!event.registrationUrl) {
    registrationStatus = "upcoming";
  }

  const distances = normalizeEventDistances(event.distances, kmToDistance);

  return {
    id: event.id,
    title: event.title,
    city: event.city,
    countryCode: event.countryCode,
    date: event.startAt,
    distances: distances.length > 0 ? distances : [defaultDistance],
    registrationStatus,
    registrationUrl: event.registrationUrl || undefined,
  };
}

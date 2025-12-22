namespace Calendarun.Contracts.Planning;

public record PlanningUserPlannedV1(
    Guid UserId,
    string UserEmail,
    Guid EventId,
    Guid PlanItemId,
    string Timezone,
    DateTimeOffset OccurredAt
);


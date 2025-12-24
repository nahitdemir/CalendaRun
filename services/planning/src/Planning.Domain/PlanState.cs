namespace Planning.Domain;

/// <summary>
/// Represents the state of a user's plan item
/// </summary>
public enum PlanState
{
    /// <summary>
    /// Event is planned but user hasn't registered yet
    /// </summary>
    Active,

    /// <summary>
    /// User has registered for the event
    /// </summary>
    Registered,

    /// <summary>
    /// User has completed the event
    /// </summary>
    Completed,

    /// <summary>
    /// Plan was cancelled by the user
    /// </summary>
    Cancelled
}

/// <summary>
/// Extension methods for PlanState enum
/// </summary>
public static class PlanStateExtensions
{
    /// <summary>
    /// Converts string to PlanState enum, defaulting to Active if invalid
    /// </summary>
    public static PlanState ToPlanState(this string? value)
    {
        return Enum.TryParse<PlanState>(value, ignoreCase: true, out var result)
            ? result
            : PlanState.Active;
    }
}


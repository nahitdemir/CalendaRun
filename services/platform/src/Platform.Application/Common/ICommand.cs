namespace Platform.Application.Common;

/// <summary>
/// Marker interface for commands (write operations).
/// Every write operation must be a Command.
/// </summary>
public interface ICommand<TResult>
{
}

/// <summary>
/// Marker interface for commands without a result.
/// </summary>
public interface ICommand : ICommand<Unit>
{
}

/// <summary>
/// Represents a void result for commands.
/// </summary>
public readonly struct Unit
{
    public static readonly Unit Value = new();
}


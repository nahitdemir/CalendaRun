namespace Platform.Application.Common;

/// <summary>
/// Handler for commands (write operations).
/// Contains business logic for a specific command.
/// </summary>
public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken ct = default);
}

/// <summary>
/// Handler for commands without a result.
/// </summary>
public interface ICommandHandler<in TCommand> : ICommandHandler<TCommand, Unit> where TCommand : ICommand
{
}


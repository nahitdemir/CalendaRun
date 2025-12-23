namespace Calendarun.Common.UseCases;

/// <summary>
/// Base interface for all use cases
/// </summary>
public interface IUseCase<in TRequest, TResponse>
{
    Task<TResponse> ExecuteAsync(TRequest request, CancellationToken ct = default);
}

/// <summary>
/// Use case without request (query)
/// </summary>
public interface IUseCase<TResponse>
{
    Task<TResponse> ExecuteAsync(CancellationToken ct = default);
}

/// <summary>
/// Command use case without response
/// </summary>
public interface ICommandUseCase<in TRequest>
{
    Task ExecuteAsync(TRequest request, CancellationToken ct = default);
}


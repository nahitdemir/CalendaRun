namespace Platform.Application.Common;

/// <summary>
/// Handler for queries (read operations).
/// Contains logic for a specific query.
/// </summary>
public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken ct = default);
}


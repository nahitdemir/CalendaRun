namespace Catalog.Application.Common;

public interface ICommand<TResult>
{
}

public interface ICommand : ICommand<Unit>
{
}

public readonly struct Unit
{
    public static readonly Unit Value = new();
}


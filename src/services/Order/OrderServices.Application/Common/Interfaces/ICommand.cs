namespace OrderServices.Application.Common.Interfaces;

/// <summary>
/// Marker interface for commands
/// Commands change state and may or may not return data
/// </summary>
public interface ICommand : IRequest<Result> { }

/// <summary>
/// Generic command interface that returns a result with data
/// </summary>
public interface ICommand<TResponse> : IRequest<Result<TResponse>> { }

/// <summary>
/// Marker interface for queries
/// Queries are read-only and always return data
/// </summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>> { }

namespace CustomerServices.Application.Common.Interfaces;

/// <summary>
/// Marker interface for commands
/// </summary>
public interface ICommand : IRequest<Result> { }

/// <summary>
/// Generic command interface
/// </summary>
public interface ICommand<TResponse> : IRequest<Result<TResponse>> { }

/// <summary>
/// Marker interface for queries
/// </summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>> { }

/// <summary>
/// Command handler interface
/// </summary>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand { }

/// <summary>
/// Generic command handler interface
/// </summary>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse> { }

/// <summary>
/// Query handler interface
/// </summary>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse> { }

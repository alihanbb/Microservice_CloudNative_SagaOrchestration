using OrderServices.Application.Common;

namespace OrderServices.Application.Behaviors;

public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IUnitOfWork _unitOfWork;

    public TransactionBehavior(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Only apply transaction behavior to commands (not queries)
        var requestType = typeof(TRequest);
        var isCommand = requestType.GetInterfaces()
            .Any(i => i.IsGenericType && 
                     (i.GetGenericTypeDefinition() == typeof(Common.Interfaces.ICommand<>) ||
                      i == typeof(Common.Interfaces.ICommand)));

        if (!isCommand)
        {
            return await next();
        }

        var response = await next();

        // Save changes if the command was successful
        if (response is Result result && result.IsSuccess)
        {
            await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        }
        else if (response is not null)
        {
            var isSuccessProperty = response.GetType().GetProperty("IsSuccess");
            if (isSuccessProperty?.GetValue(response) is true)
            {
                await _unitOfWork.SaveEntitiesAsync(cancellationToken);
            }
        }

        return response;
    }
}

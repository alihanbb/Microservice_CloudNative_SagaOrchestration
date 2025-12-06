using CustomerServices.Application.Common;

namespace CustomerServices.Api.Contracts.Responses;

public sealed record ApiResponse<T>(bool Success, T? Data, string? Message);

public sealed record ApiResponse(bool Success, string? Message);

public static class ApiResponseExtensions
{
    public static ApiResponse<T> ToApiResponse<T>(this Result<T> result)
    {
        return result.IsSuccess
            ? new ApiResponse<T>(true, result.Value, null)
            : new ApiResponse<T>(false, default, result.Error);
    }

    public static ApiResponse ToApiResponse(this Result result)
    {
        return result.IsSuccess
            ? new ApiResponse(true, "Operation completed successfully")
            : new ApiResponse(false, result.Error);
    }
}

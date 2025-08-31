using Microsoft.EntityFrameworkCore;

namespace XpertSphere.MonolithApi.Utils.Results.Pagination;

/// <summary>
/// Generic paginated result class for handling paginated data
/// </summary>
/// <typeparam name="T">The type of data in the paginated result</typeparam>
public class PaginatedResult<T>
{
    public List<T> Data { get; set; } = [];
    public PaginationMetadata Pagination { get; set; } = new();
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = [];

    /// <summary>
    /// Create a successful paginated result
    /// </summary>
    public static PaginatedResult<T> Success(
        List<T> data,
        int totalItems,
        int pageNumber,
        int pageSize,
        string message = Constants.SUCCESS_RETRIEVAL)
    {
        return new PaginatedResult<T>
        {
            Data = data,
            Pagination = new PaginationMetadata
            {
                CurrentPage = pageNumber,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling((double)totalItems / pageSize),
                HasPrevious = pageNumber > 1,
                HasNext = pageNumber < (int)Math.Ceiling((double)totalItems / pageSize)
            },
            IsSuccess = true,
            Message = message
        };
    }

    /// <summary>
    /// Create an empty successful paginated result
    /// </summary>
    public static PaginatedResult<T> Empty(int pageNumber = 1, int pageSize = 10, string message = Constants.DATA_NOT_FOUND)
    {
        return Success([], 0, pageNumber, pageSize, message);
    }

    /// <summary>
    /// Create a failed paginated result
    /// </summary>
    public static PaginatedResult<T> Failure(string error)
    {
        return new PaginatedResult<T>
        {
            IsSuccess = false,
            Message = error,
            Errors = [error]
        };
    }

    /// <summary>
    /// Create a failed paginated result with multiple errors
    /// </summary>
    public static PaginatedResult<T> Failure(List<string> errors, string message = Constants.OPERATION_FAILED)
    {
        return new PaginatedResult<T>
        {
            IsSuccess = false,
            Message = message,
            Errors = errors
        };
    }

    /// <summary>
    /// Transform the items to a different type
    /// </summary>
    public PaginatedResult<TNew> Map<TNew>(Func<T, TNew> mapper)
    {
        if (!IsSuccess)
        {
            return new PaginatedResult<TNew>
            {
                IsSuccess = false,
                Message = Message,
                Errors = Errors
            };
        }

        var mappedItems = Data.Select(mapper).ToList();
        return new PaginatedResult<TNew>
        {
            Data = mappedItems,
            Pagination = Pagination,
            IsSuccess = true,
            Message = Message
        };
    }
}
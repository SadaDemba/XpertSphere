using Microsoft.EntityFrameworkCore;

namespace XpertSphere.MonolithApi.Utils.Results;

/// <summary>
/// Generic paginated result class for handling paginated data
/// </summary>
/// <typeparam name="T">The type of items in the paginated result</typeparam>
public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = [];
    public PaginationMetadata Pagination { get; set; } = new();
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = [];

    /// <summary>
    /// Create a successful paginated result
    /// </summary>
    public static PaginatedResult<T> Success(
        List<T> items,
        int totalItems,
        int pageNumber,
        int pageSize,
        string message = Constants.SUCCESS_RETRIEVAL)
    {
        return new PaginatedResult<T>
        {
            Items = items,
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

        var mappedItems = Items.Select(mapper).ToList();
        return new PaginatedResult<TNew>
        {
            Items = mappedItems,
            Pagination = Pagination,
            IsSuccess = true,
            Message = Message
        };
    }
}

/// <summary>
/// Pagination metadata information
/// </summary>
public class PaginationMetadata
{
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }

    /// <summary>
    /// Get pagination links information
    /// </summary>
    public PaginationLinks GetLinks(string baseUrl)
    {
        return new PaginationLinks
        {
            First = HasPrevious ? $"{baseUrl}?pageNumber=1&pageSize={PageSize}" : null,
            Previous = HasPrevious ? $"{baseUrl}?pageNumber={CurrentPage - 1}&pageSize={PageSize}" : null,
            Next = HasNext ? $"{baseUrl}?pageNumber={CurrentPage + 1}&pageSize={PageSize}" : null,
            Last = HasNext ? $"{baseUrl}?pageNumber={TotalPages}&pageSize={PageSize}" : null
        };
    }
}

/// <summary>
/// Pagination navigation links
/// </summary>
public class PaginationLinks
{
    public string? First { get; set; }
    public string? Previous { get; set; }
    public string? Next { get; set; }
    public string? Last { get; set; }
}

/// <summary>
/// Extension methods for IQueryable to support pagination
/// </summary>
public static class PaginationExtensions
{
    /// <summary>
    /// Apply pagination to an IQueryable
    /// </summary>
    public static async Task<PaginatedResult<T>> ToPaginatedResultAsync<T>(
        this IQueryable<T> query,
        int pageNumber,
        int pageSize)
    {
        var totalItems = await query.CountAsync();
        
        if (totalItems == 0)
        {
            return PaginatedResult<T>.Empty(pageNumber, pageSize);
        }

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return PaginatedResult<T>.Success(items, totalItems, pageNumber, pageSize);
    }

    /// <summary>
    /// Apply pagination to a List
    /// </summary>
    public static PaginatedResult<T> ToPaginatedResult<T>(
        this List<T> list,
        int pageNumber,
        int pageSize)
    {
        var totalItems = list.Count;
        
        if (totalItems == 0)
        {
            return PaginatedResult<T>.Empty(pageNumber, pageSize);
        }

        var items = list
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return PaginatedResult<T>.Success(items, totalItems, pageNumber, pageSize);
    }
}

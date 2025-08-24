using Microsoft.EntityFrameworkCore;

namespace XpertSphere.MonolithApi.Utils.Results.Pagination;

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
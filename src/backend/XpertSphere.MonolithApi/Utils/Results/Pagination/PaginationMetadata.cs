namespace XpertSphere.MonolithApi.Utils.Results.Pagination;

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

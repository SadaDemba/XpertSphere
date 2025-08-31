using System.ComponentModel.DataAnnotations.Schema;
using XpertSphere.MonolithApi.Enums;

namespace XpertSphere.MonolithApi.DTOs.Base
{
    [NotMapped]
    public class Filter
    {
        public string? SearchTerms { get; set; }
        // Pagination
        public string PageNumber { get; set; } = "1";
        public string PageSize { get; set; } = "10";

        // Sorting
        public string? SortBy { get; set; } = "CreatedAt";
        public SortDirection SortDirection { get; set; } = SortDirection.Descending;
    }
}

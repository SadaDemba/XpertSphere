using Microsoft.EntityFrameworkCore;
using XpertSphere.MonolithApi.Utils.Pagination;

namespace XpertSphere.MonolithApi.Services.Pagination
{
    public class PaginationService<T>
    {
        public static async Task<ResponseResource<T>> Paginate(string PageNumber, string pageSize, IQueryable<T> data)
        {
            PaginationFilter? validFilter = new(int.Parse(PageNumber), int.Parse(pageSize));

            IEnumerable<T> pagedData = await data
                .Skip((validFilter.PageNumber - 1) * validFilter.PageSize)
                .Take(validFilter.PageSize).ToListAsync();

            ResponseResource<T> response = new(pagedData, validFilter.PageNumber, validFilter.PageSize);

            int totalRecords = data.Count();
            double totalPages = ((double)totalRecords / (double)validFilter.PageSize);

            int roundedTotalPages = Convert.ToInt32(Math.Ceiling(totalPages));

            response.CurrentPage =
                PageUrlService.GetPageUrl(
                    new PaginationFilter(validFilter.PageNumber, validFilter.PageSize)
                    );

            response.NextPage = validFilter.PageNumber < roundedTotalPages ?
                PageUrlService.GetPageUrl(new PaginationFilter(validFilter.PageNumber + 1, validFilter.PageSize)) : null;

            response.PreviousPage = validFilter.PageNumber > 1 ?
                PageUrlService.GetPageUrl(new PaginationFilter(validFilter.PageNumber - 1, validFilter.PageSize)) : null;

            response.FirstPage = PageUrlService.GetPageUrl(new PaginationFilter(1, validFilter.PageSize));
            response.LastPage = PageUrlService.GetPageUrl(new PaginationFilter(roundedTotalPages, validFilter.PageSize));
            response.TotalPages = roundedTotalPages;
            response.TotalRecords = totalRecords;

            return response;
        }
    }
}

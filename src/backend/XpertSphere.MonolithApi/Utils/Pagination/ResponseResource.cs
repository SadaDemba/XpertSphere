namespace XpertSphere.MonolithApi.Utils.Pagination
{
    public class ResponseResource<T>
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string? FirstPage { get; set; }
        public string? LastPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }
        public string? NextPage { get; set; }
        public string? PreviousPage { get; set; }
        public string? CurrentPage { get; set; }
        public IEnumerable<T> Data { get; set; }

        public ResponseResource(IEnumerable<T> data, int pageNumber, int pageSize)
        {
            this.PageNumber = pageNumber;
            this.PageSize = pageSize;
            this.Data = data;
        }
    }
}

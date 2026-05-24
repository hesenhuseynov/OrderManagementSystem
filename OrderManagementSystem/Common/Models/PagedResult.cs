namespace OrderManagementSystem.Common.Models
{
    public sealed   class PagedResult<T> 
    {
       public   IReadOnlyList<T> Items { get; init; } = [];
         
        public int PageNumber  { get; init; }

        public int PageSize { get; init; }

        public int TotalCount { get; set; }

        public int TotalPages => PageSize < 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize); 
    }
}

namespace SmartPlanner.Application.Common.Dtos;

    public record PagedResult<T>(
        List<T> Items,
        int PageNumber,
        int PageSize,
        int TotalCount)
    {
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        public static PagedResult<T> Create(List<T> items, int totalCount, int pageNumber, int pageSize)
            => new PagedResult<T>(items, totalCount, pageNumber, pageSize);
    }

    public record PaginationRequest(
        int PageNumber = 1,
        int PageSize = 10)
    {
        public int Skip => (PageNumber - 1) * PageSize;
        public int Take => PageSize;
    }

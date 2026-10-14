using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartPlanner.Application.Common
{
    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int PageNumber { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; }

        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        public PaginatedResult() { }

        public PaginatedResult(
            List<T> items,
            int totalCount,
            int pageNumber,
            int pageSize)
        {
            Items = items;
            TotalCount = totalCount;
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        }

        // Простой фабричный метод
        public static PaginatedResult<T> Create(
            List<T> items,
            int totalCount,
            int pageNumber,
            int pageSize)
        {
            return new PaginatedResult<T>(items, totalCount, pageNumber, pageSize);
        }
    }
}

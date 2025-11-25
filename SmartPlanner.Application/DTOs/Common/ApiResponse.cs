using System.Text.Json.Serialization;

namespace SmartPlanner.Domain.DTOs.Common
{
    public class ApiResponse<T>
    {
        public bool success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? Errors { get; set; }

        public static ApiResponse<T> Success(T data, string message = "")
        {
            return new ApiResponse<T>
            {
                success = true,
                Message = message,
                Data = data
            };
        }

        public static ApiResponse<T> Error(string errorMessage)
        {
            return new ApiResponse<T>
            {
                success = false,
                Message = errorMessage,
                Errors = new List<string> { errorMessage }
            };
        }

        public static ApiResponse<T> Error(List<string> errors, string message = "")
        {
            return new ApiResponse<T>
            {
                success = false,
                Message = message,
                Errors = errors
            };
        }
    }

    public class PagedResponse<T> : ApiResponse<List<T>>
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        public PagedResponse(List<T> data, int pageNumber, int pageSize, int totalCount)
        {
            Data = data;
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalCount = totalCount;
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            success = true;
            Message = string.Empty;
        }
    }
}
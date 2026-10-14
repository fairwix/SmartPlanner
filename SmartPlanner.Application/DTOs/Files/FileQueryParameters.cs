namespace SmartPlanner.Application.Dtos.Files
{
    public class FileQueryParameters
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Search { get; set; }
        public string? ContentType { get; set; }
        public bool? IsPublic { get; set; }
        public string? SortBy { get; set; } = "CreatedAt";
        public bool SortDescending { get; set; } = true;

        // Опционально: можешь добавить конвертацию в PageNumber/PageSize если нужно
        public int PageNumber => Page;

        public override string ToString() =>
            $"Page={Page}, PageSize={PageSize}, Search={Search}, ContentType={ContentType}, IsPublic={IsPublic}, SortBy={SortBy}, SortDescending={SortDescending}";
    }
}

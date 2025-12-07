namespace SmartPlanner.Application.Common.Dtos;

public record AdvancedPaginationRequest(
    int PageNumber = 1,
    int PageSize = 10,
    string? Search = null,
    string? SortBy = "CreatedAt",
    string? SortOrder = "desc",
    DateTime? CreatedFrom = null,
    DateTime? CreatedTo = null,
    DateTime? DueDateFrom = null,
    DateTime? DueDateTo = null,
    string[]? Categories = null,
    string[]? Priorities = null,
    bool? IsCompleted = null,
    bool? IsExpired = null,
    bool? IsOnTrack = null,
    int? MinProgress = null,
    int? MaxProgress = null) : PaginationRequest(PageNumber, PageSize)
{
    public new int Skip => (PageNumber - 1) * PageSize; // ✅ Добавить 'new'
    public new int Take => PageSize; // ✅ Добавить 'new'
}

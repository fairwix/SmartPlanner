namespace SmartPlanner.API.Dtos.GoalsBulk;

public class BulkCreateGoalsRequest
{
    public List<CreateGoalItemRequest> Goals { get; set; } = new();
}

public class CreateGoalItemRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime Deadline { get; set; }
    public int Priority { get; set; }
    public string? Category { get; set; }
    public string? Status { get; set; }
    public List<string>? Tags { get; set; } = new();
    public TimeSpan? EstimatedDuration { get; set; }
    public Guid? ParentGoalId { get; set; }
    public string? RecurrencePattern { get; set; }
}

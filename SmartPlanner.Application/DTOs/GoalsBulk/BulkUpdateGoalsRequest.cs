namespace SmartPlanner.API.Dtos.GoalsBulk;

public class BulkUpdateGoalsRequest
{
    public List<UpdateGoalItemRequest> Goals { get; set; } = new();
}

public class UpdateGoalItemRequest
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime Deadline { get; set; }
    public int Priority { get; set; }
}

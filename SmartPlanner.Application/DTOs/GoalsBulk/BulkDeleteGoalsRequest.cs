namespace SmartPlanner.API.Dtos.GoalsBulk;

public class BulkDeleteGoalsRequest
{
    public List<Guid> GoalIds { get; set; } = new();
}

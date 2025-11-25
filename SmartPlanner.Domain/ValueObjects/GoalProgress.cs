using System;

namespace SmartPlanner.Domain.Entities
{
    public class GoalProgress : BaseEntity
    {
        public Guid GoalId { get; set; }
        public int Value { get; set; }
        public int PreviousValue { get; set; }
        public string? Notes { get; set; }
        
        public virtual Goal Goal { get; set; } = null!;
    }
}
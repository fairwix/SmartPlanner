using System;

namespace SmartPlanner.Domain.Entities;

    public class GoalProgress : BaseEntity
    {
        public Guid GoalId { get; init; }
        public int Value { get; init; }
        public int PreviousValue { get; init; }
        public string? Notes { get; init; }

        public virtual Goal Goal { get; init; } = null!;
    }

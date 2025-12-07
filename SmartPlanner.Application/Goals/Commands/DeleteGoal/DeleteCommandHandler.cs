using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartPlanner.Application.Common.Interfaces;

namespace SmartPlanner.Application.Goals.Commands
{
    public class DeleteGoalCommandHandler : IRequestHandler<DeleteGoalCommand, bool>
    {
        private readonly IApplicationDbContext _context;

        public DeleteGoalCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(DeleteGoalCommand request, CancellationToken cancellationToken)
        {
            var goal = await _context.Goals
                .FirstOrDefaultAsync(g => g.Id == request.GoalId, cancellationToken);

            if (goal == null)
                return false;

            _context.Goals.Remove(goal);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}

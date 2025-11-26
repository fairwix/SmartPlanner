// SmartPlanner.Application/Goals/Commands/DeleteGoalCommandHandler.cs
using MediatR;
using SmartPlanner.Application.Common.Interfaces.Repositories;

namespace SmartPlanner.Application.Goals.Commands;

    public class DeleteGoalCommandHandler : IRequestHandler<DeleteGoalCommand, bool>
    {
        private readonly IGoalRepository _goalRepository;

        public DeleteGoalCommandHandler(IGoalRepository goalRepository)
        {
            _goalRepository = goalRepository;
        }

        public async Task<bool> Handle(DeleteGoalCommand request, CancellationToken cancellationToken)
        {
            return await _goalRepository.DeleteAsync(request.GoalId, cancellationToken);
        }
    }

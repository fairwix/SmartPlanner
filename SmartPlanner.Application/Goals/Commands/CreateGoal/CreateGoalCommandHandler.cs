using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Goals.Dtos;
using SmartPlanner.Domain.Entities;
using SmartPlanner.Domain.Enums;
using GoalCategory = SmartPlanner.Domain.Entities.GoalCategory;

namespace SmartPlanner.Application.Goals.Commands
{
    public class CreateGoalCommandHandler : IRequestHandler<CreateGoalCommand, GoalDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<CreateGoalCommandHandler> _logger;
        private readonly IMapper _mapper;

        public CreateGoalCommandHandler(
            IApplicationDbContext context,
            ILogger<CreateGoalCommandHandler> logger,
            IMapper mapper)
        {
            _context = context;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<GoalDto> Handle(CreateGoalCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating goal for user {UserId}", request.UserId);

            // Проверяем пользователя
            var userExists = await _context.Users
                .AnyAsync(u => u.Id == request.UserId, cancellationToken);

            if (!userExists)
                throw new ArgumentException($"User with ID {request.UserId} not found");

            var goal = new Goal
            {
                Title = request.Title,
                Description = request.Description,
                Category = Enum.Parse<GoalCategory>(request.Category),
                Priority = Enum.Parse<GoalPriority>(request.Priority),
                DueDate = request.DueDate,
                TargetValue = request.TargetValue,
                UserId = request.UserId,
                RewardAmount = 10
            };

            await _context.Goals.AddAsync(goal, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Goal {GoalId} created successfully", goal.Id);

            return _mapper.Map<GoalDto>(goal);
        }
    }
}

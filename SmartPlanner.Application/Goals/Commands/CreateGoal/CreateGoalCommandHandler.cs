using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Goals.Commands;
using SmartPlanner.Application.Goals.Dtos;
using SmartPlanner.Domain.Entities;

public class CreateGoalCommandHandler : IRequestHandler<CreateGoalCommand, GoalDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateGoalCommandHandler> _logger;
    private readonly IMapper _mapper; // ✅ ДОБАВЛЯЕМ AutoMapper

    public CreateGoalCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<CreateGoalCommandHandler> logger,
        IMapper mapper) // ✅ ДОБАВЛЯЕМ в конструктор
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _mapper = mapper; // ✅ Инициализируем
    }

    public async Task<GoalDto> Handle(CreateGoalCommand request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating goal for user {UserId}", request.UserId);

        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            throw new ArgumentException($"User with ID {request.UserId} not found");
        }

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

        await _unitOfWork.Goals.CreateAsync(goal, cancellationToken);
        
        user.AddReward(5);
        await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Goal {GoalId} created successfully", goal.Id);
        
        // ✅ ИСПРАВЛЯЕМ: Используем AutoMapper вместо MapToDto
        return _mapper.Map<GoalDto>(goal);
    }

    // ❌ УДАЛЯЕМ старый метод MapToDto - он больше не нужен
    // private GoalDto MapToDto(Domain.Entities.Goal goal) { ... }
}
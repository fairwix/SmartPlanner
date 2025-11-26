// Infrastructure/Mapping/GoalsBulkProfile.cs
using AutoMapper;
using SmartPlanner.API.Dtos.GoalsBulk;
using SmartPlanner.Application.Goals.Commands;

namespace SmartPlanner.API.Infrastructure.Mapping;

public class GoalsBulkProfile : Profile
{
    public GoalsBulkProfile()
    {
        // Маппинг для массового создания
        CreateMap<BulkCreateGoalsRequest, BulkCreateGoalsCommand>();
        CreateMap<CreateGoalItemRequest, CreateGoalCommand>();

        // Маппинг для массового обновления
        CreateMap<BulkUpdateGoalsRequest, BulkUpdateGoalsCommand>();
        CreateMap<UpdateGoalItemRequest, UpdateGoalCommand>();

        // Маппинг для массового удаления
        CreateMap<BulkDeleteGoalsRequest, BulkDeleteGoalsCommand>();
    }
}

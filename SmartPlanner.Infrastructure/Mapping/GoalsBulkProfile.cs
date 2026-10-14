using AutoMapper;
using SmartPlanner.API.Dtos.GoalsBulk;
using SmartPlanner.Application.Goals.Commands;

namespace SmartPlanner.Application.Infrastructure.Mapping;

public class GoalsBulkProfile : Profile
{
    public GoalsBulkProfile()
    {

        CreateMap<BulkCreateGoalsRequest, BulkCreateGoalsCommand>();
        CreateMap<CreateGoalItemRequest, CreateGoalCommand>();

        CreateMap<BulkUpdateGoalsRequest, BulkUpdateGoalsCommand>();
        CreateMap<UpdateGoalItemRequest, UpdateGoalCommand>();

        CreateMap<BulkDeleteGoalsRequest, BulkDeleteGoalsCommand>();
    }
}

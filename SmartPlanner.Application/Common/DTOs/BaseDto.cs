// SmartPlanner.Application/Common/Dtos/BaseDto.cs
namespace SmartPlanner.Application.Common.Dtos;

    public record BaseDto(Guid Id, DateTime CreatedAt, DateTime UpdatedAt);

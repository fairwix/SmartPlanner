// SmartPlanner.Application/Common/Dtos/BaseDto.cs
namespace SmartPlanner.Application.Common.Dtos
{
    public abstract class BaseDto
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
// SmartPlanner.Application/Common/Interfaces/IQuery.cs
using MediatR;

namespace SmartPlanner.Application.Common.Interfaces
{
    public interface IQuery<TResponse> : IRequest<TResponse>
    {
    }
}
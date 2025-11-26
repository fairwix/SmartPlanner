// SmartPlanner.Application/Common/Interfaces/ICommand.cs
using MediatR;

namespace SmartPlanner.Application.Common.Interfaces;

    public interface ICommand : IRequest
    {
    }

    public interface ICommand<TResponse> : IRequest<TResponse>
    {
    }

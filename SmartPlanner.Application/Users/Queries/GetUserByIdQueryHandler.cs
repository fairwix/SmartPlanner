// SmartPlanner.Application/Users/Queries/GetUserByIdQueryHandler.cs
using MediatR;
using SmartPlanner.Application.Users.Dtos;
using Microsoft.EntityFrameworkCore;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Users.Queries;

    public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto?>
    {
        private readonly IApplicationDbContext _context;

        public GetUserByIdQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UserDto?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            return user != null ? MapToDto(user) : null;
        }

        private UserDto MapToDto(User user)
        {
            return new UserDto(
                user.Id,
                user.CreatedAt,
                user.UpdatedAt,
                user.Username,
                user.Email,
                user.Interests,
                user.Balance,
                user.StreakCount,
                user.LastLogin);
        }
    }

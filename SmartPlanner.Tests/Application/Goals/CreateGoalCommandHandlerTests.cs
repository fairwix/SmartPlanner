using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SmartPlanner.Application.Goals.Commands;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Domain.Entities;
using SmartPlanner.Infrastructure.Data;
using Xunit;

namespace SmartPlanner.Tests.Application.Goals
{
    public class CreateGoalCommandHandlerTests : IDisposable
    {
            private readonly AppDbContext _context;
        private readonly ILogger<CreateGoalCommandHandler> _logger;
        private readonly IMapper _mapper;
        private readonly DbContextOptions<AppDbContext> _options;
        private readonly CreateGoalCommandHandler _handler;

            public CreateGoalCommandHandlerTests()
        {
            // Configure in-memory database for testing
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            // Initialize the database with test data
            _context = new AppDbContext(_options);

            // Initialize logger (using NullLogger for testing)
            _logger = new Mock<ILogger<CreateGoalCommandHandler>>().Object;

            // Initialize AutoMapper with test configuration
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new SmartPlanner.Application.Common.Mapping.MappingProfile());
            });
            _mapper = config.CreateMapper();

            _handler = new CreateGoalCommandHandler(_context, _logger, _mapper);
        }

        public void Dispose()
        {
            // Clean up the in-memory database after each test
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task Handle_ValidCommand_ReturnsGoalDto()
        {
            // Test implementation
        }

        [Fact]
        public async Task Handle_UserNotFound_ThrowsArgumentException()
        {
            // Test implementation
        }
    }
}

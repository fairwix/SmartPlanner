using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Moq;

public static class MockDbSetHelper
{
    public static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data) where T : class =>
        SmartPlanner.Tests.TestHelpers.AsyncQueryableExtensions.CreateMockDbSet(data);
}

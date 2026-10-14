using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartPlanner.API.Hubs;
using SmartPlanner.Domain.Entities;
using SmartPlanner.Infrastructure.Data;
using Xunit;

namespace SmartPlanner.Tests.API.Integration;

public sealed class StageOneApiTests : IClassFixture<ApiTestApplicationFactory>
{
    private readonly ApiTestApplicationFactory _factory;

    public StageOneApiTests(ApiTestApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task UserCreation_RequiresAdmin_AndDuplicateDoesNotReturnExistingProfile()
    {
        using var client = _factory.CreateClient();
        var userId = Guid.NewGuid();
        var payload = new { username = "existing_user", email = "existing@example.test", password = "ValidPass123" };

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/api/users", payload)).StatusCode);

        await SeedUserAsync(userId, payload.username, payload.email);
        client.DefaultRequestHeaders.Add("X-Test-User", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
        var response = await client.PostAsJsonAsync("/api/users", payload);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain(payload.email, body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(userId.ToString(), body, StringComparison.OrdinalIgnoreCase);

        var duplicateUsername = new
        {
            username = payload.username,
            email = "new-address@example.test",
            password = "ValidPass123"
        };
        response = await client.PostAsJsonAsync("/api/users", duplicateUsername);
        body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain(payload.email, body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(userId.ToString(), body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BulkGoalEndpoints_RunThroughApiAndOnlyChangeCurrentUsersGoals()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var ownGoalId = Guid.NewGuid();
        var otherGoalId = Guid.NewGuid();
        await SeedUserAsync(userId, "bulk_owner", "bulk-owner@example.test");
        await SeedUserAsync(otherUserId, "bulk_other", "bulk-other@example.test");
        await SeedGoalAsync(ownGoalId, userId, "Own goal");
        await SeedGoalAsync(otherGoalId, otherUserId, "Other goal");

        using var client = AuthenticatedClient(userId);
        var create = await client.PostAsJsonAsync("/api/goals/bulk", new
        {
            userId = otherUserId,
            goals = new[]
            {
                new
                {
                    title = "Bulk created", description = "", category = "Personal", priority = "Medium",
                    dueDate = DateTime.UtcNow.AddDays(3), targetValue = 10, userId = otherUserId
                }
            }
        });
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);

        var update = await client.PutAsJsonAsync("/api/goals/bulk", new
        {
            goals = new[] { new { goalId = otherGoalId, updateData = new { title = "Hijacked" } } }
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Contains("not owned", (await update.Content.ReadAsStringAsync()), StringComparison.OrdinalIgnoreCase);

        update = await client.PutAsJsonAsync("/api/goals/bulk", new
        {
            goals = new[] { new { goalId = ownGoalId, updateData = new { title = "Updated by owner" } } }
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var delete = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/goals/bulk")
        {
            Content = JsonContent.Create(new { goalIds = new[] { otherGoalId } })
        });
        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal("Other goal", (await db.Goals.SingleAsync(g => g.Id == otherGoalId)).Title);
        Assert.Equal("Updated by owner", (await db.Goals.SingleAsync(g => g.Id == ownGoalId)).Title);
        Assert.Equal(userId, (await db.Goals.SingleAsync(g => g.Title == "Bulk created")).UserId);

        delete = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/goals/bulk")
        {
            Content = JsonContent.Create(new { goalIds = new[] { ownGoalId } })
        });
        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
        db.ChangeTracker.Clear();
        Assert.Null(await db.Goals.FindAsync(ownGoalId));
    }

    [Fact]
    public async Task ChallengeProgressAndLeave_RunThroughApiPipeline()
    {
        var userId = Guid.NewGuid();
        await SeedUserAsync(userId, "challenge_user", "challenge@example.test");
        using var client = AuthenticatedClient(userId);
        var challengeId = Guid.NewGuid();
        using (var seedScope = _factory.Services.CreateScope())
        {
            var seedDb = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            seedDb.Challenges.Add(new Challenge
            {
                Id = challengeId,
                Title = "API pipeline challenge",
                Description = "test",
                Type = ChallengeType.Custom,
                StartDate = DateTime.UtcNow.AddHours(-1),
                EndDate = DateTime.UtcNow.AddDays(1),
                IsGroupChallenge = true,
                TargetValue = 10,
                CreatedBy = userId
            });
            seedDb.ChallengeParticipants.Add(new ChallengeParticipant
            {
                ChallengeId = challengeId,
                UserId = userId,
                Status = ParticipantStatus.Joined,
                JoinedAt = DateTime.UtcNow
            });
            await seedDb.SaveChangesAsync();
        }

        var progressResponse = await client.PostAsJsonAsync($"/api/challenges/{challengeId}/progress", new { progress = 4 });
        Assert.True(progressResponse.StatusCode == HttpStatusCode.OK,
            await progressResponse.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsync($"/api/challenges/{challengeId}/leave", content: null)).StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var challenge = await db.Challenges.SingleAsync(c => c.Id == challengeId);
        var participant = await db.ChallengeParticipants.SingleAsync(p => p.ChallengeId == challengeId);
        Assert.Equal(4, challenge.CurrentValue);
        Assert.Equal(4, participant.PersonalContribution);
        Assert.Equal(ParticipantStatus.Left, participant.Status);
    }

    [Fact]
    public async Task Cors_OnlyReturnsAllowOriginForConfiguredOrigin()
    {
        using var client = _factory.CreateClient();
        var disallowed = CreateCorsPreflight("https://attacker.example");
        var deniedResponse = await client.SendAsync(disallowed);
        Assert.False(deniedResponse.Headers.Contains("Access-Control-Allow-Origin"));

        var allowed = CreateCorsPreflight("https://client.example");
        var allowedResponse = await client.SendAsync(allowed);
        Assert.Equal("https://client.example", allowedResponse.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task NotificationHub_DoesNotLetCallerJoinAnotherUsersGroupOrReceiveTheirEvents()
    {
        var victimId = Guid.NewGuid();
        var attackerId = Guid.NewGuid();
        await using var victim = CreateHubConnection(victimId);
        await using var attacker = CreateHubConnection(attackerId);
        var victimReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var attackerReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        victim.On<object>("PrivateEvent", _ => victimReceived.TrySetResult());
        attacker.On<object>("PrivateEvent", _ => attackerReceived.TrySetResult());

        await victim.StartAsync();
        await attacker.StartAsync();
        await Assert.ThrowsAsync<HubException>(() =>
            attacker.InvokeAsync("SubscribeToGroup", $"user_{victimId}"));

        var hubContext = _factory.Services.GetRequiredService<IHubContext<NotificationHub>>();
        await hubContext.Clients.Group($"user_{victimId}").SendAsync("PrivateEvent", new { data = "private" });
        await victimReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(100);
        Assert.False(attackerReceived.Task.IsCompleted);
    }

    private HubConnection CreateHubConnection(Guid userId) => new HubConnectionBuilder()
        .WithUrl("http://localhost/hubs/notifications", options =>
        {
            options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
            options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            options.Headers.Add("X-Test-User", userId.ToString());
        })
        .Build();

    private HttpClient AuthenticatedClient(Guid userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", userId.ToString());
        return client;
    }

    private static HttpRequestMessage CreateCorsPreflight(string origin) => new(HttpMethod.Options, "/api/goals/bulk")
    {
        Headers = { { "Origin", origin }, { "Access-Control-Request-Method", "POST" } }
    };

    private async Task SeedUserAsync(Guid id, string username, string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Users.Add(new User { Id = id, Username = username, Email = email, PasswordHash = "hash", PasswordSalt = "salt" });
        await db.SaveChangesAsync();
    }

    private async Task SeedGoalAsync(Guid id, Guid userId, string title)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Goals.Add(new Goal
        {
            Id = id, UserId = userId, Title = title, DueDate = DateTime.UtcNow.AddDays(5), TargetValue = 10,
            Category = SmartPlanner.Domain.Entities.GoalCategory.Personal,
            Priority = GoalPriority.Medium
        });
        await db.SaveChangesAsync();
    }

}

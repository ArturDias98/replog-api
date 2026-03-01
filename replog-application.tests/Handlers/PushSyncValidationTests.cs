using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using replog_application.Commands;
using replog_application.Interfaces;
using replog_application.tests.Fixtures;
using replog_application.tests.Helpers;
using replog_shared.Enums;
using replog_shared.Models.Requests;
using replog_shared.Models.Responses;

namespace replog_application.tests.Handlers;

[Collection("Application")]
public class PushSyncValidationTests(ApplicationFixture fixture)
{
    private async Task<PushSyncResponse> HandlePushSync(string userId, List<SyncChangeDto> changes)
    {
        using var scope = fixture.Provider.CreateScope();
        var handler = scope.ServiceProvider
            .GetRequiredService<ICommandHandler<PushSyncCommand, PushSyncResponse>>();

        return await handler.HandleAsync(new PushSyncCommand
        {
            UserId = userId,
            Request = new PushSyncRequest { Changes = changes }
        });
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowValidationException_WhenChangesListIsEmpty()
    {
        var userId = Guid.NewGuid().ToString();

        await Assert.ThrowsAsync<ValidationException>(() =>
            HandlePushSync(userId, []));
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowValidationException_WhenChangeHasEmptyId()
    {
        var userId = Guid.NewGuid().ToString();

        var change = new SyncChangeDto
        {
            Id = "",
            EntityType = EntityType.Workout,
            Action = ChangeAction.Create,
            Timestamp = DateTime.UtcNow,
            Data = SyncChangeBuilder.SerializeToElement(
                new { id = "w1", userId, title = "T", date = "2026-03-01", orderIndex = 0 })
        };

        await Assert.ThrowsAsync<ValidationException>(() =>
            HandlePushSync(userId, [change]));
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowValidationException_WhenEntityTypeIsInvalid()
    {
        var userId = Guid.NewGuid().ToString();

        var change = new SyncChangeDto
        {
            Id = Guid.NewGuid().ToString(),
            EntityType = (EntityType)999,
            Action = ChangeAction.Create,
            Timestamp = DateTime.UtcNow,
            Data = SyncChangeBuilder.SerializeToElement(
                new { id = "w1", userId, title = "T", date = "2026-03-01", orderIndex = 0 })
        };

        await Assert.ThrowsAsync<ValidationException>(() =>
            HandlePushSync(userId, [change]));
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowValidationException_WhenActionIsInvalid()
    {
        var userId = Guid.NewGuid().ToString();

        var change = new SyncChangeDto
        {
            Id = Guid.NewGuid().ToString(),
            EntityType = EntityType.Workout,
            Action = (ChangeAction)999,
            Timestamp = DateTime.UtcNow,
            Data = SyncChangeBuilder.SerializeToElement(
                new { id = "w1", userId, title = "T", date = "2026-03-01", orderIndex = 0 })
        };

        await Assert.ThrowsAsync<ValidationException>(() =>
            HandlePushSync(userId, [change]));
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowValidationException_WhenTimestampIsDefault()
    {
        var userId = Guid.NewGuid().ToString();

        var change = new SyncChangeDto
        {
            Id = Guid.NewGuid().ToString(),
            EntityType = EntityType.Workout,
            Action = ChangeAction.Create,
            Timestamp = default,
            Data = SyncChangeBuilder.SerializeToElement(
                new { id = "w1", userId, title = "T", date = "2026-03-01", orderIndex = 0 })
        };

        await Assert.ThrowsAsync<ValidationException>(() =>
            HandlePushSync(userId, [change]));
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowValidationException_WhenDataIsNull()
    {
        var userId = Guid.NewGuid().ToString();

        var change = new SyncChangeDto
        {
            Id = Guid.NewGuid().ToString(),
            EntityType = EntityType.Workout,
            Action = ChangeAction.Create,
            Timestamp = DateTime.UtcNow,
            Data = null
        };

        await Assert.ThrowsAsync<ValidationException>(() =>
            HandlePushSync(userId, [change]));
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowValidationException_WhenChangesExceedsMaximum()
    {
        var userId = Guid.NewGuid().ToString();

        var changes = Enumerable.Range(0, 101)
            .Select(_ => SyncChangeBuilder.WorkoutCreate(
                Guid.NewGuid().ToString(), userId))
            .ToList();

        await Assert.ThrowsAsync<ValidationException>(() =>
            HandlePushSync(userId, changes));
    }
}

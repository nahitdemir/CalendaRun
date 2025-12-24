using Calendarun.Settings.Client;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Planning.Application.Common;
using Planning.Application.Plans.Commands;
using Planning.Domain;
using Planning.Infrastructure;

namespace Planning.UnitTests.Plans.Commands;

public class CreatePlanHandlerTests : IAsyncLifetime
{
    private PlanningDbContext _dbContext = null!;
    private CreatePlanHandler _handler = null!;
    private Mock<ISettingsClient> _settingsClientMock = null!;
    private Mock<ILogger<CreatePlanHandler>> _loggerMock = null!;
    private Guid _tenantId;
    private Guid _userId;

    public Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<PlanningDbContext>()
            .UseInMemoryDatabase(databaseName: $"CreatePlanTest_{Guid.NewGuid()}")
            .Options;

        _dbContext = new PlanningDbContext(options);
        _settingsClientMock = new Mock<ISettingsClient>();
        _loggerMock = new Mock<ILogger<CreatePlanHandler>>();
        
        // Setup default settings
        _settingsClientMock
            .Setup(x => x.GetAsync<int?>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(100);
        _settingsClientMock
            .Setup(x => x.GetAsync<string>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Europe/Istanbul");

        _handler = new CreatePlanHandler(_dbContext, _settingsClientMock.Object, _loggerMock.Object);
        
        _tenantId = Guid.NewGuid();
        _userId = Guid.NewGuid();

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _dbContext.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task HandleAsync_NewUser_CreatesUserAndPlan()
    {
        // Arrange
        var command = new CreatePlanCommand(
            TenantId: _tenantId,
            UserId: _userId,
            UserEmail: "newuser@test.com",
            EventId: Guid.NewGuid(),
            TraceId: "trace-123"
        );

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TenantId.Should().Be(_tenantId);
        result.Value.UserId.Should().Be(_userId);
        result.Value.State.Should().Be("Active");
        result.Value.Timezone.Should().Be("Europe/Istanbul");

        // Verify user created
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == "newuser@test.com");
        user.Should().NotBeNull();
        user!.TenantId.Should().Be(_tenantId);
    }

    [Fact]
    public async Task HandleAsync_ExistingUser_UseExistingUser()
    {
        // Arrange - Create existing user
        var existingUserId = Guid.NewGuid();
        _dbContext.Users.Add(new User
        {
            Id = existingUserId,
            TenantId = _tenantId,
            Email = "existing@test.com",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        var command = new CreatePlanCommand(
            TenantId: _tenantId,
            UserId: Guid.NewGuid(), // Different ID
            UserEmail: "existing@test.com",
            EventId: Guid.NewGuid(),
            TraceId: null
        );

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.UserId.Should().Be(existingUserId); // Should use existing user ID

        // Verify no duplicate user
        var userCount = await _dbContext.Users.CountAsync(u => u.Email == "existing@test.com");
        userCount.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_DuplicatePlan_ReturnsConflict()
    {
        // Arrange - Create user and existing plan
        var eventId = Guid.NewGuid();
        _dbContext.Users.Add(new User
        {
            Id = _userId,
            TenantId = _tenantId,
            Email = "user@test.com",
            CreatedAt = DateTimeOffset.UtcNow
        });
        _dbContext.UserPlanItems.Add(new UserPlanItem
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            UserId = _userId,
            EventId = eventId,
            State = "Active",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        var command = new CreatePlanCommand(
            TenantId: _tenantId,
            UserId: _userId,
            UserEmail: "user@test.com",
            EventId: eventId, // Same event
            TraceId: null
        );

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Should().Contain("already planned");
    }

    [Fact]
    public async Task HandleAsync_MaxPlansReached_ReturnsFailure()
    {
        // Arrange - Set max plans to 1
        _settingsClientMock
            .Setup(x => x.GetAsync<int?>("planning.max_plans_per_user", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Create user with 1 existing plan
        _dbContext.Users.Add(new User
        {
            Id = _userId,
            TenantId = _tenantId,
            Email = "maxed@test.com",
            CreatedAt = DateTimeOffset.UtcNow
        });
        _dbContext.UserPlanItems.Add(new UserPlanItem
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            UserId = _userId,
            EventId = Guid.NewGuid(),
            State = "Active",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        var command = new CreatePlanCommand(
            TenantId: _tenantId,
            UserId: _userId,
            UserEmail: "maxed@test.com",
            EventId: Guid.NewGuid(),
            TraceId: null
        );

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Maximum plan limit");
    }

    [Fact]
    public async Task HandleAsync_CreatesOutboxMessage()
    {
        // Arrange
        var command = new CreatePlanCommand(
            TenantId: _tenantId,
            UserId: _userId,
            UserEmail: "outbox@test.com",
            EventId: Guid.NewGuid(),
            TraceId: null
        );

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var outboxMessage = await _dbContext.OutboxMessages.FirstOrDefaultAsync();
        outboxMessage.Should().NotBeNull();
        outboxMessage!.Type.Should().Be("PlanningUserPlannedV1");
    }

    [Fact]
    public async Task HandleAsync_CreatesAuditLog()
    {
        // Arrange
        var command = new CreatePlanCommand(
            TenantId: _tenantId,
            UserId: _userId,
            UserEmail: "audit@test.com",
            EventId: Guid.NewGuid(),
            TraceId: "trace-audit"
        );

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var auditLog = await _dbContext.AuditLogs
            .FirstOrDefaultAsync(a => a.EntityId == result.Value!.PlanItemId);

        auditLog.Should().NotBeNull();
        auditLog!.Action.Should().Be("PlanCreated");
        auditLog.Entity.Should().Be("UserPlanItem");
        auditLog.TenantId.Should().Be(_tenantId);
        auditLog.TraceId.Should().Be("trace-audit");
    }
}


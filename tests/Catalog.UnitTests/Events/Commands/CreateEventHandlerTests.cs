using Catalog.Application.Common;
using Catalog.Application.Events.Commands;
using Catalog.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Catalog.UnitTests.Events.Commands;

public class CreateEventHandlerTests : IAsyncLifetime
{
    private CatalogDbContext _dbContext = null!;
    private CreateEventHandler _handler = null!;
    private Mock<ILogger<CreateEventHandler>> _loggerMock = null!;

    public Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName: $"CreateEventTest_{Guid.NewGuid()}")
            .Options;

        _dbContext = new CatalogDbContext(options);
        _loggerMock = new Mock<ILogger<CreateEventHandler>>();
        _handler = new CreateEventHandler(_dbContext, _loggerMock.Object);

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _dbContext.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_CreatesEvent()
    {
        // Arrange
        var command = new CreateEventCommand(
            TenantId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            Title: "Test Event",
            Description: "Test Description",
            StartAt: DateTimeOffset.UtcNow.AddDays(30),
            City: "Istanbul",
            CountryCode: "TR",
            RegistrationUrl: "https://example.com",
            IsGlobal: false,
            IsSuperAdmin: false,
            TraceId: "trace-123"
        );

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Title.Should().Be("Test Event");
        result.Value.TenantId.Should().Be(command.TenantId);

        // Verify in DB
        var eventEntity = await _dbContext.Events.FirstOrDefaultAsync(e => e.Id == result.Value.Id);
        eventEntity.Should().NotBeNull();
    }

    [Fact]
    public async Task HandleAsync_GlobalEventByNonSuperAdmin_UsesTenantId()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var command = new CreateEventCommand(
            TenantId: tenantId,
            UserId: Guid.NewGuid(),
            Title: "Global Event Attempt",
            Description: null,
            StartAt: DateTimeOffset.UtcNow.AddDays(30),
            City: "Istanbul",
            CountryCode: "TR",
            RegistrationUrl: null,
            IsGlobal: true, // Trying to create global
            IsSuperAdmin: false, // But not super admin
            TraceId: null
        );

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TenantId.Should().Be(tenantId); // Should use tenant ID, not null
    }

    [Fact]
    public async Task HandleAsync_GlobalEventBySuperAdmin_SetsNullTenantId()
    {
        // Arrange
        var command = new CreateEventCommand(
            TenantId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            Title: "Global Event",
            Description: null,
            StartAt: DateTimeOffset.UtcNow.AddDays(30),
            City: "Istanbul",
            CountryCode: "TR",
            RegistrationUrl: null,
            IsGlobal: true,
            IsSuperAdmin: true,
            TraceId: null
        );

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TenantId.Should().BeNull(); // Global event
    }

    [Fact]
    public async Task HandleAsync_CreatesAuditLog()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new CreateEventCommand(
            TenantId: Guid.NewGuid(),
            UserId: userId,
            Title: "Audited Event",
            Description: null,
            StartAt: DateTimeOffset.UtcNow.AddDays(30),
            City: "Ankara",
            CountryCode: "TR",
            RegistrationUrl: null,
            IsGlobal: false,
            IsSuperAdmin: false,
            TraceId: "trace-audit"
        );

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var auditLog = await _dbContext.AuditLogs
            .FirstOrDefaultAsync(a => a.EntityId == result.Value!.Id);

        auditLog.Should().NotBeNull();
        auditLog!.Action.Should().Be("EventCreated");
        auditLog.Entity.Should().Be("Event");
        auditLog.ActorUserId.Should().Be(userId);
        auditLog.TraceId.Should().Be("trace-audit");
    }
}


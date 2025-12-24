using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Platform.Application.Common;
using Platform.Application.Tenants.Commands;
using Platform.Domain.Entities;
using Platform.Infrastructure.Data;

namespace Platform.UnitTests.Tenants.Commands;

public class CreateTenantHandlerTests : IAsyncLifetime
{
    private PlatformDbContext _dbContext = null!;
    private CreateTenantHandler _handler = null!;
    private Mock<ILogger<CreateTenantHandler>> _loggerMock = null!;

    public Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(databaseName: $"CreateTenantTest_{Guid.NewGuid()}")
            .Options;

        _dbContext = new PlatformDbContext(options);
        _loggerMock = new Mock<ILogger<CreateTenantHandler>>();
        _handler = new CreateTenantHandler(_dbContext, _loggerMock.Object);

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _dbContext.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_CreatesTenant()
    {
        // Arrange
        var command = new CreateTenantCommand(
            Name: "Test Tenant",
            Slug: null,
            Description: "Test Description",
            DefaultLanguage: "en",
            DefaultCurrency: "USD",
            ActorUserId: Guid.NewGuid(),
            ActorEmail: "test@test.com"
        );

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Test Tenant");
        result.Value.Slug.Should().Be("test-tenant");
        result.Value.DefaultLanguage.Should().Be("en");
        result.Value.DefaultCurrency.Should().Be("USD");

        // Verify in DB
        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == result.Value.Id);
        tenant.Should().NotBeNull();
    }

    [Fact]
    public async Task HandleAsync_CustomSlug_UsesProvidedSlug()
    {
        // Arrange
        var command = new CreateTenantCommand(
            Name: "Test Tenant",
            Slug: "custom-slug",
            Description: null,
            DefaultLanguage: null,
            DefaultCurrency: null,
            ActorUserId: Guid.NewGuid(),
            ActorEmail: "test@test.com"
        );

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Slug.Should().Be("custom-slug");
    }

    [Fact]
    public async Task HandleAsync_DuplicateSlug_ReturnsConflict()
    {
        // Arrange - Create existing tenant
        _dbContext.Tenants.Add(new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Existing Tenant",
            Slug = "existing-tenant",
            Status = TenantStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = Guid.NewGuid()
        });
        await _dbContext.SaveChangesAsync();

        var command = new CreateTenantCommand(
            Name: "Existing Tenant", // Will generate same slug
            Slug: null,
            Description: null,
            DefaultLanguage: null,
            DefaultCurrency: null,
            ActorUserId: Guid.NewGuid(),
            ActorEmail: "test@test.com"
        );

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Should().Contain("slug already exists");
    }

    [Fact]
    public async Task HandleAsync_DefaultLanguageAndCurrency_UsesDefaults()
    {
        // Arrange
        var command = new CreateTenantCommand(
            Name: "Test Tenant",
            Slug: null,
            Description: null,
            DefaultLanguage: null,
            DefaultCurrency: null,
            ActorUserId: Guid.NewGuid(),
            ActorEmail: "test@test.com"
        );

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.DefaultLanguage.Should().Be("tr");
        result.Value.DefaultCurrency.Should().Be("TRY");
    }

    [Fact]
    public async Task HandleAsync_CreatesAuditLog()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var command = new CreateTenantCommand(
            Name: "Test Tenant",
            Slug: null,
            Description: null,
            DefaultLanguage: null,
            DefaultCurrency: null,
            ActorUserId: actorUserId,
            ActorEmail: "admin@test.com"
        );

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var auditLog = await _dbContext.AuditLogs
            .FirstOrDefaultAsync(a => a.EntityId == result.Value!.Id);

        auditLog.Should().NotBeNull();
        auditLog!.Action.Should().Be("TenantCreated");
        auditLog.EntityType.Should().Be("Tenant");
        auditLog.ActorUserId.Should().Be(actorUserId);
        auditLog.ActorEmail.Should().Be("admin@test.com");
    }
}


using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Platform.Application.Common;
using Platform.Application.Invites.Commands;
using Platform.Domain.Entities;
using Platform.Infrastructure.Data;

namespace Platform.UnitTests.Invites.Commands;

public class CreateInviteHandlerTests : IAsyncLifetime
{
    private PlatformDbContext _dbContext = null!;
    private CreateInviteHandler _handler = null!;
    private Mock<ILogger<CreateInviteHandler>> _loggerMock = null!;
    private Guid _tenantId;
    private Guid _actorUserId;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(databaseName: $"CreateInviteTest_{Guid.NewGuid()}")
            .Options;

        _dbContext = new PlatformDbContext(options);
        _loggerMock = new Mock<ILogger<CreateInviteHandler>>();
        _handler = new CreateInviteHandler(_dbContext, _loggerMock.Object);

        _tenantId = Guid.NewGuid();
        _actorUserId = Guid.NewGuid();

        // Setup test tenant
        _dbContext.Tenants.Add(new Tenant
        {
            Id = _tenantId,
            Name = "Test Tenant",
            Slug = "test-tenant",
            Status = TenantStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = _actorUserId
        });
        await _dbContext.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        _dbContext.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_CreatesInvite()
    {
        // Arrange
        var command = new CreateInviteCommand(
            TenantId: _tenantId,
            Email: "newuser@test.com",
            Role: TenantRole.TenantUser,
            ActorUserId: _actorUserId,
            ActorEmail: "admin@test.com"
        );

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Email.Should().Be("newuser@test.com");
        result.Value.Role.Should().Be("TenantUser");
        result.Value.Token.Should().NotBeNullOrEmpty();
        result.Value.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);

        // Verify in DB
        var invite = await _dbContext.Invites.FirstOrDefaultAsync(i => i.Id == result.Value.Id);
        invite.Should().NotBeNull();
        invite!.Status.Should().Be(InviteStatus.Pending);
    }

    [Fact]
    public async Task HandleAsync_DuplicatePendingInvite_ReturnsConflict()
    {
        // Arrange - Create existing pending invite
        _dbContext.Invites.Add(new Invite
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Email = "existing@test.com",
            Role = TenantRole.TenantUser,
            Token = "existing-token",
            Status = InviteStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = _actorUserId,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        });
        await _dbContext.SaveChangesAsync();

        var command = new CreateInviteCommand(
            TenantId: _tenantId,
            Email: "existing@test.com",
            Role: TenantRole.TenantAdmin,
            ActorUserId: _actorUserId,
            ActorEmail: "admin@test.com"
        );

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Should().Contain("already pending");
    }

    [Fact]
    public async Task HandleAsync_ExistingMember_ReturnsConflict()
    {
        // Arrange - Create existing membership
        _dbContext.Memberships.Add(new Membership
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            UserId = Guid.NewGuid(),
            UserEmail = "member@test.com",
            Role = TenantRole.TenantUser,
            Status = MembershipStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        var command = new CreateInviteCommand(
            TenantId: _tenantId,
            Email: "member@test.com",
            Role: TenantRole.TenantAdmin,
            ActorUserId: _actorUserId,
            ActorEmail: "admin@test.com"
        );

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Should().Contain("already a member");
    }

    [Fact]
    public async Task HandleAsync_TenantAdminRole_CreatesAdminInvite()
    {
        // Arrange
        var command = new CreateInviteCommand(
            TenantId: _tenantId,
            Email: "admin@test.com",
            Role: TenantRole.TenantAdmin,
            ActorUserId: _actorUserId,
            ActorEmail: "superadmin@test.com"
        );

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Role.Should().Be("TenantAdmin");
    }

    [Fact]
    public async Task HandleAsync_CreatesAuditLog()
    {
        // Arrange
        var command = new CreateInviteCommand(
            TenantId: _tenantId,
            Email: "audit@test.com",
            Role: TenantRole.TenantUser,
            ActorUserId: _actorUserId,
            ActorEmail: "admin@test.com"
        );

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var auditLog = await _dbContext.AuditLogs
            .FirstOrDefaultAsync(a => a.EntityId == result.Value!.Id);

        auditLog.Should().NotBeNull();
        auditLog!.Action.Should().Be("InviteCreated");
        auditLog.EntityType.Should().Be("Invite");
        auditLog.TenantId.Should().Be(_tenantId);
        auditLog.ActorEmail.Should().Be("admin@test.com");
    }
}


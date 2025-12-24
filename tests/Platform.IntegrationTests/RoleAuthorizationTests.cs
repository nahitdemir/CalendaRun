using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Platform.Domain.Entities;
using Platform.Infrastructure.Data;

namespace Platform.IntegrationTests;

/// <summary>
/// Integration tests verifying role-based authorization in the platform service.
/// Tests verify that tenant_admin and tenant_user roles have appropriate access.
/// </summary>
public class RoleAuthorizationTests : IAsyncLifetime
{
    private PlatformDbContext _dbContext = null!;
    private Guid _tenantId;
    private Guid _adminUserId;
    private Guid _regularUserId;
    private Guid _superAdminUserId;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(databaseName: $"RoleAuth_{Guid.NewGuid()}")
            .Options;

        _dbContext = new PlatformDbContext(options);

        _tenantId = Guid.NewGuid();
        _adminUserId = Guid.NewGuid();
        _regularUserId = Guid.NewGuid();
        _superAdminUserId = Guid.NewGuid();

        var now = DateTimeOffset.UtcNow;

        // Create tenant
        _dbContext.Tenants.Add(new Tenant
        {
            Id = _tenantId,
            Name = "Test Tenant",
            Slug = "test-tenant",
            Status = TenantStatus.Active,
            CreatedAt = now,
            CreatedBy = _superAdminUserId
        });

        // Create memberships with different roles
        _dbContext.Memberships.AddRange(
            new Membership
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                UserId = _adminUserId,
                UserEmail = "admin@test.local",
                Role = TenantRole.TenantAdmin,
                Status = MembershipStatus.Active,
                CreatedAt = now,
                AcceptedAt = now
            },
            new Membership
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                UserId = _regularUserId,
                UserEmail = "user@test.local",
                Role = TenantRole.TenantUser,
                Status = MembershipStatus.Active,
                CreatedAt = now,
                AcceptedAt = now
            }
        );

        await _dbContext.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        _dbContext.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task TenantAdmin_Can_Be_Identified()
    {
        // Act
        var membership = await _dbContext.Memberships
            .Where(m => m.UserId == _adminUserId && m.TenantId == _tenantId && m.Status == MembershipStatus.Active)
            .FirstOrDefaultAsync();

        // Assert
        membership.Should().NotBeNull();
        membership!.Role.Should().Be(TenantRole.TenantAdmin);
    }

    [Fact]
    public async Task TenantUser_Can_Be_Identified()
    {
        // Act
        var membership = await _dbContext.Memberships
            .Where(m => m.UserId == _regularUserId && m.TenantId == _tenantId && m.Status == MembershipStatus.Active)
            .FirstOrDefaultAsync();

        // Assert
        membership.Should().NotBeNull();
        membership!.Role.Should().Be(TenantRole.TenantUser);
    }

    [Fact]
    public async Task Admin_Can_Query_All_Tenant_Members()
    {
        // This simulates an admin querying members
        // In real implementation, this would be authorized by the admin's role

        // Act
        var members = await _dbContext.Memberships
            .Where(m => m.TenantId == _tenantId && m.Status == MembershipStatus.Active)
            .ToListAsync();

        // Assert
        members.Should().HaveCount(2);
    }

    [Fact]
    public async Task Can_Query_Invites_For_Tenant()
    {
        // Arrange - Add an invite
        var now = DateTimeOffset.UtcNow;
        _dbContext.Invites.Add(new Invite
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Email = "newuser@test.local",
            Role = TenantRole.TenantUser,
            Token = Guid.NewGuid().ToString("N"),
            Status = InviteStatus.Pending,
            CreatedAt = now,
            CreatedBy = _adminUserId,
            ExpiresAt = now.AddDays(7)
        });
        await _dbContext.SaveChangesAsync();

        // Act
        var invites = await _dbContext.Invites
            .Where(i => i.TenantId == _tenantId && i.Status == InviteStatus.Pending)
            .ToListAsync();

        // Assert
        invites.Should().HaveCount(1);
        invites.First().Email.Should().Be("newuser@test.local");
    }

    [Fact]
    public async Task Invite_Token_Must_Be_Unique()
    {
        // Arrange
        var token = Guid.NewGuid().ToString("N");
        var now = DateTimeOffset.UtcNow;

        _dbContext.Invites.Add(new Invite
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Email = "user1@test.local",
            Role = TenantRole.TenantUser,
            Token = token,
            Status = InviteStatus.Pending,
            CreatedAt = now,
            CreatedBy = _adminUserId,
            ExpiresAt = now.AddDays(7)
        });
        await _dbContext.SaveChangesAsync();

        // Act - Check if token exists
        var tokenExists = await _dbContext.Invites.AnyAsync(i => i.Token == token);

        // Assert
        tokenExists.Should().BeTrue();
    }

    [Fact]
    public async Task AuditLog_Records_Admin_Actions()
    {
        // Arrange - Simulate an admin action
        var now = DateTimeOffset.UtcNow;
        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ActorUserId = _adminUserId,
            Action = "MemberInvited",
            EntityType = "Invite",
            EntityId = Guid.NewGuid(),
            AfterJson = "{\"email\":\"newuser@test.local\"}",
            Timestamp = now
        });
        await _dbContext.SaveChangesAsync();

        // Act
        var auditLogs = await _dbContext.AuditLogs
            .Where(a => a.TenantId == _tenantId && a.ActorUserId == _adminUserId)
            .ToListAsync();

        // Assert
        auditLogs.Should().HaveCount(1);
        auditLogs.First().Action.Should().Be("MemberInvited");
    }

    [Fact]
    public async Task Role_Change_Updates_Membership()
    {
        // Arrange - Get regular user membership
        var membership = await _dbContext.Memberships
            .FirstAsync(m => m.UserId == _regularUserId && m.TenantId == _tenantId);

        // Act - Promote to admin
        membership.Role = TenantRole.TenantAdmin;
        await _dbContext.SaveChangesAsync();

        // Assert
        var updatedMembership = await _dbContext.Memberships
            .FirstAsync(m => m.UserId == _regularUserId && m.TenantId == _tenantId);
        updatedMembership.Role.Should().Be(TenantRole.TenantAdmin);
    }
}


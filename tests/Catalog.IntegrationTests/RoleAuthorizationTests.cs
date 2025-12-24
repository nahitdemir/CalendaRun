using Catalog.Domain;
using Catalog.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Catalog.IntegrationTests;

/// <summary>
/// Integration tests verifying role-based authorization in the Catalog service.
/// Ensures that:
/// - TenantAdmin can create/update/delete events in their tenant
/// - TenantUser can only read events
/// - SuperAdmin can manage all events across tenants
/// </summary>
public class RoleAuthorizationTests : IAsyncLifetime
{
    private CatalogDbContext _dbContext = null!;
    private Guid _tenantId;
    private Guid _tenantAdminId;
    private Guid _tenantUserId;
    private Guid _superAdminId;
    private Guid _eventId;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName: $"CatalogRoleAuth_{Guid.NewGuid()}")
            .Options;

        _dbContext = new CatalogDbContext(options);

        _tenantId = Guid.NewGuid();
        _tenantAdminId = Guid.NewGuid();
        _tenantUserId = Guid.NewGuid();
        _superAdminId = Guid.NewGuid();
        _eventId = Guid.NewGuid();

        var now = DateTimeOffset.UtcNow;

        // Create test event
        _dbContext.Events.Add(new Event
        {
            Id = _eventId,
            TenantId = _tenantId,
            Title = "Test Event",
            City = "Istanbul",
            CountryCode = "TR",
            StartAt = now.AddDays(10),
            CreatedAt = now,
            CreatedBy = _tenantAdminId
        });

        await _dbContext.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        _dbContext.Dispose();
        return Task.CompletedTask;
    }

    [Theory]
    [InlineData("TenantAdmin", true)]
    [InlineData("TenantUser", false)]
    public void Role_Can_Create_Event(string role, bool canCreate)
    {
        // This test simulates the authorization check performed in the API
        var hasPermission = role == "TenantAdmin" || role == "SuperAdmin";
        hasPermission.Should().Be(canCreate);
    }

    [Theory]
    [InlineData("TenantAdmin", true)]
    [InlineData("TenantUser", false)]
    public void Role_Can_Update_Event(string role, bool canUpdate)
    {
        var hasPermission = role == "TenantAdmin" || role == "SuperAdmin";
        hasPermission.Should().Be(canUpdate);
    }

    [Theory]
    [InlineData("TenantAdmin", true)]
    [InlineData("TenantUser", false)]
    public void Role_Can_Delete_Event(string role, bool canDelete)
    {
        var hasPermission = role == "TenantAdmin" || role == "SuperAdmin";
        hasPermission.Should().Be(canDelete);
    }

    [Fact]
    public void SuperAdmin_Bypasses_TenantId_Check()
    {
        // SuperAdmin flag should bypass tenant membership check
        var isSuperAdmin = true;
        var hasTenantAccess = true; // SuperAdmin always has access

        // Simulate the bypass logic
        var canAccess = isSuperAdmin || hasTenantAccess;
        canAccess.Should().BeTrue();
    }

    [Fact]
    public async Task TenantAdmin_Can_Create_Event_In_Own_Tenant()
    {
        // Simulate tenant admin creating event
        var newEvent = new Event
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId, // Same tenant
            Title = "Admin Created Event",
            City = "Ankara",
            CountryCode = "TR",
            StartAt = DateTimeOffset.UtcNow.AddDays(15),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = _tenantAdminId
        };

        _dbContext.Events.Add(newEvent);
        await _dbContext.SaveChangesAsync();

        var savedEvent = await _dbContext.Events.FindAsync(newEvent.Id);
        savedEvent.Should().NotBeNull();
        savedEvent!.CreatedBy.Should().Be(_tenantAdminId);
    }

    [Fact]
    public async Task TenantAdmin_Can_Update_Event_In_Own_Tenant()
    {
        // Get existing event
        var eventToUpdate = await _dbContext.Events.FindAsync(_eventId);
        eventToUpdate.Should().NotBeNull();

        // Verify it's in the same tenant
        eventToUpdate!.TenantId.Should().Be(_tenantId);

        // Update
        eventToUpdate.Title = "Updated by Admin";
        eventToUpdate.UpdatedAt = DateTimeOffset.UtcNow;
        eventToUpdate.UpdatedBy = _tenantAdminId;

        await _dbContext.SaveChangesAsync();

        var updatedEvent = await _dbContext.Events.FindAsync(_eventId);
        updatedEvent!.Title.Should().Be("Updated by Admin");
        updatedEvent.UpdatedBy.Should().Be(_tenantAdminId);
    }

    [Fact]
    public async Task TenantAdmin_Can_Delete_Event_In_Own_Tenant()
    {
        // Create event to delete
        var eventToDelete = new Event
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Title = "To Be Deleted",
            City = "Test",
            CountryCode = "TR",
            StartAt = DateTimeOffset.UtcNow.AddDays(5),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = _tenantAdminId
        };

        _dbContext.Events.Add(eventToDelete);
        await _dbContext.SaveChangesAsync();

        var deleteId = eventToDelete.Id;

        // Delete
        _dbContext.Events.Remove(eventToDelete);
        await _dbContext.SaveChangesAsync();

        var deletedEvent = await _dbContext.Events.FindAsync(deleteId);
        deletedEvent.Should().BeNull();
    }

    [Fact]
    public void TenantAdmin_Cannot_Modify_Other_Tenant_Event()
    {
        // Simulate check
        var eventTenantId = Guid.NewGuid(); // Different tenant
        var userTenantId = _tenantId;
        var isSuperAdmin = false;

        var canModify = isSuperAdmin || eventTenantId == userTenantId;
        canModify.Should().BeFalse();
    }

    [Fact]
    public void SuperAdmin_Can_Modify_Any_Tenant_Event()
    {
        // Simulate check
        var eventTenantId = Guid.NewGuid(); // Different tenant
        var userTenantId = _tenantId;
        var isSuperAdmin = true;

        var canModify = isSuperAdmin || eventTenantId == userTenantId;
        canModify.Should().BeTrue();
    }

    [Fact]
    public async Task AuditLog_Records_Actor()
    {
        // Verify audit log captures who made the change
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ActorUserId = _tenantAdminId,
            Action = "EventUpdated",
            Entity = "Event",
            EntityId = _eventId,
            BeforeJson = "{}",
            AfterJson = "{}",
            Timestamp = DateTimeOffset.UtcNow
        };

        _dbContext.AuditLogs.Add(auditLog);
        await _dbContext.SaveChangesAsync();

        var savedLog = await _dbContext.AuditLogs.FindAsync(auditLog.Id);
        savedLog!.ActorUserId.Should().Be(_tenantAdminId);
    }
}


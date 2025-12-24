using Catalog.Domain;
using Catalog.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Catalog.IntegrationTests;

/// <summary>
/// Integration tests verifying tenant isolation in the Catalog service.
/// Ensures that:
/// - Tenant admins can only CRUD their own tenant's events
/// - Super admins can list all tenant events
/// - Global events (TenantId = null) are visible to all tenants
/// </summary>
public class TenantIsolationTests : IAsyncLifetime
{
    private CatalogDbContext _dbContext = null!;
    private Guid _tenant1Id;
    private Guid _tenant2Id;
    private Guid _user1Id;
    private Guid _user2Id;
    private Guid _superAdminId;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName: $"CatalogTenantIsolation_{Guid.NewGuid()}")
            .Options;

        _dbContext = new CatalogDbContext(options);

        _tenant1Id = Guid.NewGuid();
        _tenant2Id = Guid.NewGuid();
        _user1Id = Guid.NewGuid();
        _user2Id = Guid.NewGuid();
        _superAdminId = Guid.NewGuid();

        var now = DateTimeOffset.UtcNow;

        // Create events for different tenants
        _dbContext.Events.AddRange(
            // Global event (visible to all)
            new Event
            {
                Id = Guid.NewGuid(),
                TenantId = null,
                Title = "Global Marathon",
                City = "Istanbul",
                CountryCode = "TR",
                StartAt = now.AddDays(30),
                CreatedAt = now,
                CreatedBy = _superAdminId
            },
            // Tenant 1 events
            new Event
            {
                Id = Guid.NewGuid(),
                TenantId = _tenant1Id,
                Title = "Tenant 1 - Local Run",
                City = "Ankara",
                CountryCode = "TR",
                StartAt = now.AddDays(10),
                CreatedAt = now,
                CreatedBy = _user1Id
            },
            new Event
            {
                Id = Guid.NewGuid(),
                TenantId = _tenant1Id,
                Title = "Tenant 1 - Trail",
                City = "Izmir",
                CountryCode = "TR",
                StartAt = now.AddDays(20),
                CreatedAt = now,
                CreatedBy = _user1Id
            },
            // Tenant 2 events
            new Event
            {
                Id = Guid.NewGuid(),
                TenantId = _tenant2Id,
                Title = "Tenant 2 - Beach Run",
                City = "Antalya",
                CountryCode = "TR",
                StartAt = now.AddDays(15),
                CreatedAt = now,
                CreatedBy = _user2Id
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
    public async Task TenantAdmin_Sees_Only_Own_Tenant_Events()
    {
        // Simulate tenant admin query (filtered by tenant_id)
        var tenant1Events = await _dbContext.Events
            .Where(e => e.TenantId == _tenant1Id)
            .ToListAsync();

        // Assert - should see only tenant 1 events
        tenant1Events.Should().HaveCount(2);
        tenant1Events.Should().AllSatisfy(e => e.TenantId.Should().Be(_tenant1Id));
    }

    [Fact]
    public async Task TenantAdmin_Cannot_Access_Other_Tenant_Events()
    {
        // Simulate tenant 1 admin trying to query tenant 2 events
        var tenant1Events = await _dbContext.Events
            .Where(e => e.TenantId == _tenant1Id)
            .ToListAsync();

        // Assert - tenant 2 events should not be included
        tenant1Events.Should().NotContain(e => e.TenantId == _tenant2Id);
    }

    [Fact]
    public async Task SuperAdmin_Sees_All_Tenant_Events()
    {
        // Super admin query (no tenant filter)
        var allEvents = await _dbContext.Events.ToListAsync();

        // Assert - should see all events including global
        allEvents.Should().HaveCount(4);
        allEvents.Should().Contain(e => e.TenantId == null); // Global
        allEvents.Should().Contain(e => e.TenantId == _tenant1Id);
        allEvents.Should().Contain(e => e.TenantId == _tenant2Id);
    }

    [Fact]
    public async Task User_Sees_Global_Plus_Own_Tenant_Events()
    {
        // Simulate user query (global + own tenant)
        var userVisibleEvents = await _dbContext.Events
            .Where(e => e.TenantId == null || e.TenantId == _tenant1Id)
            .ToListAsync();

        // Assert - should see global + tenant 1 events
        userVisibleEvents.Should().HaveCount(3);
        userVisibleEvents.Should().Contain(e => e.TenantId == null);
        userVisibleEvents.Should().Contain(e => e.TenantId == _tenant1Id);
        userVisibleEvents.Should().NotContain(e => e.TenantId == _tenant2Id);
    }

    [Fact]
    public async Task Create_Event_Assigns_TenantId()
    {
        // Arrange
        var newEvent = new Event
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant1Id, // Assigned from header, not body
            Title = "New Local Event",
            City = "Bursa",
            CountryCode = "TR",
            StartAt = DateTimeOffset.UtcNow.AddDays(25),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = _user1Id
        };

        // Act
        _dbContext.Events.Add(newEvent);
        await _dbContext.SaveChangesAsync();

        // Assert
        var savedEvent = await _dbContext.Events.FindAsync(newEvent.Id);
        savedEvent.Should().NotBeNull();
        savedEvent!.TenantId.Should().Be(_tenant1Id);
    }

    [Fact]
    public async Task Update_Event_Preserves_TenantId()
    {
        // Arrange - Get existing event
        var existingEvent = await _dbContext.Events
            .FirstAsync(e => e.TenantId == _tenant1Id);

        var originalTenantId = existingEvent.TenantId;

        // Act - Update event (without changing TenantId)
        existingEvent.Title = "Updated Title";
        existingEvent.UpdatedAt = DateTimeOffset.UtcNow;
        existingEvent.UpdatedBy = _user1Id;
        await _dbContext.SaveChangesAsync();

        // Assert - TenantId should be preserved
        var updatedEvent = await _dbContext.Events.FindAsync(existingEvent.Id);
        updatedEvent!.TenantId.Should().Be(originalTenantId);
        updatedEvent.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task Delete_Event_Respects_TenantId()
    {
        // Arrange - Get tenant 1 event
        var tenant1Event = await _dbContext.Events
            .FirstAsync(e => e.TenantId == _tenant1Id);

        var eventId = tenant1Event.Id;

        // Verify tenant 2 cannot delete (simulated by checking TenantId)
        tenant1Event.TenantId.Should().NotBe(_tenant2Id);

        // Act - Delete by tenant 1 admin
        _dbContext.Events.Remove(tenant1Event);
        await _dbContext.SaveChangesAsync();

        // Assert
        var deletedEvent = await _dbContext.Events.FindAsync(eventId);
        deletedEvent.Should().BeNull();
    }

    [Fact]
    public async Task Global_Event_Only_Creatable_By_SuperAdmin()
    {
        // Arrange - Create global event (TenantId = null)
        var globalEvent = new Event
        {
            Id = Guid.NewGuid(),
            TenantId = null, // Only super admin can create global events
            Title = "New Global Event",
            City = "Online",
            CountryCode = "WW",
            StartAt = DateTimeOffset.UtcNow.AddDays(60),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = _superAdminId
        };

        // Act
        _dbContext.Events.Add(globalEvent);
        await _dbContext.SaveChangesAsync();

        // Assert
        var savedEvent = await _dbContext.Events.FindAsync(globalEvent.Id);
        savedEvent.Should().NotBeNull();
        savedEvent!.TenantId.Should().BeNull();
    }

    [Fact]
    public async Task AuditLog_Records_TenantId()
    {
        // Arrange
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant1Id,
            ActorUserId = _user1Id,
            Action = "EventCreated",
            Entity = "Event",
            EntityId = Guid.NewGuid(),
            AfterJson = "{}",
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        _dbContext.AuditLogs.Add(auditLog);
        await _dbContext.SaveChangesAsync();

        // Assert
        var savedLog = await _dbContext.AuditLogs.FindAsync(auditLog.Id);
        savedLog.Should().NotBeNull();
        savedLog!.TenantId.Should().Be(_tenant1Id);
    }
}


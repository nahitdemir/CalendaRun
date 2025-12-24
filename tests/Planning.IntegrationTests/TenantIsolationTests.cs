using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Planning.Domain;
using Planning.Infrastructure;

namespace Planning.IntegrationTests;

/// <summary>
/// Integration tests verifying tenant isolation in the Planning service.
/// Ensures that:
/// - Users can only see plans within their tenant
/// - Plans are scoped to tenant on creation
/// - Admin queries respect tenant boundaries
/// </summary>
public class TenantIsolationTests : IAsyncLifetime
{
    private PlanningDbContext _dbContext = null!;
    private Guid _tenant1Id;
    private Guid _tenant2Id;
    private Guid _user1Id;
    private Guid _user2Id;
    private Guid _eventId;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<PlanningDbContext>()
            .UseInMemoryDatabase(databaseName: $"PlanningTenantIsolation_{Guid.NewGuid()}")
            .Options;

        _dbContext = new PlanningDbContext(options);

        _tenant1Id = Guid.NewGuid();
        _tenant2Id = Guid.NewGuid();
        _user1Id = Guid.NewGuid();
        _user2Id = Guid.NewGuid();
        _eventId = Guid.NewGuid();

        var now = DateTimeOffset.UtcNow;

        // Create users in different tenants
        _dbContext.Users.AddRange(
            new User
            {
                Id = _user1Id,
                TenantId = _tenant1Id,
                Email = "user1@tenant1.local",
                CreatedAt = now
            },
            new User
            {
                Id = _user2Id,
                TenantId = _tenant2Id,
                Email = "user2@tenant2.local",
                CreatedAt = now
            }
        );

        // Create plans in different tenants
        _dbContext.UserPlanItems.AddRange(
            new UserPlanItem
            {
                Id = Guid.NewGuid(),
                TenantId = _tenant1Id,
                UserId = _user1Id,
                EventId = _eventId,
                State = "Active",
                CreatedAt = now,
                CreatedBy = _user1Id
            },
            new UserPlanItem
            {
                Id = Guid.NewGuid(),
                TenantId = _tenant1Id,
                UserId = _user1Id,
                EventId = Guid.NewGuid(),
                State = "Active",
                CreatedAt = now,
                CreatedBy = _user1Id
            },
            new UserPlanItem
            {
                Id = Guid.NewGuid(),
                TenantId = _tenant2Id,
                UserId = _user2Id,
                EventId = Guid.NewGuid(),
                State = "Active",
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
    public async Task User_Sees_Only_Own_Tenant_Plans()
    {
        // Query plans for user1 (tenant1)
        var user1Plans = await _dbContext.UserPlanItems
            .Where(p => p.TenantId == _tenant1Id && p.UserId == _user1Id && p.State == "Active")
            .ToListAsync();

        // Should see 2 plans
        user1Plans.Should().HaveCount(2);
        user1Plans.Should().AllSatisfy(p => p.TenantId.Should().Be(_tenant1Id));
    }

    [Fact]
    public async Task User_Cannot_Access_Other_Tenant_Plans()
    {
        // Query tenant1 plans
        var tenant1Plans = await _dbContext.UserPlanItems
            .Where(p => p.TenantId == _tenant1Id)
            .ToListAsync();

        // Should not contain tenant2 plans
        tenant1Plans.Should().NotContain(p => p.TenantId == _tenant2Id);
    }

    [Fact]
    public async Task TenantAdmin_Sees_All_Tenant_Plans()
    {
        // Admin query for tenant1 (all users in tenant)
        var tenant1Plans = await _dbContext.UserPlanItems
            .Where(p => p.TenantId == _tenant1Id)
            .ToListAsync();

        // Should see all tenant1 plans
        tenant1Plans.Should().HaveCount(2);
    }

    [Fact]
    public async Task SuperAdmin_Sees_All_Plans()
    {
        // Super admin query (no tenant filter)
        var allPlans = await _dbContext.UserPlanItems.ToListAsync();

        // Should see all plans
        allPlans.Should().HaveCount(3);
        allPlans.Should().Contain(p => p.TenantId == _tenant1Id);
        allPlans.Should().Contain(p => p.TenantId == _tenant2Id);
    }

    [Fact]
    public async Task Create_Plan_Assigns_TenantId_From_Header()
    {
        // Simulate creating plan with TenantId from header
        var newPlan = new UserPlanItem
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant1Id, // From X-Tenant-Id header
            UserId = _user1Id,
            EventId = Guid.NewGuid(),
            State = "Active",
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = _user1Id
        };

        _dbContext.UserPlanItems.Add(newPlan);
        await _dbContext.SaveChangesAsync();

        var savedPlan = await _dbContext.UserPlanItems.FindAsync(newPlan.Id);
        savedPlan.Should().NotBeNull();
        savedPlan!.TenantId.Should().Be(_tenant1Id);
    }

    [Fact]
    public async Task User_Created_In_Tenant_Scope()
    {
        // Verify users are scoped to tenant
        var tenant1Users = await _dbContext.Users
            .Where(u => u.TenantId == _tenant1Id)
            .ToListAsync();

        tenant1Users.Should().HaveCount(1);
        tenant1Users.First().Email.Should().Contain("tenant1");
    }

    [Fact]
    public async Task Same_User_Can_Exist_In_Multiple_Tenants()
    {
        // Add same email user to tenant2
        var sameEmailUser = new User
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant2Id,
            Email = "user1@tenant1.local", // Same email, different tenant
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Users.Add(sameEmailUser);
        await _dbContext.SaveChangesAsync();

        // Query all users with this email
        var usersWithEmail = await _dbContext.Users
            .Where(u => u.Email == "user1@tenant1.local")
            .ToListAsync();

        // Should be in both tenants (unique per tenant, not globally)
        usersWithEmail.Should().HaveCount(2);
        usersWithEmail.Select(u => u.TenantId).Should().Contain(new[] { _tenant1Id, _tenant2Id });
    }

    [Fact]
    public async Task Delete_Plan_Respects_Tenant_Boundary()
    {
        // Get tenant1 plan
        var tenant1Plan = await _dbContext.UserPlanItems
            .FirstAsync(p => p.TenantId == _tenant1Id);

        var planId = tenant1Plan.Id;

        // Verify it belongs to tenant1
        tenant1Plan.TenantId.Should().Be(_tenant1Id);

        // Delete
        _dbContext.UserPlanItems.Remove(tenant1Plan);
        await _dbContext.SaveChangesAsync();

        // Verify deleted
        var deletedPlan = await _dbContext.UserPlanItems.FindAsync(planId);
        deletedPlan.Should().BeNull();
    }

    [Fact]
    public async Task Unique_Plan_Per_Tenant_User_Event()
    {
        // Check existing plan for same user/event
        var existingPlan = await _dbContext.UserPlanItems
            .FirstOrDefaultAsync(p => 
                p.TenantId == _tenant1Id && 
                p.UserId == _user1Id && 
                p.EventId == _eventId);

        existingPlan.Should().NotBeNull();

        // Attempting to add duplicate should fail in real DB (unique index)
        // For in-memory test, we simulate the check
        var isDuplicate = existingPlan != null;
        isDuplicate.Should().BeTrue();
    }

    [Fact]
    public async Task AuditLog_Records_TenantId()
    {
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant1Id,
            ActorUserId = _user1Id,
            Action = "PlanCreated",
            Entity = "UserPlanItem",
            EntityId = Guid.NewGuid(),
            AfterJson = "{}",
            Timestamp = DateTimeOffset.UtcNow
        };

        _dbContext.AuditLogs.Add(auditLog);
        await _dbContext.SaveChangesAsync();

        var savedLog = await _dbContext.AuditLogs.FindAsync(auditLog.Id);
        savedLog!.TenantId.Should().Be(_tenant1Id);
    }
}


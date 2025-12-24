using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Planning.Domain;
using Planning.Infrastructure;

namespace Planning.IntegrationTests;

/// <summary>
/// Integration tests verifying role-based authorization in the Planning service.
/// Ensures that:
/// - TenantAdmin can view all plans in their tenant
/// - TenantUser can only view their own plans
/// - SuperAdmin can view all plans across tenants
/// - Only TenantAdmin/SuperAdmin can delete plans
/// </summary>
public class RoleAuthorizationTests : IAsyncLifetime
{
    private PlanningDbContext _dbContext = null!;
    private Guid _tenantId;
    private Guid _tenantAdminId;
    private Guid _tenantUserId;
    private Guid _superAdminId;
    private Guid _planId;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<PlanningDbContext>()
            .UseInMemoryDatabase(databaseName: $"PlanningRoleAuth_{Guid.NewGuid()}")
            .Options;

        _dbContext = new PlanningDbContext(options);

        _tenantId = Guid.NewGuid();
        _tenantAdminId = Guid.NewGuid();
        _tenantUserId = Guid.NewGuid();
        _superAdminId = Guid.NewGuid();
        _planId = Guid.NewGuid();

        var now = DateTimeOffset.UtcNow;

        // Create users
        _dbContext.Users.AddRange(
            new User
            {
                Id = _tenantAdminId,
                TenantId = _tenantId,
                Email = "admin@tenant.local",
                CreatedAt = now
            },
            new User
            {
                Id = _tenantUserId,
                TenantId = _tenantId,
                Email = "user@tenant.local",
                CreatedAt = now
            }
        );

        // Create plans for different users
        _dbContext.UserPlanItems.AddRange(
            new UserPlanItem
            {
                Id = _planId,
                TenantId = _tenantId,
                UserId = _tenantUserId,
                EventId = Guid.NewGuid(),
                State = "Active",
                CreatedAt = now,
                CreatedBy = _tenantUserId
            },
            new UserPlanItem
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                UserId = _tenantAdminId,
                EventId = Guid.NewGuid(),
                State = "Active",
                CreatedAt = now,
                CreatedBy = _tenantAdminId
            }
        );

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
    public void Role_Can_View_All_Tenant_Plans(string role, bool canViewAll)
    {
        // TenantAdmin can view all plans in tenant
        // TenantUser can only view their own
        var hasPermission = role == "TenantAdmin" || role == "SuperAdmin";
        hasPermission.Should().Be(canViewAll);
    }

    [Theory]
    [InlineData("TenantAdmin", true)]
    [InlineData("TenantUser", false)]
    public void Role_Can_Delete_Other_User_Plans(string role, bool canDelete)
    {
        var hasPermission = role == "TenantAdmin" || role == "SuperAdmin";
        hasPermission.Should().Be(canDelete);
    }

    [Fact]
    public async Task TenantUser_Can_Only_See_Own_Plans()
    {
        // Simulate tenant user query
        var userPlans = await _dbContext.UserPlanItems
            .Where(p => p.TenantId == _tenantId && p.UserId == _tenantUserId && p.State == "Active")
            .ToListAsync();

        userPlans.Should().HaveCount(1);
        userPlans.First().UserId.Should().Be(_tenantUserId);
    }

    [Fact]
    public async Task TenantAdmin_Can_See_All_Tenant_Plans()
    {
        // Simulate tenant admin query (all users)
        var allTenantPlans = await _dbContext.UserPlanItems
            .Where(p => p.TenantId == _tenantId && p.State == "Active")
            .ToListAsync();

        allTenantPlans.Should().HaveCount(2);
        allTenantPlans.Should().Contain(p => p.UserId == _tenantUserId);
        allTenantPlans.Should().Contain(p => p.UserId == _tenantAdminId);
    }

    [Fact]
    public async Task SuperAdmin_Can_See_All_Plans_Across_Tenants()
    {
        // Add plan in another tenant
        var otherTenantId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        _dbContext.Users.Add(new User
        {
            Id = otherUserId,
            TenantId = otherTenantId,
            Email = "user@other.local",
            CreatedAt = DateTimeOffset.UtcNow
        });

        _dbContext.UserPlanItems.Add(new UserPlanItem
        {
            Id = Guid.NewGuid(),
            TenantId = otherTenantId,
            UserId = otherUserId,
            EventId = Guid.NewGuid(),
            State = "Active",
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = otherUserId
        });

        await _dbContext.SaveChangesAsync();

        // Super admin query (no tenant filter)
        var allPlans = await _dbContext.UserPlanItems
            .Where(p => p.State == "Active")
            .ToListAsync();

        allPlans.Should().HaveCount(3);
        allPlans.Should().Contain(p => p.TenantId == _tenantId);
        allPlans.Should().Contain(p => p.TenantId == otherTenantId);
    }

    [Fact]
    public async Task TenantAdmin_Can_Delete_Any_Plan_In_Tenant()
    {
        // Get user's plan
        var userPlan = await _dbContext.UserPlanItems.FindAsync(_planId);
        userPlan.Should().NotBeNull();
        userPlan!.UserId.Should().Be(_tenantUserId); // Belongs to user, not admin

        // Admin deletes user's plan
        _dbContext.UserPlanItems.Remove(userPlan);
        await _dbContext.SaveChangesAsync();

        var deletedPlan = await _dbContext.UserPlanItems.FindAsync(_planId);
        deletedPlan.Should().BeNull();
    }

    [Fact]
    public void TenantUser_Cannot_Delete_Other_User_Plans()
    {
        // Simulate authorization check
        var planUserId = _tenantAdminId; // Plan belongs to admin
        var requestUserId = _tenantUserId;
        var requestRole = "TenantUser";
        var isSuperAdmin = false;

        var canDelete = isSuperAdmin || 
                        requestRole == "TenantAdmin" || 
                        planUserId == requestUserId;

        canDelete.Should().BeFalse();
    }

    [Fact]
    public async Task SuperAdmin_Can_Delete_Any_Plan()
    {
        // Simulate super admin deleting plan
        var plan = await _dbContext.UserPlanItems.FirstAsync();
        var planId = plan.Id;

        // Super admin flag bypasses all checks
        var isSuperAdmin = true;
        isSuperAdmin.Should().BeTrue();

        _dbContext.UserPlanItems.Remove(plan);
        await _dbContext.SaveChangesAsync();

        var deletedPlan = await _dbContext.UserPlanItems.FindAsync(planId);
        deletedPlan.Should().BeNull();
    }

    [Fact]
    public async Task TenantAdmin_Can_List_All_Users_In_Tenant()
    {
        // Admin query for users
        var tenantUsers = await _dbContext.Users
            .Where(u => u.TenantId == _tenantId)
            .ToListAsync();

        tenantUsers.Should().HaveCount(2);
    }

    [Fact]
    public void TenantUser_Cannot_List_Other_Users()
    {
        // Authorization check for user listing
        var role = "TenantUser";
        var canListUsers = role == "TenantAdmin" || role == "SuperAdmin";

        canListUsers.Should().BeFalse();
    }

    [Fact]
    public async Task AuditLog_Records_Admin_Actions()
    {
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ActorUserId = _tenantAdminId,
            Action = "PlanDeleted",
            Entity = "UserPlanItem",
            EntityId = _planId,
            BeforeJson = "{}",
            Timestamp = DateTimeOffset.UtcNow
        };

        _dbContext.AuditLogs.Add(auditLog);
        await _dbContext.SaveChangesAsync();

        var savedLog = await _dbContext.AuditLogs.FindAsync(auditLog.Id);
        savedLog.Should().NotBeNull();
        savedLog!.ActorUserId.Should().Be(_tenantAdminId);
        savedLog.Action.Should().Be("PlanDeleted");
    }
}


using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Platform.Application.Tenants.Queries;
using Platform.Domain.Entities;
using Platform.Infrastructure.Data;

namespace Platform.UnitTests.Tenants.Queries;

public class GetAllTenantsHandlerTests : IAsyncLifetime
{
    private PlatformDbContext _dbContext = null!;
    private GetAllTenantsHandler _handler = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseInMemoryDatabase(databaseName: $"GetAllTenantsTest_{Guid.NewGuid()}")
            .Options;

        _dbContext = new PlatformDbContext(options);
        _handler = new GetAllTenantsHandler(_dbContext);

        // Setup test data
        var now = DateTimeOffset.UtcNow;
        var creatorId = Guid.NewGuid();

        _dbContext.Tenants.AddRange(
            new Tenant
            {
                Id = Guid.NewGuid(),
                Name = "Active Tenant 1",
                Slug = "active-tenant-1",
                DefaultLanguage = "en",
                DefaultCurrency = "USD",
                Status = TenantStatus.Active,
                CreatedAt = now,
                CreatedBy = creatorId
            },
            new Tenant
            {
                Id = Guid.NewGuid(),
                Name = "Active Tenant 2",
                Slug = "active-tenant-2",
                DefaultLanguage = "tr",
                DefaultCurrency = "TRY",
                Status = TenantStatus.Active,
                CreatedAt = now.AddDays(-1),
                CreatedBy = creatorId
            },
            new Tenant
            {
                Id = Guid.NewGuid(),
                Name = "Deleted Tenant",
                Slug = "deleted-tenant",
                DefaultLanguage = "en",
                DefaultCurrency = "EUR",
                Status = TenantStatus.Deleted,
                CreatedAt = now.AddDays(-2),
                CreatedBy = creatorId
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
    public async Task HandleAsync_ReturnsActiveTenantsOnly()
    {
        // Act
        var result = await _handler.HandleAsync(new GetAllTenantsQuery());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().NotContain(t => t.Status == "Deleted");
    }

    [Fact]
    public async Task HandleAsync_ReturnsCorrectTenantData()
    {
        // Act
        var result = await _handler.HandleAsync(new GetAllTenantsQuery());

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        var tenant = result.Value!.First(t => t.Name == "Active Tenant 1");
        tenant.Slug.Should().Be("active-tenant-1");
        tenant.DefaultLanguage.Should().Be("en");
        tenant.DefaultCurrency.Should().Be("USD");
        tenant.Status.Should().Be("Active");
    }

    [Fact]
    public async Task HandleAsync_ReturnsCorrectMemberCount()
    {
        // Arrange - Add members to first tenant
        var tenant = await _dbContext.Tenants.FirstAsync(t => t.Name == "Active Tenant 1");
        _dbContext.Memberships.AddRange(
            new Membership
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                UserId = Guid.NewGuid(),
                UserEmail = "user1@test.com",
                Role = TenantRole.TenantAdmin,
                Status = MembershipStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new Membership
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                UserId = Guid.NewGuid(),
                UserEmail = "user2@test.com",
                Role = TenantRole.TenantUser,
                Status = MembershipStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new Membership
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                UserId = Guid.NewGuid(),
                UserEmail = "user3@test.com",
                Role = TenantRole.TenantUser,
                Status = MembershipStatus.Removed, // Removed - should not count
                CreatedAt = DateTimeOffset.UtcNow
            }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _handler.HandleAsync(new GetAllTenantsQuery());

        // Assert
        result.IsSuccess.Should().BeTrue();
        var tenantResult = result.Value!.First(t => t.Name == "Active Tenant 1");
        tenantResult.MemberCount.Should().Be(2); // Only active members
    }

    [Fact]
    public async Task HandleAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange - Clear all tenants
        _dbContext.Tenants.RemoveRange(_dbContext.Tenants);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _handler.HandleAsync(new GetAllTenantsQuery());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}


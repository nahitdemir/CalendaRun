using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Platform.Application.Common;
using Platform.Domain.Entities;
using Platform.Infrastructure.Data;
using StackExchange.Redis;
using System.Text.Json;

namespace Platform.Application.Invites.Commands;

public class AcceptInviteHandler : ICommandHandler<AcceptInviteCommand, Result<AcceptInviteResult>>
{
    private readonly PlatformDbContext _db;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<AcceptInviteHandler> _logger;

    public AcceptInviteHandler(
        PlatformDbContext db, 
        IConnectionMultiplexer redis,
        ILogger<AcceptInviteHandler> logger)
    {
        _db = db;
        _redis = redis;
        _logger = logger;
    }

    public async Task<Result<AcceptInviteResult>> HandleAsync(AcceptInviteCommand command, CancellationToken ct = default)
    {
        var invite = await _db.Invites
            .Include(i => i.Tenant)
            .FirstOrDefaultAsync(i => i.Token == command.Token, ct);

        if (invite == null)
        {
            return Result<AcceptInviteResult>.NotFound("Invite not found");
        }

        if (invite.Status != InviteStatus.Pending)
        {
            return Result<AcceptInviteResult>.Failure($"Invite is {invite.Status}");
        }

        if (invite.ExpiresAt < DateTimeOffset.UtcNow)
        {
            invite.Status = InviteStatus.Expired;
            await _db.SaveChangesAsync(ct);
            return Result<AcceptInviteResult>.Failure("Invite has expired");
        }

        // Verify email matches (case-insensitive)
        if (!invite.Email.Equals(command.UserEmail, StringComparison.OrdinalIgnoreCase))
        {
            return Result<AcceptInviteResult>.Forbidden("Email does not match invite");
        }

        // Check if already a member
        var existingMembership = await _db.Memberships
            .FirstOrDefaultAsync(m => m.TenantId == invite.TenantId && m.UserId == command.UserId, ct);

        if (existingMembership != null)
        {
            if (existingMembership.Status == MembershipStatus.Active)
            {
                return Result<AcceptInviteResult>.Conflict("Already a member of this tenant");
            }

            // Reactivate membership
            existingMembership.Status = MembershipStatus.Active;
            existingMembership.Role = invite.Role;
            existingMembership.AcceptedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            // Create membership
            var membership = new Membership
            {
                Id = Guid.NewGuid(),
                TenantId = invite.TenantId,
                UserId = command.UserId,
                UserEmail = command.UserEmail,
                Role = invite.Role,
                Status = MembershipStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow,
                InvitedBy = invite.CreatedBy,
                AcceptedAt = DateTimeOffset.UtcNow
            };

            _db.Memberships.Add(membership);
        }

        // Update invite
        invite.Status = InviteStatus.Accepted;
        invite.AcceptedAt = DateTimeOffset.UtcNow;

        // Add audit log
        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = invite.TenantId,
            ActorUserId = command.UserId,
            ActorEmail = command.UserEmail,
            Action = "InviteAccepted",
            EntityType = "Membership",
            AfterJson = JsonSerializer.Serialize(new { invite.Email, Role = invite.Role.ToString() }),
            Timestamp = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        // Invalidate membership cache
        var redisDb = _redis.GetDatabase();
        await redisDb.KeyDeleteAsync($"membership:{command.UserId}:{invite.TenantId}");

        _logger.LogInformation("Invite accepted: {UserId} joined tenant {TenantId}", 
            command.UserId, invite.TenantId);

        return Result<AcceptInviteResult>.Success(new AcceptInviteResult(
            invite.TenantId,
            invite.Tenant.Name,
            invite.Role.ToString()
        ));
    }
}


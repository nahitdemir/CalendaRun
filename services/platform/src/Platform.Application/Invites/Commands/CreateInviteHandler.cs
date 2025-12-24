using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Platform.Application.Common;
using Platform.Domain.Entities;
using Platform.Infrastructure.Data;
using System.Text.Json;

namespace Platform.Application.Invites.Commands;

public class CreateInviteHandler : ICommandHandler<CreateInviteCommand, Result<CreateInviteResult>>
{
    private readonly PlatformDbContext _db;
    private readonly ILogger<CreateInviteHandler> _logger;

    public CreateInviteHandler(PlatformDbContext db, ILogger<CreateInviteHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<CreateInviteResult>> HandleAsync(CreateInviteCommand command, CancellationToken ct = default)
    {
        // Check if already invited
        var existingInvite = await _db.Invites
            .FirstOrDefaultAsync(i => i.TenantId == command.TenantId && 
                                       i.Email == command.Email && 
                                       i.Status == InviteStatus.Pending, ct);

        if (existingInvite != null)
        {
            return Result<CreateInviteResult>.Conflict("Invite already pending for this email");
        }

        // Check if already a member
        var existingMembership = await _db.Memberships
            .FirstOrDefaultAsync(m => m.TenantId == command.TenantId && 
                                       m.UserEmail == command.Email && 
                                       m.Status == MembershipStatus.Active, ct);

        if (existingMembership != null)
        {
            return Result<CreateInviteResult>.Conflict("User is already a member of this tenant");
        }

        var now = DateTimeOffset.UtcNow;
        var invite = new Invite
        {
            Id = Guid.NewGuid(),
            TenantId = command.TenantId,
            Email = command.Email,
            Role = command.Role,
            Token = Guid.NewGuid().ToString("N"),
            Status = InviteStatus.Pending,
            CreatedAt = now,
            CreatedBy = command.ActorUserId,
            ExpiresAt = now.AddDays(7)
        };

        _db.Invites.Add(invite);

        // Add audit log
        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = command.TenantId,
            ActorUserId = command.ActorUserId,
            ActorEmail = command.ActorEmail,
            Action = "InviteCreated",
            EntityType = "Invite",
            EntityId = invite.Id,
            AfterJson = JsonSerializer.Serialize(new { invite.Email, Role = invite.Role.ToString() }),
            Timestamp = now
        });

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Invite created: {Email} to tenant {TenantId} by {UserId}", 
            command.Email, command.TenantId, command.ActorUserId);

        return Result<CreateInviteResult>.Success(new CreateInviteResult(
            invite.Id,
            invite.Email,
            invite.Role.ToString(),
            invite.Token,
            invite.ExpiresAt
        ));
    }
}


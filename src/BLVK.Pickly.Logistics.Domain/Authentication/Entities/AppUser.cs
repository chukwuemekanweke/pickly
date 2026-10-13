using BLVK.Pickly.Logistics.Contracts.Common;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using Microsoft.AspNetCore.Identity;

namespace BLVK.Pickly.Logistics.Domain.Authentication.Entities;

public sealed class AppUser : IdentityUser<Guid>, IAuditableEntity, ISoftDelete
{
    private AppUser()
    {
    }

    private AppUser(string email)
    {
        var normalizedEmail = email.Trim();

        UserName = normalizedEmail;
        Email = normalizedEmail;
    }

    public DateTimeOffset CreatedAtUtc { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public string? UpdatedBy { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }
    public string? DeletedBy { get; private set; }
    public static AppUser Create(string email) =>
        new(email);

    public static AppUser Create(string email, string firstName, string lastName) =>
        new(email);

    public void MarkEmailVerified()
    {
        EmailConfirmed = true;
    }

    public void SetCreatedAudit(DateTimeOffset utcNow, string actorId)
    {
        CreatedAtUtc = utcNow;
        CreatedBy = string.IsNullOrWhiteSpace(actorId) ? ActorDefaults.SystemActorId : actorId;
    }

    public void SetUpdatedAudit(DateTimeOffset utcNow, string actorId)
    {
        UpdatedAtUtc = utcNow;
        UpdatedBy = string.IsNullOrWhiteSpace(actorId) ? ActorDefaults.SystemActorId : actorId;
    }

    public void SetDeletedAudit(DateTimeOffset utcNow, string actorId)
    {
        IsDeleted = true;
        DeletedAtUtc = utcNow;
        DeletedBy = string.IsNullOrWhiteSpace(actorId) ? ActorDefaults.SystemActorId : actorId;
    }
}

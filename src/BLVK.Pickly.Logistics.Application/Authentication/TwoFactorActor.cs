using BLVK.Pickly.Logistics.Domain.Authentication.Entities;

namespace BLVK.Pickly.Logistics.Application.Authentication;

public sealed record TwoFactorActor(AppUser User, Guid StakeholderId);

namespace BLVK.Pickly.Logistics.WebAPI.Features.Stakeholders.LoginActivity;

public sealed record GetLoginActivityRequest(int Limit = 20, string? Cursor = null);

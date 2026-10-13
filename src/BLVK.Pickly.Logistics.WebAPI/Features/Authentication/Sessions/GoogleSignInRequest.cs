namespace BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Sessions;

public sealed record GoogleSignInRequest(string FlowToken, string IdToken = "");

namespace BLVK.Pickly.Logistics.Domain.Deliveries.Services;

public enum DeliveryRouteStatus { Success = 1, NoRoute = 2, RateLimited = 3, ProviderUnavailable = 4, InvalidResponse = 5, UnsupportedProfile = 6, MissingConfiguration = 7, Timeout = 8 }

namespace BLVK.Pickly.Logistics.Domain.Common.Exceptions;

public sealed class AggregateStateException(string message) : Exception(message);

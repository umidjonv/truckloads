namespace TL.Shared.Common.Dtos.AIProcessing;

public record PostItemResult(
    string? Type,
    string? Weight,
    string? Volume,
    string? Price,
    string? StartLocation,
    string? DestinationLocation,
    string? PhoneNumber,
    string? TypeOfTruck,
    string? Cargo
);
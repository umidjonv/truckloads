using System;
using MongoDB.Bson;

namespace TL.Shared.Common.Dtos.AIProcessing;

public class PostsCollectionDto
{
    public ObjectId Id { get; set; }

    public string? Type { get; set; }

    public string? Weight { get; set; }

    public string? Volume { get; set; }

    public string? Price { get; set; }

    public string? StartLocation { get; set; }

    public string? DestinationLocation { get; set; }

    public string? PhoneNumber { get; set; }

    public string? TypeOfTruck { get; set; }

    public string? Cargo { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public bool IsProcessed { get; set; } = false;
}
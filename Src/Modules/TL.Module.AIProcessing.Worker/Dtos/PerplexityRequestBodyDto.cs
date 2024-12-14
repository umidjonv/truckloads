namespace TL.Module.AIProcessing.Worker.Dtos;

public class PerplexityRequestBodyDto
{
    public string Model { get; set; } = "llama-3.1-sonar-small-128k-online";

    public PerplexityRequestBodyMessageDto[] Messages { get; set; } = [];
}
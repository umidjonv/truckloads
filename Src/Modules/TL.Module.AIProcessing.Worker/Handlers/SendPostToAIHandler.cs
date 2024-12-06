using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TL.Module.AIProcessing.Worker.Dtos;
using TL.Shared.Common.Dtos.AIProcessing;

namespace TL.Module.AIProcessing.Worker.Handlers;

public class SendPostToAIHandler(
    IHttpClientFactory httpClientFactory,
    IConfigurationManager configurationManager,
    ILogger<SendPostToAIHandler> logger)
    : IRequestHandler<SendPostToAIParams, SendPostToAIResult>
{
    public async Task<SendPostToAIResult> Handle(SendPostToAIParams request,
        CancellationToken cancellationToken)
    {
        var url = configurationManager["PerplexityAI:Url"]; // https://api.perplexity.ai
        var path = configurationManager["PerplexityAI:Path"] ?? "/"; // /chat/completions

        if (string.IsNullOrWhiteSpace(url))
        {
            logger.LogError("[{0}] PerplexityAI:Url is empty!", nameof(SendPostToAIHandler));
            throw new ArgumentException("PerplexityAI url is empty!");
        }

        using var client = httpClientFactory.CreateClient("PerplexityAI");
        var body = new PerplexityRequestBodyDto()
        {
            Messages =
            [
                new PerplexityRequestBodyMessageDto()
                {
                    Content = ConvertPostToQuestion(request.Message),
                    Role = PerplexityRequestBodyMessageDto.MessageRole.System
                }
            ]
        };

        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        client.BaseAddress = new Uri(url);
        var response = await client.PostAsync(path, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        var jsonDocument = JsonDocument.Parse(responseBody);

        var contents = new SendPostToAIResult([]);

        foreach (var choice in jsonDocument.RootElement.GetProperty("choices").EnumerateArray())
        {
            var contentArray = choice.GetProperty("message").GetProperty("content").EnumerateArray();

            foreach (var item in contentArray)
            {
                var cargoDetails = new PostItemResult(
                    item.GetProperty("Type").GetString(),
                    item.GetProperty("Weight").GetString(),
                    item.GetProperty("Volume").GetString(),
                    item.GetProperty("Price").GetString(),
                    item.GetProperty("StartLocation").GetString(),
                    item.GetProperty("DestinationLocation").GetString(),
                    item.GetProperty("PhoneNumber").GetString(),
                    item.GetProperty("TypeOfTruck").GetString(),
                    item.GetProperty("Cargo").GetString());

                contents.Posts.Add(cargoDetails);
            }
        }

        return contents;
    }

    private static string ConvertPostToQuestion(string message)
    {
        var sb = new StringBuilder(@"I have one or more messages.
                                    You need to convert them to a json array that has Type, Weight, Volume, Price, StartLocation, DestinationLocation, PhoneNumber, TypeOfTruck, Cargo field data.
                                    StartLocation and DestinationLocation should behave like this: CountryCode (State, City) - example (860 (Tashkent)) in english.
                                    If any of these fields are missing, you may not display them. 
                                    If the message is not related to the shipment, you must return an empty json array. 
                                    Your answer should contain only json, without unnecessary words.
                                    Here is the post:");
        sb.AppendLine("");

        sb.Append(message);

        return sb.ToString();
    }
}
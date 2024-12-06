using MediatR;

namespace TL.Shared.Common.Dtos.AIProcessing;

public record SendPostToAIParams(string Message) : IRequest<SendPostToAIResult>;
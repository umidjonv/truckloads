using MediatR;

namespace TL.Shared.Common.Dtos.Telegram;

public record InsertMessageParams(long ChatId, string Message) : IRequest;
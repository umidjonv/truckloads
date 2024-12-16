using MediatR;

namespace TL.Shared.Common.Dtos.Telegram;

public record InsertChatParams(long ChatId, string ChatName) : IRequest;
using MediatR;

namespace TL.Shared.Common.Dtos.Telegram;

public record GetChatNewMessageParams<T>(long Id) : IRequest<GetChatNewMessageResult<T>>;
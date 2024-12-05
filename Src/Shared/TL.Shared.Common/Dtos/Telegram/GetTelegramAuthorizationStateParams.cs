using MediatR;

namespace TL.Shared.Common.Dtos.Telegram;

public record GetTelegramAuthorizationStateParams<T> : IRequest<GetTelegramAuthorizationStateResult<T>>;
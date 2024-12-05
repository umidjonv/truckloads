using MediatR;

namespace TL.Shared.Common.Dtos.Telegram;

public record GetAllTelegramChatsParams<T> : IRequest<GetAllTelegramChatsResult<T>>;
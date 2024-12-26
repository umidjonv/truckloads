using MediatR;

namespace TL.Shared.Common.Dtos.Telegram;

public record GetAllChatsParams : IRequest<List<GetAllChatsResult>>;

public record SwitchAllowChatParams(long ChatId, bool Allow) : IRequest;
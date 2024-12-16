using MediatR;

namespace TL.Shared.Common.Dtos.Telegram;

public record GetAllowedChatIdsParams : IRequest<GetAllowedChatsResult>;
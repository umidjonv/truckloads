using MediatR;

namespace TL.Shared.Common.Dtos.Telegram;

public record GetTelegramSettingsParams : IRequest<GetTelegramSettingsResult>;
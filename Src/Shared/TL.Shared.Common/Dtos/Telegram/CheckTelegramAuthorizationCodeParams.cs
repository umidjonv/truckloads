using MediatR;

namespace TL.Shared.Common.Dtos.Telegram;

public record CheckTelegramAuthorizationCodeParams(string Code) : IRequest<CheckTelegramAuthorizationCodeResults>;
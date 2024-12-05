using MediatR;

namespace TL.Shared.Common.Dtos.Telegram;

public record LoginTelegramClientParams(bool IsReauthorization = false) : IRequest;
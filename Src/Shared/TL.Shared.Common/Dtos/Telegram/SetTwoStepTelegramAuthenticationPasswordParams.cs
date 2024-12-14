using MediatR;

namespace TL.Shared.Common.Dtos.Telegram;

public record SetTwoStepTelegramAuthenticationPasswordParams(string Password)
    : IRequest<SetTwoStepTelegramAuthenticationPasswordResult>;
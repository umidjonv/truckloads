using MediatR;

namespace TL.Shared.Common.Dtos.Telegram;

public record InsertUserParams(long ChatId, long UserId, string UserName) : IRequest<bool>;
using MediatR;

namespace TL.Shared.Common.Dtos.Telegram;

public record InsertSettingsParams(string PhoneNumber, string ApiId, string ApiHash) : IRequest;
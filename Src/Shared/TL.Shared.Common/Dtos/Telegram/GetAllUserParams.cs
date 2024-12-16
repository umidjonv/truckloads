using MediatR;
using TL.Shared.Common.Dtos.Telegram;


public class GetAllUserParams() : IRequest<List<UserParams>>;

using System.Collections.Generic;

namespace TL.Shared.Common.Dtos.Telegram;

public record GetAllTelegramChatsResult<T>(List<T> Chats);
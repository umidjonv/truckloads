namespace TL.Module.Telegram.Bot.Consumer;

public interface IUserNotifyConsumer
{
    
}

public class UserNotifyConsumer : IUserNotifyConsumer
{
    // TODO: get all users from TelegramDbContext.Users
    // then
    // send to telegram by chatId and userId
}
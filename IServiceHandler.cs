
public interface IServiceHandler
{
    Task HandleCommand(string command, IMessageHandler messageHandler);
}

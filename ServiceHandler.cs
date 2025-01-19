using System.Threading.Tasks;

public abstract class ServiceHandler : IServiceHandler
{
    public abstract Task HandleCommand(string command, IMessageHandler messageHandler);
}

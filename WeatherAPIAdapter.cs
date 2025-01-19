using System.Threading.Tasks;

public class WeatherAPIAdapter : IServiceHandler
{
    private readonly WeatherHandler _weatherHandler;

    public WeatherAPIAdapter()
    {
        _weatherHandler = new WeatherHandler(); //initializing the WeatherHandler's instance to handle weather data fetching
    }

    public async Task HandleCommand(string command, IMessageHandler messageHandler)
    //implement the "IServiceHandler" interface, defining how this adapter should process commands, making it compatible with other parts of the system
    {
        string result = await _weatherHandler.GetWeather(command);
        messageHandler.AddMessage(result, false);  
    }
}

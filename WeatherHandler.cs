using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public class WeatherHandler
{
    private readonly string _apiKey = "1be37a81bf61808d3aa13aa843ab0def"; //API key authenticates requests to the OpenWeatherMap API
    private readonly HttpClient _client; //an instance of "HttpClient", used to make HTTP requests

    public WeatherHandler()
    {
        _client = new HttpClient();
    }

    public async Task<string> GetWeather(string command)
    {
        string city = ExtractCityFromCommand(command); //extract the city name
        string uri = $"http://api.openweathermap.org/data/2.5/weather?q={Uri.EscapeDataString(city)}&appid={_apiKey}&units=metric";
        //construct uri for the API request, ensure that the city name is URL-encoded
        var response = await _client.GetAsync(uri); //send an asynchronous HTTP GET request and wait for response
        var content = await response.Content.ReadAsStringAsync(); //read the response content as a string asynchronously
        var json = JObject.Parse(content); //parse the string content into a JSON object for data extraction

        //Error Handling: if the request is not successful, return an error message
        if (!response.IsSuccessStatusCode)
        {
            return $"Failed to retrieve weather data for {city}: {response.ReasonPhrase}";
        }

        //extract temperature and weather description from the JSON object, return the information
        double temp = (double)json["main"]["temp"]; //access the "temp" in the json's "main" structure, then parse it to type double
        string description = (string)json["weather"][0]["description"]; //access the keyword "weather" in json object, "weather" is an array containing 1 or more items, the "0" represents the first weather condition (the most relevant)
        return $"It's currently {temp}°C in {city} with {description}.";
    }

    private string ExtractCityFromCommand(string command)
    {
        string lowerCommand = command.ToLower();
        string keyword = "weather in "; //define the keyword to look for within the command
        int index = lowerCommand.IndexOf(keyword);

        //return the starting index of the keyword if found, or "-1" if the keyword is not found
        if (index != -1)
        {
            //calculate where the city name should start by adding the length of the keyword to its starting index
            index += keyword.Length;
            string[] parts = command.Substring(index).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            //extract the substring that begins right after the keyword and split it into parts at every space
            // StringSplitOptions.RemoveEmptyEntries ensures that no empty strings are included in the resulting array if there are multiple spaces

            
            if (parts.Length > 0)
            {
                return parts[0]; //return the city name
            }
        }
        //if the command does not contain the keyword or if no text follows the keyword, return "Unknown city"
        return "Unknown city";
    }

}

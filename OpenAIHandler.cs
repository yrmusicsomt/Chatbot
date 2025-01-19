
//using System;
//using System.Net.Http;
//using System.Text;
//using System.Text.Json;
//using System.Threading.Tasks;

//public class OpenAIHandler : ServiceHandler //inherits from the ServiceHandler abstract class
//{
//    private static readonly HttpClient _client = new HttpClient(); //an instance of "HttpClient", used for Http communication
////private const string ApiKey = "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"; //API key for authenticating requests to OpenAI API
//    private const string ApiUrl = "https://api.openai.com/v1/chat/completions"; //Endpoint URL for sending requests to get chat from OpenAI

//    public override async Task HandleCommand(string command, IMessageHandler messageHandler)
//    {
//        try
//        {
//            //Preparing the API request
//            var requestData = new //hold the data to be sent
//            {
//                model = "gpt-3.5-turbo", //model identifer 
//                messages = new[] { new { role = "user", content = command } } //prepare the data payload for interacting with AI model
//            };

//            var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl); //initialize a "HttpRequestMessage" with HTTP POST method and sets the URL to 'ApiUrl'
//            request.Headers.Add("Authorization", "Bearer " + ApiKey); //add the authorization header using the "ApiKey"
//            request.Content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");
//            //serialize "requestData" to JSON format and sets it as the content of the request, "application/json" as the content type


//            //Sending the Request and Receiving the Response
//            HttpResponseMessage response = await _client.SendAsync(request); //send the prepared request asynchrnously and awaits the response
//            response.EnsureSuccessStatusCode(); //ensure the successful status code response, throw an exception if the HTTP response status indicates a failure

//            //Processing the Response
//            var jsonResponse = JsonSerializer.Deserialize<JsonDocument>(await response.Content.ReadAsStringAsync()); //read the JSON response and parse
//            var responseText = jsonResponse.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString(); //extract the content of the response

//            //Communicating back
//            messageHandler.AddMessage("AI Response: " + responseText, false); //send the parsed AI response back to the system
//        }

//        //Handle any exceptions by catching them and sending an error message back through the same message handler
//        catch (Exception ex)
//        {
//            messageHandler.AddMessage("Error: " + ex.Message, false); 
//        }
//    }
//}
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class OpenAIHandler : ServiceHandler //inherits from the ServiceHandler abstract class
{
    private static readonly HttpClient _client = new HttpClient(); //an instance of "HttpClient", used for Http communication
    private const string ApiUrl = "https://api.openai.com/v1/chat/completions"; //Endpoint URL for sending requests to get chat from OpenAI

    public override async Task HandleCommand(string command, IMessageHandler messageHandler)
    {
        try
        {
            // Retrieve the API key from an environment variable
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrEmpty(apiKey))
            {
                messageHandler.AddMessage("Error: OpenAI API key is not set.", false);
                return;
            }

            // Preparing the API request
            var requestData = new //hold the data to be sent
            {
                model = "gpt-3.5-turbo", //model identifier 
                messages = new[] { new { role = "user", content = command } } //prepare the data payload for interacting with AI model
            };

            var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl); //initialize a "HttpRequestMessage" with HTTP POST method and sets the URL to 'ApiUrl'
            request.Headers.Add("Authorization", "Bearer " + apiKey); //add the authorization header using the "apiKey"
            request.Content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");
            //serialize "requestData" to JSON format and set it as the content of the request, "application/json" as the content type

            // Sending the Request and Receiving the Response
            HttpResponseMessage response = await _client.SendAsync(request); //send the prepared request asynchronously and await the response
            response.EnsureSuccessStatusCode(); //ensure the successful status code response, throw an exception if the HTTP response status indicates a failure

            // Processing the Response
            var jsonResponse = JsonSerializer.Deserialize<JsonDocument>(await response.Content.ReadAsStringAsync()); //read the JSON response and parse
            var responseText = jsonResponse.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString(); //extract the content of the response

            // Communicating back
            messageHandler.AddMessage("AI Response: " + responseText, false); //send the parsed AI response back to the system
        }

        // Handle any exceptions by catching them and sending an error message back through the same message handler
        catch (Exception ex)
        {
            messageHandler.AddMessage("Error: " + ex.Message, false);
        }
    }
}

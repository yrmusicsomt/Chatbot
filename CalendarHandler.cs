using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System; //dotnet
using System.IO; //filestream, streamreader
using System.Text.RegularExpressions; //regex, allowing match based on patterns
using System.Threading; //managing classes and methods running in the background
using System.Threading.Tasks; //asynchronous 

public class CalendarHandler : ServiceHandler
{
    private static CalendarService _service; // instance of CalendarService, used to interact with Google Calendar API
    private static string ApplicationName = "Google Calendar API C#"; //name of the application, used in the initialization of the CalendarService

    public CalendarHandler()
    {
        InitializeAsync().Wait();  // Ensuring the API client is initialized synchronously on startup
    }

    private static async Task InitializeAsync()
    {
        string[] Scopes = { CalendarService.Scope.CalendarReadonly, CalendarService.Scope.CalendarEvents };
        //define the permissions the application will request from the user (read-only access and manage events)
        string credentialsPath = "credentials.json";  // load OAuth 2 credentials from file and stores the token in specified directory
        string tokenPath = "tokenStore";  // folder to store the user's token

        UserCredential credential;
        using (var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read)) //handle OAuth 2.0 authorization and token storage
        {
            credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.Load(stream).Secrets,
                Scopes,
                "user", //load the client secrets from the stream and uses the defined scopes along with a user identifier (user) to authenticate
                CancellationToken.None, //specify that the operation should not expect any cancellation request
                new FileDataStore(tokenPath, true));
                //initialize a persistent storage for the tokens, enable tokens to be stored locally and reused
                //"true" indicates that the datastore and create the directory in case it does not exist
        }

        _service = new CalendarService(new BaseClientService.Initializer //initialize the Calendar Service, which mainly interacts with the Google Calendar API
        {
            HttpClientInitializer = credential, //set credentials, ensuring all requests are authenticated
            ApplicationName = ApplicationName, //set name, which will be used for logging with Google
        });
        Console.WriteLine("Google Calendar API service initialized."); //log the confirmation message to the console
    }

    public override async Task HandleCommand(string command, IMessageHandler messageHandler)
    {
        try
        {
            (DateTime startDate, DateTime endDate) = ExtractDateTimeFromCommand(command); //parse the command, extract the start and end dates for the event

            await AddCalendarEvent("Scheduled Event", "Office", "Team meeting", startDate, endDate); //add a new event to the Google Calendar using the dates extracted with specified titles
            messageHandler.AddMessage("Calendar event added successfully.", false); //successful message
        }
        catch (Exception ex)
        {
            messageHandler.AddMessage($"Failed to add event: {ex.Message}", false); //report the error in case the command is not in the right format
        }
    }

    private static async Task AddCalendarEvent(string summary, string location, string description, DateTime start, DateTime end)
    {
        //create new "Event" object
        var eventToAdd = new Event
        {
            Summary = summary,
            Location = location,
            Description = description,
            Start = new EventDateTime { DateTime = start, TimeZone = "Australia/Melbourne" },
            End = new EventDateTime { DateTime = end, TimeZone = "Australia/Melbourne" }
        };

        try
        {
            //construct a request to insert the new event to the user's primary calendar, wait for method execution until the request completes
            var request = _service.Events.Insert(eventToAdd, "primary");
            Event createdEvent = await request.ExecuteAsync();
            Console.WriteLine($"Event created: {createdEvent.HtmlLink}"); //the event is successfully created, link added
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to create event: {ex.Message}");
            throw;  // Re-throw to handle it in the calling method
        }
    }

    private (DateTime, DateTime) ExtractDateTimeFromCommand(string command)
    {
        // Regex to extract date and time from the command
        Regex datePattern = new Regex(@"(\d{1,2}/\d{1,2}/\d{4}) at (\d{1,2}[:;]\d{2} [ap]m)", RegexOptions.IgnoreCase);
        Match match = datePattern.Match(command);
        if (match.Success)
        {
            string datePart = match.Groups[1].Value;
            // Replace ';' with ':' in the time part
            string timePart = match.Groups[2].Value.Replace(';', ':');
            DateTime eventDate;
            try
            {
                eventDate = DateTime.ParseExact($"{datePart} {timePart}", "dd/MM/yyyy hh:mm tt", System.Globalization.CultureInfo.InvariantCulture);
                //System.Globalization.CultureInfo.InvariantCulture is used to avoid issues with culture-specific date formats
            }
            catch (FormatException)
            {
                throw new ArgumentException("Invalid date or time format in command.");
            }
            return (eventDate, eventDate.AddHours(1));  // Assuming the event lasts one hour
        }
        throw new ArgumentException("Date or time not found in command.");
    }

}

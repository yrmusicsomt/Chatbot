using Newtonsoft.Json.Linq;
using SplashKitSDK;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public class InputBox : IMessageHandler
{
    //Declare variables to manage the graphical window: input text, message history, service handlers, graphical settings
    private Window _window;
    private string _text = "";
    private List<(string message, bool isUser)> _messages = new List<(string, bool)>();
    private IServiceHandler _calendarHandler;
    private IServiceHandler _openAIHandler;
    private WeatherAPIAdapter _weatherAPIAdapter ; 
    private Font _font;
    private int _fontSize = 12;

    //Constructor: initialize InputBox with service handlers and graphical settings
    public InputBox(Window window, WeatherAPIAdapter weatherAPIAdapter, IServiceHandler calendarHandler, IServiceHandler openAIHandler)
    {
        _window = window;
        _weatherAPIAdapter = weatherAPIAdapter; // Initialize weatherHandler
        _calendarHandler = calendarHandler;
        _openAIHandler = openAIHandler;
        _font = SplashKit.LoadFont("Roboto", "fonts/Roboto-Regular.ttf");
        _fontSize = 12;
    }
    //Method to handle user input from the keyboard
    public void HandleInput()
    {
        foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
        {
            if (SplashKit.KeyTyped(key))
            {
                if (key == KeyCode.ReturnKey) //when "Enter" is pressed, process and clear the input
                {
                    _messages.Add((_text, true)); // Add user input to messages list
                    ProcessInput(_text); //Process the input text for commands
                    _text = ""; // Clear current input
                }
                else if (key != KeyCode.BackspaceKey) //If Backspace key is not pressed
                {
                    _text += (char)key; //add new characters
                }
                else if (_text.Length > 0) //if Backspace is pressed
                {
                    _text = _text.Substring(0, _text.Length - 1); //Remove the last character
                }
            }
        }
    }

    //Method processes different types of user input
    //Route the input to the correct service handler based on its content 
    private async void ProcessInput(string input) //async: perform operation without blocking the code execution
    {
        if (input.ToLower().Contains("weather")) //if the input string contains "weather"
        {
            //await: wait for the operation to complete before proceeding
            await _weatherAPIAdapter.HandleCommand(input, this); //route to weatherAPIadapter
        }
        else if (IsCalendarRequest(input)) //if the input string matches the calendar-related patterns
        {
            await _calendarHandler.HandleCommand(input, this); //route to calendarhandler
        }
        else
        {
            await _openAIHandler.HandleCommand(input, this);
            //if the input doesn't match any cases, it is treated as a general query by OpenAIHandler
        }
    }

    private bool IsCalendarRequest(string text)
    {
        return Regex.IsMatch(text, @"\b(schedule|meeting|appointment)\b", RegexOptions.IgnoreCase);
        //check if the specified pattern matches any part of the input text
        //look for the words "schedule", "meeting", or "appointment"
        //the "\b" around each word ensures that the method only looks for the whole word, not part of a larger word
        //case-insensive: "schedule" = "sCHeDuLe" = "SCHEDULE"
    }

    public void AddMessage(string message, bool isUser)
    {
        _messages.Add((message, isUser));
        //add a new message to the message list
        //isUser is a boolean flag where "true" indicates the message is from the user, and "false" indicates it is a system response
    }


    //Creating UI elements
    private void DrawRoundedRectangle(int x, int y, int width, int height, Color color, int cornerRadius)
    {
        // Draw the central rectangle
        _window.FillRectangle(color, x + cornerRadius, y, width - 2 * cornerRadius, height);

        // Draw the top and bottom rectangles
        _window.FillRectangle(color, x, y + cornerRadius, width, height - 2 * cornerRadius);

        // Draw the four corners as quarter-circles
        _window.FillCircle(color, x + cornerRadius, y + cornerRadius, cornerRadius);
        _window.FillCircle(color, x + width - cornerRadius, y + cornerRadius, cornerRadius);
        _window.FillCircle(color, x + cornerRadius, y + height - cornerRadius, cornerRadius);
        _window.FillCircle(color, x + width - cornerRadius, y + height - cornerRadius, cornerRadius);
    }

  
    public void Draw()
    {
        const int textPadding = 10;
        int cornerRadius = 10; 
        int messageHeight = 20; 

        _window.Clear(Color.White); //clearing the window and fills it with white color
        _window.DrawText("> " + _text, Color.Black, _font, _fontSize, 10, _window.Height - 30); //drawing the input prompt starting with ">"

        int y = 20;
        
        foreach (var (message, isUser) in _messages)
        {
            //for each message, caculate the with of the message to determine the width of the message box
            int textWidth = SplashKit.TextWidth(message, _font, _fontSize);
            int messageWidth = Math.Min(textWidth + 20, _window.Width - 40); 

            if (isUser) //if the message is user input
            {
                // Draw user messages on the right
                int x = _window.Width - messageWidth - 10;
                DrawRoundedRectangle(x, y, messageWidth, messageHeight, Color.LightBlue, cornerRadius);
                _window.DrawText(message, Color.Blue, _font, _fontSize, x + textPadding, y + (messageHeight / 2 - 10));
            }
            else
            {
                // Draw program responses on the left
                DrawRoundedRectangle(10, y, messageWidth, messageHeight, Color.LightGreen, cornerRadius);
                _window.DrawText(message, Color.Green, _font, _fontSize, 15, y + (messageHeight / 2 - 10));
            }
            y += messageHeight + 10; // Increment y by message height plus some padding
        }
    }


}


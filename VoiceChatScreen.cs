
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using NAudio.SoundFont;
using SplashKitSDK;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public class VoiceChatScreen : IMessageHandler
{
    private Window _window;
    private string _inputText = "";
    private bool _isRecording = false;
    private SpeechRecognizer _recognizer;
    private Font _font;
    private int _fontSize = 12;
    private IServiceHandler _calendarHandler;
    private IServiceHandler _openAIHandler;
    private WeatherAPIAdapter _weatherAPIAdapter;
    private List<(string message, bool isUser)> _messages = new List<(string message, bool isUser)>();

    public VoiceChatScreen(Window window, WeatherAPIAdapter weatherAPIAdapter, IServiceHandler calendarHandler, IServiceHandler openAIHandler)
    {
        _window = window;
        _calendarHandler = calendarHandler;
        _openAIHandler = openAIHandler;
        _weatherAPIAdapter = weatherAPIAdapter;
        _font = SplashKit.LoadFont("Roboto", "fonts/Roboto-Regular.ttf");
        _fontSize = 16; // Setting a specific font size for clarity
        InitializeVoiceBot();
    }

    public void HandleInput()
    {
        foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
        {
            if (SplashKit.KeyTyped(key))
            {
                Console.WriteLine($"Key pressed: {key}"); //list the keys typed 

                //If "Enter" / "Return" key is pressed, it calls the ToggleRecording method, which might start/stop recording
                if (key == KeyCode.ReturnKey)
                {
                    Console.WriteLine("Enter pressed with text: " + _inputText);
                    ToggleRecording();
                }


                //If "Backspace" key is pressed and there is text in the "_inputText" buffer, it removes the last character from this buffer and logs the updated text
                else if (key == KeyCode.BackspaceKey && _inputText.Length > 0)
                {
                    _inputText = _inputText.Substring(0, _inputText.Length - 1);
                    Console.WriteLine("Backspace pressed, new text: " + _inputText);
                }

                //For any other keys, it appends the character corresponding to the pressed key to the "_inputText" buffer and logs the updated text
                else if (key != KeyCode.BackspaceKey && key != KeyCode.ReturnKey)
                {
                    _inputText += (char)key;
                    Console.WriteLine("Current text: " + _inputText);
                }
            }
        }

        // Check if the left mouse button is clicked to start/stop recording
        if (SplashKit.MouseClicked(MouseButton.LeftButton))
        {
            //Get mouse position and check within specific coordinates to call the "ToggleRecording" method
            float mouseX = SplashKit.MouseX();
            float mouseY = SplashKit.MouseY();
            
            if (mouseX > 250 && mouseX < 350 && mouseY > 50 && mouseY < 80)
            {
                ToggleRecording();
            }
        }
    }

    public void Draw()
    {
        const int textPadding = 10;
        int cornerRadius = 10; // Radius for rounded corners
        int messageHeight = 20; // Adjust height based on font size

        _window.Clear(Color.White);
        _window.DrawText("Voice Chat:", Color.Black, _font, _fontSize, 50, 20);
        DrawRoundedRectangle(50, 50, _window.Width - 100, 40, Color.LightGray, 10);
        _window.DrawText("> " + _inputText, Color.Black, _font, _fontSize, 60, 60);

        // Draw button
        string buttonLabel = _isRecording ? "Stop Recording" : "Start Recording";
        DrawRoundedRectangle(250, 50, 100, 30, Color.LightBlue, 10);
        _window.DrawText(buttonLabel, Color.Blue, _font, _fontSize, 260, 55);

        // Draw messages
        int y = 100;
        foreach (var (message, isUser) in _messages)
        {
            int textWidth = SplashKit.TextWidth(message, _font, _fontSize);
            int messageWidth = Math.Min(textWidth + 20, _window.Width - 40); // Add some padding and limit width

            if (isUser)
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

        _window.Refresh();
    }

    //Add message to the message history, differentiating between the user and system response
    public void AddMessage(string message, bool isUser)
    {
        _messages.Add((message, isUser));
    }

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

    private void ToggleRecording()
    {
        if (_isRecording)
        {
            //call the "StopVoiceBot" method and set the "isRecording" flag to false, indicating the recording has stopped
            StopVoiceBot();
            _isRecording = false;
        }
        else
        {
            //check if there is text in "_inputText", start the voice bot
            if (!string.IsNullOrEmpty(_inputText))
            {
                StartVoiceBot(_inputText);
                _inputText = ""; // Clear the input field after starting recognition
            }
            _isRecording = true;
        }
    }

    private async void StartVoiceBot(string filePath)
    {
        try
        {
            var audioConfig = AudioConfig.FromWavFileInput(filePath); //configure the audio input with wav filepath
            var speechConfig = SpeechConfig.FromSubscription("b7500466c779418b9a01583f02cdc8f5", "eastus"); //authenticate and connect to the speech service
            _recognizer = new SpeechRecognizer(speechConfig, audioConfig);

            //recognizing: logs partial results of the recognition process
            _recognizer.Recognizing += (s, e) =>
            //+=: attach an event handler to an event
            //s: sender (object that raised the event), e: eventArgs (contain event details)
            // (s, e) => lambda operator, define inline functions
            
            {
                Console.WriteLine($"Recognizing: {e.Result.Text}"); //log the text that is being recognized
            };

            //recognized: the speech is successfully recognized
            _recognizer.Recognized += (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedSpeech) //if the reason for the event trigger is the successful speech recognition
                {
                    Console.WriteLine($"Recognized: {e.Result.Text}"); //log the recognized text
                    HandleRecognizedCommand(e.Result.Text);
                }
                else if (e.Result.Reason == ResultReason.NoMatch) //if no recognizable speech could be found
                {
                    Console.WriteLine("No speech could be recognized."); //log the error message
                }
            };

            _recognizer.Canceled += (s, e) =>
            {
                Console.WriteLine($"Canceled: {e.Reason}"); //log the reason for the cancellation (such as direct cancellation command)
                if (e.Reason == CancellationReason.Error)
                {
                    Console.WriteLine($"ErrorDetails: {e.ErrorDetails}");
                }
            };

            _recognizer.SessionStarted += (s, e) =>
            {
                Console.WriteLine("Session started."); //indicate that the recording session has started
            };

            _recognizer.SessionStopped += (s, e) =>
            {
                ToggleRecording();
                Console.WriteLine("Session stopped.");//indicate that the recording session has stopped
            };

            await _recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);
            Console.WriteLine("Recognition started for file: " + filePath); //log the recognition process has started for a specific file, including the filepath
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error starting voice recognition: " + ex.Message); //handle error
        }
    }

    private async void HandleRecognizedCommand(string command)
    {
        AddMessage(command, true); // Display user command in chat
        await ProcessInput(command);
    }

    //Route to the suitable service handlers by analyzing content
    private async Task ProcessInput(string input)
    {
        if (input.ToLower().Contains("weather"))
        {
            await _weatherAPIAdapter.HandleCommand(input, this);
        }
        else if (IsCalendarRequest(input))  
        {
            await _calendarHandler.HandleCommand(input, this);
        }
        else
        {
            await _openAIHandler.HandleCommand(input, this);
        }
    }

    //check if the message is a calendar request
    private bool IsCalendarRequest(string text)
    {
        return Regex.IsMatch(text, @"\b(schedule|meeting|appointment)\b", RegexOptions.IgnoreCase);
    }

    //stop the ongoing speech recognition session
    private async void StopVoiceBot()
    {
        if (_recognizer != null)
        {
            await _recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false); //asynchronously stop the speech recognition
            Console.WriteLine("Voice recognition stopped.");
            _recognizer.Dispose(); //dispose the "SpeechRecognizer" object
            _recognizer = null; //clean up references and manage memory
        }
    }

    //Set up the speech recognition capabilities 
    private async void InitializeVoiceBot()
    {
        try
        {
            var speechConfig = SpeechConfig.FromSubscription("b7500466c779418b9a01583f02cdc8f5", "eastus"); //configure service
            var audioConfig = AudioConfig.FromDefaultMicrophoneInput(); //set the audio input configuration to use the default microphone of the system
            _recognizer = new SpeechRecognizer(speechConfig, audioConfig); //instantiate "SpeechRecognizer" 
            await _recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false); //start the continuous recognition process asynchrnously until stopped
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing voice bot: {ex.Message}"); //handle error
        }
    }
}

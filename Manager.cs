using System;
using System.Net;
using System.Net.Sockets;
using SplashKitSDK;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.Threading.Tasks;

public class Manager
{
    private Window _window;
    private InputBox _inputBox;
    private Bitmap _background;
    private bool _displayChat = false;
    private bool _displayVoiceChat = false;  // Flag for voice chat UI
    private SpeechRecognizer _recognizer;
    private VoiceChatScreen _voiceChatScreen;  // Voice chat UI class
    private WeatherAPIAdapter _weatherService; 
    private IServiceHandler _calendarHandler;
    private IServiceHandler _openAIHandler;

    public static void Main()
    {
        new Manager().Run();
    }

    public Manager()
    {
        _window = new Window("Chat with Calendar", 1111, 625);
        _weatherService = new WeatherAPIAdapter(); // Directly using the adapter which encapsulates WeatherHandler
        _calendarHandler = new CalendarHandler(); 
        _openAIHandler = new OpenAIHandler(); 

        // Pass the handlers and services to the InputBox
        _inputBox = new InputBox(_window, _weatherService, _calendarHandler, _openAIHandler);

        //Initialize the voicebot with background and UI
        InitializeVoiceBot();
        _background = SplashKit.LoadBitmap("background", "image/background.png");
        _voiceChatScreen = new VoiceChatScreen(_window, _weatherService, _calendarHandler, _openAIHandler);
    }

    public void Run()
    {
        TcpListener server = SetupServer(); //Initialize a TCP server

        while (!_window.CloseRequested) //keep the application running until a close event is requested
        {
            SplashKit.ProcessEvents();
            HandleUserInput();
            Draw();
        }

        //Clean up and close resources
        StopVoiceBot();
        server.Stop();
        _window.Close();
    }

    //Setting up and starting a TCP server that listens for incoming network connections on port 8081
    private TcpListener SetupServer()
    {
        TcpListener server = new TcpListener(IPAddress.Any, 8081);
        server.Start();
        Console.WriteLine("Server started on port 8081.");
        server.BeginAcceptTcpClient(new AsyncCallback(OnClientConnected), server);
        return server;
    }

    //Manage the user input
    private void HandleUserInput()
    {
        //capture the position of the mouse to decide whether the user chose ChatBot or VoiceBot
        if (SplashKit.MouseClicked(MouseButton.LeftButton))
        {
            float mouseX = SplashKit.MouseX();
            float mouseY = SplashKit.MouseY();

            if (mouseX > 236 && mouseX < 381 && mouseY > 429 && mouseY < 495)
            {
                _displayChat = true;
                _displayVoiceChat = false;
            }
            else if (mouseX > 765 && mouseX < 911 && mouseY > 432 && mouseY < 496)
            {
                _displayVoiceChat = true;
                _displayChat = false;
            }
        }

        if (_displayVoiceChat)
        {
            _voiceChatScreen.HandleInput();
        }
        else if (_displayChat)
        {
            _inputBox.HandleInput();
        }
    }

    //Drawing the UI
    private void Draw()
    {
        _window.Clear(Color.White);
        if (_background != null)
        {
            _window.DrawBitmap(_background, 0, 0);
        }

        if (_displayChat)
        {
            _inputBox.Draw();
        }
        else if (_displayVoiceChat)
        {
            _voiceChatScreen.Draw();
        }
        _window.Refresh(60);
    }

    //Finalize the acceptance of a pending client connection and continue listening for additional connections
    private static void OnClientConnected(IAsyncResult ar)
    {
        TcpListener listener = (TcpListener)ar.AsyncState;//// Cast the AsyncState of the IAsyncResult object back to TcpListener, which is the object that started the async operation.
        TcpClient client = listener.EndAcceptTcpClient(ar); // End the asynchronous accept operation and retrieve the connected TcpClient object.
        Console.WriteLine("Client connected."); // Log to the console that a client has successfully connected.
        listener.BeginAcceptTcpClient(new AsyncCallback(OnClientConnected), listener);
        // Continue to asynchronously accept further client connections. This sets up the server to accept additional connections
        // while it deals with the current connection. The AsyncCallback(OnClientConnected) specifies this method to be called
        // again once a new client connection is ready to be processed, thus allowing the server to handle multiple simultaneous connections
    }

    //Speech recognition setup 
    private async void InitializeVoiceBot()
    {
        try
        {
            var speechConfig = SpeechConfig.FromSubscription("b7500466c779418b9a01583f02cdc8f5", "eastus");//configure service
            var audioConfig = AudioConfig.FromDefaultMicrophoneInput();//set the audio input configuration to use the default microphone of the system
            _recognizer = new SpeechRecognizer(speechConfig, audioConfig);//instantiate "SpeechRecognizer"
            await _recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);//start the continous recognition process asynchroously until stopped
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing voice bot: {ex.Message}");//handle error
        }
    }

    //Stop speech recognition
    private async void StopVoiceBot()
    {
        if (_recognizer != null)
        {
            await _recognizer.StopContinuousRecognitionAsync(); //asynchronously stop the speech recognition
        }
    }
}




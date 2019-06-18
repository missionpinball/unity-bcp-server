using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Text;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Threading;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Net;

/// <summary>
/// Singleton BCP Server class.  Manages the socket server to send and receive BCP messages.
/// </summary>
public class BcpServer : MonoBehaviour
{
    /// <summary>
    /// The BCP Server version
    /// </summary>
    public const string CONTROLLER_VERSION = "0.53.0";

    /// <summary>
    /// The BCP Server name
    /// </summary>
    public const string CONTROLLER_NAME = "Unity Media Controller";

    /// <summary>
    /// The BCP Specification document version implemented
    /// </summary>
    public const string BCP_SPECIFICATION_VERSION = "1.1";

    /// <summary>
    /// The TCP listener.
    /// </summary>
    private TcpListener _listener;

    /// <summary>
    /// The TCP client.
    /// </summary>
    private TcpClient _client;

    /// <summary>
    /// Private flag indicating whether or not the server is actively listening for a client connection.
    /// </summary>
	private volatile bool _listeningForClient;

    /// <summary>
    /// Private flag indicating whether or not a BCP message receiver/reader thread is currently running.
    /// </summary>
    private volatile bool _readerRunning;

    /// <summary>
    /// Gets the static singleton object instance.
    /// </summary>
    /// <value>
    /// The instance.
    /// </value>
	public static BcpServer Instance { get; private set; }

    /// <summary>
    /// Gets a value indicating whether a client is currently connected.
    /// </summary>
    /// <value>
    ///   <c>true</c> if a client is currently connected; otherwise, <c>false</c>.
    /// </value>
    public bool ClientConnected
    {
        get
        {
            return (_client != null && _client.Connected);
        }
    }


    /// <summary>
    /// Called when the script instance is being loaded.
    /// </summary>
    void Awake()
	{
		// Save a reference to the BcpServer component as our singleton instance
		Instance = this;
        _listeningForClient = false;
#if UNITY_EDITOR
		EditorPlayMode.PlayModeChanged += OnPlayModeChanged;
#endif

	}

    /// <summary>
    /// Called when the script instance is disabled.
    /// </summary>
	public void OnDisable()
	{
#if UNITY_EDITOR
		EditorPlayMode.PlayModeChanged -= OnPlayModeChanged;
#endif
	}
	
    /// <summary>
    /// Initializes the BCP server and launches a reader worker thread to receive message from the MPF pin controller.
    /// </summary>
    /// <param name="port">The port to listen on.</param>
	public void Init(int port)
    {
        BcpLogger.Trace("BcpServer: Initializing");
        BcpLogger.Trace("BcpServer: " + CONTROLLER_NAME + " " + CONTROLLER_VERSION);
        BcpLogger.Trace("BcpServer: BCP Specification Version " + BCP_SPECIFICATION_VERSION);

        // Create TCP/IP socket
        _listeningForClient = true;
        _listener = new TcpListener(IPAddress.Any, port);
        BcpLogger.Trace("BcpServer: Establishing local endpoint for the socket (" + _listener.LocalEndpoint.ToString() + ")");

        Thread listenerThread = new Thread(new ThreadStart(ListenForConnection));
        listenerThread.Start();
        BcpLogger.Trace("BcpServer: Waiting for a connection...");

	}
	

    /// <summary>
    /// Finalizes an instance of the <see cref="BcpServer"/> class.  Closes the BCP server if it is running.
    /// </summary>
	~BcpServer()
	{
		Close();
	}


    /// <summary>
    /// OnGUI is called for rendering and handling GUI events.
    /// </summary>
    void OnGUI()
    {
#if UNITY_EDITOR
        // Display button to shutdown the server application (only when running in the editor)
        if (EditorApplication.isPlaying)
        {
            if (GUI.Button(new Rect(10, 10, 150, 50), "Shutdown Server"))
            {
                Close();
                EditorApplication.isPlaying = false;
            }
        }
#endif

        // Display waiting for connection message if client is not currently connected
        if (!ClientConnected)
        {
            // Create popup window rectangle in the center of the screen
            int screenWidth = Screen.width;
            int screenHeight = Screen.height;

            int windowWidth = 500;
            int windowHeight = 80;
            int windowX = (screenWidth - windowWidth) / 2;
            int windowY = 0;
            // int windowY = (screenHeight - windowHeight) / 2;

            // Position the window in the center of the screen.
            Rect windowRect0 = new Rect(windowX, windowY, windowWidth, windowHeight);

            // Display waiting for connection popup window
            GUILayout.Window(0, windowRect0, WaitForConnectionMessage, "Mission Pinball Framework");
        }

    }

#if UNITY_EDITOR
    /// <summary>
    /// Event handler called when Editor play mode changes (only applies when running in the Editor).
    /// </summary>
    /// <param name="currentMode">The current mode.</param>
    /// <param name="changedMode">The changed mode.</param>
    private static void OnPlayModeChanged(PlayModeState currentMode, PlayModeState changedMode)
	{
		// Shut down the server if the editor is stopping (avoid hanging worker threads and locking up Unity)
		if (changedMode == PlayModeState.Stopped) {
			BcpLogger.Trace("BcpServer: Editor stopping - shutting down BCP Server");
			Instance.Close ();
		}
	}
#endif

    /// <summary>
    /// Window function to display waiting for connection message.
    /// </summary>
    /// <param name="windowId">The window identifier.</param>
    void WaitForConnectionMessage(int windowId)
    {
        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Client disconnected");
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("Waiting for connection from client...");
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }


    /// <summary>
    /// Listens for a connection from MPF pin controller client.
    /// </summary>
    /// <remarks>
    /// This function runs in its own thread.
    /// </remarks>
    private void ListenForConnection()
    {
        BcpLogger.Trace("BcpServer: ListenForConnection thread start");

        _listener.Start();

        while (_listeningForClient)
        {
            try
            {
                if (ClientConnected)
                {
                    Thread.Sleep(1000);
                }
                else
                {
                    // Blocks until a client has connected to the server
                    BcpLogger.Trace("BcpServer: Waiting for client to connect...");
                    _client = _listener.AcceptTcpClient();

                    // Create a thread to handle communication with connected client
                    Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientCommunications));
                    clientThread.Start(_client);
                }
            }
            catch (Exception e)
            {
                BcpLogger.Trace("BcpServer: ListenForConnection exception: " + e.ToString());
            }
        }

        BcpLogger.Trace("BcpServer: ListenForConnection thread finish");
    }

    /// <summary>
    /// Handles client communications with the MPF pin controller via TCP socket.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <remarks>
    /// This function runs in its own thread and receives all messages from the MPF pin controller client.  These messages are 
    /// posted to a message queue in a thread-safe manner for the main Unity thread to process them.  The use of a separate 
    /// thread allows the use of blocking methods (simpler) when communicating with the MPF client.
    /// </remarks>
    private void HandleClientCommunications(object client)
    {
        BcpLogger.Trace("BcpServer: HandleClientCommunications thread start");
        _readerRunning = true;
        TcpClient tcpClient = (TcpClient)client;
        NetworkStream clientStream = tcpClient.GetStream();

        StringBuilder messageBuffer = new StringBuilder(1024);
        byte[] buffer = new byte[1024];
        int bytesRead;

        while (_readerRunning)
        {
            bytesRead = 0;

            try
            {
                // Blocks until a client sends a message
                bytesRead = clientStream.Read(buffer, 0, 1024);
            
                if (bytesRead > 0)
                {
                    messageBuffer.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));

                    // Determine if message is complete (check for message termination character)
                    // If not complete, save the buffer contents and continue to read packets, appending
                    // to saved buffer.  Once completed, convert to a BCP message.
                    int terminationCharacterPos = 0;
                    while ((terminationCharacterPos = messageBuffer.ToString().IndexOf("\n")) > -1)
                    {
                        BcpLogger.Trace("BcpServer: >>>>>>>>>>>>>> Received raw message: " + messageBuffer.ToString(0, terminationCharacterPos + 1));

                        // Convert received data to a BcpMessage
                        BcpMessage message = BcpMessage.CreateFromRawMessage(messageBuffer.ToString(0, terminationCharacterPos + 1));
                        if (message != null)
                        {
                            BcpLogger.Trace("BcpServer: >>>>>>>>>>>>>> Received \"" + message.Command + "\" message: " + message.ToString());

                            // Add BCP message to the queue to be processed
                            BcpMessageManager.Instance.AddMessageToQueue(message);
                        }

                        // Remove the converted message from the buffer
                        messageBuffer.Remove(0, terminationCharacterPos + 1);
                    }
                }
                else
                {
                    // The client has disconnected from the server
                    _readerRunning = false;
                }
            }
            catch (Exception e)
            {
                // A socket error has occurred
                BcpLogger.Trace("BcpServer: Client reader thread exception: " + e.ToString());
                _readerRunning = false;
            }
        }

        BcpLogger.Trace("BcpServer: HandleClientCommunications thread finish");
        BcpLogger.Trace("BcpServer: Closing TCP/Socket client");
        tcpClient.Close();
    }


    /// <summary>
    /// Closes the BCP Server.
    /// </summary>
	public void Close()
	{
		BcpLogger.Trace("BcpServer: Close start");

        try
        {
            _listeningForClient = false;
            
            if (ClientConnected)
			{
				// Send goodbye message to connected client
				Send(BcpMessage.GoodbyeMessage());
                _client.Close();
			}

            _listener.Stop();
        }
        catch
        {
        }

        BcpLogger.Trace("BcpServer: Close finished");
    }

    
    /// <summary>
    /// Sends the specified BCP message to the MPF pin controller.
    /// </summary>
    /// <param name="message">The BCP message.</param>
    /// <remarks>
    /// This function is called by the main Unity thread and does not run in its own thread.  It will block the rest of the application
    /// while sending (should be a quick process unless MPF client has a communication failure).
    /// </remarks>
	public bool Send(BcpMessage message) 
    {
	    if (!ClientConnected)
            return false;

        try
        {
            NetworkStream clientStream = _client.GetStream();
            byte[] packet;
            int length = message.ToPacket(out packet);
            if (length > 0)
            {
                clientStream.Write(packet, 0, length);
                clientStream.Flush();
                BcpLogger.Trace("BcpServer: <<<<<<<<<<<<<< Sending \"" + message.Command + "\" message: " + message.ToString());
            }
        }
        catch (Exception e)
        {
            BcpLogger.Trace("BcpServer: Sending \"" + message.Command + "\" message FAILED: " + e.ToString());
            return false;
        }

        return true;
	}


    /// <summary>
    /// Sends a DMD frame to the MPF pin controller to be output to hardware.
    /// </summary>
    /// <param name="frameData">The DMD frame data (must be 4096 bytes).</param>
    /// <returns></returns>
    /// <remarks>
    /// Used by the media controller to send a DMD frame to the pin controller which the pin controller displays on the physical DMD. 
    /// Note that this command does not used named parameters, rather, the data is sent after the command, like this:
    /// dmd_frame?<raw byte string>
    /// This command is a special one in that it’s sent with ASCII encoding instead of UTF-8.
    /// The data is a raw byte string that is exactly 4096 bytes. (1 bytes per pixel, 128×32 DMD resolution = 4096 pixels.) The 4 low bits 
    /// of each byte are the intensity (0-15), and the 4 high bits are ignored.
    /// </remarks>
    public bool SendDmdFrame(byte[] frameData)
    {
        if (!ClientConnected)
            return false;

        try
        {
            if (frameData.Length != 4096)
                throw new ArgumentException("Frame data is not the correct length (" + frameData.Length + " bytes, expected 4096 bytes)", "frameData");

            NetworkStream clientStream = _client.GetStream();

            byte[] messageCommand = Encoding.ASCII.GetBytes("dmd_frame?");
            byte[] messageTermination = Encoding.ASCII.GetBytes("\n");

            clientStream.Write(messageCommand, 0, messageCommand.Length);
            clientStream.Write(frameData, 0, frameData.Length);
            clientStream.Write(messageTermination, 0, messageTermination.Length);
            clientStream.Flush();

            BcpLogger.Trace("BcpServer: <<<<<<<<<<<<<< Sending \"dmd_frame\" message: frame data (" + frameData.Length + " bytes)");
        }
        catch (Exception e)
        {
            BcpLogger.Trace("BcpServer: Sending \"dmd_frame\" message FAILED: " + e.ToString());
            return false;
        }

        return true;
    }
    
}

#if UNITY_EDITOR
/// <summary>
/// The Unity Editor play mode
/// </summary>
public enum PlayModeState
{
	Stopped,
	Playing,
	Paused
}

/// <summary>
/// Helper class for determining Unity Editor play mode state changes
/// </summary>
[InitializeOnLoad]
public class EditorPlayMode
{
    /// <summary>
    /// The current Unity Editor play mode state
    /// </summary>
	private static PlayModeState _currentState = PlayModeState.Stopped;

    /// <summary>
    /// Initializes the <see cref="EditorPlayMode"/> class.
    /// </summary>
	static EditorPlayMode()
	{
		EditorApplication.playModeStateChanged += OnUnityPlayModeChanged;
	}

    /// <summary>
    /// Event occurs when Unity Editor play mode is changed.
    /// </summary>
	public static event Action<PlayModeState, PlayModeState> PlayModeChanged;

    /// <summary>
    /// Sets the play mode state variable to "isPlaying".
    /// </summary>
	public static void Play()
	{
		EditorApplication.isPlaying = true;
	}

    /// <summary>
    /// Sets the play mode state variable to "isPaused".
    /// </summary>
    public static void Pause()
	{
		EditorApplication.isPaused = true;
	}

    /// <summary>
    /// Sets the play mode state variable to "stopped".
    /// </summary>
    public static void Stop()
	{
		EditorApplication.isPlaying = false;
	}


    /// <summary>
    /// Called when the Unity Editor play mode is changed.
    /// </summary>
    /// <param name="currentState">Current state of the Unity Editor.</param>
    /// <param name="changedState">New state of the Unity Editor.</param>
	private static void OnPlayModeChanged(PlayModeState currentState, PlayModeState changedState)
	{
		if (PlayModeChanged != null)
			PlayModeChanged(currentState, changedState);
	}

    /// <summary>
    /// Called when the Unity Editor play mode is changed.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
	private static void OnUnityPlayModeChanged(PlayModeStateChange state)
    {
		var changedState = PlayModeState.Stopped;
		switch (_currentState)
		{
		case PlayModeState.Stopped:
			if (EditorApplication.isPlayingOrWillChangePlaymode)
			{
				changedState = PlayModeState.Playing;
			}
			break;
		case PlayModeState.Playing:
			if (EditorApplication.isPaused)
			{
				changedState = PlayModeState.Paused;
			}
			else
			{
				changedState = PlayModeState.Stopped;
			}
			break;
		case PlayModeState.Paused:
			if (EditorApplication.isPlayingOrWillChangePlaymode)
			{
				changedState = PlayModeState.Playing;
			}
			else
			{
				changedState = PlayModeState.Stopped;
			}
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
		
		// Fire PlayModeChanged event.
		OnPlayModeChanged(_currentState, changedState);
		
		// Set current state.
		_currentState = changedState;
	}
	
}
#endif

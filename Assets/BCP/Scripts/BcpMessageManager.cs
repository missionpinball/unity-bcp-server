using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Text.RegularExpressions;


/// <summary>
/// A singleton class (only one instance) to manage sending and receiving of BCP messages between 
/// a pinball controller (PC) and the media controller (MC) server.
/// </summary>
/// <remarks>
/// The <see cref="BcpMessageManager"/> uses events to notify when specific messages are received.  Event handlers
/// can be added to these events.  The BCP Server events and delegates follow the standard convention used in Microsoft
/// .NET where the delegate method's first parameter is of type <see cref="Object"/> and refers to the instance that 
/// raises the event. Its second parameter is derived from type <see cref="EventArgs"/> (in this case <see cref="BcpMessageEventArgs"/>)
/// and holds the event data.
/// </remarks>
/// <example>
/// The following is a Unity script C# example that establishes an event handler for 'mode_start' BCP commands.
/// <code>
/// using UnityEngine;
/// 
/// public class ModeManager : MonoBehaviour
/// {
///     // Called just after the Unity object is enabled. This happens when a MonoBehaviour instance is created.
///     void OnEnbale()
///     {
///         // Adds an 'OnModeStart' event handler
///         BcpMessageManager.OnModeStart += ModeStarted;
///     }
/// 
///     // Called when the Unity object becomes disabled or inactive
///     void OnDisable()
///     {
///         // Removes an 'OnModeStart' event handler
///         BcpMessageManager.OnModeStart -= ModeStarted;
///     }
/// 
///     // OnModeStart event handler function
///     public void ModeStarted(object sender, ModeStartMessageEventArgs e)
///     {
///         // Put mode start code here
///         string modeName = e.Name;
///     }
/// }
/// </code>
/// </example>
public class BcpMessageManager : MonoBehaviour 
{
    /// <summary>
    /// The BCP Message Protocol version implemented by the BCP Server.
    /// </summary>
	public const string BCP_VERSION = "1.0";

    // Variables available to be edited in the Unity editor/object inspector
    
    /// <summary>
    /// The TCP listener port for the BCP Server.
    /// </summary>
	public int listenerPort = 9001;

    /// <summary>
    /// The message queue size (maximum number of messages that can be queued for processing).
    /// </summary>
    public int messageQueueSize = 100;


    #region Event Handling Delegates
    // Event handling delegates

    /// <summary>
    /// Represents the method that will handle a BCP message event that has default BCP message parameters.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="BcpMessageEventArgs"/> instance containing the event message data.</param>
    public delegate void BcpMessageEventHandler(object sender, BcpMessageEventArgs e);

    /// <summary>
    /// Represents the method that will handle a 'hello' BCP message event.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="HelloMessageEventArgs"/> instance containing the event message data.</param>
    public delegate void HelloMessageEventHandler(object sender, HelloMessageEventArgs e);

    /// <summary>
    /// Represents the method that will handle a 'ball_start' BCP message event.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="BallStartMessageEventArgs"/> instance containing the event message data.</param>
    public delegate void BallStartMessageEventHandler(object sender, BallStartMessageEventArgs e);

    /// <summary>
    /// Represents the method that will handle a 'error' BCP message event.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="ErrorMessageEventArgs"/> instance containing the event message data.</param>
    public delegate void ErrorMessageEventHandler(object sender, ErrorMessageEventArgs e);

    /// <summary>
    /// Represents the method that will handle a 'mode_start' BCP message event.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="ModeStartMessageEventArgs"/> instance containing the event message data.</param>
    public delegate void ModeStartMessageEventHandler(object sender, ModeStartMessageEventArgs e);

    /// <summary>
    /// Represents the method that will handle a 'mode_stop' BCP message event.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="ModeStopMessageEventArgs"/> instance containing the event message data.</param>
    public delegate void ModeStopMessageEventHandler(object sender, ModeStopMessageEventArgs e);

    /// <summary>
    /// Represents the method that will handle a 'player_score' BCP message event.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="PlayerScoreMessageEventArgs"/> instance containing the event message data.</param>
    public delegate void PlayerScoreMessageEventHandler(object sender, PlayerScoreMessageEventArgs e);

    /// <summary>
    /// Represents the method that will handle a 'player_added' BCP message event.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="PlayerAddedMessageEventArgs"/> instance containing the event message data.</param>
    public delegate void PlayerAddedMessageEventHandler(object sender, PlayerAddedMessageEventArgs e);

    /// <summary>
    /// Represents the method that will handle a 'player_turn_start' BCP message event.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="PlayerTurnStartMessageEventArgs"/> instance containing the event message data.</param>
    public delegate void PlayerTurnStartMessageEventHandler(object sender, PlayerTurnStartMessageEventArgs e);

    /// <summary>
    /// Represents the method that will handle a 'player_variable' BCP message event.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="PlayerVariableMessageEventArgs"/> instance containing the event message data.</param>
    public delegate void PlayerVariableMessageEventHandler(object sender, PlayerVariableMessageEventArgs e);

    /// <summary>
    /// Represents the method that will handle a 'switch' BCP message event.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="SwitchMessageEventArgs"/> instance containing the event message data.</param>
    public delegate void SwitchMessageEventHandler(object sender, SwitchMessageEventArgs e);

    /// <summary>
    /// Represents the method that will handle a 'trigger' BCP message event.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="TriggerMessageEventArgs"/> instance containing the event message data.</param>
    public delegate void TriggerMessageEventHandler(object sender, TriggerMessageEventArgs e);

    /// <summary>
    /// Represents the method that will handle a 'reset' BCP message event.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="ResetMessageEventArgs"/> instance containing the event message data.</param>
    public delegate void ResetMessageEventHandler(object sender, ResetMessageEventArgs e);

    /// <summary>
    /// Represents the method that will handle a 'config' BCP message event.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="BcpMessageEventArgs"/> instance containing the event message data.</param>
    public delegate void ConfigMessageEventHandler(object sender, BcpMessageEventArgs e);

    /// <summary>
    /// Represents the method that will handle a 'timer' BCP message event.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="TimerMessageEventArgs"/> instance containing the event message data.</param>
    public delegate void TimerMessageEventHandler(object sender, TimerMessageEventArgs e);

    #endregion

    #region Events
    // Event handling events
    /// <summary>
    /// Occurs when a BCP message is received.
    /// </summary>
    public static event BcpMessageEventHandler OnMessage;

    /// <summary>
    /// Occurs when a "hello" BCP message is received.  This is the initial handshake command upon first connection.  It sends the protocol version 
    /// that the origin controller speaks.  The media controller will respond with either its own "hello" command, or the error "unknown protocol version."  
    /// </summary>
    public static event HelloMessageEventHandler OnHello;

    /// <summary>
    /// Occurs when a "goodbye" BCP message is received.  Indicates the MPF pin controller is shutting down.
    /// </summary>
    public static event BcpMessageEventHandler OnGoodbye;

    /// <summary>
    /// Occurs when an "attract_start" BCP message is received (notification that attract mode has started).
    /// </summary>
    public static event BcpMessageEventHandler OnAttractStart;

    /// <summary>
    /// Occurs when an "attract_stop" BCP message is received (notification that attract mode has stopped).  Typically this will be followed by a 
    /// "game_start" message, though it could also be followed by service mode starting.
    /// </summary>
    public static event BcpMessageEventHandler OnAttractStop;

    /// <summary>
    /// Occurs when a "ball_start" BCP message is received.  Indicates that a ball has started. It passes the player number (“1″, “2”, etc.) and the 
    /// ball number as parameters. This command will be sent every time a ball starts, even if the same player is shooting again after an extra ball.
    /// </summary>
    public static event BallStartMessageEventHandler OnBallStart;

    /// <summary>
    /// Occurs when a "ball_end" BCP message is received.  Indicates the current ball has ended.
    /// </summary>
    public static event BcpMessageEventHandler OnBallEnd;

    /// <summary>
    /// Occurs when a "game_start" BCP message is received.  Indicates that a game has started.
    /// </summary>
    public static event BcpMessageEventHandler OnGameStart;

    /// <summary>
    /// Occurs when a "game_end" BCP message is received.  Indicates that a game has ended.
    /// </summary>
    public static event BcpMessageEventHandler OnGameEnd;

    /// <summary>
    /// Occurs when an "error" BCP message is received.  Indicates that a recent BCP message sent to BCP Triggered an error.
    /// </summary>
    public static event ErrorMessageEventHandler OnError;

    /// <summary>
    /// Occurs when a "mode_start" BCP message is received.  Indicates the specified game mode has just started.
    /// </summary>
    public static event ModeStartMessageEventHandler OnModeStart;

    /// <summary>
    /// Occurs when a "mode_stop" BCP message is received.  Indicates the specified game mode has stopped.
    /// </summary>
    public static event ModeStopMessageEventHandler OnModeStop;

    /// <summary>
    /// Occurs when a "player_score" BCP message is received.  Indicates the current player's score has changed.  It’s possible 
    /// that these events will come in rapid succession.
    /// </summary>
    public static event PlayerScoreMessageEventHandler OnPlayerScore;

    /// <summary>
    /// Occurs when a "player_added" BCP message is received.  Indicates a player has just been added, with the player number 
    /// passed via the player parameter. If the machine is in attract mode, this will be posted before game_start. Typically 
    /// these events only occur during Ball 1.
    /// </summary>
    public static event PlayerAddedMessageEventHandler OnPlayerAdded;

    /// <summary>
    /// Occurs when a "player_turn_start" BCP message is received.  Indicates a new player’s turn has begun. If a player has an extra 
    /// ball, this command will not be sent between balls. (However a new "ball_start" message will be sent when the same player’s 
    /// additional balls start.
    /// </summary>
    public static event PlayerTurnStartMessageEventHandler OnPlayerTurnStart;

    /// <summary>
    /// Occurs when a "player_variable" BCP message is received.  This is a generic "catch all" which sends player-specific variables 
    /// to the media controller any time they change.
    /// </summary>
    public static event PlayerVariableMessageEventHandler OnPlayerVariable;

    /// <summary>
    /// Occurs when a "switch" BCP message is received.  This message is used to send switch inputs to things like video modes, high 
    /// score name entry, and service menu navigation. Note that the pin controller should not send the state of every switch change 
    /// at all times, as the media controller doesn’t need it and that would add lots of unnecessary commands. Instead the pin 
    /// controller should only send switches based on some mode of operation that needs them. (For example, when the video mode 
    /// starts, the pin controller would start sending the switch states of the flipper buttons, and when the video mode ends, it 
    /// would stop.)
    /// </summary>
    public static event SwitchMessageEventHandler OnSwitch;

    /// <summary>
    /// Occurs when a "trigger" BCP message is received.  This message allows the one side to trigger the other side to do something. 
    /// For example, the pin controller might send trigger commands to tell the media controller to start shows, play sound effects, 
    /// or update the display. The media controller might send a trigger to the pin controller to flash the strobes at the down beat 
    /// of a music track or to pulse the knocker in concert with a replay show.
    /// </summary>
    public static event TriggerMessageEventHandler OnTrigger;

    /// <summary>
    /// Occurs when a "reset" BCP message is received.
    /// </summary>
    public static event ResetMessageEventHandler OnReset;

    /// <summary>
    /// Occurs when a "config" BCP message is received.  
    /// </summary>
    public static event ConfigMessageEventHandler OnConfig;

    /// <summary>
    /// Occurs when a "set" BCP message is received.  Tells the other side to set the value of one or more variables. For sanity 
    /// reasons, all variable are to be lower case, must start with a letter, and may contain only lower case letters, numbers, 
    /// and underscores.  Variable names should be lowercased on arrival.  Variable names can be no more than 32 characters.  
    /// Variable values are of unbounded length.  A value can be blank.
    /// 
    /// Setting a variable should have an immediate effect. For example if the system audio volume is set, it is expected that 
    /// audio will immediate take on that volume value. Or if the high score is currently being displayed and its variable it set, 
    /// it should immediately update the display.
    /// </summary>
    public static event BcpMessageEventHandler OnSet;

    /// <summary>
    /// Occurs when a "get" BCP message is received.  Asks the other side to send the value of one or more variables.  Variable 
    /// names are to be stripped of leading and trailing spaces and lower-cased.  The other side responds with a “set” command.  
    /// If an unknown variable is requested, its value is returned as an empty string. For sanity reasons, all variable are to 
    /// be lower case, must start with a letter, and may contain only lowercase letters, numbers, and underscores.  Variable 
    /// names should be lowercased on arrival.  Variable names can be no more than 32 characters.
    /// </summary>
    public static event BcpMessageEventHandler OnGet;

    /// <summary>
    /// Occurs when a "timer" BCP message is received.  Notifies the media controller about timer action that needs to be 
    /// communicated to the player. 
    /// </summary>
    public static event TimerMessageEventHandler OnTimer;

    #endregion


    // Private variables

    /// <summary>
    /// The internal BCP message queue.
    /// </summary>
	private Queue<BcpMessage> _messageQueue = new Queue<BcpMessage>();

    /// <summary>
    /// The message queue lock (to support multi-threaded access).
    /// </summary>
	private object _queueLock = new object();

    /// <summary>
    /// The message callback function for all received BCP messages (called before any specific message handlers).
    /// </summary>
	private BcpMessageCallback _allMessageCallback;

    /// <summary>
    /// The BCP message handler callback table.  Stores callback functions for processing each BCP message command.
    /// </summary>
	private Hashtable _messageHandlerCallbackTable = new Hashtable();

    /// <summary>
    /// Gets the static singleton object instance.
    /// </summary>
    /// <value>
    /// The instance.
    /// </value>
	public static BcpMessageManager Instance { get; private set; }



    /// <summary>
    /// Called when the script instance is being loaded.
    /// </summary>
	void Awake()
	{
		// Save a reference to the BcpMessageHandler component as our singleton instance
        if (Instance == null)
		    Instance = this;
	}

    /// <summary>
    /// Called on the frame when a script is enabled just before any of the Update methods is called the first time.
    /// </summary>
    /// <remarks>
    /// Sets up internal message handler callback functions for processing received BCP messages.  Also initiates the 
    /// socket communications between the pinball controller and media controller server (Unity).
    /// </remarks>
	void Start() {
		// Setup message handler callback functions for processing received messages
        BcpLogger.Trace("Setting up message handler callback functions");
		SetAllMessageCallback (AllMessageHandler);
		SetMessageCallback("hello", HelloMessageHandler);
		SetMessageCallback("goodbye", GoodbyeMessageHandler);
        SetMessageCallback("attract_start", AttractStartMessageHandler);
        SetMessageCallback("attract_stop", AttractStopMessageHandler);
        SetMessageCallback("game_start", GameStartMessageHandler);
        SetMessageCallback("game_end", GameEndMessageHandler);
        SetMessageCallback("ball_start", BallStartMessageHandler);
        SetMessageCallback("ball_end", BallEndMessageHandler);
        SetMessageCallback("mode_start", ModeStartMessageHandler);
		SetMessageCallback("mode_stop", ModeStopMessageHandler);
        SetMessageCallback("player_added", PlayerAddedMessageHandler);
        SetMessageCallback("player_turn_start", PlayerTurnStartMessageHandler);
        SetMessageCallback("player_variable", PlayerVariableMessageHandler);
        SetMessageCallback("player_score", PlayerScoreMessageHandler);
        SetMessageCallback("switch", SwitchMessageHandler);
        SetMessageCallback("trigger", TriggerMessageHandler);
        SetMessageCallback("error", ErrorMessageHandler);
        SetMessageCallback("reset", ResetMessageHandler);
        SetMessageCallback("config", ConfigMessageHandler);
        SetMessageCallback("set", SetMessageHandler);
        SetMessageCallback("get", GetMessageHandler);
        SetMessageCallback("timer", TimerMessageHandler);

		// Setup the socket communications between PC and MC (Unity) (start listening)
        BcpLogger.Trace("Setting up BCP server (listening on port " + listenerPort.ToString() + ")");
        BcpServer.Instance.Init(listenerPort);
		
	}

	
    /// <summary>
    /// Processes any BCP messages that have been received and queued for processing.
    /// </summary>
    /// <remarks>
    /// Update is called every frame, if the MonoBehaviour is enabled.  This function runs in the main Unity thread and 
    /// must access the received message queue in a thread-safe manner.
    /// </remarks>
	void Update () {

		BcpMessage currentMessage = null;
        bool checkMessages = true;

        // Process messages as long as there are messages in the received queue
        while (checkMessages)
        {
            // The MessageQueue must be accessed in a thread-safe manner (using a lock) since several different
            // threads access it.
            lock (_queueLock)
            {

                // Check for new messages
                if (_messageQueue.Count > 0)
                {
                    // Remove message from queue so it can be processed
                    currentMessage = _messageQueue.Dequeue();
                }
                else
                {
                    // Queue is empty, nothing to do right now
                    currentMessage = null;
                    checkMessages = false;
                }
            }

            if (currentMessage != null)
            {
                // Process message (call message handler callback functions)
                BcpLogger.Trace("Processing \"" + currentMessage.Command + "\" message");

                try
                {
                    // First, call all message handler function (if one is set)
                    if (_allMessageCallback != null)
                        _allMessageCallback(currentMessage);

                    // Call specific message handler function
                    BcpMessageCallback messageCallback = _messageHandlerCallbackTable[currentMessage.Command] as BcpMessageCallback;
                    if (messageCallback != null)
                    {
                        messageCallback(currentMessage);
                    }
                    else
                    {
                        // Unknown message, write error to log
                        BcpLogger.Trace("Unknown BCP message '" + currentMessage.Command + "' (no message handler set)");
                    }
                }
                catch (Exception e)
                {
                    // Unknown message, write error to log
                    BcpLogger.Trace("An exception occurred while processing '" + currentMessage.Command + "' message (" + currentMessage.RawMessage + "): " + e.ToString());
                }

            }
        }

	}

    /// <summary>
    /// Adds a BCP message to the queue for processing.
    /// </summary>
    /// <param name="message">The BCP message.</param>
    /// <remarks>
    /// This function may be accessed from several different threads and therefore must use thread-safe access methods.
    /// </remarks>
	public void AddMessageToQueue(BcpMessage message) {
		lock (_queueLock) {
			if (_messageQueue.Count < messageQueueSize) {
				_messageQueue.Enqueue(message);
			}
		}
	}


    /// <summary>
    /// Sets the method to call back on when any message is received.
    /// </summary>
    /// <param name="messageCallback">The message callback function.</param>
	protected void SetAllMessageCallback(BcpMessageCallback messageCallback)
	{
		_allMessageCallback = messageCallback;
	}

    /// <summary>
    /// Sets the callback function to be called for the specific message command
    /// </summary>
    /// <param name="key">The key (message command name/text).</param>
    /// <param name="messageCallback">The message callback function.</param>
	protected void SetMessageCallback(string key, BcpMessageCallback messageCallback)
	{
		_messageHandlerCallbackTable.Add(key, messageCallback);
	}

    /// <summary>
    /// Converts an unprocessed socket packet to a BCP message.
    /// </summary>
    /// <param name="buffer">The packet buffer.</param>
    /// <param name="length">The buffer length.</param>
    /// <returns><see cref="BcpMessage"/></returns>
	public static BcpMessage PacketToBcpMessage(byte[] buffer, int length) {
		return StringToBcpMessage(Encoding.UTF8.GetString (buffer, 0, length));
	}


    /// <summary>
    /// Converts a message string (in URL text format) to a BCP message.
    /// </summary>
    /// <param name="rawMessageBuffer">The raw message string/buffer.</param>
    /// <returns><see cref="BcpMessage"/></returns>
	public static BcpMessage StringToBcpMessage(string rawMessageBuffer) {
		BcpMessage bcpMessage = new BcpMessage ();
		
		// Remove line feed and carriage return characters
		rawMessageBuffer = rawMessageBuffer.Replace("\n", String.Empty);
		rawMessageBuffer = rawMessageBuffer.Replace("\r", String.Empty);

        bcpMessage.RawMessage = rawMessageBuffer;
        
        // Message text occurs before the question mark (?)
		if (rawMessageBuffer.Contains ("?")) {
			int messageDelimiterPos = rawMessageBuffer.IndexOf ('?');
			bcpMessage.Command = WWW.UnEscapeURL(rawMessageBuffer.Substring (0, messageDelimiterPos)).Trim();
			rawMessageBuffer = rawMessageBuffer.Substring (rawMessageBuffer.IndexOf ('?') + 1);
			
			foreach (string parameter in Regex.Split(rawMessageBuffer, "&"))
			{
				string[] parameterValuePair = Regex.Split(parameter, "=");
				if (parameterValuePair.Length == 2)
				{
					bcpMessage.Parameters.Add(WWW.UnEscapeURL(parameterValuePair[0]).Trim(), WWW.UnEscapeURL(parameterValuePair[1]).Trim());
				}
				else
				{
					// only one key with no value specified in query string
					bcpMessage.Parameters.Add(WWW.UnEscapeURL(parameterValuePair[0].Trim()), string.Empty);
				}
			}
			
		} 
        else 
        {
			// No parameters in the message, the entire message contains just the message text
			bcpMessage.Command = WWW.UnEscapeURL(rawMessageBuffer).Trim();
		}
		
		return bcpMessage;
	}

    /// <summary>
    /// Converts a BCP message to a socket packet to be sent to a pinball controller.
    /// </summary>
    /// <param name="message">The BCP message.</param>
    /// <param name="packet">The generated packet (by reference).</param>
    /// <returns>The length of the generated packet</returns>
	public static int BcpMessageToPacket(BcpMessage message, out byte[] packet) 
    {
		StringBuilder parameters = new StringBuilder ();
		foreach (string name in message.Parameters) 
        {
			if (parameters.Length > 0)
				parameters.Append("&");
			else
				parameters.Append("?");

			parameters.Append(WWW.EscapeURL(name));
			string value = message.Parameters[name];
			if (!String.IsNullOrEmpty(value))
				parameters.Append("=" + WWW.EscapeURL(value));

		}

        // Apply message termination character (line feed)
        parameters.Append("\n");
        
        packet = Encoding.UTF8.GetBytes(WWW.EscapeURL(message.Command) + parameters.ToString());
		return packet.Length;
	}


	/************************************************************************************
	 * Internal BCP Message Handler functions (called when a specific message type is received)
	 ************************************************************************************/

    /// <summary>
    /// Internal message handler callback function for all BCP messages received (called before individual message handlers). 
    /// Raises the <see cref="OnMessage"/> event.
    /// </summary>
    /// <param name="message">The BCP message.</param>
	protected void AllMessageHandler(BcpMessage message) 
    {
        // Raise the OnMessage event by invoking the delegate.
        if (OnMessage != null)
        {
            try
            {
                OnMessage(this, new BcpMessageEventArgs(message));
            }
            catch (Exception e)
            {
                BcpServer.Instance.Send(BcpMessage.ErrorMessage("An error occurred in the AllMessageHandler while processing a '" + message.Command + "' message: " + e.Message, message.RawMessage));
            }
        }
    }


    /// <summary>
    /// Internal message handler for all "hello" messages. Raises the <see cref="OnHello"/> event.
    /// </summary>
    /// <param name="message">The "hello" BCP message.</param>
    protected void HelloMessageHandler(BcpMessage message) {

        string version = message.Parameters["version"] ?? String.Empty;
        if (String.IsNullOrEmpty(version))
        {
            BcpLogger.Trace("ERROR: 'hello' message did not contain a valid value for the 'version' parameter");
            BcpServer.Instance.Send(BcpMessage.ErrorMessage("The 'hello' message did not contain a valid value for the 'version' parameter", message.RawMessage, message.Id));
        }
        else
        {
            if (version == BCP_VERSION)
                BcpServer.Instance.Send(BcpMessage.HelloMessage(BCP_VERSION));
            else
                BcpServer.Instance.Send(BcpMessage.ErrorMessage("unknown protocol version", message.RawMessage, message.Id));
        }

        // Raise the OnHello event by invoking the delegate. Pass in 
        // the object that initated the event (this) as well as the BcpMessage. 
        if (OnHello != null)
        {
            try
            {
                OnHello(this, new HelloMessageEventArgs(message, version));
            }
            catch (Exception e)
            {
                BcpServer.Instance.Send(BcpMessage.ErrorMessage("An error occurred while processing a '" + message.Command + "' message: " + e.Message, message.RawMessage));
            }
        }
	}

    /// <summary>
    /// Internal message handler for all "goodbye" messages. Raises the <see cref="OnGoodbye"/> event.
    /// </summary>
    /// <param name="message">The "goodbye" BCP message.</param>
    protected void GoodbyeMessageHandler(BcpMessage message)
    {
        // Raise the OnGoodbye event by invoking the delegate. Pass in 
        // the object that initated the event (this) as well as the BcpMessage. 
        if (OnGoodbye != null)
        {
            try
            {
                OnGoodbye(this, new BcpMessageEventArgs(message));
            }
            catch (Exception e)
            {
                BcpServer.Instance.Send(BcpMessage.ErrorMessage("An error occurred while processing a '" + message.Command + "' message: " + e.Message, message.RawMessage));
            }
        }
        
        // Shutdown the media controller server (close socket listener)
		BcpServer.Instance.Close();

		// Shutdown the Unity application
#if UNITY_EDITOR
        if (EditorApplication.isPlaying)
			EditorApplication.isPlaying = false;
#endif
		Application.Quit (); 
	}


    /// <summary>
    /// Internal message handler for all "attract_start" messages. Raises the <see cref="OnAttractStart"/> event.
    /// </summary>
    /// <param name="message">The "attract_start" BCP message.</param>
    protected void AttractStartMessageHandler(BcpMessage message)
    {
        // Raise the OnAttractStart event by invoking the delegate.
        if (OnAttractStart != null)
        {
            try
            {
                OnAttractStart(this, new BcpMessageEventArgs(message));
            }
            catch (Exception e)
            {
                BcpServer.Instance.Send(BcpMessage.ErrorMessage("An error occurred while processing a '" + message.Command + "' message: " + e.Message, message.RawMessage));
            }
        }

    }

    /// <summary>
    /// Internal message handler for all "attract_stop" messages. Raises the <see cref="OnAttractStop"/> event.
    /// </summary>
    /// <param name="message">The "attract_stop" BCP message.</param>
    protected void AttractStopMessageHandler(BcpMessage message)
    {
        // Raise the OnAttractStop event by invoking the delegate.
        if (OnAttractStop != null)
        {
            try
            {
                OnAttractStop(this, new BcpMessageEventArgs(message));
            }
            catch (Exception e)
            {
                BcpServer.Instance.Send(BcpMessage.ErrorMessage("An error occurred while processing a '" + message.Command + "' message: " + e.Message, message.RawMessage));
            }
        }

    }

    /// <summary>
    /// Internal message handler for all "game_start" messages. Raises the <see cref="OnGameStart"/> event.
    /// </summary>
    /// <param name="message">The "game_start" BCP message.</param>
    protected void GameStartMessageHandler(BcpMessage message)
    {
        if (OnGameStart != null)
        {
            try
            {
                OnGameStart(this, new BcpMessageEventArgs(message));
            }
            catch (Exception e)
            {
                BcpServer.Instance.Send(BcpMessage.ErrorMessage("An error occurred while processing a '" + message.Command + "' message: " + e.Message, message.RawMessage));
            }
        }
    }

    /// <summary>
    /// Internal message handler for all "game_end" messages. Raises the <see cref="OnGameEnd"/> event.
    /// </summary>
    /// <param name="message">The "game_end" BCP message.</param>
    protected void GameEndMessageHandler(BcpMessage message)
    {
        if (OnGameEnd != null)
        {
            try
            {
                OnGameEnd(this, new BcpMessageEventArgs(message));
            }
            catch (Exception e)
            {
                BcpServer.Instance.Send(BcpMessage.ErrorMessage("An error occurred while processing a '" + message.Command + "' message: " + e.Message, message.RawMessage));
            }
        }
    }

    /// <summary>
    /// Internal message handler for all "ball_start" messages. Raises the <see cref="OnBallStart"/> event.
    /// </summary>
    /// <param name="message">The "ball_start" BCP message.</param>
    protected void BallStartMessageHandler(BcpMessage message)
    {
        if (OnBallStart != null)
        {
            try
            {
                int player = int.Parse(message.Parameters["player"]);
                int ball = int.Parse(message.Parameters["ball"]);
                OnBallStart(this, new BallStartMessageEventArgs(message, player, ball));
            }
            catch (Exception e)
            {
                BcpServer.Instance.Send(BcpMessage.ErrorMessage("An error occurred while processing a '" + message.Command + "' message: " + e.Message, message.RawMessage));
            }
        }
	}


    /// <summary>
    /// Internal message handler for all "ball_end" messages. Raises the <see cref="OnBallEnd"/> event.
    /// </summary>
    /// <param name="message">The "ball_end" BCP message.</param>
    protected void BallEndMessageHandler(BcpMessage message)
    {
        if (OnBallEnd != null)
            OnBallEnd(this, new BcpMessageEventArgs(message));
    }

    /// <summary>
    /// Internal message handler for all "mode_start" messages. Raises the <see cref="OnModeStart"/> event.
    /// </summary>
    /// <param name="message">The "mode_start" BCP message.</param>
    protected void ModeStartMessageHandler(BcpMessage message)
    {
        if (OnModeStart != null)
        {
            try
            {
                string name = message.Parameters["name"];
                if (String.IsNullOrEmpty(name))
                    throw new ArgumentException("Message parameter value expected", "name");

                int priority = int.Parse(message.Parameters["priority"]);

                OnModeStart(this, new ModeStartMessageEventArgs(message, name, priority));
            }
            catch (Exception e)
            {
                BcpServer.Instance.Send(BcpMessage.ErrorMessage("An error occurred while processing a '" + message.Command + "' message: " + e.Message, message.RawMessage));
            }
        }
	}

    /// <summary>
    /// Internal message handler for all "mode_stop" messages. Raises the <see cref="OnModeStop"/> event.
    /// </summary>
    /// <param name="message">The "mode_stop" BCP message.</param>
    protected void ModeStopMessageHandler(BcpMessage message)
    {
        if (OnModeStop != null)
        {
            try
            {
                string name = message.Parameters["name"];
                if (String.IsNullOrEmpty(name))
                    throw new ArgumentException("Message parameter value expected", "name");

                OnModeStop(this, new ModeStopMessageEventArgs(message, name));
            }
            catch (Exception e)
            {
                BcpServer.Instance.Send(BcpMessage.ErrorMessage("An error occurred while processing a '" + message.Command + "' message: " + e.Message, message.RawMessage));
            }
        }
    }

    /// <summary>
    /// Internal message handler for all "player_added" messages. Raises the <see cref="OnPlayerAdded"/> event.
    /// </summary>
    /// <param name="message">The "player_added" BCP message.</param>
    protected void PlayerAddedMessageHandler(BcpMessage message)
    {
        if (OnPlayerAdded != null)
        {
            try
            {
                int number = int.Parse(message.Parameters["number"]);
                OnPlayerAdded(this, new PlayerAddedMessageEventArgs(message, number));
            }
            catch (Exception e)
            {
                BcpServer.Instance.Send(BcpMessage.ErrorMessage("An error occurred while processing a '" + message.Command + "' message: " + e.Message, message.RawMessage));
            }
        }
    }

    /// <summary>
    /// Internal message handler for all "player_turn_start" messages. Raises the <see cref="OnPlayerTurnStart"/> event.
    /// </summary>
    /// <param name="message">The "player_turn_start" BCP message.</param>
    protected void PlayerTurnStartMessageHandler(BcpMessage message)
    {
        if (OnPlayerTurnStart != null)
        {
            try
            {
                int player = int.Parse(message.Parameters["player"]);
                OnPlayerTurnStart(this, new PlayerTurnStartMessageEventArgs(message, player));
            }
            catch (Exception e)
            {
                BcpServer.Instance.Send(BcpMessage.ErrorMessage("An error occurred while processing a '" + message.Command + "' message: " + e.Message, message.RawMessage));
            }
        }
    }

    /// <summary>
    /// Internal message handler for all "player_variable" messages. Raises the <see cref="OnPlayerVariable"/> event.
    /// </summary>
    /// <param name="message">The "player_variable" BCP message.</param>
    protected void PlayerVariableMessageHandler(BcpMessage message)
    {
        if (OnPlayerVariable != null)
        {
            try
            {
                string name = message.Parameters["name"];
                if (String.IsNullOrEmpty(name))
                    throw new ArgumentException("Message parameter value expected", "name");

                string value = message.Parameters["value"];
                if (String.IsNullOrEmpty(value))
                    throw new ArgumentException("Message parameter value expected", "value");

                string previousValue = message.Parameters["prev_value"];
                if (String.IsNullOrEmpty(previousValue))
                    throw new ArgumentException("Message parameter value expected", "prev_value");

                string change = message.Parameters["change"];
                if (String.IsNullOrEmpty(change))
                    throw new ArgumentException("Message parameter value expected", "change");

                OnPlayerVariable(this, new PlayerVariableMessageEventArgs(message, name, value, previousValue, change));

            }
            catch (Exception e)
            {
                BcpServer.Instance.Send(BcpMessage.ErrorMessage("An error occurred while processing a '" + message.Command + "' message: " + e.Message, message.RawMessage));
            }
        }
    }

    /// <summary>
    /// Internal message handler for all "player_score" messages. Raises the <see cref="OnPlayerScore"/> event.
    /// </summary>
    /// <param name="message">The "player_score" BCP message.</param>
    protected void PlayerScoreMessageHandler(BcpMessage message)
    {
        if (OnPlayerScore != null)
        {
            try
            {
                int value = int.Parse(message.Parameters["value"]);
                int previousValue = int.Parse(message.Parameters["prev_value"]);
                int change = int.Parse(message.Parameters["change"]);

                OnPlayerScore(this, new PlayerScoreMessageEventArgs(message, value, previousValue, change));
            }
            catch (Exception e)
            {
                BcpServer.Instance.Send(BcpMessage.ErrorMessage("An error occurred while processing a '" + message.Command + "' message: " + e.Message, message.RawMessage));
            }
        }
    }


    /// <summary>
    /// Internal message handler for all "switch" messages. Raises the <see cref="OnSwitch"/> event.
    /// </summary>
    /// <param name="message">The "switch" BCP message.</param>
    protected void SwitchMessageHandler(BcpMessage message)
    {
        if (OnSwitch != null)
        {
            try
            {
                string name = message.Parameters["name"];
                if (String.IsNullOrEmpty(name))
                    throw new ArgumentException("Message parameter value expected", "name");

                int value = int.Parse(message.Parameters["value"]);

                OnSwitch(this, new SwitchMessageEventArgs(message, name, value));
            }
            catch (Exception e)
            {
                BcpServer.Instance.Send(BcpMessage.ErrorMessage("An error occurred while processing a '" + message.Command + "' message: " + e.Message, message.RawMessage));
            }
        }
    }

    /// <summary>
    /// Internal message handler for all "trigger" messages. Raises the <see cref="OnTrigger"/> event.
    /// </summary>
    /// <param name="message">The "trigger" BCP message.</param>
    protected void TriggerMessageHandler(BcpMessage message)
    {
        if (OnTrigger != null)
        {
            try
            {
                string name = message.Parameters["name"];
                if (String.IsNullOrEmpty(name))
                    throw new ArgumentException("Message parameter value expected", "name");

                OnTrigger(this, new TriggerMessageEventArgs(message, name));
            }
            catch (Exception e)
            {
                BcpServer.Instance.Send(BcpMessage.ErrorMessage("An error occurred while processing a '" + message.Command + "' message: " + e.Message, message.RawMessage));
            }
        }
    }

    /// <summary>
    /// Internal message handler for all "error" messages. Raises the <see cref="OnError"/> event.
    /// </summary>
    /// <param name="message">The "error" BCP message.</param>
    protected void ErrorMessageHandler(BcpMessage message)
    {
        if (OnError != null)
        {
            try
            {
                string text = message.Parameters["message"];
                if (String.IsNullOrEmpty(text))
                    throw new ArgumentException("Message parameter value expected", "message");

                OnError(this, new ErrorMessageEventArgs(message, text));
            }
            catch (Exception e)
            {
                BcpServer.Instance.Send(BcpMessage.ErrorMessage("An error occurred while processing a '" + message.Command + "' message: " + e.Message, message.RawMessage));
            }
        }
    }

    /// <summary>
    /// Internal message handler for all "reset" messages. Raises the <see cref="OnReset"/> event.
    /// </summary>
    /// <param name="message">The "reset" BCP message.</param>
    protected void ResetMessageHandler(BcpMessage message)
    {
        if (OnReset != null)
        {
            try
            {
                OnReset(this, new ResetMessageEventArgs(message, (message.Parameters["hard"] == null) ? false : bool.Parse(message.Parameters["hard"])));
            }
            catch (Exception e)
            {
                BcpServer.Instance.Send(BcpMessage.ErrorMessage("An error occurred while processing a '" + message.Command + "' message: " + e.Message, message.RawMessage));
            }
        }
    }

    /// <summary>
    /// Internal message handler for all "config" messages. Raises the <see cref="OnConfig"/> event.
    /// </summary>
    /// <param name="message">The "config" BCP message.</param>
    protected void ConfigMessageHandler(BcpMessage message)
    {
        if (OnConfig != null)
        {
            try
            {
                OnConfig(this, new BcpMessageEventArgs(message));
            }
            catch (Exception e)
            {
                BcpServer.Instance.Send(BcpMessage.ErrorMessage("An error occurred while processing a '" + message.Command + "' message: " + e.Message, message.RawMessage));
            }
        }
    }

    /// <summary>
    /// Internal message handler for all "set" messages. Raises the <see cref="OnSet"/> event.
    /// </summary>
    /// <param name="message">The "set" BCP message.</param>
    protected void SetMessageHandler(BcpMessage message)
    {
        if (OnSet != null)
        {
            try
            {
                OnSet(this, new BcpMessageEventArgs(message));
            }
            catch (Exception e)
            {
                BcpServer.Instance.Send(BcpMessage.ErrorMessage("An error occurred while processing a '" + message.Command + "' message: " + e.Message, message.RawMessage));
            }
        }
    }

    /// <summary>
    /// Internal message handler for all "get" messages. Raises the <see cref="OnGet"/> event.
    /// </summary>
    /// <param name="message">The "get" BCP message.</param>
    protected void GetMessageHandler(BcpMessage message)
    {
        if (OnGet != null)
        {
            try
            {
                OnGet(this, new BcpMessageEventArgs(message));
            }
            catch (Exception e)
            {
                BcpServer.Instance.Send(BcpMessage.ErrorMessage("An error occurred while processing a '" + message.Command + "' message: " + e.Message, message.RawMessage));
            }
        }
    }

    /// <summary>
    /// Internal message handler for all "timer" messages. Raises the <see cref="OnTimer"/> event.
    /// </summary>
    /// <param name="message">The "timer" BCP message.</param>
    protected void TimerMessageHandler(BcpMessage message)
    {
        if (OnTimer != null)
        {
            try
            {
                string name = message.Parameters["Name"];
                if (String.IsNullOrEmpty(name))
                    throw new ArgumentException("Message parameter value expected", "name");

                string action = message.Parameters["Action"];
                if (String.IsNullOrEmpty(action))
                    throw new ArgumentException("Message parameter value expected", "action");

                int ticks = int.Parse(message.Parameters["Ticks"]);

                OnTimer(this, new TimerMessageEventArgs(message, name, action, ticks));
            }
            catch (Exception e)
            {
                BcpServer.Instance.Send(BcpMessage.ErrorMessage("An error occurred while processing a '" + message.Command + "' message: " + e.Message, message.RawMessage));
            }
        }
    }

}



#region Message callback support classes

/// <summary>
/// BCP message handler callback function signature
/// </summary>
/// <param name="bcpMessage">The BCP message.</param>
public delegate void BcpMessageCallback(BcpMessage bcpMessage);


/// <summary>
/// Base class for all BCP message event arguments classes.  Contains the <see cref="BcpMessage"/> for the event.
/// </summary>
public class BcpMessageEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the BCP message.
    /// </summary>
    /// <value>
    /// The BCP message.
    /// </value>
    public BcpMessage BcpMessage { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BcpMessageEventArgs"/> class.
    /// </summary>
    /// <param name="bcpMessage">The BCP message.</param>
    public BcpMessageEventArgs(BcpMessage bcpMessage)
    {
        this.BcpMessage = bcpMessage;
    }
}

/// <summary>
/// Event arguments for the "hello" BCP message.
/// </summary>
public class HelloMessageEventArgs : BcpMessageEventArgs
{
    /// <summary>
    /// Gets or sets the BCP message protocol version.
    /// </summary>
    /// <value>
    /// The BCP message protocol version.
    /// </value>
    public string Version { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HelloMessageEventArgs"/> class.
    /// </summary>
    /// <param name="bcpMessage">The BCP message.</param>
    /// <param name="version">The BCP message protocol version.</param>
    public HelloMessageEventArgs(BcpMessage bcpMessage, string version) :
        base(bcpMessage)
    {
        this.Version = version;
    }
}

/// <summary>
/// Event arguments for the "ball_start" BCP message.
/// </summary>
public class BallStartMessageEventArgs : BcpMessageEventArgs
{
    /// <summary>
    /// Gets or sets the player number.
    /// </summary>
    /// <value>
    /// The player number.
    /// </value>
    public int Player { get; set; }

    /// <summary>
    /// Gets or sets the ball number.
    /// </summary>
    /// <value>
    /// The ball number.
    /// </value>
    public int Ball { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BallStartMessageEventArgs"/> class.
    /// </summary>
    /// <param name="bcpMessage">The BCP message.</param>
    /// <param name="player">The player number.</param>
    /// <param name="ball">The ball number.</param>
    public BallStartMessageEventArgs(BcpMessage bcpMessage, int player, int ball) :
        base(bcpMessage)
    {
        this.Player = player;
        this.Ball = ball;
    }
}


/// <summary>
/// Event arguments for the "error" BCP message.
/// </summary>
public class ErrorMessageEventArgs : BcpMessageEventArgs
{
    /// <summary>
    /// Gets or sets the error message text.
    /// </summary>
    /// <value>
    /// The error message text.
    /// </value>
    public string Message { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorMessageEventArgs"/> class.
    /// </summary>
    /// <param name="bcpMessage">The BCP message.</param>
    /// <param name="message">The error message text.</param>
    public ErrorMessageEventArgs(BcpMessage bcpMessage, string message) :
        base(bcpMessage)
    {
        this.Message = message;
    }
}


/// <summary>
/// Event arguments for the "mode_start" BCP message.
/// </summary>
public class ModeStartMessageEventArgs : BcpMessageEventArgs
{
    /// <summary>
    /// Gets or sets the mode name.
    /// </summary>
    /// <value>
    /// The mode name.
    /// </value>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the mode priority.
    /// </summary>
    /// <value>
    /// The mode priority.
    /// </value>
    public int Priority { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModeStartMessageEventArgs"/> class.
    /// </summary>
    /// <param name="bcpMessage">The BCP message.</param>
    /// <param name="name">The mode name.</param>
    /// <param name="priority">The mode priority.</param>
    public ModeStartMessageEventArgs(BcpMessage bcpMessage, string name, int priority) :
        base(bcpMessage)
    {
        this.Name = name;
        this.Priority = priority;
    }
}


/// <summary>
/// Event arguments for the "mode_stop" BCP message.
/// </summary>
public class ModeStopMessageEventArgs : BcpMessageEventArgs
{
    /// <summary>
    /// Gets or sets the mode name.
    /// </summary>
    /// <value>
    /// The mode name.
    /// </value>
    public string Name { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModeStopMessageEventArgs"/> class.
    /// </summary>
    /// <param name="bcpMessage">The BCP message.</param>
    /// <param name="name">The mode name.</param>
    public ModeStopMessageEventArgs(BcpMessage bcpMessage, string name) :
        base(bcpMessage)
    {
        this.Name = name;
    }
}


/// <summary>
/// Event arguments for the "player_score" BCP message.
/// </summary>
public class PlayerScoreMessageEventArgs : BcpMessageEventArgs
{
    /// <summary>
    /// Gets or sets the current player's score value.
    /// </summary>
    /// <value>
    /// The current player's score value.
    /// </value>
    public int Value { get; set; }

    /// <summary>
    /// Gets or sets the current player's previous score value.
    /// </summary>
    /// <value>
    /// The current player's previous score value.
    /// </value>
    public int PreviousValue { get; set; }

    /// <summary>
    /// Gets or sets the change in the current player's score.
    /// </summary>
    /// <value>
    /// The change in the current player's score.
    /// </value>
    public int Change { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerScoreMessageEventArgs"/> class.
    /// </summary>
    /// <param name="bcpMessage">The BCP message.</param>
    /// <param name="value">The current player's score value.</param>
    /// <param name="previousValue">The current player's previous score value.</param>
    /// <param name="change">The change in the current player's score.</param>
    public PlayerScoreMessageEventArgs(BcpMessage bcpMessage, int value, int previousValue, int change) :
        base(bcpMessage)
    {
        this.Value = value;
        this.PreviousValue = previousValue;
        this.Change = change;
    }
}


/// <summary>
/// Event arguments for the "player_added" BCP message.
/// </summary>
public class PlayerAddedMessageEventArgs : BcpMessageEventArgs
{
    /// <summary>
    /// Gets or sets the player number.
    /// </summary>
    /// <value>
    /// The player number.
    /// </value>
    public int Number { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerAddedMessageEventArgs"/> class.
    /// </summary>
    /// <param name="bcpMessage">The BCP message.</param>
    /// <param name="number">The player number.</param>
    public PlayerAddedMessageEventArgs(BcpMessage bcpMessage, int number) :
        base(bcpMessage)
    {
        this.Number = number;
    }
}


/// <summary>
/// Event arguments for the "player_turn_start" BCP message.
/// </summary>
public class PlayerTurnStartMessageEventArgs : BcpMessageEventArgs
{
    /// <summary>
    /// Gets or sets the player number.
    /// </summary>
    /// <value>
    /// The player number.
    /// </value>
    public int Player { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerTurnStartMessageEventArgs"/> class.
    /// </summary>
    /// <param name="bcpMessage">The BCP message.</param>
    /// <param name="player">The player number.</param>
    public PlayerTurnStartMessageEventArgs(BcpMessage bcpMessage, int player) :
        base(bcpMessage)
    {
        this.Player = player;
    }
}


/// <summary>
/// Event arguments for the "player_variable" BCP message.
/// </summary>
public class PlayerVariableMessageEventArgs : BcpMessageEventArgs
{
    /// <summary>
    /// Gets or sets the player variable name.
    /// </summary>
    /// <value>
    /// The player variable name.
    /// </value>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the player variable value.
    /// </summary>
    /// <value>
    /// The player variable value.
    /// </value>
    public string Value { get; set; }

    /// <summary>
    /// Gets or sets the previous value of the player variable.
    /// </summary>
    /// <value>
    /// The previous value of the player variable.
    /// </value>
    public string PreviousValue { get; set; }

    /// <summary>
    /// Gets or sets the change in value of the player variable.
    /// </summary>
    /// <value>
    /// The change in value of the player variable.
    /// </value>
    public string Change { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerVariableMessageEventArgs"/> class.
    /// </summary>
    /// <param name="bcpMessage">The BCP message.</param>
    /// <param name="name">The player variable name.</param>
    /// <param name="value">The player variable value.</param>
    /// <param name="previousValue">The previous value of the player variable.</param>
    /// <param name="change">The change in value of the player variable.</param>
    public PlayerVariableMessageEventArgs(BcpMessage bcpMessage, string name, string value, string previousValue, string change) :
        base(bcpMessage)
    {
        this.Name = name;
        this.Value = value;
        this.PreviousValue = previousValue;
        this.Change = change;
    }
}


/// <summary>
/// Event arguments for the "switch" BCP message.
/// </summary>
public class SwitchMessageEventArgs : BcpMessageEventArgs
{
    /// <summary>
    /// Gets or sets the switch name.
    /// </summary>
    /// <value>
    /// The switch name.
    /// </value>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the switch value.
    /// </summary>
    /// <value>
    /// The switch value.
    /// </value>
    public int Value { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SwitchMessageEventArgs"/> class.
    /// </summary>
    /// <param name="bcpMessage">The BCP message.</param>
    /// <param name="name">The switch name.</param>
    /// <param name="value">The switch value.</param>
    public SwitchMessageEventArgs(BcpMessage bcpMessage, string name, int value) :
        base(bcpMessage)
    {
        this.Name = name;
        this.Value = value;
    }
}


/// <summary>
/// Event arguments for the "trigger" BCP message.
/// </summary>
public class TriggerMessageEventArgs : BcpMessageEventArgs
{
    /// <summary>
    /// Gets or sets the trigger name.
    /// </summary>
    /// <value>
    /// The trigger name.
    /// </value>
    public string Name { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TriggerMessageEventArgs"/> class.
    /// </summary>
    /// <param name="bcpMessage">The BCP message.</param>
    /// <param name="name">The trigger name.</param>
    public TriggerMessageEventArgs(BcpMessage bcpMessage, string name) :
        base(bcpMessage)
    {
        this.Name = name;
    }
}


/// <summary>
/// Event arguments for the "reset" BCP message.
/// </summary>
public class ResetMessageEventArgs : BcpMessageEventArgs
{
    /// <summary>
    /// Gets or sets a value indicating whether this reset message is a hard reset.
    /// </summary>
    /// <value>
    ///   <c>true</c> if hard; otherwise, <c>false</c>.
    /// </value>
    public bool Hard { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResetMessageEventArgs"/> class.
    /// </summary>
    /// <param name="bcpMessage">The BCP message.</param>
    /// <param name="hard">if set to <c>true</c> [hard].</param>
    public ResetMessageEventArgs(BcpMessage bcpMessage, bool hard) :
        base(bcpMessage)
    {
        this.Hard = hard;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResetMessageEventArgs"/> class.
    /// </summary>
    /// <param name="bcpMessage">The BCP message.</param>
    public ResetMessageEventArgs(BcpMessage bcpMessage) :
        base(bcpMessage)
    {
        this.Hard = false;
    }
}


/// <summary>
/// Event arguments for the "timer" BCP message.
/// </summary>
public class TimerMessageEventArgs : BcpMessageEventArgs
{
    /// <summary>
    /// Gets or sets the name of the timer.
    /// </summary>
    /// <value>
    /// The timer name.
    /// </value>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the timer action.
    /// </summary>
    /// <value>
    /// The timer action.
    /// </value>
    public string Action { get; set; }

    /// <summary>
    /// Gets or sets the timer ticks.
    /// </summary>
    /// <value>
    /// The timer ticks.
    /// </value>
    public int Ticks { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TimerMessageEventArgs"/> class.
    /// </summary>
    /// <param name="bcpMessage">The BCP message.</param>
    /// <param name="name">The timer name.</param>
    /// <param name="action">The timer action.</param>
    /// <param name="ticks">The timer ticks.</param>
    public TimerMessageEventArgs(BcpMessage bcpMessage, string name, string action, int ticks) :
        base(bcpMessage)
    {
        this.Name = name;
        this.Action = action;
        this.Ticks = ticks;
    }

}



#endregion

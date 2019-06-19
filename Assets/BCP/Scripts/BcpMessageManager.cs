using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using BCP.SimpleJSON;
using UnityEngine.Networking;


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
///     void OnEnable()
///     {
///         // Adds an 'OnModeStart' event handler
///         BcpMessageController.OnModeStart += ModeStarted;
///     }
/// 
///     // Called when the Unity object becomes disabled or inactive
///     void OnDisable()
///     {
///         // Removes an 'OnModeStart' event handler
///         BcpMessageController.OnModeStart -= ModeStarted;
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
	public const string BCP_VERSION = "1.1";

    // Variables available to be edited in the Unity editor/object inspector
    
    /// <summary>
    /// The TCP listener port for the BCP Server.
    /// </summary>
	public int listenerPort = 9001;

    /// <summary>
    /// The message queue size (maximum number of messages that can be queued for processing).
    /// </summary>
    public int messageQueueSize = 100;

    [Header("Registered BCP Messages")]
    [Tooltip("Send all machine variables and their changes from MPF via BCP")]
    public bool machineVariables = true;

    [Tooltip("Send all player variables and their changes from MPF via BCP (includes player score)")]
    public bool playerVariables = true;

    [Tooltip("Send all switch messages from MPF via BCP")]
    public bool switches = true;

    [Tooltip("Send all mode messages (start, stop) from MPF via BCP")]
    public bool modes = true;

    [Tooltip("Send all core messages (ball, player turn, etc.) from MPF via BCP")]
    public bool coreEvents = true;

    [Tooltip("Send all high score messages from MPF via BCP")]
    public bool highScores = true;

    [Tooltip("Send all tilt messages from MPF via BCP")]
    public bool tilt = true;

    [Tooltip("Comma-separated list of timer names to register with MPF")]
    public string timers = String.Empty;

    [TextArea(3, 10)]
    [Tooltip("Comma-separated list of additional trigger names to register with MPF")]
    public string additionalTriggers = String.Empty;

    [Tooltip("Ignore unknown messages from MPF via BCP. When false, unknown messages will be logged as errors")]
    public bool ignoreUnknownMessages = true;

    // Private variables

    /// <summary>
    /// The BCP message controller
    /// </summary>
    private BcpMessageController _messageController;

    /// <summary>
    /// The internal BCP message queue.
    /// </summary>
	private Queue<BcpMessage> _messageQueue = new Queue<BcpMessage>();

    /// <summary>
    /// The message queue lock (to support multi-threaded access).
    /// </summary>
	private object _queueLock = new object();

    private JSONObject _settings = new JSONObject();

    /// <summary>
    /// The machine variable store.
    /// </summary>
    private JSONObject _machineVars = new JSONObject();

    /// <summary>
    /// The player variable store.
    /// </summary>
    private Dictionary<int, JSONObject> _playerVars = new Dictionary<int, JSONObject>();

    /// <summary>
    /// The current player number.
    /// </summary>
    private int _currentPlayer = 0;

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

        _messageController = new BcpMessageController(!String.IsNullOrEmpty(timers));
	}

    /// <summary>
    /// Called on the frame when a script is enabled just before any of the Update methods is called the first time.
    /// </summary>
    /// <remarks>
    /// Sets up internal message handler callback functions for processing received BCP messages.  Also initiates the 
    /// socket communications between the pinball controller and media controller server (Unity).
    /// </remarks>
	void Start()
    {
		// Setup message handler callback functions for processing received messages
        BcpLogger.Trace("Setting up message handler callback functions");

        // Setup the socket communications between PC and MC (Unity) (start listening)
        BcpLogger.Trace("Setting up BCP server (listening on port " + listenerPort.ToString() + ")");
        BcpServer.Instance.Init(listenerPort);

        // Register message event handlers
        BcpMessageController.OnHello += Hello;
        BcpMessageController.OnGoodbye += Goodbye;
        BcpMessageController.OnSettings += Settings;
        BcpMessageController.OnMachineVariable += MachineVariable;
        BcpMessageController.OnPlayerVariable += PlayerVariable;
        BcpMessageController.OnModeStop += ModeStop;
        BcpMessageController.OnPlayerTurnStart += PlayerTurnStart;
    }

    /// <summary>
    /// Called when the MonoBehaviour will be destroyed..
    /// </summary>
    void OnDestroy()
    {
        // Unregister event handlers
        BcpMessageController.OnHello -= Hello;
        BcpMessageController.OnGoodbye -= Goodbye;
        BcpMessageController.OnSettings -= Settings;
        BcpMessageController.OnMachineVariable -= MachineVariable;
        BcpMessageController.OnPlayerVariable -= PlayerVariable;
        BcpMessageController.OnModeStop -= ModeStop;
        BcpMessageController.OnPlayerTurnStart -= PlayerTurnStart;
    }

    /// <summary>
    /// Processes any BCP messages that have been received and queued for processing.
    /// </summary>
    /// <remarks>
    /// Update is called every frame, if the MonoBehaviour is enabled.  This function runs in the main Unity thread and 
    /// must access the received message queue in a thread-safe manner.
    /// </remarks>
    void Update ()
    {
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
                try
                {
                    _messageController.ProcessMessage(currentMessage, ignoreUnknownMessages);
                }
                catch (Exception ex)
                {
                    BcpLogger.Trace("An exception occurred while processing '" + currentMessage.Command + "' message (" + currentMessage.RawMessage + "): " + ex.ToString());
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
	public void AddMessageToQueue(BcpMessage message)
    {
		lock (_queueLock) {
			if (_messageQueue.Count < messageQueueSize) {
				_messageQueue.Enqueue(message);
			}
		}
	}

    /// <summary>
    /// Gets the value of the specified setting.
    /// </summary>
    /// <param name="name">The setting name.</param>
    /// <returns></returns>
    public JSONNode GetSetting(string name)
    {
        return _settings[name];
    }

    /// <summary>
    /// Gets the value of the specified machine variable.
    /// </summary>
    /// <param name="name">The machine variable name.</param>
    /// <returns></returns>
    public JSONNode GetMachineVariable(string name)
    {
        return _machineVars[name];
    }

    /// <summary>
    /// Gets the value of the specified player variable.
    /// </summary>
    /// <param name="player">The player number.</param>
    /// <param name="name">The machine variable name.</param>
    /// <returns></returns>
    public JSONNode GetPlayerVariable(int player, string name)
    {
        if (!_playerVars.ContainsKey(player))
            return JSONNull.CreateOrGet();

        return _playerVars[player][name];
    }

    /// <summary>
    /// Gets the value of the specified machine variable.
    /// </summary>
    /// <param name="name">The machine variable name.</param>
    /// <returns></returns>
    public JSONNode GetPlayerVariableCurrentPlayer(string name)
    {
        if (!_playerVars.ContainsKey(_currentPlayer))
            return JSONNull.CreateOrGet();

        return _playerVars[_currentPlayer][name];
    }

    /// <summary>
    /// Event handler called when receiving a BCP 'hello' command.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="HelloMessageEventArgs"/> instance containing the event data.</param>
    public void Hello(object sender, HelloMessageEventArgs e)
    {
        if (e.Version == BCP_VERSION)
            BcpServer.Instance.Send(BcpMessage.HelloMessage(BCP_VERSION, BcpServer.CONTROLLER_NAME, BcpServer.CONTROLLER_VERSION));
        else
            throw new Exception("'hello' message received an unknown protocol version");

        // Register the events that MPF will send via BCP.
        RegisterMonitorsAndTriggers();
    }

    /// <summary>
    /// Event handler called when a goodbye message is received.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="BcpMessageEventArgs"/> instance containing the event data.</param>
    public void Goodbye(object sender, BcpMessageEventArgs e)
    {
        // Shutdown the media controller server (close socket listener)
        BcpServer.Instance.Close();

        // Shutdown the Unity application
#if UNITY_EDITOR
        if (EditorApplication.isPlaying)
            EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }

    /// <summary>
    /// Event handler called when a settings event is received. 
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="BcpMessageEventArgs"/> instance containing the event data.</param>
    public void Settings(object sender, BcpMessageEventArgs e)
    {
        _settings = e.BcpMessage.Parameters["settings"].AsObject;
    }

    /// <summary>
    /// Event handler called when a machine variable event is received. Store new machine variable value in the
    /// machine variable store.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="MachineVariableMessageEventArgs"/> instance containing the event data.</param>
    public void MachineVariable(object sender, MachineVariableMessageEventArgs e)
    {
        _machineVars[e.Name] = e.Value;
    }

    /// <summary>
    /// Event handler called when a player variable event is received. Store new player variable value in the
    /// player variable store.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="PlayerVariableMessageEventArgs"/> instance containing the event data.</param>
    public void PlayerVariable(object sender, PlayerVariableMessageEventArgs e)
    {
        // Create player variable store for the player if it doesn't already exist
        if (!_playerVars.ContainsKey(e.PlayerNum))
            _playerVars.Add(e.PlayerNum, new JSONObject());

        _playerVars[e.PlayerNum][e.Name] = e.Value;
    }

    /// <summary>
    /// Event handler called when a mode stops.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="ModeStopMessageEventArgs"/> instance containing the event data.</param>
    public void ModeStop(object sender, ModeStopMessageEventArgs e)
    {
        if (e.Name == "game")
            _playerVars.Clear();

    }

    /// <summary>
    /// Event handler called when a players turn starts.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="PlayerTurnStartMessageEventArgs"/> instance containing the event data.</param>
    public void PlayerTurnStart(object sender, PlayerTurnStartMessageEventArgs e)
    {
        // Update the current player num
        _currentPlayer = e.PlayerNum;
    }

    /// <summary>
    /// Registers the events in MPF that will be sent via BCP.
    /// </summary>
    /// <remarks>
    /// Without registering messages with MPF, no message traffic will be sent to the Unity BCP server.
    /// </remarks>
    protected void RegisterMonitorsAndTriggers()
    {
        if (machineVariables) BcpServer.Instance.Send(BcpMessage.MonitorMachineVarsMessage());
        if (playerVariables) BcpServer.Instance.Send(BcpMessage.MonitorPlayerVarsMessage());
        if (switches) BcpServer.Instance.Send(BcpMessage.MonitorSwitchMessages());
        if (modes) BcpServer.Instance.Send(BcpMessage.MonitorModeMessages());
        if (coreEvents) BcpServer.Instance.Send(BcpMessage.MonitorCoreMessages());
        if (highScores)
        {
            BcpServer.Instance.Send(BcpMessage.RegisterTriggerMessage("high_score_enter_initials"));
            BcpServer.Instance.Send(BcpMessage.RegisterTriggerMessage("high_score_award_display"));
        }
        if (tilt)
        {
            BcpMessage.RegisterTriggerMessage("tilt");
            BcpMessage.RegisterTriggerMessage("tilt_warning");
            BcpMessage.RegisterTriggerMessage("slam_tilt");
        }
        char[] charSeparators = new char[] { ',' };

        // Register timers (timer names stored in a comma-separated list)
        foreach (string timer in timers.Split(charSeparators, StringSplitOptions.RemoveEmptyEntries))
        {
            BcpServer.Instance.Send(BcpMessage.RegisterTriggerMessage(String.Format("timer_{0}_started", timer.Trim().ToLower())));
            BcpServer.Instance.Send(BcpMessage.RegisterTriggerMessage(String.Format("timer_{0}_paused", timer.Trim().ToLower())));
            BcpServer.Instance.Send(BcpMessage.RegisterTriggerMessage(String.Format("timer_{0}_stopped", timer.Trim().ToLower())));
            BcpServer.Instance.Send(BcpMessage.RegisterTriggerMessage(String.Format("timer_{0}_complete", timer.Trim().ToLower())));
            BcpServer.Instance.Send(BcpMessage.RegisterTriggerMessage(String.Format("timer_{0}_tick", timer.Trim().ToLower())));
            BcpServer.Instance.Send(BcpMessage.RegisterTriggerMessage(String.Format("timer_{0}_time_added", timer.Trim().ToLower())));
            BcpServer.Instance.Send(BcpMessage.RegisterTriggerMessage(String.Format("timer_{0}_time_subtracted", timer.Trim().ToLower())));
        }

        // Register additional custom triggers (stored in string in a comma-separated list)
        foreach (string trigger in additionalTriggers.Split(charSeparators, StringSplitOptions.RemoveEmptyEntries))
        {
            BcpServer.Instance.Send(BcpMessage.RegisterTriggerMessage(trigger.Trim()));
        }
    }

}


/// <summary>
/// Class handles all message callbacks and raises the appropriate events
/// </summary>
public class BcpMessageController
{
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
    /// Represents the method that will handle a 'machine_variable' BCP message event.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="MachineVariableMessageEventArgs"/> instance containing the event message data.</param>
    public delegate void MachineVariableMessageEventHandler(object sender, MachineVariableMessageEventArgs e);

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
    /// Represents the method that will handle a 'settings' BCP message event.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="SettingsMessageEventArgs" /> instance containing the event message data.</param>
    public delegate void SettingsMessageEventHandler(object sender, BcpMessageEventArgs e);

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

    /// <summary>
    /// Represents the method that will handle a 'tilt' BCP message event.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="BcpMessageEventArgs"/> instance containing the event message data.</param>
    public delegate void TiltMessageEventHandler(object sender, BcpMessageEventArgs e);

    /// <summary>
    /// Represents the method that will handle a 'slam_tilt' BCP message event.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="BcpMessageEventArgs"/> instance containing the event message data.</param>
    public delegate void SlamTiltMessageEventHandler(object sender, BcpMessageEventArgs e);

    /// <summary>
    /// Represents the method that will handle a 'tilt_warning' BCP message event.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="TiltWarningMessageEventArgs"/> instance containing the event message data.</param>
    public delegate void TiltWarningMessageEventHandler(object sender, TiltWarningMessageEventArgs e);

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
    /// Occurs when a "ball_start" BCP message is received.  Indicates that a ball has started. It passes the player number (“1″, “2”, etc.) and the 
    /// ball number as parameters. This command will be sent every time a ball starts, even if the same player is shooting again after an extra ball.
    /// </summary>
    public static event BallStartMessageEventHandler OnBallStart;

    /// <summary>
    /// Occurs when a "ball_end" BCP message is received.  Indicates the current ball has ended.
    /// </summary>
    public static event BcpMessageEventHandler OnBallEnd;

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
    /// Occurs when a "machine_variable" BCP message is received.  This is a generic "catch all" which sends machine variables 
    /// to the media controller any time they change.
    /// </summary>
    public static event MachineVariableMessageEventHandler OnMachineVariable;

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
    /// Occurs when a "settings" BCP message is received.
    /// </summary>
    public static event SettingsMessageEventHandler OnSettings;

    /// <summary>
    /// Occurs when a "timer" BCP message is received.  Notifies the media controller about timer action that needs to be 
    /// communicated to the player. 
    /// </summary>
    public static event TimerMessageEventHandler OnTimer;

    /// <summary>
    /// Occurs when a "tilt" BCP message is received.  Notifies the media controller that the current player has just tilted.
    /// </summary>
    public static event TiltMessageEventHandler OnTilt;

    /// <summary>
    /// Occurs when a "slam_tilt" BCP message is received.  Notifies the media controller that the current player has just slam tilted.
    /// </summary>
    public static event SlamTiltMessageEventHandler OnSlamTilt;

    /// <summary>
    /// Occurs when a "tilt_warning" BCP message is received.  Notifies the media controller that the current player has just been 
    /// issued a tilt warning.
    /// </summary>
    public static event TiltWarningMessageEventHandler OnTiltWarning;

    #endregion


    /// <summary>
    /// The message callback function for all received BCP messages (called before any specific message handlers).
    /// </summary>
    private BcpMessageCallback _allMessageCallback;

    /// <summary>
    /// The BCP message handler callback table.  Stores callback functions for processing each BCP message command.
    /// </summary>
	private Hashtable _messageHandlerCallbackTable = new Hashtable();

    /// <summary>
    /// Flag indicating whether or not to process timer trigger messages.
    /// </summary>
    private bool _processTimers;

    /// <summary>
    /// Initializes a new instance of the <see cref="BcpMessageController" /> class.
    /// </summary>
    /// <param name="processTimers">if set to <c>true</c> process timer trigger messages.</param>
    public BcpMessageController(bool processTimers = false)
    {
        _processTimers = processTimers;

        // Setup message processing callbacks
        SetAllMessageCallback(AllMessageHandler);
        SetMessageCallback("hello", HelloMessageHandler);
        SetMessageCallback("goodbye", GoodbyeMessageHandler);
        SetMessageCallback("ball_start", BallStartMessageHandler);
        SetMessageCallback("ball_end", BallEndMessageHandler);
        SetMessageCallback("mode_start", ModeStartMessageHandler);
        SetMessageCallback("mode_stop", ModeStopMessageHandler);
        SetMessageCallback("player_added", PlayerAddedMessageHandler);
        SetMessageCallback("player_turn_start", PlayerTurnStartMessageHandler);
        SetMessageCallback("player_variable", PlayerVariableMessageHandler);
        SetMessageCallback("machine_variable", MachineVariableMessageHandler);
        SetMessageCallback("switch", SwitchMessageHandler);
        SetMessageCallback("trigger", TriggerMessageHandler);
        SetMessageCallback("error", ErrorMessageHandler);
        SetMessageCallback("reset", ResetMessageHandler);
        SetMessageCallback("settings", SettingsMessageHandler);
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
    /// Gets the message callback for the specified command.
    /// </summary>
    /// <param name="command">The message command.</param>
    /// <returns></returns>
    public BcpMessageCallback GetMessageCallback(string command)
    {
        return _messageHandlerCallbackTable[command] as BcpMessageCallback;
    }

    /// <summary>
    /// Processes a BCP message.
    /// </summary>
    /// <param name="message">The BCP message to process.</param>
    /// <param name="ignoreUnknownMessages">if set to <c>true</c> ignore unknown messages (log message only).</param>
    /// <exception cref="BcpMessageException">Unknown BCP message '" + message.Command + "' (no message handler set).</exception>
    public void ProcessMessage(BcpMessage message, bool ignoreUnknownMessages=true)
    {
        // Process message (call message handler callback functions)
        BcpLogger.Trace("Processing \"" + message.Command + "\" message");

        // First, call all message handler function (if one is set)
        _allMessageCallback?.Invoke(message);

        // Call specific message handler function
        BcpMessageCallback messageCallback = _messageHandlerCallbackTable[message.Command] as BcpMessageCallback;
        if (messageCallback != null)
        {
            messageCallback(message);
        }
        else
        {
            // Unknown message
            if (ignoreUnknownMessages)
            {
                // Ignore the unknown message, but put a message in the log
                BcpLogger.Trace("Unknown BCP message '" + message.Command + "' (no message handler set): " + message);
            }
            else
            {
                // Throw an exception for the unknown message
                throw new BcpMessageException("Unknown BCP message '" + message.Command + "' (no message handler set).", message);
            }
        }

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
    protected void HelloMessageHandler(BcpMessage message)
    {
        try
        {
            string version = message.Parameters["version"].Value;
            if (String.IsNullOrEmpty(version))
                throw new Exception("'hello' message did not contain a valid value for the 'version' parameter");

            string controllerName = message.Parameters["controller_name"].Value;
            string controllerVersion = message.Parameters["controller_version"].Value;

            // Raise the OnHello event by invoking the delegate. Pass in 
            // the object that initiated the event (this) as well as the BcpMessage. 
            if (OnHello != null)
            {
                OnHello(this, new HelloMessageEventArgs(message, version, controllerName, controllerVersion));
            }

        }
        catch (Exception e)
        {
            BcpLogger.Trace("ERROR: " + e.Message);
            BcpServer.Instance.Send(BcpMessage.ErrorMessage("An error occurred while processing a '" + message.Command + "' message: " + e.Message, message.RawMessage));
        }

    }

    /// <summary>
    /// Internal message handler for all "goodbye" messages. Raises the <see cref="OnGoodbye"/> event.
    /// </summary>
    /// <param name="message">The "goodbye" BCP message.</param>
    protected void GoodbyeMessageHandler(BcpMessage message)
    {
        // Raise the OnGoodbye event by invoking the delegate. Pass in 
        // the object that initiated the event (this) as well as the BcpMessage. 
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

    }


    /// <summary>
    /// Internal message handler for all "ball_start" messages. Raises the <see cref="OnBallStart"/> event.
    /// </summary>
    /// <param name="message">The "ball_start" BCP message.</param>
    protected void BallStartMessageHandler(BcpMessage message)
    {
        if (OnBallStart != null)
        {
            int playerNum = message.Parameters["player_num"].AsInt;
            int ball = message.Parameters["ball"].AsInt;
            OnBallStart(this, new BallStartMessageEventArgs(message, playerNum, ball));
        }
    }


    /// <summary>
    /// Internal message handler for all "ball_end" messages. Raises the <see cref="OnBallEnd"/> event.
    /// </summary>
    /// <param name="message">The "ball_end" BCP message.</param>
    protected void BallEndMessageHandler(BcpMessage message)
    {
        OnBallEnd?.Invoke(this, new BcpMessageEventArgs(message));
    }

    /// <summary>
    /// Internal message handler for all "mode_start" messages. Raises the <see cref="OnModeStart"/> event.
    /// </summary>
    /// <param name="message">The "mode_start" BCP message.</param>
    protected void ModeStartMessageHandler(BcpMessage message)
    {
        if (OnModeStart != null)
        {
            string name = message.Parameters["name"].Value;
            if (String.IsNullOrEmpty(name))
                throw new ArgumentException("Message parameter value expected", "name");

            int priority = message.Parameters["priority"].AsInt;

            OnModeStart(this, new ModeStartMessageEventArgs(message, name, priority));
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
            string name = message.Parameters["name"].Value;
            if (String.IsNullOrEmpty(name))
                throw new ArgumentException("Message parameter value expected", "name");

            OnModeStop(this, new ModeStopMessageEventArgs(message, name));
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
            int playerNum = message.Parameters["player_num"].AsInt;
            OnPlayerAdded(this, new PlayerAddedMessageEventArgs(message, playerNum));
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
            int playerNum = message.Parameters["player_num"].AsInt;
            OnPlayerTurnStart(this, new PlayerTurnStartMessageEventArgs(message, playerNum));
        }
    }

    /// <summary>
    /// Internal message handler for all "player_variable" messages. Raises the <see cref="OnPlayerVariable"/> event.
    /// </summary>
    /// <param name="message">The "player_variable" BCP message.</param>
    protected void PlayerVariableMessageHandler(BcpMessage message)
    {
        if (OnPlayerVariable != null || OnPlayerScore != null)
        {
            int playerNum = message.Parameters["player_num"].AsInt;

            string name = message.Parameters["name"].Value;
            if (String.IsNullOrEmpty(name))
                throw new ArgumentException("Message parameter value expected", "name");

            JSONNode value = message.Parameters["value"];
            JSONNode previousValue = message.Parameters["prev_value"];
            JSONNode change = message.Parameters["change"];

            OnPlayerVariable?.Invoke(this, new PlayerVariableMessageEventArgs(message, playerNum, name, value, previousValue, change));

            // Send an additional special notification for player score (the player_score message has been removed from the BCP spec)
            if (name == "score" && OnPlayerScore != null)
            {
                int scoreValue = message.Parameters["value"].AsInt;
                int scorePreviousValue = message.Parameters["prev_value"].AsInt;
                int scoreChange = message.Parameters["change"].AsInt;

                OnPlayerScore(this, new PlayerScoreMessageEventArgs(message, playerNum, scoreValue, scorePreviousValue, scoreChange));
            }
        }

    }

    /// <summary>
    /// Internal message handler for all "machine_variable" messages. Raises the <see cref="OnMachineVariable"/> event.
    /// </summary>
    /// <param name="message">The "machine_variable" BCP message.</param>
    protected void MachineVariableMessageHandler(BcpMessage message)
    {
        if (OnMachineVariable != null)
        {
            string name = message.Parameters["name"].Value;
            if (String.IsNullOrEmpty(name))
                throw new ArgumentException("Message parameter value expected", "name");

            JSONNode value = message.Parameters["value"];

            OnMachineVariable(this, new MachineVariableMessageEventArgs(message, name, value));
        }

    }

    /// <summary>
    /// Internal message handler for all "switch" messages. Raises the <see cref="OnSwitch" /> event.
    /// </summary>
    /// <param name="message">The "switch" BCP message.</param>
    /// <exception cref="BcpMessageException">
    /// An error occurred while processing a 'switch' message: missing required parameter 'name'.
    /// or
    /// An error occurred while processing a 'switch' message: missing required parameter 'state'.
    /// or
    /// An error occurred while processing a 'switch' message: invalid parameter value 'state'.
    /// </exception>
    protected void SwitchMessageHandler(BcpMessage message)
    {
        if (OnSwitch != null)
        {
            string name = message.Parameters["name"].Value;
            if (String.IsNullOrEmpty(name))
                throw new BcpMessageException("An error occurred while processing a 'switch' message: missing required parameter 'name'.", message);

            if (!message.Parameters["state"].IsNumber)
                throw new BcpMessageException("An error occurred while processing a 'switch' message: missing or invalid required parameter 'state'.", message);

            int state = message.Parameters["state"].AsInt;

            OnSwitch(this, new SwitchMessageEventArgs(message, name, state));
        }
    }


    /// <summary>
    /// Internal message handler for all "trigger" messages. Raises the <see cref="OnTrigger"/> event.
    /// </summary>
    /// <param name="message">The "trigger" BCP message.</param>
    protected void TriggerMessageHandler(BcpMessage message)
    {
        string name = message.Parameters["name"];
        if (String.IsNullOrEmpty(name))
            throw new ArgumentException("Message parameter value expected", "name");

        // Some specialized BCP messages are now sent as triggers and must be pre-processed here

        // Timers
        if (_processTimers)
        {
            Regex timer_message = new Regex("^timer_(?<name>\\w+)_(?<action>started|stopped|paused|completed|tick|time_added|time_subtracted)");
            Match match = timer_message.Match(name);
            if (match.Success)
            {
                TimerMessageHandler(message, match.Groups["name"].ToString(), match.Groups["action"].ToString());
                return;
            }
        }

        // Tilt messages
        if (name == "tilt")
        {
            OnTilt?.Invoke(this, new BcpMessageEventArgs(new BcpMessage("tilt")));
            return;
        }
        else if (name == "slam_tilt")
        {
            OnSlamTilt?.Invoke(this, new BcpMessageEventArgs(new BcpMessage("slam_tilt")));
            return;
        }
        else if (name == "tilt_warning")
        {
            int warnings = int.Parse(message.Parameters["warnings"]);
            int warnings_remaining = int.Parse(message.Parameters["warnings_remaining"]);
            OnTiltWarning?.Invoke(this, new TiltWarningMessageEventArgs(new BcpMessage("tilt_warning"), warnings, warnings_remaining));
            return;
        }

        // Now call trigger message handlers
        OnTrigger?.Invoke(this, new TriggerMessageEventArgs(message, name));
    }

    /// <summary>
    /// Internal message handler for all "error" messages. Raises the <see cref="OnError"/> event.
    /// </summary>
    /// <param name="message">The "error" BCP message.</param>
    protected void ErrorMessageHandler(BcpMessage message)
    {
        if (OnError != null)
        {
            string text = message.Parameters["message"].Value;
            if (String.IsNullOrEmpty(text))
                throw new ArgumentException("Message parameter value expected", "message");

            string command = message.Parameters["command"].Value;

            OnError(this, new ErrorMessageEventArgs(message, text, command));
        }
    }

    /// <summary>
    /// Internal message handler for all "reset" messages. Raises the <see cref="OnReset"/> event.
    /// </summary>
    /// <param name="message">The "reset" BCP message.</param>
    protected void ResetMessageHandler(BcpMessage message)
    {
        // Check if any event handlers are established for the reset command
        if (OnReset != null)
        {
            // Call the reset event handlers
            try
            {
                OnReset(this, new ResetMessageEventArgs(message, message.Parameters["hard"].AsBool));
            }
            catch (Exception e)
            {
                BcpServer.Instance.Send(BcpMessage.ErrorMessage("An error occurred while processing a '" + message.Command + "' message: " + e.Message, message.RawMessage));
            }
        }

        // A BCP reset message must be responded to with a reset complete
        if (BcpServer.Instance != null)
            BcpServer.Instance.Send(BcpMessage.ResetCompleteMessage());
    }

    /// <summary>
    /// Internal message handler for all "settings" messages. Raises the <see cref="OnSettings"/> event.
    /// </summary>
    /// <param name="message">The "reset" BCP message.</param>
    protected void SettingsMessageHandler(BcpMessage message)
    {
        // Check if any event handlers are established for the settings command
        if (OnSettings != null)
        {
            // Call the settings event handlers
            try
            {
                OnSettings(this, new BcpMessageEventArgs(message));
            }
            catch (Exception e)
            {
                BcpServer.Instance.Send(BcpMessage.ErrorMessage("An error occurred while processing a '" + message.Command + "' message: " + e.Message, message.RawMessage));
            }
        }
    }

    /// <summary>
    /// Internal message handler for all "timer" messages. Raises the <see cref="OnTimer" /> event.
    /// </summary>
    /// <param name="message">The "timer" BCP message.</param>
    /// <param name="name">The timer name.</param>
    /// <param name="action">The timer action.</param>
    /// <exception cref="ArgumentException">
    /// Message parameter value expected;name
    /// or
    /// Message parameter value expected;action
    /// </exception>
    protected void TimerMessageHandler(BcpMessage message, string name, string action)
    {
        if (OnTimer != null)
        {
            try
            {
                if (String.IsNullOrEmpty(name))
                    throw new ArgumentException("Message parameter value expected", "name");

                if (String.IsNullOrEmpty(action))
                    throw new ArgumentException("Message parameter value expected", "action");

                int ticks = message.Parameters["ticks"].AsInt;
                int ticks_remaining = message.Parameters["ticks_remaining"].AsInt;

                int ticks_added = 0;
                if (action == "time_added")
                    ticks_added = message.Parameters["ticks_added"].AsInt;
                else if (action == "time_subtracted")
                    ticks_added = -message.Parameters["ticks_subtracted"].AsInt;

                OnTimer(this, new TimerMessageEventArgs(message, name, action, ticks, ticks_remaining, ticks_added));
            }
            catch (Exception e)
            {
                BcpServer.Instance.Send(BcpMessage.ErrorMessage("An error occurred while processing a '" + message.Command + "' message: " + e.Message, message.RawMessage));
            }
        }
    }

    /// <summary>
    /// Internal message handler for all "slam_tilt" messages. Raises the <see cref="OnSlamTilt"/> event.
    /// </summary>
    /// <param name="message">The "slam_tilt" BCP message.</param>
    protected void SlamTiltMessageHandler(BcpMessage message)
    {
        if (OnSlamTilt != null)
        {
            try
            {
                OnSlamTilt(this, new BcpMessageEventArgs(message));
            }
            catch (Exception e)
            {
                BcpServer.Instance.Send(BcpMessage.ErrorMessage("An error occurred while processing a '" + message.Command + "' message: " + e.Message, message.RawMessage));
            }
        }
    }

    /// <summary>
    /// Internal message handler for all "tilt_warning" messages. Raises the <see cref="OnTiltWarning"/> event.
    /// </summary>
    /// <param name="message">The "tilt_warning" BCP message.</param>
    protected void TiltWarningMessageHandler(BcpMessage message)
    {
        if (OnTiltWarning != null)
        {
            try
            {
                int warnings = message.Parameters["warnings"].AsInt;
                int warningsRemaining = message.Parameters["warnings_remaining"].AsInt;

                OnTiltWarning(this, new TiltWarningMessageEventArgs(message, warnings, warningsRemaining));
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
    /// Gets or sets the name of the controller that is sending the message.
    /// </summary>
    public string ControllerName { get; set; }

    /// <summary>
    /// Gets or sets the version of the controller that is sending the message.
    /// </summary>
    public string ControllerVersion { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HelloMessageEventArgs"/> class.
    /// </summary>
    /// <param name="bcpMessage">The BCP message.</param>
    /// <param name="version">The BCP message protocol version.</param>
    /// <param name="controllerName">The name of the controller.</param>
    /// <param name="controllerVersion">The version of the controller.</param>
    public HelloMessageEventArgs(BcpMessage bcpMessage, string version, string controllerName, string controllerVersion) :
        base(bcpMessage)
    {
        this.Version = version;
        this.ControllerName = controllerName;
        this.ControllerVersion = controllerVersion;
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
    public int PlayerNum { get; set; }

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
    /// <param name="playerNum">The player number.</param>
    /// <param name="ball">The ball number.</param>
    public BallStartMessageEventArgs(BcpMessage bcpMessage, int playerNum, int ball) :
        base(bcpMessage)
    {
        this.PlayerNum = playerNum;
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
    /// Gets or sets the command that was invalid and caused the error.
    /// </summary>
    /// <value>
    /// The command.
    /// </value>
    public string Command { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorMessageEventArgs"/> class.
    /// </summary>
    /// <param name="bcpMessage">The BCP message.</param>
    /// <param name="message">The error message text.</param>
    public ErrorMessageEventArgs(BcpMessage bcpMessage, string message, string command="") :
        base(bcpMessage)
    {
        this.Message = message;
        this.Command = command;
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
    /// Gets or sets the player number value.
    /// </summary>
    /// <value>
    /// The player number.
    /// </value>
    public int PlayerNum { get; set; }

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
    /// <param name="playerNum">The player number.</param>
    /// <param name="value">The specified player's score value.</param>
    /// <param name="previousValue">The specified player's previous score value.</param>
    /// <param name="change">The change in the specified player's score.</param>
    public PlayerScoreMessageEventArgs(BcpMessage bcpMessage, int playerNum, int value, int previousValue, int change) :
        base(bcpMessage)
    {
        this.PlayerNum = playerNum;
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
    public int PlayerNum { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerAddedMessageEventArgs"/> class.
    /// </summary>
    /// <param name="bcpMessage">The BCP message.</param>
    /// <param name="playerNum">The player number.</param>
    public PlayerAddedMessageEventArgs(BcpMessage bcpMessage, int playerNum) :
        base(bcpMessage)
    {
        this.PlayerNum = playerNum;
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
    public int PlayerNum { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerTurnStartMessageEventArgs"/> class.
    /// </summary>
    /// <param name="bcpMessage">The BCP message.</param>
    /// <param name="playerNum">The player number.</param>
    public PlayerTurnStartMessageEventArgs(BcpMessage bcpMessage, int playerNum) :
        base(bcpMessage)
    {
        this.PlayerNum = playerNum;
    }
}


/// <summary>
/// Event arguments for the "player_variable" BCP message.
/// </summary>
public class PlayerVariableMessageEventArgs : BcpMessageEventArgs
{
    /// <summary>
    /// Gets or sets the player number.
    /// </summary>
    /// <value>
    /// The player number.
    /// </value>
    public int PlayerNum { get; set; }

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
    public JSONNode Value { get; set; }

    /// <summary>
    /// Gets or sets the previous value of the player variable.
    /// </summary>
    /// <value>
    /// The previous value of the player variable.
    /// </value>
    public JSONNode PreviousValue { get; set; }

    /// <summary>
    /// Gets or sets the change in value of the player variable.
    /// </summary>
    /// <value>
    /// The change in value of the player variable.
    /// </value>
    public JSONNode Change { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerVariableMessageEventArgs"/> class.
    /// </summary>
    /// <param name="bcpMessage">The BCP message.</param>
    /// <param name="playerNum">The player number.</param>
    /// <param name="name">The player variable name.</param>
    /// <param name="value">The player variable value.</param>
    /// <param name="previousValue">The previous value of the player variable.</param>
    /// <param name="change">The change in value of the player variable.</param>
    public PlayerVariableMessageEventArgs(BcpMessage bcpMessage, int playerNum, string name, JSONNode value, JSONNode previousValue, JSONNode change) :
        base(bcpMessage)
    {
        this.PlayerNum = playerNum;
        this.Name = name;
        this.Value = value;
        this.PreviousValue = previousValue;
        this.Change = change;
    }
}


/// <summary>
/// Event arguments for the "machine_variable" BCP message.
/// </summary>
public class MachineVariableMessageEventArgs : BcpMessageEventArgs
{
    /// <summary>
    /// Gets or sets the machine variable name.
    /// </summary>
    /// <value>
    /// The machine variable name.
    /// </value>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the machine variable value.
    /// </summary>
    /// <value>
    /// The machine variable value.
    /// </value>
    public JSONNode Value { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MachineVariableMessageEventArgs"/> class.
    /// </summary>
    /// <param name="bcpMessage">The BCP message.</param>
    /// <param name="name">The player variable name.</param>
    /// <param name="value">The player variable value.</param>
    public MachineVariableMessageEventArgs(BcpMessage bcpMessage, string name, JSONNode value) :
        base(bcpMessage)
    {
        this.Name = name;
        this.Value = value;
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
    /// Gets or sets the switch state.
    /// </summary>
    /// <value>
    /// The switch state.
    /// </value>
    public int State { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SwitchMessageEventArgs"/> class.
    /// </summary>
    /// <param name="bcpMessage">The BCP message.</param>
    /// <param name="name">The switch name.</param>
    /// <param name="state">The switch state.</param>
    public SwitchMessageEventArgs(BcpMessage bcpMessage, string name, int state) :
        base(bcpMessage)
    {
        this.Name = name;
        this.State = state;
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
    /// Gets or sets the timer ticks remaining.
    /// </summary>
    /// <value>
    /// The timer ticks remaining.
    /// </value>
    public int TicksRemaining { get; set; }

    /// <summary>
    /// Gets or sets the timer ticks added (or subtracted if negative).
    /// </summary>
    /// <value>
    /// The timer ticks added.
    /// </value>
    public int TicksAdded { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TimerMessageEventArgs"/> class.
    /// </summary>
    /// <param name="bcpMessage">The BCP message.</param>
    /// <param name="name">The timer name.</param>
    /// <param name="action">The timer action.</param>
    /// <param name="ticks">The timer ticks.</param>
    public TimerMessageEventArgs(BcpMessage bcpMessage, string name, string action, int ticks, int ticks_remaining, int ticks_added=0) :
        base(bcpMessage)
    {
        this.Name = name;
        this.Action = action;
        this.Ticks = ticks;
        this.TicksRemaining = ticks_remaining;
        this.TicksAdded = ticks_added;
    }

}


/// <summary>
/// Event arguments for the "tilt_warning" BCP message.
/// </summary>
public class TiltWarningMessageEventArgs : BcpMessageEventArgs
{
    /// <summary>
    /// Gets or sets the number of tilt warnings.
    /// </summary>
    /// <value>
    /// The tilt warning count.
    /// </value>
    public int Warnings { get; set; }

    /// <summary>
    /// Gets or sets the number of tilt warnings remaining before a tilt.
    /// </summary>
    /// <value>
    /// The number of tilt warnings remaining before a tilt.
    /// </value>
    public int WarningsRemaining { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TiltWarningMessageEventArgs"/> class.
    /// </summary>
    /// <param name="bcpMessage">The BCP message.</param>
    /// <param name="warnings">The number of tilt warnings.</param>
    /// <param name="warningsRemaining">The number of tilt warnings remaining before a tilt.</param>
    public TiltWarningMessageEventArgs(BcpMessage bcpMessage, int warnings, int warningsRemaining) :
        base(bcpMessage)
    {
        this.Warnings = warnings;
        this.WarningsRemaining = warningsRemaining;
    }

}



#endregion

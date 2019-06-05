using UnityEngine;
using System;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;
using System.Collections.Generic;
using BCP.SimpleJSON;

/// <summary>
/// Custom PlayMaker action for MPF that sends an Event when a high_score_enter_initials BCP Trigger 
/// command is received.
/// </summary>
[ActionCategory("BCP")]
[Tooltip("Sends a series of special events to control the high score initials entry process.")]
public class GetBCPHighScoreEnterInitials : FsmStateAction
{
    [RequiredField]
    [Tooltip("The maximum number of characters permitted")]
    public int maxCharacters;

    [RequiredField]
    [Tooltip("The name of the event that initializes the state (initial trigger)")]
    public string initializationEventName;

    [RequiredField]
    [UIHint(UIHint.TextArea)]
    [Tooltip("The available characters the user is presented during intitial selection")]
    public string characterSet;

    [RequiredField]
    [Tooltip("The name of the event to shift left/decrement")]
    public string shiftLeftEvent;

    [RequiredField]
    [Tooltip("The name of the event to shift right/increment")]
    public string shiftRightEvent;

    [RequiredField]
    [Tooltip("The name of the event to select")]
    public string selectEvent;

    [RequiredField]
    [Tooltip("The name of the event to abort")]
    public string abortEvent;

    [RequiredField]
    [Tooltip("The number of seconds before the enter initials process times out")]
    public float timeoutSeconds;

    [UIHint(UIHint.Variable)]
    [Tooltip("The variable that contains the selected initials")]
    public FsmString selectedInitials;

    [UIHint(UIHint.Variable)]
    [Tooltip("The PlayMaker event to send when the current character position is changed")]
    public FsmEvent characterPositionChangedEvent;

    [UIHint(UIHint.Variable)]
    [Tooltip("The PlayMaker event to send when the currently selected character is changed")]
    public FsmEvent characterChangedEvent;

    private int currentCharacter;
    private int currentPosition;
    private List<string> initials = null;
    private List<string> characterList = null;
    private float timeoutSecondsRemaining;


    public override void Awake()
    {
        base.Awake();
    }

    /// <summary>
    /// Resets this instance to default values.
    /// </summary>
    public override void Reset()
    {
        BcpLogger.Trace("GetBCPHighScoreEnterInitials: Reset");

        base.Reset();
        maxCharacters = 3;
        timeoutSeconds = 20.0f;
        shiftLeftEvent = "sw_left_flipper";
        shiftRightEvent = "sw_right_flipper";
        selectEvent = "sw_start";
        abortEvent = "sw_esc";
        characterSet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ_- ";

        currentCharacter = 0;
        currentPosition = 0;

        if (initials == null)
            initials = new List<string>(maxCharacters);
        else
            initials.Clear();

        for (int index = 0; index < maxCharacters; index++)
            initials.Add("");

        BuildCharacterList();
    }

    /// <summary>
    /// Called every frame by Unity. Updates the timer.
    /// </summary>
    public override void OnUpdate()
    {
        if (!this.Finished)
        {
            timeoutSecondsRemaining -= Time.deltaTime;
            if (timeoutSecondsRemaining <= 0.0f)
            {
                BcpLogger.Trace("GetBCPHighScoreEnterInitials: Timeout reached");
                // Abort();
            }
        }
    }

    /// <summary>
    /// Called when the state becomes active. Adds the MPF BCP event handlers.
    /// </summary>
    public override void OnEnter()
    {
        BcpLogger.Trace("GetBCPHighScoreEnterInitials: OnEnter");
        BcpLogger.Trace("GetBCPHighScoreEnterInitials: Events (" + shiftLeftEvent + ", " + shiftRightEvent + ", " + selectEvent + ")");
        base.OnEnter();

        FsmTransition lastTransition = Fsm.LastTransition;
        if (lastTransition.EventName == initializationEventName)
        {
            timeoutSecondsRemaining = timeoutSeconds;
            currentCharacter = 0;
            currentPosition = 0;

            if (initials == null)
                initials = new List<string>(maxCharacters);
            else
                initials.Clear();

            for (int index = 0; index < maxCharacters; index++)
                initials.Add("");

            BuildCharacterList();

            PositionChanged();
            CharacterChanged();
        }

        BcpMessageController.OnSwitch += Switch;
    }

    /// <summary>
    /// Called before leaving the current state. Removes the MPF BCP event handlers.
    /// </summary>
    public override void OnExit()
    {
        BcpLogger.Trace("GetBCPHighScoreEnterInitials: OnExit");

        BcpMessageController.OnSwitch -= Switch;
        base.OnExit();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void Switch(object sender, SwitchMessageEventArgs e)
    {
        BcpLogger.Trace("GetBCPHighScoreEnterInitials: Switch (" + e.Name + ", " + e.State.ToString() + ")");

        if (e.State != 1)
            return;

        if (e.Name == shiftLeftEvent) ShiftLeft();
        else if (e.Name == shiftRightEvent) ShiftRight();
        else if (e.Name == selectEvent) Select();
        else if (e.Name == abortEvent) Abort();
    }

    /// <summary>
    /// Called when user presses shift left button
    /// </summary>
    private void ShiftLeft()
    {
        BcpLogger.Trace("GetBCPHighScoreEnterInitials: ShiftLeft");

        currentCharacter--;
        if (currentCharacter < 0)
            currentCharacter = characterList.Count - 1;

        CharacterChanged();
    }

    /// <summary>
    /// Called when user presses shift right button
    /// </summary>
    private void ShiftRight()
    {
        BcpLogger.Trace("GetBCPHighScoreEnterInitials: ShiftRight");

        currentCharacter++;
        if (currentCharacter >= characterList.Count)
            currentCharacter = 0;

        CharacterChanged();
    }

    /// <summary>
    /// Called when user presses select button
    /// </summary>
    private void Select()
    {
        BcpLogger.Trace("GetBCPHighScoreEnterInitials: Select");

        // Check for special characters (back, end)
        if (characterList[currentCharacter] == "back")
        {
            if (currentPosition > 0)
            {
                currentPosition--;
                PositionChanged();
            }

        }
        else if (characterList[currentCharacter] == "end")
        {
            Done();
        }
        else
        {

            // Add selected character to saved initials string
            initials[currentPosition] += characterList[currentCharacter];

            // Go to next position (or end)
            currentPosition++;
            if (currentPosition >= maxCharacters)
            {
                Done();
            }
            else
            {
                PositionChanged();
                BuildCharacterList();
            }
        }
    }

    /// <summary>
    /// Aborts the initial entering process sending back an empty string
    /// </summary>
    private void Abort()
    {
        BcpLogger.Trace("GetBCPHighScoreEnterInitials: Abort");

        initials.Clear();

        for (int index = 0; index < maxCharacters; index++)
            initials.Add("");

        Done();
    }

    /// <summary>
    /// Called whenever the current character changes
    /// </summary>
    private void CharacterChanged()
    {
        BcpLogger.Trace("GetBCPHighScoreEnterInitials: CharacterChanged");

        // Set event data (new character) and send character changed event
        FsmEventData data = new FsmEventData();
        data.StringData = characterList[currentCharacter];
        Fsm.EventData = data;
        Fsm.Event(characterChangedEvent);
    }

    /// <summary>
    /// Called whenever the current character position changes
    /// </summary>
    private void PositionChanged()
    {
        BcpLogger.Trace("GetBCPHighScoreEnterInitials: PositionChanged");

        // Set event data (new position) and send position changed event
        FsmEventData data = new FsmEventData();
        data.IntData = currentPosition + 1;
        Fsm.EventData = data;
        Fsm.Event(characterPositionChangedEvent);
    }

    /// <summary>
    /// Build a list of characters to select from
    /// </summary>
    private void BuildCharacterList()
    {
        BcpLogger.Trace("GetBCPHighScoreEnterInitials: BuildCharacterList");

        if (characterList == null)
            characterList = new List<string>();

        characterList.Clear();

        for (int index = 0; index < characterSet.Length; index++)
            characterList.Add(characterSet[index].ToString());

        if (currentCharacter > 1)
            characterList.Add("back");

        characterList.Add("end");

    }

    /// <summary>
    /// Called internally when the user has completed entering their initials.
    /// </summary>
    private void Done()
    {
        BcpLogger.Trace("GetBCPHighScoreEnterInitials: Done");

        string finalInitials = string.Join("", initials).TrimEnd();
        if (!selectedInitials.IsNone)
            selectedInitials.Value = finalInitials;

        FsmEventData data = new FsmEventData();
        data.StringData = finalInitials;

        // Call the base class Finish function to finish the action
        Finish();
    }


}

using UnityEngine;
using System;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

/// <summary>
/// Custom PlayMaker action for MPF that sends an Event when an MPF 'player_variable' command is received.
/// </summary>
[ActionCategory("BCP")]
[Tooltip("Sends an Event when an MPF 'player_variable' command is received.")]
public class GetBCPPlayerVariableChange : FsmStateAction
{
    [RequiredField]
    [UIHint(UIHint.Variable)]
    [Tooltip("The name of the MPF player variable to listen for")]
    public string playerVariableName;

    [RequiredField]
    [UIHint(UIHint.Variable)]
    [Tooltip("The variable to receive the value of the specified MPF player variable")]
    public FsmInt playerNum;

    [RequiredField]
    [UIHint(UIHint.Variable)]
    [Tooltip("The variable to receive the value of the specified MPF player variable")]
    public FsmString value;

    [UIHint(UIHint.Variable)]
    [Tooltip("The variable to receive the previous value of the specified MPF player variable")]
    public FsmString previousValue;

    [UIHint(UIHint.Variable)]
    [Tooltip("The variable to receive the change in value of the specified MPF player variable")]
    public FsmString change;

    [UIHint(UIHint.Variable)]
    [Tooltip("The PlayMaker event to send when an MPF 'player_variable' command is received")]
    public FsmEvent sendEvent;

    /// <summary>
    /// Resets this instance to default values.
    /// </summary>
    public override void Reset()
    {
        playerVariableName = null;
        playerNum = null;
        value = null;
        previousValue = null;
        change = null;
        sendEvent = null;
    }

    /// <summary>
    /// Called when the state becomes active. Adds the MPF BCP 'player_variable' event handler.
    /// </summary>
    public override void OnEnter()
    {
        base.OnEnter();
        BcpMessageController.OnPlayerVariable += PlayerVariable;
    }

    /// <summary>
    /// Called before leaving the current state. Removes the MPF BCP 'player_variable' event handler.
    /// </summary>
    public override void OnExit()
    {
		BcpMessageController.OnPlayerVariable -= PlayerVariable;
		base.OnExit();
    }

    /// <summary>
    /// Event handler called when a player variable event is received.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="PlayerVariableMessageEventArgs"/> instance containing the event data.</param>
    public void PlayerVariable(object sender, PlayerVariableMessageEventArgs e)
    {
        // Determine if this player variable message is the one we are interested in.  If so, send specified FSM event.
        if (!String.IsNullOrEmpty(playerVariableName) && e.Name == playerVariableName)
        {
            if (!playerNum.IsNone)
                playerNum.Value = e.PlayerNum;

            if (!value.IsNone)
                value.Value = e.Value;

            if (!previousValue.IsNone)
                previousValue.Value = e.PreviousValue;

            if (!change.IsNone)
                change.Value = e.Change;

			Fsm.Event(sendEvent);
		}
    }

}

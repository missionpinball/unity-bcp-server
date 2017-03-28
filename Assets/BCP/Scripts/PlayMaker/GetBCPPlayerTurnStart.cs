using UnityEngine;
using System;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

/// <summary>
/// Custom PlayMaker action for MPF that sends an Event when an MPF 'player_turn_start' command is received.
/// </summary>
[ActionCategory("BCP")]
[Tooltip("Sends an Event when an MPF 'player_turn_start' command is received.")]
public class GetBCPPlayerTurnStart : FsmStateAction
{
    [RequiredField]
    [UIHint(UIHint.Variable)]
    [Tooltip("The variable to receive the player number whose turn has just started")]
    public FsmInt player;

    [UIHint(UIHint.Variable)]
    [Tooltip("The PlayMaker event to send when an MPF 'player_turn_start' command is received")]
    public FsmEvent sendEvent;

    /// <summary>
    /// Resets this instance to default values.
    /// </summary>
    public override void Reset()
    {
        player = null;
        sendEvent = null;
    }

    /// <summary>
    /// Called when the state becomes active. Adds the MPF BCP 'player_turn_start' event handler.
    /// </summary>
    public override void OnEnter()
    {
        base.OnEnter();
        BcpMessageController.OnPlayerTurnStart += PlayerTurnStart;
    }

    /// <summary>
    /// Called before leaving the current state. Removes the MPF BCP 'player_turn_start' event handler.
    /// </summary>
    public override void OnExit()
    {
		BcpMessageController.OnPlayerTurnStart -= PlayerTurnStart;
		base.OnExit();
    }

    /// <summary>
    /// Event handler called when a player turn start event is received.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="PlayerTurnStartMessageEventArgs"/> instance containing the event data.</param>
    public void PlayerTurnStart(object sender, PlayerTurnStartMessageEventArgs e)
    {
		player.Value = e.PlayerNum;
		Fsm.Event(sendEvent);
    }

}

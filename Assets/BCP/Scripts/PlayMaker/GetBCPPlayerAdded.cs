using UnityEngine;
using System;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

/// <summary>
/// Custom PlayMaker action for MPF that sends an Event when an MPF 'player_added' command is received.
/// </summary>
[ActionCategory("BCP")]
[Tooltip("Sends an Event when an MPF 'player_added' command is received.")]
public class GetBCPPlayerAdded : FsmStateAction
{
	[RequiredField]
    [UIHint(UIHint.Variable)]
    [Tooltip("The variable to receive the player number for the newly added player")]
    public FsmInt number;

    [UIHint(UIHint.Variable)]
    [Tooltip("The PlayMaker event to send when an MPF 'player_added' command is received")]
    public FsmEvent sendEvent;

    /// <summary>
	/// Resets this instance to default values.
	/// </summary>
	public override void Reset()
	{
		number = null;
        sendEvent = null;
    }
	
	/// <summary>
	/// Called when the state becomes active. Adds the MPF BCP 'player_added' event handler.
	/// </summary>
	public override void OnEnter()
	{
		base.OnEnter();
		BcpMessageController.OnPlayerAdded += PlayerAdded;
	}
	
	/// <summary>
	/// Called before leaving the current state. Removes the MPF BCP 'player_added' event handler.
	/// </summary>
	public override void OnExit()
	{
		BcpMessageController.OnPlayerAdded -= PlayerAdded;
		base.OnExit();
	}
	
	
	/// <summary>
	/// Event handler called when a player added event is received.
	/// </summary>
	/// <param name="sender">The sender.</param>
	/// <param name="e">The <see cref="PlayerAddedMessageEventArgs"/> instance containing the event data.</param>
	public void PlayerAdded(object sender, PlayerAddedMessageEventArgs e)
	{
		number.Value = e.PlayerNum;
		Fsm.Event(sendEvent);
	}
	
}

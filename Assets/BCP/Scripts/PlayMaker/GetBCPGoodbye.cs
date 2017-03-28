using UnityEngine;
using System;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

/// <summary>
/// Custom PlayMaker action for MPF that sends an Event when an MPF 'goodbye' command is received.
/// </summary>
[ActionCategory("BCP")]
[Tooltip("Sends an Event when an MPF 'goodbye' command is received.")]
public class GetBCPGoodbye : FsmStateAction
{
    [UIHint(UIHint.Variable)]
    [Tooltip("The PlayMaker event to send when an MPF 'goodbye' command is received")]
    public FsmEvent sendEvent;

    /// <summary>
    /// Resets this instance to default values.
    /// </summary>
    public override void Reset()
    {
        sendEvent = null;
    }

    /// <summary>
    /// Called when the state becomes active. Adds the MPF BCP 'goodbye' event handler.
    /// </summary>
    public override void OnEnter()
    {
        base.OnEnter();
        BcpMessageController.OnGoodbye += Goodbye;
    }

    /// <summary>
    /// Called before leaving the current state. Removes the MPF BCP 'goodbye' event handler.
    /// </summary>
    public override void OnExit()
    {
		BcpMessageController.OnGoodbye -= Goodbye;
		base.OnExit();
    }

    /// <summary>
    /// Event handler called when a goodbye message is received.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="BcpMessageEventArgs"/> instance containing the event data.</param>
    public void Goodbye(object sender, BcpMessageEventArgs e)
    {
        Fsm.Event(sendEvent);
    }
    
}

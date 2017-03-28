using UnityEngine;
using System;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

/// <summary>
/// Custom PlayMaker action for MPF that sends an Event when an MPF 'tilt' command is received.
/// </summary>
[ActionCategory("BCP")]
[Tooltip("Sends an Event when an MPF 'tilt' command is received.")]
public class GetBCPTilt : FsmStateAction
{
    [UIHint(UIHint.Variable)]
    [Tooltip("The PlayMaker event to send when an MPF 'tilt' command is received")]
    public FsmEvent sendEvent;

    /// <summary>
    /// Resets this instance to default values.
    /// </summary>
    public override void Reset()
    {
        sendEvent = null;
    }

    /// <summary>
    /// Called when the state becomes active. Adds the MPF BCP 'tilt' event handler.
    /// </summary>
    public override void OnEnter()
    {
        base.OnEnter();
        BcpMessageController.OnTilt += Tilt;
    }

    /// <summary>
    /// Called before leaving the current state. Removes the MPF BCP 'tilt' event handler.
    /// </summary>
    public override void OnExit()
    {
        BcpMessageController.OnTilt -= Tilt;
        base.OnExit();
    }

    /// <summary>
    /// Event handler called when a tilt event occurs.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="BcpMessageEventArgs"/> instance containing the event data.</param>
    public void Tilt(object sender, BcpMessageEventArgs e)
    {
        Fsm.Event(sendEvent);
    }

}

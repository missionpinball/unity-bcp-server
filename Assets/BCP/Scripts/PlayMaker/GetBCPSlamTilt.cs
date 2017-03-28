using UnityEngine;
using System;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

/// <summary>
/// Custom PlayMaker action for MPF that sends an Event when an MPF 'slam_tilt' command is received.
/// </summary>
[ActionCategory("BCP")]
[Tooltip("Sends an Event when an MPF 'slam_tilt' command is received.")]
public class GetBCPSlamTilt : FsmStateAction
{
    [UIHint(UIHint.Variable)]
    [Tooltip("The PlayMaker event to send when an MPF 'slam_tilt' command is received")]
    public FsmEvent sendEvent;

    /// <summary>
    /// Resets this instance to default values.
    /// </summary>
    public override void Reset()
    {
        sendEvent = null;
    }

    /// <summary>
    /// Called when the state becomes active. Adds the MPF BCP 'slam_tilt' event handler.
    /// </summary>
    public override void OnEnter()
    {
        base.OnEnter();
        BcpMessageController.OnSlamTilt += SlamTilt;
    }

    /// <summary>
    /// Called before leaving the current state. Removes the MPF BCP 'slam_tilt' event handler.
    /// </summary>
    public override void OnExit()
    {
        BcpMessageController.OnSlamTilt -= SlamTilt;
        base.OnExit();
    }

    /// <summary>
    /// Event handler called when a slam tilt event occurs.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="BcpMessageEventArgs"/> instance containing the event data.</param>
    public void SlamTilt(object sender, BcpMessageEventArgs e)
    {
        Fsm.Event(sendEvent);
    }

}

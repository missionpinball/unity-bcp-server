using UnityEngine;
using System;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

/// <summary>
/// Custom PlayMaker action for MPF that sends an Event when an MPF 'reset' command is received.
/// </summary>
[ActionCategory("BCP")]
[Tooltip("Sends an Event when an MPF 'reset' command is received.")]
public class GetBCPReset : FsmStateAction
{
    [UIHint(UIHint.Variable)]
    [Tooltip("Flag indicating whether or not this is a 'hard' reset")]
    public FsmBool hard;

    [UIHint(UIHint.Variable)]
    [Tooltip("The PlayMaker event to send when an MPF 'reset' command is received")]
    public FsmEvent sendEvent;

    /// <summary>
    /// Resets this instance to default values.
    /// </summary>
    public override void Reset()
    {
        hard = null;
        sendEvent = null;
    }

    /// <summary>
    /// Called when the state becomes active. Adds the MPF BCP 'reset' event handler.
    /// </summary>
    public override void OnEnter()
    {
        base.OnEnter();
        BcpMessageController.OnReset += BcpReset;
    }

    /// <summary>
    /// Called before leaving the current state. Removes the MPF BCP 'reset' event handler.
    /// </summary>
    public override void OnExit()
    {
        BcpMessageController.OnReset -= BcpReset;
        base.OnExit();
    }

    /// <summary>
    /// Event handler called when a reset command is received.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="ResetMessageEventArgs"/> instance containing the event data.</param>
    public void BcpReset(object sender, ResetMessageEventArgs e)
    {
        if (!hard.IsNone)
            hard.Value = e.Hard;

        Fsm.Event(sendEvent);
    }

}

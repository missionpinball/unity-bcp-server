using UnityEngine;
using System;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

/// <summary>
/// Custom PlayMaker action for MPF that sends an Event when an MPF 'hello' command is received.
/// </summary>
[ActionCategory("BCP")]
[Tooltip("Sends an Event when an MPF 'hello' command is received.")]
public class GetBCPHello : FsmStateAction
{
    [UIHint(UIHint.Variable)]
    [Tooltip("The variable to receive the value of the version from the 'hello' command")]
    public FsmString version;

    [UIHint(UIHint.Variable)]
    [Tooltip("The PlayMaker event to send when an MPF 'hello' command is received")]
    public FsmEvent sendEvent;

    /// <summary>
    /// Resets this instance to default values.
    /// </summary>
    public override void Reset()
    {
        version = null;
        sendEvent = null;
    }

    /// <summary>
    /// Called when the state becomes active. Adds the MPF BCP 'hello' event handler.
    /// </summary>
    public override void OnEnter()
    {
        base.OnEnter();
        BcpMessageManager.OnHello += Hello;
    }

    /// <summary>
    /// Called before leaving the current state. Removes the MPF BCP 'hello' event handler.
    /// </summary>
    public override void OnExit()
    {
        BcpMessageManager.OnHello -= Hello;
        base.OnExit();
    }

    /// <summary>
    /// Event handler called when receiving a BCP 'hello' command.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="HelloMessageEventArgs"/> instance containing the event data.</param>
    public void Hello(object sender, HelloMessageEventArgs e)
    {
        version.Value = e.Version;
        Fsm.Event(sendEvent);
    }

}

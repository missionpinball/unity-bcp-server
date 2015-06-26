using UnityEngine;
using System;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

/// <summary>
/// Custom PlayMaker action for MPF that sends an Event when an MPF 'config' command is received.
/// </summary>
[ActionCategory("BCP")]
[Tooltip("Sends an Event when an MPF 'config' command is received.")]
public class GetBCPConfig : FsmStateAction
{
    [RequiredField]
    [UIHint(UIHint.Variable)]
    [Tooltip("The name of the MPF configuration variable to listen for")]
    public string variableName;

    [RequiredField]
    [UIHint(UIHint.Variable)]
    [Tooltip("The variable to receive the value of the specified MPF configuration variable")]
    public FsmString value;

    [UIHint(UIHint.Variable)]
    [Tooltip("The PlayMaker event to send when an MPF 'config' command is received")]
    public FsmEvent sendEvent;

    /// <summary>
    /// Resets this instance to default values.
    /// </summary>
    public override void Reset()
    {
        variableName = null;
        value = null;
        sendEvent = null;
    }

    /// <summary>
    /// Called when the state becomes active. Adds the MPF BCP 'config' event handler.
    /// </summary>
    public override void OnEnter()
    {
        base.OnEnter();
        BcpMessageManager.OnConfig += Config;
    }

    /// <summary>
    /// Called before leaving the current state. Removes the MPF BCP 'config' event handler.
    /// </summary>
    public override void OnExit()
    {
        BcpMessageManager.OnConfig -= Config;
        base.OnExit();
    }

    /// <summary>
    /// Event handler called when a config command is received.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="BcpMessageEventArgs"/> instance containing the event data.</param>
    public void Config(object sender, BcpMessageEventArgs e)
    {
        // Determine if this config variable message is the one we are interested in.  If so, send specified FSM event.
        if (!String.IsNullOrEmpty(variableName) && e.BcpMessage.Parameters[variableName] != null)
        {
            value.Value = e.BcpMessage.Parameters[variableName];
            Fsm.Event(sendEvent);
        }
    }

}

using UnityEngine;
using System;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

/// <summary>
/// Custom PlayMaker action for MPF that sends an Event when an MPF 'switch' command is received.
/// </summary>
[ActionCategory("BCP")]
[Tooltip("Sends an Event when an MPF 'switch' inactive command is received.")]
public class GetBCPSwitchInactive : FsmStateAction
{
    [RequiredField]
    [UIHint(UIHint.Variable)]
    [Tooltip("The name of the MPF switch to listen for")]
    public string switchName;

    [UIHint(UIHint.Variable)]
    [Tooltip("The PlayMaker event to send when an MPF 'switch' inactive command is received")]
    public FsmEvent sendEvent;

    /// <summary>
    /// Resets this instance to default values.
    /// </summary>
    public override void Reset()
    {
        switchName = null;
        sendEvent = null;
    }

    /// <summary>
    /// Called when the state becomes active. Adds the MPF BCP 'switch' event handler.
    /// </summary>
    public override void OnEnter()
    {
        base.OnEnter();
        BcpMessageController.OnSwitch += Switch;
    }

    /// <summary>
    /// Called before leaving the current state. Removes the MPF BCP 'switch' event handler.
    /// </summary>
    public override void OnExit()
    {
        BcpMessageController.OnSwitch -= Switch;
        base.OnExit();
    }

    /// <summary>
    /// Event handler called when a switch changes states.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="SwitchMessageEventArgs"/> instance containing the event data.</param>
    public void Switch(object sender, SwitchMessageEventArgs e)
    {
        // Determine if this switch message is the one we are interested in (name and value equal desired values).  If so, send specified FSM event.
        if (!String.IsNullOrEmpty(switchName) && e.Name == switchName && e.State == 0)
            Fsm.Event(sendEvent);
    }

}

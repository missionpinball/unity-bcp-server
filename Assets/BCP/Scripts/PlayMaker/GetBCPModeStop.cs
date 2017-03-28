using UnityEngine;
using System;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

/// <summary>
/// Custom PlayMaker action for MPF that sends an Event when an MPF 'mode_stop' command is received.
/// </summary>
[ActionCategory("BCP")]
[Tooltip("Sends an Event when an MPF 'mode_stop' command is received.")]
public class GetBCPModeStop : FsmStateAction
{
    [RequiredField]
    [UIHint(UIHint.Variable)]
    [Tooltip("The name of the MPF mode to listen for")]
    public string modeName;

    [UIHint(UIHint.Variable)]
    [Tooltip("The PlayMaker event to send when an MPF 'mode_stop' command is received")]
    public FsmEvent sendEvent;

    /// <summary>
    /// Resets this instance to default values.
    /// </summary>
    public override void Reset()
    {
        modeName = null;
        sendEvent = null;
    }

    /// <summary>
    /// Called when the state becomes active. Adds the MPF BCP 'mode_stop' event handler.
    /// </summary>
    public override void OnEnter()
    {
        base.OnEnter();
        BcpMessageController.OnModeStop += ModeStop;
    }

    /// <summary>
    /// Called before leaving the current state. Removes the MPF BCP 'mode_stop' event handler.
    /// </summary>
    public override void OnExit()
    {
		BcpMessageController.OnModeStop -= ModeStop;
		base.OnExit();
    }

    /// <summary>
    /// Event handler called when a mode stops.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="ModeStopMessageEventArgs"/> instance containing the event data.</param>
    public void ModeStop(object sender, ModeStopMessageEventArgs e)
    {
        // Determine if this mode message is the one we are interested in.  If so, send specified FSM event.
        if (!String.IsNullOrEmpty(modeName) && e.Name == modeName)
            Fsm.Event(sendEvent);
    
    }

}

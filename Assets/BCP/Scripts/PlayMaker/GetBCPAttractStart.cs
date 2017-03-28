using UnityEngine;
using System;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

/// <summary>
/// Custom PlayMaker action for MPF that sends an Event when an MPF attract 'mode_start' command is received.
/// </summary>
[ActionCategory("BCP")]
[Tooltip("Sends an Event when an MPF 'mode_start' attract command is received.")]
public class GetBCPAttractStart : FsmStateAction
{
    [RequiredField]
    [UIHint(UIHint.Variable)]
    [Tooltip("The name of the MPF attract mode to listen for")]
    public string modeName;

    [UIHint(UIHint.Variable)]
    [Tooltip("The optional variable to receive the value of the priority for the specified mode")]
    public FsmInt priority;

    [UIHint(UIHint.Variable)]
    [Tooltip("The PlayMaker event to send when an MPF attract 'mode_start' command is received")]
    public FsmEvent sendEvent;

    /// <summary>
    /// Resets this instance to default values.
    /// </summary>
    public override void Reset()
    {
        modeName = "attract";
        priority = null;
        sendEvent = null;
    }

    /// <summary>
    /// Called when the state becomes active. Adds the MPF BCP 'mode_start' event handler.
    /// </summary>
    public override void OnEnter()
    {
        base.OnEnter();
        BcpMessageController.OnModeStart += ModeStart;
    }

    /// <summary>
    /// Called before leaving the current state. Removes the MPF BCP 'mode_start' event handler.
    /// </summary>
    public override void OnExit()
    {
        BcpMessageController.OnModeStart -= ModeStart;
        base.OnExit();
    }

    /// <summary>
    /// Event handler called when a mode is started.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="ModeStartMessageEventArgs"/> instance containing the event data.</param>
    public void ModeStart(object sender, ModeStartMessageEventArgs e)
    {
        // Determine if this mode message is the one we are interested in.  If so, send specified FSM event.
        if (!String.IsNullOrEmpty(modeName) && e.Name == modeName)
        {
            if (!priority.IsNone)
                priority.Value = e.Priority;
            Fsm.Event(sendEvent);
        }
    }

}

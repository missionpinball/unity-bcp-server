using UnityEngine;
using System;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

/// <summary>
/// Custom PlayMaker action for MPF that sends an Event when an MPF 'tilt_warning' command is received.
/// </summary>
[ActionCategory("BCP")]
[Tooltip("Sends an Event when an MPF 'tilt_warning' command is received.")]
public class GetBCPTiltWarning : FsmStateAction
{
    [UIHint(UIHint.Variable)]
    [Tooltip("The variable to receive the number of tilt warnings value")]
    public FsmInt warnings;

    [UIHint(UIHint.Variable)]
    [Tooltip("The variable to receive the number of tilt warnings remaining before a tilt value")]
    public FsmInt warningsRemaining;

    [UIHint(UIHint.Variable)]
    [Tooltip("The PlayMaker event to send when an MPF 'tilt_warning' command is received")]
    public FsmEvent sendEvent;

    /// <summary>
    /// Resets this instance to default values.
    /// </summary>
    public override void Reset()
    {
        warnings = null;
        warningsRemaining = null;
        sendEvent = null;
    }

    /// <summary>
    /// Called when the state becomes active. Adds the MPF BCP 'tilt_warning' event handler.
    /// </summary>
    public override void OnEnter()
    {
        base.OnEnter();
        BcpMessageController.OnTiltWarning += TiltWarning;
    }

    /// <summary>
    /// Called before leaving the current state. Removes the MPF BCP 'tilt_warning' event handler.
    /// </summary>
    public override void OnExit()
    {
        BcpMessageController.OnTiltWarning -= TiltWarning;
        base.OnExit();
    }

    /// <summary>
    /// Event handler called when a tilt warning event occurs.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="TiltWarningMessageEventArgs"/> instance containing the event data.</param>
    public void TiltWarning(object sender, TiltWarningMessageEventArgs e)
    {
        if (!warnings.IsNone)
            warnings.Value = e.Warnings;

        if (!warningsRemaining.IsNone)
            warningsRemaining.Value = e.WarningsRemaining;

        Fsm.Event(sendEvent);
    }

}

using UnityEngine;
using System;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

/// <summary>
/// Custom PlayMaker action for MPF that sends an Event when an MPF 'timer' complte command is received.
/// </summary>
[ActionCategory("BCP")]
[Tooltip("Sends an Event when an MPF 'timer' complete command is received.")]
public class GetBCPTimerComplete : FsmStateAction
{
    [RequiredField]
    [UIHint(UIHint.Variable)]
    [Tooltip("The name of the MPF timer to listen for")]
    public string timerName;

    [RequiredField]
    [UIHint(UIHint.Variable)]
    [Tooltip("The variable to receive the timer ticks value")]
    public FsmInt ticks;

    [UIHint(UIHint.Variable)]
    [Tooltip("The PlayMaker event to send when an MPF 'timer' complete command is received")]
    public FsmEvent sendEvent;

    /// <summary>
    /// Resets this instance to default values.
    /// </summary>
    public override void Reset()
    {
        timerName = null;
        ticks = null;
        sendEvent = null;
    }

    /// <summary>
    /// Called when the state becomes active. Adds the MPF BCP 'timer' event handler.
    /// </summary>
    public override void OnEnter()
    {
        base.OnEnter();
        BcpMessageController.OnTimer += Timer;
    }

    /// <summary>
    /// Called before leaving the current state. Removes the MPF BCP 'timer' event handler.
    /// </summary>
    public override void OnExit()
    {
        BcpMessageController.OnTimer -= Timer;
        base.OnExit();
    }

    /// <summary>
    /// Event handler called when a timer event occurs.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="TimerMessageEventArgs"/> instance containing the event data.</param>
    public void Timer(object sender, TimerMessageEventArgs e)
    {
        // Determine if this timer message is the one we are interested in.  If so, send specified FSM event.
        if (!String.IsNullOrEmpty(timerName) && e.Name == timerName && e.Action == "complete")
        {
            ticks.Value = e.Ticks;
            Fsm.Event(sendEvent);
        }
    }

}

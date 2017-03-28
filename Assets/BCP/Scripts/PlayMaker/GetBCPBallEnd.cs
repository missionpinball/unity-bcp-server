using UnityEngine;
using System;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

/// <summary>
/// Custom PlayMaker action for MPF that sends an Event when an MPF 'ball_end' command is received.
/// </summary>
[ActionCategory("BCP")]
[Tooltip("Sends an Event when an MPF 'ball_end' command is received.")]
public class GetBCPBallEnd : FsmStateAction
{
    [UIHint(UIHint.Variable)]
    [Tooltip("The PlayMaker event to send when an MPF 'ball_end' command is received")]
    public FsmEvent sendEvent;

    /// <summary>
    /// Resets this instance to default values.
    /// </summary>
    public override void Reset()
    {
        sendEvent = null;
    }

    /// <summary>
    /// Called when the state becomes active. Adds the MPF BCP 'ball_end' event handler.
    /// </summary>
    public override void OnEnter()
    {
        base.OnEnter();
        BcpMessageController.OnBallEnd += BallEnd;
    }

    /// <summary>
    /// Called before leaving the current state. Removes the MPF BCP 'ball_end' event handler.
    /// </summary>
    public override void OnExit()
    {
		BcpMessageController.OnBallEnd -= BallEnd;
		base.OnExit();
    }


    /// <summary>
    /// Event handler called when a ball end event is received.
    /// </summary>
    /// <param name="sender">The sender.</param>
    public void BallEnd(object sender, BcpMessageEventArgs e)
    {
        Fsm.Event(sendEvent);
    }

}

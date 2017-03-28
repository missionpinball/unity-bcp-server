using UnityEngine;
using System;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

/// <summary>
/// Custom PlayMaker action for MPF that sends an Event when an MPF 'ball_start' command is received.
/// </summary>
[ActionCategory("BCP")]
[Tooltip("Sends an Event when an MPF 'ball_start' command is received.")]
public class GetBCPBallStart : FsmStateAction
{
    [RequiredField]
    [UIHint(UIHint.Variable)]
    [Tooltip("The variable to receive the current player number for the newly started ball")]
    public FsmInt player;

    [RequiredField]
    [UIHint(UIHint.Variable)]
    [Tooltip("The variable to receive the ball number for the newly started ball")]
    public FsmInt ball;

    [UIHint(UIHint.Variable)]
    [Tooltip("The PlayMaker event to send when an MPF 'ball_start' command is received")]
    public FsmEvent sendEvent;

    /// <summary>
    /// Resets this instance to default values.
    /// </summary>
    public override void Reset()
    {
        player = null;
        ball = null;
        sendEvent = null;
    }

    /// <summary>
    /// Called when the state becomes active. Adds the MPF BCP 'ball_start' event handler.
    /// </summary>
    public override void OnEnter()
    {
        base.OnEnter();
        BcpMessageController.OnBallStart += BallStart;
    }

    /// <summary>
    /// Called before leaving the current state. Removes the MPF BCP 'ball_start' event handler.
    /// </summary>
    public override void OnExit()
    {
		BcpMessageController.OnBallStart -= BallStart;
		base.OnExit();
    }


    /// <summary>
    /// Event handler called when a ball start event is received.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="BallStartMessageEventArgs"/> instance containing the event data.</param>
    public void BallStart(object sender, BallStartMessageEventArgs e)
    {
        player.Value = e.PlayerNum;
        ball.Value = e.Ball;
		Fsm.Event(sendEvent);
	}

}

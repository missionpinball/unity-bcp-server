using UnityEngine;
using System;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

/// <summary>
/// Custom PlayMaker action for MPF that sends an Event when an MPF 'player_score' command is received.
/// </summary>
[ActionCategory("BCP")]
[Tooltip("Sends an Event when an MPF 'player_score' command is received.")]
public class GetBCPPlayerScore : FsmStateAction
{
    [RequiredField]
    [UIHint(UIHint.Variable)]
    [Tooltip("The variable to receive the score value for the current player")]
    public FsmInt score;

	[UIHint(UIHint.Variable)]
    [Tooltip("The variable to receive the previous score value for the current player")]
    public FsmInt previousScore;

	[UIHint(UIHint.Variable)]
    [Tooltip("The variable to receive the change in value for the current player's score")]
    public FsmInt change;

    [UIHint(UIHint.Variable)]
    [Tooltip("The PlayMaker event to send when an MPF 'player_score' command is received")]
    public FsmEvent sendEvent;

    /// <summary>
    /// Resets this instance to default values.
    /// </summary>
    public override void Reset()
    {
        score = null;
        previousScore = null;
        change = null;
        sendEvent = null;
    }

    /// <summary>
    /// Called when the state becomes active. Adds the MPF BCP 'player_score' event handler.
    /// </summary>
    public override void OnEnter()
    {
        base.OnEnter();
        BcpMessageController.OnPlayerScore += PlayerScore;
    }

    /// <summary>
    /// Called before leaving the current state. Removes the MPF BCP 'player_score' event handler.
    /// </summary>
    public override void OnExit()
    {
		BcpMessageController.OnPlayerScore -= PlayerScore;
		base.OnExit();
    }

    /// <summary>
    /// Event handler called when a player score event is received.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="PlayerScoreMessageEventArgs"/> instance containing the event data.</param>
    public void PlayerScore(object sender, PlayerScoreMessageEventArgs e)
    {
		score.Value = e.Value;

		if (!previousScore.IsNone)
			previousScore.Value = e.PreviousValue;
		if (!change.IsNone)
			change.Value = e.Change;

		Fsm.Event(sendEvent);
    }

}

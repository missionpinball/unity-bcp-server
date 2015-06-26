using UnityEngine;
using System;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

/// <summary>
/// Custom PlayMaker action for MPF that sends an Event when an MPF 'game_end' command is received.
/// </summary>
[ActionCategory("MPF")]
[Tooltip("Sends an Event when an MPF 'game_end' command is received.")]
public class GetMPFGameEnd : FsmStateAction
{
    [UIHint(UIHint.Variable)]
    [Tooltip("The PlayMaker event to send when an MPF 'game_end' command is received")]
    public FsmEvent sendEvent;

    /// <summary>
    /// Resets this instance to default values.
    /// </summary>
    public override void Reset()
    {
        sendEvent = null;
    }

    /// <summary>
    /// Called when the state becomes active. Adds the MPF BCP 'game_end' event handler.
    /// </summary>
    public override void OnEnter()
    {
        base.OnEnter();
        BcpMessageManager.OnGameEnd += GameEnd;
    }

    /// <summary>
    /// Called before leaving the current state. Removes the MPF BCP 'game_end' event handler.
    /// </summary>
    public override void OnExit()
    {
		BcpMessageManager.OnGameEnd -= GameEnd;
		base.OnExit();
    }


    /// <summary>
    /// Event handler called when a game is ended.
    /// </summary>
    /// <param name="sender">The sender.</param>
    public void GameEnd(object sender, BcpMessageEventArgs e)
    {
        Fsm.Event(sendEvent);
    }


}

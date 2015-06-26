using UnityEngine;
using System;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

/// <summary>
/// Custom PlayMaker action for MPF that sends an Event when an MPF 'attract_start' command is received.
/// </summary>
[ActionCategory("BCP")]
[Tooltip("Sends an Event when an MPF 'attract_start' command is received.")]
public class GetBCPAttractStart : FsmStateAction
{
    [UIHint(UIHint.Variable)]
    [Tooltip("The PlayMaker event to send when an MPF 'attract_start' command is received")]
    public FsmEvent sendEvent;

    /// <summary>
    /// Resets this instance to default values.
    /// </summary>
    public override void Reset()
    {
        sendEvent = null;
    }

    /// <summary>
    /// Called when the state becomes active. Adds the MPF BCP 'attract_start' event handler.
    /// </summary>
    public override void OnEnter()
    {
        base.OnEnter();
        BcpMessageManager.OnAttractStart += AttractModeStart;
    }

    /// <summary>
    /// Called before leaving the current state. Removes the MPF BCP 'attract_start' event handler.
    /// </summary>
    public override void OnExit()
    {
		BcpMessageManager.OnAttractStart -= AttractModeStart;
		base.OnExit();
    }


    /// <summary>
    /// Event handler called when attract mode is started.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    public void AttractModeStart(object sender, BcpMessageEventArgs e)
    {
        Fsm.Event(sendEvent);
    }


}

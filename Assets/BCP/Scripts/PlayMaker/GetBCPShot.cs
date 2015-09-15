using UnityEngine;
using System;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

/// <summary>
/// Custom PlayMaker action for MPF that sends an Event when an MPF 'shot' command is received.
/// </summary>
[ActionCategory("BCP")]
[Tooltip("Sends an Event when an MPF 'shot' command is received.")]
public class GetBCPShot : FsmStateAction
{
    [RequiredField]
    [UIHint(UIHint.Variable)]
    [Tooltip("The name of the MPF shot to listen for")]
    public string shotName;

    [UIHint(UIHint.Variable)]
    [Tooltip("The profile of the MPF shot")]
    public FsmString profile;

    [UIHint(UIHint.Variable)]
    [Tooltip("The state of the MPF shot")]
    public FsmString state;

    [UIHint(UIHint.Variable)]
    [Tooltip("The PlayMaker event to send when an MPF 'shot' command is received")]
    public FsmEvent sendEvent;

    /// <summary>
    /// Resets this instance to default values.
    /// </summary>
    public override void Reset()
    {
        shotName = null;
        profile = null;
        state = null;
        sendEvent = null;
    }

    /// <summary>
    /// Called when the state becomes active. Adds the MPF BCP 'shot' event handler.
    /// </summary>
    public override void OnEnter()
    {
        base.OnEnter();
        BcpMessageManager.OnShot += Shot;
    }

    /// <summary>
    /// Called before leaving the current state. Removes the MPF BCP 'shot' event handler.
    /// </summary>
    public override void OnExit()
    {
        BcpMessageManager.OnShot -= Shot;
        base.OnExit();
    }

    /// <summary>
    /// Event handler called when a shot is hit.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="ShotMessageEventArgs"/> instance containing the event data.</param>
    public void Shot(object sender, ShotMessageEventArgs e)
    {
        // Determine if this shot message is the one we are interested in (name equals desired value).  If so, send specified FSM event.
        if (!String.IsNullOrEmpty(shotName) && e.Name == shotName)
        {
            if (!profile.IsNone)
                profile.Value = e.Profile;

            if (!state.IsNone)
                state.Value = e.State;

            Fsm.Event(sendEvent);
        }
    }

}

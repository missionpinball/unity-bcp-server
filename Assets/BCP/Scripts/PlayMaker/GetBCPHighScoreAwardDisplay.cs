using UnityEngine;
using System;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

/// <summary>
/// Custom PlayMaker action for MPF that sends an Event when a high_score_award_display BCP Trigger 
/// command is received.
/// </summary>
[ActionCategory("BCP")]
[Tooltip("Sends an Event when the special BCP Trigger high_score_award_display command is received.")]
public class GetBCPHighScoreAwardDisplay : FsmStateAction
{
    [RequiredField]
    [UIHint(UIHint.Variable)]
    [Tooltip("The variable to receive the award label value")]
    public FsmString award;

    [RequiredField]
    [UIHint(UIHint.Variable)]
    [Tooltip("The variable to receive the player name/initials")]
    public FsmString playerName;

    [RequiredField]
    [UIHint(UIHint.Variable)]
    [Tooltip("The variable to receive the high score value")]
    public FsmInt value;

    [UIHint(UIHint.Variable)]
    [Tooltip("The PlayMaker event to send when the high_score_award_display BCP Trigger is received")]
    public FsmEvent sendEvent;

    private string triggerName;
    private bool registered;

    /// <summary>
    /// Resets this instance to default values.
    /// </summary>
    public override void Reset()
    {
        base.Reset();
        triggerName = "high_score_award_display";
        award = null;
        playerName = null;
        value = null;
        sendEvent = null;
        registered = false;
    }

    /// <summary>
    /// Called when the state becomes active. Adds the MPF BCP Trigger event handler.
    /// </summary>
    public override void OnEnter()
    {
        base.OnEnter();

        // Auto-register the trigger name with the pin controller (if necessary)
        if (!registered)
        {
            BcpServer.Instance.Send(BcpMessage.RegisterTriggerMessage(triggerName));
            registered = true;
        }

        BcpMessageController.OnTrigger += Trigger;
    }

    /// <summary>
    /// Called before leaving the current state. Removes the MPF BCP Trigger event handler.
    /// </summary>
    public override void OnExit()
    {
        BcpMessageController.OnTrigger -= Trigger;
        base.OnExit();
    }

    /// <summary>
    /// Event handler called when a trigger message is received from MPF.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="TriggerMessageEventArgs"/> instance containing the event data.</param>
    public void Trigger(object sender, TriggerMessageEventArgs e)
    {
        // Determine if this trigger message is the one we are interested in.  If so, send specified FSM event.
        if (!String.IsNullOrEmpty(triggerName) && e.Name == triggerName)
        {
            try
            {
                award = e.BcpMessage.Parameters["award"].Value;
                playerName = e.BcpMessage.Parameters["player_name"].Value;
                value = e.BcpMessage.Parameters["value"].AsInt;
                Fsm.Event(sendEvent);
            }
            catch (Exception ex)
            {
                BcpServer.Instance.Send(BcpMessage.ErrorMessage("An error occurred while processing a 'high_score_award_display' trigger message: " + ex.Message, e.BcpMessage.RawMessage));
            }

        }
    }

}

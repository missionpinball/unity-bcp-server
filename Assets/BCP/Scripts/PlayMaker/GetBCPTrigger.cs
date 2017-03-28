using UnityEngine;
using System;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

/// <summary>
/// Custom PlayMaker action for MPF that sends an Event when an BCP Trigger command is received.
/// </summary>
[ActionCategory("BCP")]
[Tooltip("Sends an Event when an BCP Trigger command is received.")]
public class GetBCPTrigger : FsmStateAction
{
    [RequiredField]
    [UIHint(UIHint.Variable)]
    [Tooltip("The name of the BCP Trigger to listen for")]
    public string triggerName;

    [RequiredField]
    [UIHint(UIHint.Variable)]
    [Tooltip("Whether or not to automatically register the trigger name with MPF")]
    public bool autoRegisterTriggerName;

    [UIHint(UIHint.Variable)]
    [Tooltip("The PlayMaker event to send when the specified BCP Trigger is received")]
    public FsmEvent sendEvent;

    private bool registered;

    /// <summary>
    /// Resets this instance to default values.
    /// </summary>
    public override void Reset()
    {
        base.Reset();
        triggerName = null;
        autoRegisterTriggerName = true;
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
        if (autoRegisterTriggerName && !String.IsNullOrEmpty(triggerName) && !registered)
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
            Fsm.Event(sendEvent);
    }

}

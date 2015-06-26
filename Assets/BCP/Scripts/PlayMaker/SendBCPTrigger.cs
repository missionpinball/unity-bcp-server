using UnityEngine;
using System;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

/// <summary>
/// Custom PlayMaker action for MPF that sends a Trigger BCP command to MPF.
/// </summary>
[ActionCategory("BCP")]
[Tooltip("Sends Trigger BCP command to MPF.")]
public class SendBCPTrigger : FsmStateAction
{
    [RequiredField]
    [UIHint(UIHint.Variable)]
    [Tooltip("The name of the trigger to send to the MPF pinball controller")]
    public string triggerName;

    /// <summary>
    /// Resets this instance to default values.
    /// </summary>
    public override void Reset()
    {
        triggerName = null;
    }

    /// <summary>
    /// Called when the state becomes active. Sends the BCP Trigger command to MPF.
    /// </summary>
    public override void OnEnter()
    {
        if (!String.IsNullOrEmpty(triggerName))
            BcpServer.Instance.Send(BcpMessage.TriggerMessage(triggerName));

        Finish();
    }

}

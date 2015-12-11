using UnityEngine;
using System;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

/// <summary>
/// Custom PlayMaker action for MPF that sends a 'external_show_start' BCP command to MPF.
/// </summary>
[ActionCategory("BCP")]
[Tooltip("Sends 'external_show_stop' BCP command to MPF. Stops an external show that is currently running.")]
public class SendBCPExternalShowStop : FsmStateAction
{
    [RequiredField]
    [UIHint(UIHint.Variable)]
    [Tooltip("The name of the external show to stop on the MPF pinball controller.")]
    public string name;

    /// <summary>
    /// Resets this instance to default values.
    /// </summary>
    public override void Reset()
    {
        name = null;
    }

    /// <summary>
    /// Called when the state becomes active. Sends the BCP External Show Start command to MPF.
    /// </summary>
    public override void OnEnter()
    {
        if (!String.IsNullOrEmpty(name))
            BcpServer.Instance.Send(BcpMessage.ExternalShowStopMessage(name));

        Finish();
    }

}

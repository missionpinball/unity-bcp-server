using UnityEngine;
using System;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

/// <summary>
/// Custom PlayMaker action for MPF that sends a Reset Complete BCP command to MPF.
/// </summary>
[ActionCategory("BCP")]
[Tooltip("Sends Reset Complete BCP command to MPF.")]
public class SendBCPResetComplete : FsmStateAction
{
    /// <summary>
    /// Resets this instance to default values.
    /// </summary>
    public override void Reset()
    {
    }

    /// <summary>
    /// Called when the state becomes active. Sends the BCP Reset Complete command to MPF.
    /// </summary>
    public override void OnEnter()
    {
        BcpServer.Instance.Send(BcpMessage.ResetCompleteMessage());
        Finish();
    }

}

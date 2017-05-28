using UnityEngine;
using System;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

/// <summary>
/// Custom PlayMaker action for MPF that sends a 'switch' BCP command to MPF.
/// </summary>
[ActionCategory("BCP")]
[Tooltip("Sends 'switch' BCP command to MPF. Can be used to simlulate switch activations (useful in debugging).")]
public class SendBCPSwitch : FsmStateAction
{
    [RequiredField]
    [UIHint(UIHint.Variable)]
    [Tooltip("The name of the switch to send to the MPF pinball controller")]
    public string switchName;

    [RequiredField]
    [UIHint(UIHint.Variable)]
    [Tooltip("The state of the switch to send to the MPF pinball controller (ex: '1' or '0')")]
    public int switchState;

    /// <summary>
    /// Resets this instance to default values.
    /// </summary>
    public override void Reset()
    {
        switchName = null;
        switchState = 0;
    }

    /// <summary>
    /// Called when the state becomes active. Sends the BCP Trigger command to MPF.
    /// </summary>
    public override void OnEnter()
    {
        if (!String.IsNullOrEmpty(switchName))
            BcpServer.Instance.Send(BcpMessage.SwitchMessage(switchName, switchState));

        Finish();
    }

}

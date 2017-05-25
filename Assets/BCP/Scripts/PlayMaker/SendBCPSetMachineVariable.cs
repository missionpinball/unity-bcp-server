using UnityEngine;
using System;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

/// <summary>
/// Custom PlayMaker action for MPF that sends a Set Machine Variable command to MPF.
/// </summary>
[ActionCategory("BCP")]
[Tooltip("Sends Set Machine Variable BCP command to MPF.")]
public class SendBCPTSetMachineVariable : FsmStateAction
{
    [RequiredField]
    [UIHint(UIHint.Variable)]
    [Tooltip("The name of the machine variable to set")]
    public string name;

    [RequiredField]
    [UIHint(UIHint.Variable)]
    [Tooltip("The value of the machine variable to set")]
    public string value;

    /// <summary>
    /// Resets this instance to default values.
    /// </summary>
    public override void Reset()
    {
        name = null;
        value = null;
    }

    /// <summary>
    /// Called when the state becomes active. Sends the BCP Trigger command to MPF.
    /// </summary>
    public override void OnEnter()
    {
        if (!String.IsNullOrEmpty(name))

            if (name == null)
                name = "NoneType:";

            BcpServer.Instance.Send(BcpMessage.SetMachineVariableMessage(name, value));

        Finish();
    }

}

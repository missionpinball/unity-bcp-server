using UnityEngine;
using System;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;
using BCP.SimpleJSON;

/// <summary>
/// Custom PlayMaker action for MPF that sends an Event when an MPF 'machine_variable' command is received
/// (when the machine variable changes).
/// </summary>
[ActionCategory("BCP")]
[Tooltip("Retrieves the current value of an MPF machine_variable.")]
public class GetBCPMachineVariableString : FsmStateAction
{
    [RequiredField]
    [UIHint(UIHint.Variable)]
    [Tooltip("The name of the MPF machine variable to retrieve")]
    public string machineVariableName;

    [RequiredField]
    [UIHint(UIHint.Variable)]
    [Tooltip("The variable to receive the value of the specified MPF machine variable")]
    public FsmString value;

    /// <summary>
    /// Resets this instance to default values.
    /// </summary>
    public override void Reset()
    {
        machineVariableName = null;
        value = null;
    }

    /// <summary>
    /// Called when the state becomes active. Adds the MPF BCP 'machine_variable' event handler.
    /// </summary>
    public override void OnEnter()
    {
        base.OnEnter();

        if (!String.IsNullOrEmpty(machineVariableName))
        {
            JSONNode variable = BcpMessageManager.Instance.GetMachineVariable(machineVariableName);
            if (variable != null)
                value.Value = variable.Value;
        }

        Finish();
    }

}

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
[Tooltip("Sends an Event when an MPF 'machine_variable' command is received.")]
public class GetBCPMachineVariableChange : FsmStateAction
{
    [RequiredField]
    [UIHint(UIHint.Variable)]
    [Tooltip("The name of the MPF machine variable to listen for")]
    public string machineVariableName;

    [UIHint(UIHint.Variable)]
    [Tooltip("The string variable to receive the new value of the specified MPF machine variable")]
    public FsmString stringValue;

    [UIHint(UIHint.Variable)]
    [Tooltip("The int variable to receive the new value of the specified MPF machine variable")]
    public FsmInt intValue;

    [UIHint(UIHint.Variable)]
    [Tooltip("The float variable to receive the new value of the specified MPF machine variable")]
    public FsmFloat floatValue;

    [UIHint(UIHint.Variable)]
    [Tooltip("The boolean variable to receive the new value of the specified MPF machine variable")]
    public FsmBool boolValue;

    [UIHint(UIHint.Variable)]
    [Tooltip("The PlayMaker event to send when an MPF 'machine_variable' command is received")]
    public FsmEvent sendEvent;

    /// <summary>
    /// Resets this instance to default values.
    /// </summary>
    public override void Reset()
    {
        machineVariableName = null;
        stringValue = null;
        intValue = null;
        floatValue = null;
        boolValue = null;
        sendEvent = null;
    }

    /// <summary>
    /// Called when the state becomes active. Adds the MPF BCP 'machine_variable' event handler.
    /// </summary>
    public override void OnEnter()
    {
        base.OnEnter();
        BcpMessageController.OnMachineVariable += MachineVariable;
    }

    /// <summary>
    /// Called before leaving the current state. Removes the MPF BCP 'machine_variable' event handler.
    /// </summary>
    public override void OnExit()
    {
        BcpMessageController.OnMachineVariable -= MachineVariable;
        base.OnExit();
    }

    /// <summary>
    /// Event handler called when a machine variable event is received.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="MachineVariableMessageEventArgs"/> instance containing the event data.</param>
    public void MachineVariable(object sender, MachineVariableMessageEventArgs e)
    {
        // Determine if this machine variable message is the one we are interested in.  If so, send specified FSM event.
        if (!String.IsNullOrEmpty(machineVariableName) && e.Name == machineVariableName)
        {
            JSONNode variable = e.Value;
            if (variable != null)
            {
                if (stringValue != null && !stringValue.IsNone) stringValue.Value = variable.Value;
                if (intValue != null && !intValue.IsNone) intValue.Value = variable.AsInt;
                if (floatValue != null && !floatValue.IsNone) floatValue.Value = variable.AsFloat;
                if (boolValue != null && !boolValue.IsNone) boolValue.Value = variable.AsBool;
            }

            Fsm.Event(sendEvent);
        }
    }

}

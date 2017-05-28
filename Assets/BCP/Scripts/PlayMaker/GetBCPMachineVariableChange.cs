using UnityEngine;
using System;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

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

    [RequiredField]
    [UIHint(UIHint.Variable)]
    [Tooltip("The variable to receive the value of the specified MPF machine variable")]
    public FsmString value;

    [UIHint(UIHint.Variable)]
    [Tooltip("The PlayMaker event to send when an MPF 'machine_variable' command is received")]
    public FsmEvent sendEvent;

    /// <summary>
    /// Resets this instance to default values.
    /// </summary>
    public override void Reset()
    {
        machineVariableName = null;
        value = null;
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
            if (!value.IsNone)
                value.Value = e.Value;

            Fsm.Event(sendEvent);
        }
    }

}

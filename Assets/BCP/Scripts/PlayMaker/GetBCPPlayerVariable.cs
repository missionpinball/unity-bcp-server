using UnityEngine;
using System;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;
using BCP.SimpleJSON;

/// <summary>
/// Custom PlayMaker action for MPF that retrieves the value of a 'player_variable'.
/// </summary>
[ActionCategory("BCP")]
[Tooltip("Retrieves the value of an MPF player_variable.")]
public class GetBCPPlayerVariable : FsmStateAction
{
    [RequiredField]
    [UIHint(UIHint.Variable)]
    [Tooltip("The name of the MPF player variable to retrieve")]
    public string playerVariableName;

    [RequiredField]
    [UIHint(UIHint.Variable)]
    [Tooltip("The player number of the MPF player variable to retrieve")]
    public int playerNum;

    [UIHint(UIHint.Variable)]
    [Tooltip("The string variable to receive the value of the specified MPF player variable")]
    public FsmString stringValue;

    [UIHint(UIHint.Variable)]
    [Tooltip("The int variable to receive the value of the specified MPF player variable")]
    public FsmInt intValue;

    [UIHint(UIHint.Variable)]
    [Tooltip("The float variable to receive the value of the specified MPF player variable")]
    public FsmFloat floatValue;

    [UIHint(UIHint.Variable)]
    [Tooltip("The boolean variable to receive the value of the specified MPF player variable")]
    public FsmBool boolValue;

    /// <summary>
    /// Resets this instance to default values.
    /// </summary>
    public override void Reset()
    {
        playerVariableName = null;
        playerNum = 1;
        stringValue = null;
        intValue = null;
        floatValue = null;
        boolValue = null;
    }

    /// <summary>
    /// Called when the state becomes active. Adds the MPF BCP 'machine_variable' event handler.
    /// </summary>
    public override void OnEnter()
    {
        base.OnEnter();

        if (!String.IsNullOrEmpty(playerVariableName))
        {
            JSONNode variable = BcpMessageManager.Instance.GetPlayerVariable(playerNum, playerVariableName);
            if (variable != null)
            {
                if (stringValue != null && !stringValue.IsNone) stringValue.Value = variable.Value;
                if (intValue != null && !intValue.IsNone) intValue.Value = variable.AsInt;
                if (floatValue != null && !floatValue.IsNone) floatValue.Value = variable.AsFloat;
                if (boolValue != null && !boolValue.IsNone) boolValue.Value = variable.AsBool;
            }
        }

        Finish();
    }

}

using UnityEngine;
using System;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;
using BCP.SimpleJSON;

/// <summary>
/// Custom PlayMaker action for MPF that retrieves the current value for a named setting.
/// </summary>
[ActionCategory("BCP")]
[Tooltip("Retrieves the current value of an MPF setting.")]
public class GetBCPSetting : FsmStateAction
{
    [RequiredField]
    [UIHint(UIHint.Variable)]
    [Tooltip("The name of the MPF setting to retrieve")]
    public string settingName;

    [UIHint(UIHint.Variable)]
    [Tooltip("The string variable to receive the value of the specified MPF setting")]
    public FsmString stringValue;

    [UIHint(UIHint.Variable)]
    [Tooltip("The int variable to receive the value of the specified MPF setting")]
    public FsmInt intValue;

    [UIHint(UIHint.Variable)]
    [Tooltip("The float variable to receive the value of the specified MPF setting")]
    public FsmFloat floatValue;

    [UIHint(UIHint.Variable)]
    [Tooltip("The boolean variable to receive the value of the specified MPF setting")]
    public FsmBool boolValue;

    /// <summary>
    /// Resets this instance to default values.
    /// </summary>
    public override void Reset()
    {
        settingName = null;
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

        if (!String.IsNullOrEmpty(settingName))
        {
            JSONNode variable = BcpMessageManager.Instance.GetSetting(settingName);
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

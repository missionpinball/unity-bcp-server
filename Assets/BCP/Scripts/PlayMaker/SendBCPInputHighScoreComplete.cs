using UnityEngine;
using System;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;
using BCP.SimpleJSON;

/// <summary>
/// Custom PlayMaker action for MPF that sends a Trigger BCP command to MPF.
/// </summary>
[ActionCategory("BCP")]
[Tooltip("Sends 'text_input_high_score_complete' Trigger BCP command to MPF.")]
public class SendBCPInputHighScoreComplete : FsmStateAction
{
    [RequiredField]
    [UIHint(UIHint.Variable)]
    [Tooltip("The text initials of the high score player to send to MPF")]
    public string text;

    /// <summary>
    /// Resets this instance to default values.
    /// </summary>
    public override void Reset()
    {
        text = null;
    }

    /// <summary>
    /// Called when the state becomes active. Sends the BCP Trigger command to MPF.
    /// </summary>
    public override void OnEnter()
    {
        BcpMessage message = BcpMessage.TriggerMessage("text_input_high_score_complete");
        message.Parameters["text"] = new JSONString(text);
        BcpServer.Instance.Send(message);

        Finish();
    }

}

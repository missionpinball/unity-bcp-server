using UnityEngine;
using System;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

/// <summary>
/// Custom PlayMaker action for MPF that sends a 'external_show_frame' BCP command to MPF.
/// </summary>
[ActionCategory("BCP")]
[Tooltip("Sends 'external_show_frame' BCP command to MPF. Used to send LED/light/flasher/GI data to the MPF light controller for the specified external show.")]
public class SendBCPExternalShowFrame : FsmStateAction
{
    [RequiredField]
    [UIHint(UIHint.Variable)]
    [Tooltip("The name of the external show.")]
    public string name;

    [UIHint(UIHint.Variable)]
    [Tooltip("Concatenated RGB color data (6 character hex string) for each of the LEDs specified in the list when the show was started.")]
    public string ledData;

    [UIHint(UIHint.Variable)]
    [Tooltip("Concatenated brightness data (2 character hex string) for each of the lights in the list when the show was started.")]
    public string lightData;

    [UIHint(UIHint.Variable)]
    [Tooltip("Concatenated flasher pulse time data (2 character hex string) for each of the flashers in the list when the show was started.")]
    public string flasherData;

    [UIHint(UIHint.Variable)]
    [Tooltip("Concatenated GI brightness data (2 character hex string) for each of the GI strings in the list when the show was started.")]
    public string giData;

    /// <summary>
    /// Resets this instance to default values.
    /// </summary>
    public override void Reset()
    {
        name = null;
        ledData = "";
        lightData = "";
        flasherData = "";
        giData = "";
    }

    /// <summary>
    /// Called when the state becomes active. Sends the BCP External Show Update command to MPF (if there is at least some data to send).
    /// </summary>
    public override void OnEnter()
    {
        if (!String.IsNullOrEmpty(name) 
            && (!String.IsNullOrEmpty(ledData) || !String.IsNullOrEmpty(lightData) || !String.IsNullOrEmpty(flasherData) || !String.IsNullOrEmpty(giData)))
            BcpServer.Instance.Send(BcpMessage.ExternalShowFrameMessage(name, ledData, lightData, flasherData, giData));

        Finish();
    }

}

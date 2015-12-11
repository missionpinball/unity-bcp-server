using UnityEngine;
using System;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

/// <summary>
/// Custom PlayMaker action for MPF that sends a 'external_show_start' BCP command to MPF.
/// </summary>
[ActionCategory("BCP")]
[Tooltip("Sends 'external_show_start' BCP command to MPF. Can be used to send prioritized LED/light/flasher/GI data to the MPF light controller which allows shows to be generated and controlled in Unity.")]
public class SendBCPExternalShowStart : FsmStateAction
{
    [RequiredField]
    [UIHint(UIHint.Variable)]
    [Tooltip("The name of the external show to start on the MPF pinball controller.  The name will be used to reference the show in subsequent commands.")]
    public string name;

    [UIHint(UIHint.Variable)]
    [Tooltip("The priority of the show (defaults to 0 if not specified).")]
    public int priority;

    [UIHint(UIHint.Variable)]
    [Tooltip("A comma-separated list of the LED names that will be controlled by this external show.  The RGB color data (6 character hex string) for each of the LEDs in the list must be passed in each frame for this show.")]
    public string leds;

    [UIHint(UIHint.Variable)]
    [Tooltip("A comma-separated list of the light names that will be controlled by this external show.  The brightness data (2 character hex string) for each of the lights in the list must be passed in each frame for this show.")]
    public string lights;

    [UIHint(UIHint.Variable)]
    [Tooltip("A comma-separated list of the flasher names that will be controlled by this external show.  The pulse time data (milliseconds) for each of the flashers in the list must be passed in each frame for this show.")]
    public string flashers;

    [UIHint(UIHint.Variable)]
    [Tooltip("A comma-separated list of the GI names that will be controlled by this external show.  The brightness data (2 character hex string) for each of the GI strings in the list must be passed in each frame for this show.")]
    public string gis;

    /// <summary>
    /// Resets this instance to default values.
    /// </summary>
    public override void Reset()
    {
        name = null;
        priority = 0;
        leds = "";
        lights = "";
        flashers = "";
        gis = "";
    }

    /// <summary>
    /// Called when the state becomes active. Sends the BCP External Show Start command to MPF.
    /// </summary>
    public override void OnEnter()
    {
        if (!String.IsNullOrEmpty(name))
            BcpServer.Instance.Send(BcpMessage.ExternalShowStartMessage(name, priority, true, leds, lights, flashers, gis));

        Finish();
    }

}

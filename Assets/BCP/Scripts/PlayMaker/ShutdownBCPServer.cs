using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

/// <summary>
/// Custom PlayMaker action for MPF that shuts down the BCP server and quits the Unity application.
/// </summary>
[ActionCategory("BCP")]
[Tooltip("Shuts down the BCP server and quits the Unity application.")]
public class ShutdownBCPServer : FsmStateAction
{
    /// <summary>
    /// Called when the state becomes active.
    /// </summary>
    public override void OnEnter()
    {
        base.OnEnter();
        BcpServer.Instance.Close();

        // Shutdown the Unity application
#if UNITY_EDITOR
        if (EditorApplication.isPlaying)
            EditorApplication.isPlaying = false;
#endif
        Application.Quit();
        Finish();
    }
}

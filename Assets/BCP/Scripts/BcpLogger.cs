﻿using UnityEngine;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;

/// <summary>
/// This singleton class provides a timestamped log file that will only be available in debug builds.
/// </summary>
public class BcpLogger : MonoBehaviour {

    /// <summary>
    /// The log file name.
    /// </summary>
    public string LogFile = "log.txt";

    /// <summary>
    /// Flag indicating whether or not log messages should be echoed to the console.
    /// </summary>
    public bool EchoToConsole = true;

    /// <summary>
    /// Flag indicating whether or not each log entry should be stamped with the current date and time.
    /// </summary>
    public bool AddTimeStamp = true;

    /// <summary>
    /// The output stream.
    /// </summary>
    private StreamWriter OutputStream;

    /// <summary>
    /// The static singleton object instance.
    /// </summary>
    static BcpLogger Singleton = null;

    /// <summary>
    /// Gets the static singleton object instance.
    /// </summary>
    /// <value>
    /// The instance.
    /// </value>
    public static BcpLogger Instance
    {
        get { return Singleton; }
    }

    /// <summary>
    /// Initializes the singleton object and the output stream.
    /// </summary>
    void Awake()
    {

        if (Singleton != null)
        {
            UnityEngine.Debug.LogError("Multiple BcpLogger Singletons exist!");
            return;
        }

        Singleton = this;

//#if !FINAL
        // Open the log file to append the new log to it.
        OutputStream = new StreamWriter(LogFile, true);
//#endif
    }


    /// <summary>
    /// Called when the object is destroyed.  Closes the output stream.
    /// </summary>
    void OnDestroy()
    {
//#if !FINAL
        if (OutputStream != null)
        {
            OutputStream.Close();
            OutputStream = null;
        }
//#endif
    }

    /// <summary>
    /// Writes the specified message to the output stream and optionally the console.
    /// </summary>
    /// <param name="message">The message.</param>
    private void Write(string message)
    {
//#if !FINAL
        if (AddTimeStamp)
        {
            DateTime now = DateTime.Now;
            message = string.Format("[{0:H:mm:ss}] {1}", now, message);
        }

        if (OutputStream != null)
        {

            OutputStream.WriteLine(message);
            OutputStream.Flush();
        }

        if (EchoToConsole)
        {
            UnityEngine.Debug.Log(message);
        }
//#endif
    }

    /// <summary>
    /// Outputs the specified message to the log file.
    /// </summary>
    /// <param name="Message">The log message.</param>
  //  [Conditional("DEBUG"), Conditional("PROFILE")]
    public static void Trace(string Message)
    {
//#if !FINAL
        if (BcpLogger.Instance != null)
            BcpLogger.Instance.Write(Message);
        else
            // Fallback if the debugging system hasn't been initialized yet.
            UnityEngine.Debug.Log(Message);
//#endif
    }


}
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Text;

/// <summary>
/// A class that represents a single BCP message and an arbitrary number of parameter name/value pairs.
/// </summary>
public class BcpMessage
{
    /// <summary>
    /// An optional message identifier used to track individual messages and their replies (replies 
    /// will use the same message identifier as the original message).
    /// </summary>
    public string Id;

    /// <summary>
    /// The message command
    /// </summary>
    public string Command;

    /// <summary>
    /// The collection of message parameters (name/value pairs)
    /// </summary>
    public NameValueCollection Parameters;

    /// <summary>
    /// The raw message string
    /// </summary>
    public string RawMessage;

    /// <summary>
    /// Initializes a new instance of the <see cref="BcpMessage"/> class.
    /// </summary>
    public BcpMessage()
    {
        Id = String.Empty;
        Command = String.Empty;
        Parameters = new NameValueCollection();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BcpMessage" /> class with the given command name.
    /// </summary>
    /// <param name="command">The message command.</param>
    public BcpMessage(string command)
    {
        Id = String.Empty;
        Command = command;
        Parameters = new NameValueCollection();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BcpMessage"/> class with a single parameter (name/value pair).
    /// </summary>
    /// <param name="command">The message command.</param>
    /// <param name="parameterName">Name of the parameter.</param>
    /// <param name="parameterValue">The parameter value.</param>
    public BcpMessage(string command, string parameterName, string parameterValue)
    {
        Id = String.Empty;
        Command = command;
        Parameters = new NameValueCollection();

        Parameters.Add(parameterName, parameterValue);
    }

    /// <summary>
    /// Returns a <see cref="System.String" /> that represents this instance.
    /// </summary>
    /// <returns>
    /// A <see cref="System.String" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        // Simply return RawMessage if it exists
        if (!String.IsNullOrEmpty(RawMessage))
            return RawMessage;

        // Build parameter string
        StringBuilder parameters = new StringBuilder();
        if (!String.IsNullOrEmpty(Id))
            parameters.Append("?id=" + WWW.EscapeURL(Id));

        foreach (string name in Parameters)
        {
            string value = Parameters[name];
            string delimiter = (parameters.Length > 0) ? "&" : "?";

            if (String.IsNullOrEmpty(value))
                parameters.Append(delimiter + WWW.EscapeURL(name));
            else
                parameters.Append(delimiter + WWW.EscapeURL(name) + "=" + WWW.EscapeURL(value));
        }

        // Return full message string
        return WWW.EscapeURL(Command) + parameters.ToString() + "\n";
    }

    /// <summary>
    /// Creates a new hello message.
    /// </summary>
    /// <param name="version">The BCP protocol version implemented.</param>
    /// <param name="controllerName">The name of the controller sending the message.</param>
    /// <param name="controllerVersion">The version of the controller sending the message.</param>
    /// <returns></returns>
    public static BcpMessage HelloMessage(string version, string controllerName, string controllerVersion)
    {
        BcpMessage message = new BcpMessage("hello");
        message.Parameters.Add("version", version);
        message.Parameters.Add("controller_name", controllerName);
        message.Parameters.Add("controller_version", controllerVersion);
        return message;
    }

    /// <summary>
    /// Creates a new goodbye message.
    /// </summary>
    /// <returns></returns>
    public static BcpMessage GoodbyeMessage()
    {
        return new BcpMessage("goodbye");
    }

    /// <summary>
    /// Creates a new error message.
    /// </summary>
    /// <param name="message">The message string.</param>
    /// <param name="command">The command that failed (original raw message text).</param>
    /// <returns></returns>
    public static BcpMessage ErrorMessage(string message, string command)
    {
        return ErrorMessage(message, command, String.Empty);
    }

    /// <summary>
    /// Creates a new error message.
    /// </summary>
    /// <param name="message">The message string.</param>
    /// <param name="command">The command that failed (original raw message text).</param>
    /// <param name="id">The optional message identifier of the original message that failed.</param>
    /// <returns></returns>
    public static BcpMessage ErrorMessage(string message, string command, string id)
    {
        BcpMessage errorMessage = new BcpMessage("error");
        errorMessage.Parameters.Add("message", message);
        errorMessage.Parameters.Add("command", command);
        if (!String.IsNullOrEmpty(id))
            errorMessage.Parameters.Add("id", id);
        return errorMessage;
    }

    /// <summary>
    /// Creates a new message to request monitoring of machine variables.
    /// </summary>
    /// <returns></returns>
    public static BcpMessage MonitorMachineVarsMessage()
    {
        return new BcpMessage("monitor_start", "category", "machine_vars");
    }

    /// <summary>
    /// Creates a new message to request monitoring of player variables.
    /// </summary>
    /// <returns></returns>
    public static BcpMessage MonitorPlayerVarsMessage()
    {
        return new BcpMessage("monitor_start", "category", "player_vars");

    }

    /// <summary>
    /// Creates a new message to request monitoring of switch messages.
    /// </summary>
    /// <returns></returns>
    public static BcpMessage MonitorSwitchMessages()
    {
        return new BcpMessage("monitor_start", "category", "switches");
    }

    /// <summary>
    /// Creates a new message to request monitoring of mode messages.
    /// </summary>
    /// <returns></returns>
    public static BcpMessage MonitorModeMessages()
    {
        return new BcpMessage("monitor_start", "category", "modes");
    }

    /// <summary>
    /// Creates a new message to request monitoring of core messages.
    /// </summary>
    /// <returns></returns>
    public static BcpMessage MonitorCoreMessages()
    {
        return new BcpMessage("monitor_start", "category", "core_events");
    }

    /// <summary>
    /// Creates a new message to register the specified trigger event.
    /// </summary>
    /// <param name="eventName">Name of the event.</param>
    /// <returns></returns>
    public static BcpMessage RegisterTriggerMessage(string eventName)
    {
        return new BcpMessage("register_trigger", "event", eventName);
    }

    /// <summary>
    /// Creates a new switch message.
    /// </summary>
    /// <param name="name">The name of the switch.</param>
    /// <param name="state">The switch state ("1" for active, "0" for inactive).</param>
    /// <returns></returns>
    public static BcpMessage SwitchMessage(string name, string state)
    {
        BcpMessage message = new BcpMessage("switch");
        message.Parameters.Add("name", name);
        message.Parameters.Add("state", state);
        return message;
    }

    /// <summary>
    /// Creates a new trigger message.
    /// </summary>
    /// <param name="name">The name of the trigger.</param>
    /// <returns></returns>
    public static BcpMessage TriggerMessage(string name)
    {
        return new BcpMessage("trigger", "name", name);
    }

}

/// <summary>
/// Custom exception class for BcpMessage-related errors.
/// </summary>
public class BcpMessageException : Exception
{
    /// <summary>
    /// The BCP message
    /// </summary>
    private BcpMessage _bcpMessage;

    /// <summary>
    /// Initializes a new instance of the <see cref="BcpMessageException"/> class.
    /// </summary>
    public BcpMessageException()
    {
        _bcpMessage = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BcpMessageException"/> class.
    /// </summary>
    /// <param name="message">The message.</param>
    public BcpMessageException(string message)
        :base(message)
    {
        _bcpMessage = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BcpMessageException"/> class.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="bcpMessage">The BCP message.</param>
    public BcpMessageException(string message, BcpMessage bcpMessage)
        : base(message)
    {
        _bcpMessage = bcpMessage;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BcpMessageException"/> class.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The inner exception.</param>
    public BcpMessageException(string message, Exception innerException)
        :base(message, innerException)
    {
        _bcpMessage = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BcpMessageException"/> class.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="bcpMessage">The BCP message.</param>
    /// <param name="innerException">The inner exception.</param>
    public BcpMessageException(string message, BcpMessage bcpMessage, Exception innerException)
        : base(message, innerException)
    {
        _bcpMessage = bcpMessage;
    }

    /// <summary>
    /// Returns a <see cref="System.String" /> that represents this instance.
    /// </summary>
    /// <returns>
    /// A <see cref="System.String" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        StringBuilder description = new StringBuilder();
        description.AppendFormat("{0}: {1}", this.GetType().Name, this.Message);

        if (_bcpMessage != null)
            description.Append(" Raw BcpMessage: " + _bcpMessage.ToString());

        if (this.InnerException != null)
        {
            description.AppendFormat(" ---> {0}", this.InnerException);
            description.AppendFormat(
                "{0}   --- End of inner exception stack trace ---{0}",
                Environment.NewLine);
        }

        description.Append(this.StackTrace);

        return description.ToString();
    }
}
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using BCP.SimpleJSON;
using UnityEngine.Networking;

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
    public JSONObject Parameters;

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
        Parameters = new JSONObject();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BcpMessage" /> class with the given command name.
    /// </summary>
    /// <param name="command">The message command.</param>
    public BcpMessage(string command)
    {
        Id = String.Empty;
        Command = command;
        Parameters = new JSONObject();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BcpMessage"/> class with a single parameter (name/value pair).
    /// </summary>
    /// <param name="command">The message command.</param>
    /// <param name="parameterName">Name of the parameter.</param>
    /// <param name="parameterValue">The parameter value.</param>
    public BcpMessage(string command, string parameterName, JSONNode parameterValue)
    {
        Id = String.Empty;
        Command = command;
        Parameters = new JSONObject();

        Parameters.Add(parameterName, parameterValue);
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
        Parameters = new JSONObject();

        Parameters.Add(parameterName, new JSONString(parameterValue));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BcpMessage"/> class with a single parameter (name/value pair).
    /// </summary>
    /// <param name="command">The message command.</param>
    /// <param name="parameterName">Name of the parameter.</param>
    /// <param name="parameterValue">The parameter value.</param>
    public BcpMessage(string command, string parameterName, int parameterValue)
    {
        Id = String.Empty;
        Command = command;
        Parameters = new JSONObject();

        Parameters.Add(parameterName, new JSONNumber(parameterValue));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BcpMessage"/> class with a single parameter (name/value pair).
    /// </summary>
    /// <param name="command">The message command.</param>
    /// <param name="parameterName">Name of the parameter.</param>
    /// <param name="parameterValue">The parameter value.</param>
    public BcpMessage(string command, string parameterName, float parameterValue)
    {
        Id = String.Empty;
        Command = command;
        Parameters = new JSONObject();

        Parameters.Add(parameterName, new JSONNumber(parameterValue));
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
        foreach (KeyValuePair<string, JSONNode> param in this.Parameters)
        {
            if (parameters.Length > 0)
                parameters.Append("&");
            else
                parameters.Append("?");

            parameters.Append(UnityWebRequest.EscapeURL(param.Key));

            string value = BcpMessage.ConvertToBcpParameterString(param.Value);
            if (!String.IsNullOrEmpty(value))
                parameters.Append("=" + UnityWebRequest.EscapeURL(value));
        }

        // Apply message termination character (line feed)
        parameters.Append("\n");
        return UnityWebRequest.EscapeURL(this.Command) + parameters.ToString();
    }

    /// <summary>
    /// Converts a BCP message to a socket packet to be sent to a pinball controller.
    /// </summary>
    /// <param name="message">The BCP message.</param>
    /// <param name="packet">The generated packet (by reference).</param>
    /// <returns>The length of the generated packet</returns>
	public int ToPacket(out byte[] packet)
    {
        packet = Encoding.UTF8.GetBytes(this.ToString());
        return packet.Length;
    }

    /// <summary>
    /// Converts a parameter value to a BCP parameter string.
    /// </summary>
    /// <param name="node">The parameter value node.</param>
    /// <returns></returns>
    public static string ConvertToBcpParameterString(JSONNode node)
    {
        if (node == null)
            return null;

        switch (node.Tag)
        {
            case JSONNodeType.String:
                return node.Value;

            case JSONNodeType.Boolean:
                return node.AsBool ? "bool:True" : "bool:False";

            case JSONNodeType.NullValue:
                return "NoneType:";

            case JSONNodeType.Number:
                if ((float)node.AsInt == node.AsFloat)
                    return "int:" + node.Value;
                else
                    return "float:" + node.Value;
        }

        return null;
    }

    /// <summary>
    /// Converts a BCP parameter string to a node.
    /// </summary>
    /// <param name="value">The parameter string value.</param>
    /// <returns></returns>
    public static JSONNode ConvertBcpParameterStringToNode(string value)
    {
        if (value.ToLower().StartsWith("bool:"))
            return new JSONBool(value.Substring(5));

        else if (value.ToLower().StartsWith("nonetype:"))
            return JSONNull.CreateOrGet();

        else if (value.ToLower().StartsWith("int:"))
            return new JSONNumber(value.Substring(4));

        else if (value.ToLower().StartsWith("float:"))
            return new JSONNumber(value.Substring(6));

        else
            return new JSONString(value);

    }

    /// <summary>
    /// Converts a message string (in URL text format) to a BCP message.
    /// </summary>
    /// <param name="rawMessage">The raw message buffer string.</param>
    /// <returns>
    ///   <see cref="BcpMessage" />
    /// </returns>
    public static BcpMessage CreateFromRawMessage(string rawMessage)
    {
        BcpMessage bcpMessage = new BcpMessage();

        // Remove line feed and carriage return characters
        rawMessage = rawMessage.Replace("\n", String.Empty);
        rawMessage = rawMessage.Replace("\r", String.Empty);

        bcpMessage.RawMessage = rawMessage;
        rawMessage = UnityWebRequest.UnEscapeURL(rawMessage);

        // Message text occurs before the question mark (?)
        if (rawMessage.Contains("?"))
        {
            int messageDelimiterPos = rawMessage.IndexOf('?');

            // BCP commands are not case sensitive so we convert to lower case
            // BCP parameter names are not case sensitive, but parameter values are
            bcpMessage.Command = rawMessage.Substring(0, messageDelimiterPos).Trim().ToLower();
            rawMessage = rawMessage.Substring(messageDelimiterPos + 1);

            foreach (string parameter in Regex.Split(rawMessage, "&"))
            {
                string[] parameterValuePair = Regex.Split(parameter, "=");
                string name = parameterValuePair[0].Trim().ToLower();

                if (parameterValuePair.Length == 2)
                {
                    string value = parameterValuePair[1].Trim();

                    // Special handling for "json" parameter
                    if (name == "json")
                        try
                        {
                            JSONNode node = JSONNode.Parse(value);
                            bcpMessage.Parameters.Add(name, node);
                        }
                        catch
                        {
                            bcpMessage.Parameters.Add(name, JSONNull.CreateOrGet());
                        }
                    else
                    {
                        bcpMessage.Parameters.Add(name, BcpMessage.ConvertBcpParameterStringToNode(value));
                    }
                        
                }
                else
                {
                    // only one key with no value specified in query string
                    bcpMessage.Parameters.Add(name, JSONNull.CreateOrGet());
                }
            }

            // Add JSON-encoded parameters to the message parameters
            if (bcpMessage.Parameters["json"].IsObject)
            {
                foreach (KeyValuePair<string, JSONNode> parameter in bcpMessage.Parameters["json"].AsObject)
                    bcpMessage.Parameters.Add(parameter.Key, parameter.Value);
            }

        }
        else
        {
            // No parameters in the message, the entire message contains just the message text
            bcpMessage.Command = rawMessage.Trim();
        }

        return bcpMessage;
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
        message.Parameters.Add("version", new JSONString(version));
        message.Parameters.Add("controller_name", new JSONString(controllerName));
        message.Parameters.Add("controller_version", new JSONString(controllerVersion));
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
    /// Creates a new reset complete message.
    /// </summary>
    /// <returns></returns>
    public static BcpMessage ResetCompleteMessage()
    {
        return new BcpMessage("reset_complete");
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
        errorMessage.Parameters.Add("message", new JSONString(message));
        errorMessage.Parameters.Add("command", new JSONString(command));
        if (!String.IsNullOrEmpty(id))
            errorMessage.Parameters.Add("id", new JSONString(id));
        return errorMessage;
    }

    /// <summary>
    /// Creates a new set machine variable message.
    /// </summary>
    /// <param name="name">The machine variable name.</param>
    /// <param name="value">The machine variable value.</param>
    /// <returns></returns>
    public static BcpMessage SetMachineVariableMessage(string name, string value)
    {
        BcpMessage setMachineVarMessage = new BcpMessage("set_machine_var");
        setMachineVarMessage.Parameters.Add("name", new JSONString(name));
        setMachineVarMessage.Parameters.Add("value", new JSONString(value));
        return setMachineVarMessage;
    }

    /// <summary>
    /// Creates a new set machine variable message.
    /// </summary>
    /// <param name="name">The machine variable name.</param>
    /// <param name="value">The machine variable value.</param>
    /// <returns></returns>
    public static BcpMessage SetMachineVariableMessage(string name, int value)
    {
        BcpMessage setMachineVarMessage = new BcpMessage("set_machine_var");
        setMachineVarMessage.Parameters.Add("name", new JSONString(name));
        setMachineVarMessage.Parameters.Add("value", new JSONNumber(value));
        return setMachineVarMessage;
    }

    /// <summary>
    /// Creates a new set machine variable message.
    /// </summary>
    /// <param name="name">The machine variable name.</param>
    /// <param name="value">The machine variable value.</param>
    /// <returns></returns>
    public static BcpMessage SetMachineVariableMessage(string name, float value)
    {
        BcpMessage setMachineVarMessage = new BcpMessage("set_machine_var");
        setMachineVarMessage.Parameters.Add("name", new JSONString(name));
        setMachineVarMessage.Parameters.Add("value", new JSONNumber(value));
        return setMachineVarMessage;
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
    public static BcpMessage SwitchMessage(string name, int state)
    {
        BcpMessage message = new BcpMessage("switch");
        message.Parameters.Add("name", new JSONString(name));
        message.Parameters.Add("state", new JSONNumber(state));
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


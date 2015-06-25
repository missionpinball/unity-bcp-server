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
    /// <returns></returns>
    public static BcpMessage HelloMessage(string version)
    {
        BcpMessage message = new BcpMessage();
        message.Command = "hello";
        message.Parameters.Add("version", version);
        return message;
    }

	/// <summary>
	/// Creates a new goodbye message.
	/// </summary>
	/// <returns></returns>
	public static BcpMessage GoodbyeMessage()
	{
		BcpMessage message = new BcpMessage();
		message.Command = "goodbye";
		return message;
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
        BcpMessage errorMessage = new BcpMessage();
        errorMessage.Command = "error";
        errorMessage.Parameters.Add("message", message);
        errorMessage.Parameters.Add("command", command);
        if (!String.IsNullOrEmpty(id))
            errorMessage.Parameters.Add("id", id);
        return errorMessage;
    }

    /// <summary>
    /// Creates a new switch message.
    /// </summary>
    /// <param name="name">The name of the switch.</param>
    /// <param name="state">The switch state ("1" for active, "0" for inactive).</param>
    /// <returns></returns>
    public static BcpMessage SwitchMessage(string name, string state)
    {
        BcpMessage message = new BcpMessage();
        message.Command = "switch";
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
        BcpMessage message = new BcpMessage();
        message.Command = "trigger";
        message.Parameters.Add("name", name);
        return message;
    }



}



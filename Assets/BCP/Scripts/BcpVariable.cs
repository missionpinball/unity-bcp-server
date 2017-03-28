using System;
using System.Collections;

/// <summary>
/// Class to store variables transmitted via BCP.
/// </summary>
public class BcpVariable : Object
{
    /// <summary>
    /// The type of variable stored in the object.
    /// </summary>
    public enum VariableType
    {
        String,
        Int,
        Float,
        Boolean,
        NoneType
    }

    /// <summary>
    /// Gets or sets the variable name.
    /// </summary>
    /// <value>
    /// The name.
    /// </value>
    public string Name { get; set; }

    /// <summary>
    /// Gets the variable type.
    /// </summary>
    /// <value>
    /// The variable type.
    /// </value>
    public VariableType Type { get { return _type; } }

    /// <summary>
    /// The variable type (internal)
    /// </summary>
    protected VariableType _type;

    /// <summary>
    /// The variable value (stored as a string)
    /// </summary>
    protected string _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="BcpVariable"/> class.
    /// </summary>
    /// <param name="value">The value (as string).</param>
    public BcpVariable(string value)
    {
        AssignFromBcpParameterString(value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BcpVariable"/> class.
    /// </summary>
    /// <param name="value">The variable value (as a string).</param>
    /// <param name="type">The variable type.</param>
    public BcpVariable(string value, VariableType type)
    {
        _value = value.Trim();
        _type = type;
    }

    /// <summary>
    /// Assigns the parameter value and type from BCP parameter string.
    /// </summary>
    /// <param name="value">The value.</param>
    public void AssignFromBcpParameterString(string value)
    {
        value = value.Trim();

        if (value.StartsWith("int:", StringComparison.InvariantCultureIgnoreCase))
        {
            _value = value.Substring(4);
            _type = VariableType.Int;
        }
        else if (value.StartsWith("float:", StringComparison.InvariantCultureIgnoreCase))
        {
            _value = value.Substring(6);
            _type = VariableType.Float;
        }
        else if (value.StartsWith("bool:", StringComparison.InvariantCultureIgnoreCase))
        {
            _value = value.Substring(5).ToLowerInvariant();
            _type = VariableType.Boolean;
        }
        else if (value.StartsWith("NoneType:", StringComparison.InvariantCultureIgnoreCase))
        {
            _value = string.Empty;
            _type = VariableType.NoneType;
        }
        else
        {
            _value = value;
            _type = VariableType.String;
        }
    }

    /// <summary>
    /// Returns a <see cref="System.String" /> that represents this instance.
    /// </summary>
    /// <returns>
    /// A <see cref="System.String" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return _value;
    }

    /// <summary>
    /// Converts the string representation of a number to an integer.
    /// </summary>
    /// <returns></returns>
    public int ToInt()
    {
        return int.Parse(_value);
    }

    /// <summary>
    /// Converts the string representation of a number to a float.
    /// </summary>
    /// <returns></returns>
    public float ToFloat()
    {
        return float.Parse(_value);
    }

    /// <summary>
    /// Converts the string representation of a number to a boolean.
    /// </summary>
    /// <returns></returns>
    public bool ToBoolean()
    {
        return bool.Parse(_value);
    }

    /// <summary>
    /// Performs an implicit conversion from <see cref="BcpVariable"/> to <see cref="System.Int32"/>.
    /// </summary>
    /// <param name="variable">The BCP variable.</param>
    /// <returns>
    /// The result of the conversion.
    /// </returns>
    public static implicit operator int(BcpVariable variable)
    {
        return variable.ToInt();
    }

    /// <summary>
    /// Performs an implicit conversion from <see cref="BcpVariable"/> to <see cref="System.Single"/>.
    /// </summary>
    /// <param name="variable">The BCP variable.</param>
    /// <returns>
    /// The result of the conversion.
    /// </returns>
    public static implicit operator float(BcpVariable variable)
    {
        return variable.ToFloat();
    }

    /// <summary>
    /// Performs an implicit conversion from <see cref="BcpVariable"/> to <see cref="System.Boolean"/>.
    /// </summary>
    /// <param name="variable">The variable.</param>
    /// <returns>
    /// The result of the conversion.
    /// </returns>
    public static implicit operator bool(BcpVariable variable)
    {
        return variable.ToBoolean();
    }

    /// <summary>
    /// Performs an implicit conversion from <see cref="System.String"/> to <see cref="BcpVariable"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>
    /// The result of the conversion.
    /// </returns>
    public static implicit operator BcpVariable(string value)
    {
        return new BcpVariable(value);
    }

    /// <summary>
    /// Performs an implicit conversion from <see cref="System.Int32"/> to <see cref="BcpVariable"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>
    /// The result of the conversion.
    /// </returns>
    public static implicit operator BcpVariable(int value)
    {
        return new BcpVariable(value.ToString(), VariableType.Int);
    }

    /// <summary>
    /// Performs an implicit conversion from <see cref="System.Single"/> to <see cref="BcpVariable"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>
    /// The result of the conversion.
    /// </returns>
    public static implicit operator BcpVariable(float value)
    {
        return new BcpVariable(value.ToString(), VariableType.Float);
    }

    /// <summary>
    /// Performs an implicit conversion from <see cref="System.Boolean"/> to <see cref="BcpVariable"/>.
    /// </summary>
    /// <param name="value">if set to <c>true</c> [value].</param>
    /// <returns>
    /// The result of the conversion.
    /// </returns>
    public static implicit operator BcpVariable(bool value)
    {
        return new BcpVariable(value.ToString(), VariableType.Boolean);
    }

}


/// <summary>
/// A dictionary of <see cref="BcpVariable"/> objects.
/// </summary>
public class BcpVariableDictionary : DictionaryBase
{
    /// <summary>
    /// Gets or sets the <see cref="BcpVariable"/> with the specified key.
    /// </summary>
    /// <value>
    /// The <see cref="BcpVariable"/>.
    /// </value>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    public BcpVariable this[string key]
    {
        get { return (BcpVariable)this.Dictionary[key]; }

        set { this.Dictionary[key] = value; }
    }

    /// <summary>
    /// Adds the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="variable">The variable.</param>
    public void Add(string key, BcpVariable variable)
    {
        this.Dictionary.Add(key, variable);
    }

    /// <summary>
    /// Determines whether the dictionary contains the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    public bool Contains(string key)
    {
        return this.Dictionary.Contains(key);
    }

    /// <summary>
    /// Removes the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    public void Remove(string key)
    {
        this.Dictionary.Remove(key);
    }

    /// <summary>
    /// Gets the dictionary keys.
    /// </summary>
    /// <value>
    /// The keys.
    /// </value>
    public ICollection Keys
    {
        get { return this.Dictionary.Keys; }
    }
}

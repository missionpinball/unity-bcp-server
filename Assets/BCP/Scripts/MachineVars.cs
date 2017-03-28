using System.Collections;

/// <summary>
/// Manages machine variables from the pin controller.
/// </summary>
public class MachineVars
{
    private BcpVariableDictionary _variables;

    /// <summary>
    /// Initializes a new instance of the <see cref="MachineVars"/> class.
    /// </summary>
    public MachineVars()
    {
        _variables = new BcpVariableDictionary();
    }

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
        get { return (BcpVariable)this._variables[key]; }

        set { this._variables[key] = value; }
    }

    /// <summary>
    /// Removes all variables from the machine variables store.
    /// </summary>
    public void Clear()
    {
        this._variables.Clear();
    }

    /// <summary>
    /// Adds the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="variable">The variable.</param>
    public void Add(string key, BcpVariable variable)
    {
        this._variables.Add(key, variable);
    }

    /// <summary>
    /// Adds the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    public void Add(string key, string value)
    {
        if (this._variables.Contains(key))
            this._variables[key].AssignFromBcpParameterString(value);
        else
            this._variables.Add(key, new BcpVariable(value));
    }

    /// <summary>
    /// Determines whether the dictionary contains the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    public bool Contains(string key)
    {
        return this._variables.Contains(key);
    }

    /// <summary>
    /// Removes the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    public void Remove(string key)
    {
        this._variables.Remove(key);
    }

    /// <summary>
    /// Gets the dictionary keys.
    /// </summary>
    /// <value>
    /// The keys.
    /// </value>
    public ICollection Keys
    {
        get { return this._variables.Keys; }
    }

}

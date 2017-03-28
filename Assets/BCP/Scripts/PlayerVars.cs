using System.Collections;

/// <summary>
/// Manages player variables from the pin controller.
/// </summary>
public class PlayerVars
{
    private BcpVariableDictionary _variables;

    /// <summary>
    /// Gets the static singleton object instance.
    /// </summary>
    /// <value>
    /// The instance.
    /// </value>
	public static PlayerVars Instance { get; private set; }

    /// <summary>
    /// Called when the script instance is being loaded.
    /// </summary>
    public void Awake()
    {
        // Save a reference to the BcpServer component as our singleton instance
        Instance = this;
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
    /// Adds the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="variable">The variable.</param>
    public void Add(string key, BcpVariable variable)
    {
        this._variables.Add(key, variable);
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

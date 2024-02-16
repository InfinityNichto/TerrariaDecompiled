namespace System.Diagnostics.Contracts;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
[Conditional("CONTRACTS_FULL")]
public sealed class ContractOptionAttribute : Attribute
{
	private readonly string _category;

	private readonly string _setting;

	private readonly bool _enabled;

	private readonly string _value;

	public string Category => _category;

	public string Setting => _setting;

	public bool Enabled => _enabled;

	public string? Value => _value;

	public ContractOptionAttribute(string category, string setting, bool enabled)
	{
		_category = category;
		_setting = setting;
		_enabled = enabled;
	}

	public ContractOptionAttribute(string category, string setting, string value)
	{
		_category = category;
		_setting = setting;
		_value = value;
	}
}

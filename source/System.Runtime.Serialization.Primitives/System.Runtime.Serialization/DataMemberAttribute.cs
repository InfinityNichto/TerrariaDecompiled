namespace System.Runtime.Serialization;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class DataMemberAttribute : Attribute
{
	private string _name;

	private bool _isNameSetExplicitly;

	private int _order = -1;

	public string? Name
	{
		get
		{
			return _name;
		}
		set
		{
			_name = value;
			_isNameSetExplicitly = true;
		}
	}

	public bool IsNameSetExplicitly => _isNameSetExplicitly;

	public int Order
	{
		get
		{
			return _order;
		}
		set
		{
			if (value < 0)
			{
				throw new InvalidDataContractException(System.SR.OrderCannotBeNegative);
			}
			_order = value;
		}
	}

	public bool IsRequired { get; set; }

	public bool EmitDefaultValue { get; set; } = true;

}

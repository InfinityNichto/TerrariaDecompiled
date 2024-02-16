using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel.DataAnnotations.Schema;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class ColumnAttribute : Attribute
{
	private int _order = -1;

	private string _typeName;

	public string? Name { get; }

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
				throw new ArgumentOutOfRangeException("value");
			}
			_order = value;
		}
	}

	public string? TypeName
	{
		get
		{
			return _typeName;
		}
		[param: DisallowNull]
		set
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				throw new ArgumentException(System.SR.Format(System.SR.ArgumentIsNullOrWhitespace, "value"), "value");
			}
			_typeName = value;
		}
	}

	public ColumnAttribute()
	{
	}

	public ColumnAttribute(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentException(System.SR.Format(System.SR.ArgumentIsNullOrWhitespace, "name"), "name");
		}
		Name = name;
	}
}

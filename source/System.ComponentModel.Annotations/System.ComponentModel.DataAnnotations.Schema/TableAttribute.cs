using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel.DataAnnotations.Schema;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class TableAttribute : Attribute
{
	private string _schema;

	public string Name { get; }

	public string? Schema
	{
		get
		{
			return _schema;
		}
		[param: DisallowNull]
		set
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				throw new ArgumentException(System.SR.Format(System.SR.ArgumentIsNullOrWhitespace, "value"), "value");
			}
			_schema = value;
		}
	}

	public TableAttribute(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentException(System.SR.Format(System.SR.ArgumentIsNullOrWhitespace, "name"), "name");
		}
		Name = name;
	}
}

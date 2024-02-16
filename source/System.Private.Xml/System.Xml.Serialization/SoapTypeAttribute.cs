using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Serialization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface)]
public class SoapTypeAttribute : Attribute
{
	private string _ns;

	private string _typeName;

	private bool _includeInSchema = true;

	public bool IncludeInSchema
	{
		get
		{
			return _includeInSchema;
		}
		set
		{
			_includeInSchema = value;
		}
	}

	public string TypeName
	{
		get
		{
			if (_typeName != null)
			{
				return _typeName;
			}
			return string.Empty;
		}
		[param: AllowNull]
		set
		{
			_typeName = value;
		}
	}

	public string? Namespace
	{
		get
		{
			return _ns;
		}
		set
		{
			_ns = value;
		}
	}

	public SoapTypeAttribute()
	{
	}

	public SoapTypeAttribute(string? typeName)
	{
		_typeName = typeName;
	}

	public SoapTypeAttribute(string? typeName, string? ns)
	{
		_typeName = typeName;
		_ns = ns;
	}
}

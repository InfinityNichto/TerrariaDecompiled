using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Serialization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface)]
public class XmlTypeAttribute : Attribute
{
	private bool _includeInSchema = true;

	private bool _anonymousType;

	private string _ns;

	private string _typeName;

	public bool AnonymousType
	{
		get
		{
			return _anonymousType;
		}
		set
		{
			_anonymousType = value;
		}
	}

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

	public XmlTypeAttribute()
	{
	}

	public XmlTypeAttribute(string? typeName)
	{
		_typeName = typeName;
	}
}

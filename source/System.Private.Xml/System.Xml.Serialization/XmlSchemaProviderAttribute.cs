namespace System.Xml.Serialization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
public sealed class XmlSchemaProviderAttribute : Attribute
{
	private readonly string _methodName;

	private bool _any;

	public string? MethodName => _methodName;

	public bool IsAny
	{
		get
		{
			return _any;
		}
		set
		{
			_any = value;
		}
	}

	public XmlSchemaProviderAttribute(string? methodName)
	{
		_methodName = methodName;
	}
}

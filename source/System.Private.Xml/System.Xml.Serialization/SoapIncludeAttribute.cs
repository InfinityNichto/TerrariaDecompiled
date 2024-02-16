namespace System.Xml.Serialization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Interface, AllowMultiple = true)]
public class SoapIncludeAttribute : Attribute
{
	private Type _type;

	public Type Type
	{
		get
		{
			return _type;
		}
		set
		{
			_type = value;
		}
	}

	public SoapIncludeAttribute(Type type)
	{
		_type = type;
	}
}

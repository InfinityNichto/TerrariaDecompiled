namespace System.Xml.Serialization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface, AllowMultiple = false)]
public sealed class XmlSerializerAssemblyAttribute : Attribute
{
	private string _assemblyName;

	private string _codeBase;

	public string? CodeBase
	{
		get
		{
			return _codeBase;
		}
		set
		{
			_codeBase = value;
		}
	}

	public string? AssemblyName
	{
		get
		{
			return _assemblyName;
		}
		set
		{
			_assemblyName = value;
		}
	}

	public XmlSerializerAssemblyAttribute()
		: this(null, null)
	{
	}

	public XmlSerializerAssemblyAttribute(string? assemblyName)
		: this(assemblyName, null)
	{
	}

	public XmlSerializerAssemblyAttribute(string? assemblyName, string? codeBase)
	{
		_assemblyName = assemblyName;
		_codeBase = codeBase;
	}
}

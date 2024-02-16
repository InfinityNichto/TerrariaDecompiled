namespace System.Runtime.Serialization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, Inherited = false, AllowMultiple = false)]
public sealed class DataContractAttribute : Attribute
{
	private string _name;

	private string _ns;

	private bool _isNameSetExplicitly;

	private bool _isNamespaceSetExplicitly;

	private bool _isReference;

	private bool _isReferenceSetExplicitly;

	public bool IsReference
	{
		get
		{
			return _isReference;
		}
		set
		{
			_isReference = value;
			_isReferenceSetExplicitly = true;
		}
	}

	public bool IsReferenceSetExplicitly => _isReferenceSetExplicitly;

	public string? Namespace
	{
		get
		{
			return _ns;
		}
		set
		{
			_ns = value;
			_isNamespaceSetExplicitly = true;
		}
	}

	public bool IsNamespaceSetExplicitly => _isNamespaceSetExplicitly;

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
}

namespace System.Xml.Serialization;

public abstract class XmlMapping
{
	private readonly TypeScope _scope;

	private bool _generateSerializer;

	private bool _isSoap;

	private readonly ElementAccessor _accessor;

	private string _key;

	private readonly bool _shallow;

	private readonly XmlMappingAccess _access;

	internal ElementAccessor Accessor => _accessor;

	internal TypeScope? Scope => _scope;

	public string ElementName => System.Xml.Serialization.Accessor.UnescapeName(Accessor.Name);

	public string XsdElementName => Accessor.Name;

	public string? Namespace => _accessor.Namespace;

	internal bool GenerateSerializer
	{
		get
		{
			return _generateSerializer;
		}
		set
		{
			_generateSerializer = value;
		}
	}

	internal bool IsReadable => (_access & XmlMappingAccess.Read) != 0;

	internal bool IsWriteable => (_access & XmlMappingAccess.Write) != 0;

	internal bool IsSoap
	{
		get
		{
			return _isSoap;
		}
		set
		{
			_isSoap = value;
		}
	}

	internal string? Key => _key;

	internal XmlMapping(TypeScope scope, ElementAccessor accessor)
		: this(scope, accessor, XmlMappingAccess.Read | XmlMappingAccess.Write)
	{
	}

	internal XmlMapping(TypeScope scope, ElementAccessor accessor, XmlMappingAccess access)
	{
		_scope = scope;
		_accessor = accessor;
		_access = access;
		_shallow = scope == null;
	}

	public void SetKey(string? key)
	{
		SetKeyInternal(key);
	}

	internal void SetKeyInternal(string key)
	{
		_key = key;
	}

	internal static string GenerateKey(Type type, XmlRootAttribute root, string ns)
	{
		if (root == null)
		{
			root = (XmlRootAttribute)XmlAttributes.GetAttr(type, typeof(XmlRootAttribute));
		}
		return type.FullName + ":" + ((root == null) ? string.Empty : root.GetKey()) + ":" + ((ns == null) ? string.Empty : ns);
	}

	internal void CheckShallow()
	{
		if (_shallow)
		{
			throw new InvalidOperationException(System.SR.XmlMelformMapping);
		}
	}

	internal static bool IsShallow(XmlMapping[] mappings)
	{
		for (int i = 0; i < mappings.Length; i++)
		{
			if (mappings[i] == null || mappings[i]._shallow)
			{
				return true;
			}
		}
		return false;
	}
}

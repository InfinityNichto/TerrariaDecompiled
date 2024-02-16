using System.Diagnostics.CodeAnalysis;

namespace System.Xml;

public class XmlQualifiedName
{
	private string _name;

	private string _ns;

	private int _hash;

	public static readonly XmlQualifiedName Empty = new XmlQualifiedName(string.Empty);

	public string Namespace => _ns;

	public string Name => _name;

	public bool IsEmpty
	{
		get
		{
			if (Name.Length == 0)
			{
				return Namespace.Length == 0;
			}
			return false;
		}
	}

	public XmlQualifiedName()
		: this(string.Empty, string.Empty)
	{
	}

	public XmlQualifiedName(string? name)
		: this(name, string.Empty)
	{
	}

	public XmlQualifiedName(string? name, string? ns)
	{
		_ns = ns ?? string.Empty;
		_name = name ?? string.Empty;
	}

	public override int GetHashCode()
	{
		if (_hash == 0)
		{
			_hash = Name.GetHashCode();
		}
		return _hash;
	}

	public override string ToString()
	{
		if (Namespace.Length != 0)
		{
			return Namespace + ":" + Name;
		}
		return Name;
	}

	public override bool Equals([NotNullWhen(true)] object? other)
	{
		if (this == other)
		{
			return true;
		}
		XmlQualifiedName xmlQualifiedName = other as XmlQualifiedName;
		if (xmlQualifiedName != null)
		{
			if (Name == xmlQualifiedName.Name)
			{
				return Namespace == xmlQualifiedName.Namespace;
			}
			return false;
		}
		return false;
	}

	public static bool operator ==(XmlQualifiedName? a, XmlQualifiedName? b)
	{
		if ((object)a == b)
		{
			return true;
		}
		if ((object)a == null || (object)b == null)
		{
			return false;
		}
		if (a.Name == b.Name)
		{
			return a.Namespace == b.Namespace;
		}
		return false;
	}

	public static bool operator !=(XmlQualifiedName? a, XmlQualifiedName? b)
	{
		return !(a == b);
	}

	public static string ToString(string name, string ns)
	{
		if (ns != null && ns.Length != 0)
		{
			return ns + ":" + name;
		}
		return name;
	}

	internal void Init(string name, string ns)
	{
		_name = name ?? string.Empty;
		_ns = ns ?? string.Empty;
		_hash = 0;
	}

	internal void SetNamespace(string ns)
	{
		_ns = ns ?? string.Empty;
	}

	internal void Verify()
	{
		XmlConvert.VerifyNCName(_name);
		if (_ns.Length != 0)
		{
			XmlConvert.ToUri(_ns);
		}
	}

	internal void Atomize(XmlNameTable nameTable)
	{
		_name = nameTable.Add(_name);
		_ns = nameTable.Add(_ns);
	}

	internal static XmlQualifiedName Parse(string s, IXmlNamespaceResolver nsmgr, out string prefix)
	{
		ValidateNames.ParseQNameThrow(s, out prefix, out var localName);
		string text = nsmgr.LookupNamespace(prefix);
		if (text == null)
		{
			if (prefix.Length != 0)
			{
				throw new XmlException(System.SR.Xml_UnknownNs, prefix);
			}
			text = string.Empty;
		}
		return new XmlQualifiedName(localName, text);
	}

	internal XmlQualifiedName Clone()
	{
		return (XmlQualifiedName)MemberwiseClone();
	}
}

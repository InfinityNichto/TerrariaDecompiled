namespace System.Xml.Xsl;

internal sealed class XmlQualifiedNameTest : XmlQualifiedName
{
	private readonly bool _exclude;

	private static readonly XmlQualifiedNameTest s_wc = New("*", "*");

	public static XmlQualifiedNameTest Wildcard => s_wc;

	public bool IsWildcard => (object)this == Wildcard;

	public bool IsNameWildcard => (object)base.Name == "*";

	public bool IsNamespaceWildcard => (object)base.Namespace == "*";

	private XmlQualifiedNameTest(string name, string ns, bool exclude)
		: base(name, ns)
	{
		_exclude = exclude;
	}

	public static XmlQualifiedNameTest New(string name, string ns)
	{
		if (ns == null && name == null)
		{
			return Wildcard;
		}
		return new XmlQualifiedNameTest((name == null) ? "*" : name, (ns == null) ? "*" : ns, exclude: false);
	}

	private bool IsNameSubsetOf(XmlQualifiedNameTest other)
	{
		if (!other.IsNameWildcard)
		{
			return base.Name == other.Name;
		}
		return true;
	}

	private bool IsNamespaceSubsetOf(XmlQualifiedNameTest other)
	{
		if (!other.IsNamespaceWildcard && (_exclude != other._exclude || !(base.Namespace == other.Namespace)))
		{
			if (other._exclude && !_exclude)
			{
				return base.Namespace != other.Namespace;
			}
			return false;
		}
		return true;
	}

	public bool IsSubsetOf(XmlQualifiedNameTest other)
	{
		if (IsNameSubsetOf(other))
		{
			return IsNamespaceSubsetOf(other);
		}
		return false;
	}

	public bool HasIntersection(XmlQualifiedNameTest other)
	{
		if (IsNamespaceSubsetOf(other) || other.IsNamespaceSubsetOf(this))
		{
			if (!IsNameSubsetOf(other))
			{
				return other.IsNameSubsetOf(this);
			}
			return true;
		}
		return false;
	}

	public override string ToString()
	{
		if ((object)this == Wildcard)
		{
			return "*";
		}
		if (base.Namespace.Length == 0)
		{
			return base.Name;
		}
		if ((object)base.Namespace == "*")
		{
			return "*:" + base.Name;
		}
		if (_exclude)
		{
			return "{~" + base.Namespace + "}:" + base.Name;
		}
		return "{" + base.Namespace + "}:" + base.Name;
	}
}

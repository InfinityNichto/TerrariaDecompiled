namespace System.Xml.Linq;

internal struct NamespaceCache
{
	private XNamespace _ns;

	private string _namespaceName;

	public XNamespace Get(string namespaceName)
	{
		if ((object)namespaceName == _namespaceName)
		{
			return _ns;
		}
		_namespaceName = namespaceName;
		_ns = XNamespace.Get(namespaceName);
		return _ns;
	}
}

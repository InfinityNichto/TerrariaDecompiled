namespace System.Xml.Schema;

internal sealed class NamespaceListV1Compat : NamespaceList
{
	public NamespaceListV1Compat(string namespaces, string targetNamespace)
		: base(namespaces, targetNamespace)
	{
	}

	public override bool Allows(string ns)
	{
		if (base.Type == ListType.Other)
		{
			return ns != base.Excluded;
		}
		return base.Allows(ns);
	}
}

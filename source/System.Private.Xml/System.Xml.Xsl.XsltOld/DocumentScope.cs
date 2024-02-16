namespace System.Xml.Xsl.XsltOld;

internal class DocumentScope
{
	protected NamespaceDecl scopes;

	internal NamespaceDecl Scopes => scopes;

	internal NamespaceDecl AddNamespace(string prefix, string uri, string prevDefaultNsUri)
	{
		scopes = new NamespaceDecl(prefix, uri, prevDefaultNsUri, scopes);
		return scopes;
	}

	internal string ResolveAtom(string prefix)
	{
		for (NamespaceDecl next = scopes; next != null; next = next.Next)
		{
			if (Ref.Equal(next.Prefix, prefix))
			{
				return next.Uri;
			}
		}
		return null;
	}

	internal string ResolveNonAtom(string prefix)
	{
		for (NamespaceDecl next = scopes; next != null; next = next.Next)
		{
			if (next.Prefix == prefix)
			{
				return next.Uri;
			}
		}
		return null;
	}
}

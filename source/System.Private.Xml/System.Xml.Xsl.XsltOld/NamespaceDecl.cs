using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Xsl.XsltOld;

internal sealed class NamespaceDecl
{
	private string _prefix;

	private string _nsUri;

	private string _prevDefaultNsUri;

	private NamespaceDecl _next;

	internal string Prefix => _prefix;

	internal string Uri => _nsUri;

	internal string PrevDefaultNsUri => _prevDefaultNsUri;

	internal NamespaceDecl Next => _next;

	internal NamespaceDecl(string prefix, string nsUri, string prevDefaultNsUri, NamespaceDecl next)
	{
		Init(prefix, nsUri, prevDefaultNsUri, next);
	}

	[MemberNotNull("_prefix")]
	[MemberNotNull("_nsUri")]
	[MemberNotNull("_prevDefaultNsUri")]
	internal void Init(string prefix, string nsUri, string prevDefaultNsUri, NamespaceDecl next)
	{
		_prefix = prefix;
		_nsUri = nsUri;
		_prevDefaultNsUri = prevDefaultNsUri;
		_next = next;
	}
}

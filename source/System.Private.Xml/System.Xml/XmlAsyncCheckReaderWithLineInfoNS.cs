using System.Collections.Generic;

namespace System.Xml;

internal class XmlAsyncCheckReaderWithLineInfoNS : XmlAsyncCheckReaderWithLineInfo, IXmlNamespaceResolver
{
	private readonly IXmlNamespaceResolver _readerAsIXmlNamespaceResolver;

	public XmlAsyncCheckReaderWithLineInfoNS(XmlReader reader)
		: base(reader)
	{
		_readerAsIXmlNamespaceResolver = (IXmlNamespaceResolver)reader;
	}

	IDictionary<string, string> IXmlNamespaceResolver.GetNamespacesInScope(XmlNamespaceScope scope)
	{
		return _readerAsIXmlNamespaceResolver.GetNamespacesInScope(scope);
	}

	string IXmlNamespaceResolver.LookupNamespace(string prefix)
	{
		return _readerAsIXmlNamespaceResolver.LookupNamespace(prefix);
	}

	string IXmlNamespaceResolver.LookupPrefix(string namespaceName)
	{
		return _readerAsIXmlNamespaceResolver.LookupPrefix(namespaceName);
	}
}

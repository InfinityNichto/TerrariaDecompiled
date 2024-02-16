using System.Xml.XPath;

namespace System.Xml.Xsl.XsltOld;

internal sealed class NamespaceEvent : Event
{
	private string _namespaceUri;

	private string _name;

	public NamespaceEvent(NavigatorInput input)
	{
		_namespaceUri = input.Value;
		_name = input.LocalName;
	}

	public override void ReplaceNamespaceAlias(Compiler compiler)
	{
		if (_namespaceUri.Length == 0)
		{
			return;
		}
		NamespaceInfo namespaceInfo = compiler.FindNamespaceAlias(_namespaceUri);
		if (namespaceInfo != null)
		{
			_namespaceUri = namespaceInfo.nameSpace;
			if (namespaceInfo.prefix != null)
			{
				_name = namespaceInfo.prefix;
			}
		}
	}

	public override bool Output(Processor processor, ActionFrame frame)
	{
		bool flag = processor.BeginEvent(XPathNodeType.Namespace, null, _name, _namespaceUri, empty: false);
		flag = processor.EndEvent(XPathNodeType.Namespace);
		return true;
	}
}

using System.Xml.XPath;

namespace System.Xml.Xsl.XsltOld;

internal class BeginEvent : Event
{
	private readonly XPathNodeType _nodeType;

	private string _namespaceUri;

	private readonly string _name;

	private string _prefix;

	private readonly bool _empty;

	private readonly object _htmlProps;

	public BeginEvent(Compiler compiler)
	{
		NavigatorInput input = compiler.Input;
		_nodeType = input.NodeType;
		_namespaceUri = input.NamespaceURI;
		_name = input.LocalName;
		_prefix = input.Prefix;
		_empty = input.IsEmptyTag;
		if (_nodeType == XPathNodeType.Element)
		{
			_htmlProps = HtmlElementProps.GetProps(_name);
		}
		else if (_nodeType == XPathNodeType.Attribute)
		{
			_htmlProps = HtmlAttributeProps.GetProps(_name);
		}
	}

	public override void ReplaceNamespaceAlias(Compiler compiler)
	{
		if (_nodeType == XPathNodeType.Attribute && _namespaceUri.Length == 0)
		{
			return;
		}
		NamespaceInfo namespaceInfo = compiler.FindNamespaceAlias(_namespaceUri);
		if (namespaceInfo != null)
		{
			_namespaceUri = namespaceInfo.nameSpace;
			if (namespaceInfo.prefix != null)
			{
				_prefix = namespaceInfo.prefix;
			}
		}
	}

	public override bool Output(Processor processor, ActionFrame frame)
	{
		return processor.BeginEvent(_nodeType, _prefix, _name, _namespaceUri, _empty, _htmlProps, search: false);
	}
}

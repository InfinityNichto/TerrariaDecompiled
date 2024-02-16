using System.Xml.XPath;

namespace System.Xml.Xsl.XsltOld;

internal sealed class EndEvent : Event
{
	private readonly XPathNodeType _nodeType;

	internal EndEvent(XPathNodeType nodeType)
	{
		_nodeType = nodeType;
	}

	public override bool Output(Processor processor, ActionFrame frame)
	{
		return processor.EndEvent(_nodeType);
	}
}

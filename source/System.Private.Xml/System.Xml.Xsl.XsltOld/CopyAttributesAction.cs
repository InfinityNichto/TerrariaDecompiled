using System.Xml.XPath;

namespace System.Xml.Xsl.XsltOld;

internal sealed class CopyAttributesAction : Action
{
	private static readonly CopyAttributesAction s_Action = new CopyAttributesAction();

	internal static CopyAttributesAction GetAction()
	{
		return s_Action;
	}

	internal override void Execute(Processor processor, ActionFrame frame)
	{
		while (processor.CanContinue)
		{
			switch (frame.State)
			{
			default:
				return;
			case 0:
				if (!frame.Node.HasAttributes || !frame.Node.MoveToFirstAttribute())
				{
					frame.Finished();
					return;
				}
				frame.State = 2;
				goto case 2;
			case 2:
				if (SendBeginEvent(processor, frame.Node))
				{
					frame.State = 3;
					break;
				}
				return;
			case 3:
				if (SendTextEvent(processor, frame.Node))
				{
					frame.State = 4;
					break;
				}
				return;
			case 4:
				if (SendEndEvent(processor, frame.Node))
				{
					frame.State = 5;
					break;
				}
				return;
			case 5:
				if (frame.Node.MoveToNextAttribute())
				{
					frame.State = 2;
					break;
				}
				frame.Node.MoveToParent();
				frame.Finished();
				return;
			case 1:
				return;
			}
		}
	}

	private static bool SendBeginEvent(Processor processor, XPathNavigator node)
	{
		return processor.BeginEvent(XPathNodeType.Attribute, node.Prefix, node.LocalName, node.NamespaceURI, empty: false);
	}

	private static bool SendTextEvent(Processor processor, XPathNavigator node)
	{
		return processor.TextEvent(node.Value);
	}

	private static bool SendEndEvent(Processor processor, XPathNavigator node)
	{
		return processor.EndEvent(XPathNodeType.Attribute);
	}
}

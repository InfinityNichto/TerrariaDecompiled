using System.Xml.XPath;

namespace System.Xml.Xsl.XsltOld;

internal sealed class CopyNodeSetAction : Action
{
	private static readonly CopyNodeSetAction s_Action = new CopyNodeSetAction();

	internal static CopyNodeSetAction GetAction()
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
				if (frame.NextNode(processor))
				{
					frame.State = 2;
					goto case 2;
				}
				frame.Finished();
				return;
			case 2:
				if (SendBeginEvent(processor, frame.Node))
				{
					frame.State = 3;
					continue;
				}
				return;
			case 3:
			{
				XPathNodeType nodeType = frame.Node.NodeType;
				if (nodeType == XPathNodeType.Element || nodeType == XPathNodeType.Root)
				{
					processor.PushActionFrame(CopyNamespacesAction.GetAction(), frame.NodeSet);
					frame.State = 4;
				}
				else if (SendTextEvent(processor, frame.Node))
				{
					frame.State = 7;
					continue;
				}
				return;
			}
			case 4:
				processor.PushActionFrame(CopyAttributesAction.GetAction(), frame.NodeSet);
				frame.State = 5;
				return;
			case 5:
				if (frame.Node.HasChildren)
				{
					processor.PushActionFrame(GetAction(), frame.Node.SelectChildren(XPathNodeType.All));
					frame.State = 6;
					return;
				}
				frame.State = 7;
				break;
			case 6:
				frame.State = 7;
				continue;
			case 7:
				break;
			case 1:
				return;
			}
			if (SendEndEvent(processor, frame.Node))
			{
				frame.State = 0;
				continue;
			}
			break;
		}
	}

	private static bool SendBeginEvent(Processor processor, XPathNavigator node)
	{
		return processor.CopyBeginEvent(node, node.IsEmptyElement);
	}

	private static bool SendTextEvent(Processor processor, XPathNavigator node)
	{
		return processor.CopyTextEvent(node);
	}

	private static bool SendEndEvent(Processor processor, XPathNavigator node)
	{
		return processor.CopyEndEvent(node);
	}
}

using System.Xml.XPath;

namespace System.Xml.Xsl.XsltOld;

internal sealed class CopyNamespacesAction : Action
{
	private static readonly CopyNamespacesAction s_Action = new CopyNamespacesAction();

	internal static CopyNamespacesAction GetAction()
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
				if (!frame.Node.MoveToFirstNamespace(XPathNamespaceScope.ExcludeXml))
				{
					frame.Finished();
					return;
				}
				frame.State = 2;
				goto case 2;
			case 2:
				if (processor.BeginEvent(XPathNodeType.Namespace, null, frame.Node.LocalName, frame.Node.Value, empty: false))
				{
					frame.State = 4;
					break;
				}
				return;
			case 4:
				if (processor.EndEvent(XPathNodeType.Namespace))
				{
					frame.State = 5;
					break;
				}
				return;
			case 5:
				if (frame.Node.MoveToNextNamespace(XPathNamespaceScope.ExcludeXml))
				{
					frame.State = 2;
					break;
				}
				frame.Node.MoveToParent();
				frame.Finished();
				return;
			case 1:
			case 3:
				return;
			}
		}
	}
}

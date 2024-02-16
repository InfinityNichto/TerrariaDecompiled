using System.Xml.XPath;

namespace System.Xml.Xsl.XsltOld;

internal class CopyAction : ContainerAction
{
	private bool _empty;

	internal override void Compile(Compiler compiler)
	{
		CompileAttributes(compiler);
		if (compiler.Recurse())
		{
			CompileTemplate(compiler);
			compiler.ToParent();
		}
		if (containedActions == null)
		{
			_empty = true;
		}
	}

	internal override bool CompileAttribute(Compiler compiler)
	{
		string localName = compiler.Input.LocalName;
		if (Ref.Equal(localName, compiler.Atoms.UseAttributeSets))
		{
			AddAction(compiler.CreateUseAttributeSetsAction());
			return true;
		}
		return false;
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
				if (Processor.IsRoot(frame.Node))
				{
					processor.PushActionFrame(frame);
					frame.State = 8;
					return;
				}
				if (processor.CopyBeginEvent(frame.Node, _empty))
				{
					frame.State = 5;
					break;
				}
				return;
			case 5:
				frame.State = 6;
				if (frame.Node.NodeType == XPathNodeType.Element)
				{
					processor.PushActionFrame(CopyNamespacesAction.GetAction(), frame.NodeSet);
					return;
				}
				break;
			case 6:
				if (frame.Node.NodeType == XPathNodeType.Element && !_empty)
				{
					processor.PushActionFrame(frame);
					frame.State = 7;
					return;
				}
				if (processor.CopyTextEvent(frame.Node))
				{
					frame.State = 7;
					break;
				}
				return;
			case 7:
				if (processor.CopyEndEvent(frame.Node))
				{
					frame.Finished();
				}
				return;
			case 8:
				frame.Finished();
				return;
			case 1:
			case 2:
			case 3:
			case 4:
				return;
			}
		}
	}
}

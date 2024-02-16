using System.Xml.XPath;

namespace System.Xml.Xsl.XsltOld;

internal class CommentAction : ContainerAction
{
	internal override void Compile(Compiler compiler)
	{
		CompileAttributes(compiler);
		if (compiler.Recurse())
		{
			CompileTemplate(compiler);
			compiler.ToParent();
		}
	}

	internal override void Execute(Processor processor, ActionFrame frame)
	{
		switch (frame.State)
		{
		case 0:
			if (processor.BeginEvent(XPathNodeType.Comment, string.Empty, string.Empty, string.Empty, empty: false))
			{
				processor.PushActionFrame(frame);
				frame.State = 1;
			}
			break;
		case 1:
			if (processor.EndEvent(XPathNodeType.Comment))
			{
				frame.Finished();
			}
			break;
		}
	}
}

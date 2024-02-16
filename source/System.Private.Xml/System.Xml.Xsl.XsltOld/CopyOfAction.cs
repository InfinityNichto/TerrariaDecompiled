using System.Xml.XPath;
using MS.Internal.Xml.XPath;

namespace System.Xml.Xsl.XsltOld;

internal class CopyOfAction : CompiledAction
{
	private int _selectKey = -1;

	internal override void Compile(Compiler compiler)
	{
		CompileAttributes(compiler);
		CheckRequiredAttribute(compiler, _selectKey != -1, "select");
		CheckEmpty(compiler);
	}

	internal override bool CompileAttribute(Compiler compiler)
	{
		string localName = compiler.Input.LocalName;
		string value = compiler.Input.Value;
		if (Ref.Equal(localName, compiler.Atoms.Select))
		{
			_selectKey = compiler.AddQuery(value);
			return true;
		}
		return false;
	}

	internal override void Execute(Processor processor, ActionFrame frame)
	{
		switch (frame.State)
		{
		case 0:
		{
			Query valueQuery = processor.GetValueQuery(_selectKey);
			object obj = valueQuery.Evaluate(frame.NodeSet);
			if (obj is XPathNodeIterator)
			{
				processor.PushActionFrame(CopyNodeSetAction.GetAction(), new XPathArrayIterator(valueQuery));
				frame.State = 3;
				break;
			}
			if (obj is XPathNavigator nav)
			{
				processor.PushActionFrame(CopyNodeSetAction.GetAction(), new XPathSingletonIterator(nav));
				frame.State = 3;
				break;
			}
			string text = XmlConvert.ToXPathString(obj);
			if (processor.TextEvent(text))
			{
				frame.Finished();
				break;
			}
			frame.StoredOutput = text;
			frame.State = 2;
			break;
		}
		case 2:
			processor.TextEvent(frame.StoredOutput);
			frame.Finished();
			break;
		case 3:
			frame.Finished();
			break;
		case 1:
			break;
		}
	}
}

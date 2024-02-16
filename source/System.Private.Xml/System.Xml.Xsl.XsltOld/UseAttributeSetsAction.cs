namespace System.Xml.Xsl.XsltOld;

internal class UseAttributeSetsAction : CompiledAction
{
	private XmlQualifiedName[] _useAttributeSets;

	private string _useString;

	internal XmlQualifiedName[] UsedSets => _useAttributeSets;

	internal override void Compile(Compiler compiler)
	{
		_useString = compiler.Input.Value;
		if (_useString.Length == 0)
		{
			_useAttributeSets = Array.Empty<XmlQualifiedName>();
			return;
		}
		string[] array = XmlConvert.SplitString(_useString);
		try
		{
			_useAttributeSets = new XmlQualifiedName[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				_useAttributeSets[i] = compiler.CreateXPathQName(array[i]);
			}
		}
		catch (XsltException)
		{
			if (!compiler.ForwardCompatibility)
			{
				throw;
			}
			_useAttributeSets = Array.Empty<XmlQualifiedName>();
		}
	}

	internal override void Execute(Processor processor, ActionFrame frame)
	{
		switch (frame.State)
		{
		default:
			return;
		case 0:
			frame.Counter = 0;
			frame.State = 2;
			break;
		case 2:
			break;
		}
		if (frame.Counter < _useAttributeSets.Length)
		{
			AttributeSetAction attributeSet = processor.RootAction.GetAttributeSet(_useAttributeSets[frame.Counter]);
			frame.IncrementCounter();
			processor.PushActionFrame(attributeSet, frame.NodeSet);
		}
		else
		{
			frame.Finished();
		}
	}
}

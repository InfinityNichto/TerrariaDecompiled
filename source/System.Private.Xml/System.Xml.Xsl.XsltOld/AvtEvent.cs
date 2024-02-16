namespace System.Xml.Xsl.XsltOld;

internal sealed class AvtEvent : TextEvent
{
	private readonly int _key;

	public AvtEvent(int key)
	{
		_key = key;
	}

	public override bool Output(Processor processor, ActionFrame frame)
	{
		return processor.TextEvent(processor.EvaluateString(frame, _key));
	}

	public override string Evaluate(Processor processor, ActionFrame frame)
	{
		return processor.EvaluateString(frame, _key);
	}
}

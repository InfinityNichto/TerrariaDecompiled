namespace System.Xml.Xsl.XsltOld;

internal class TextEvent : Event
{
	private readonly string _text;

	protected TextEvent()
	{
	}

	public TextEvent(string text)
	{
		_text = text;
	}

	public TextEvent(Compiler compiler)
	{
		NavigatorInput input = compiler.Input;
		_text = input.Value;
	}

	public override bool Output(Processor processor, ActionFrame frame)
	{
		return processor.TextEvent(_text);
	}

	public virtual string Evaluate(Processor processor, ActionFrame frame)
	{
		return _text;
	}
}

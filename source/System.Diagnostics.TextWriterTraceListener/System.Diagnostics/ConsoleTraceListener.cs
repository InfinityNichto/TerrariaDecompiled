namespace System.Diagnostics;

public class ConsoleTraceListener : TextWriterTraceListener
{
	public ConsoleTraceListener()
		: base(Console.Out)
	{
	}

	public ConsoleTraceListener(bool useErrorStream)
		: base(useErrorStream ? Console.Error : Console.Out)
	{
	}

	public override void Close()
	{
	}
}

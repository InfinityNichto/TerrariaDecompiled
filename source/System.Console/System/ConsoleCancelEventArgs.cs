namespace System;

public sealed class ConsoleCancelEventArgs : EventArgs
{
	private readonly ConsoleSpecialKey _type;

	public bool Cancel { get; set; }

	public ConsoleSpecialKey SpecialKey => _type;

	internal ConsoleCancelEventArgs(ConsoleSpecialKey type)
	{
		_type = type;
	}
}

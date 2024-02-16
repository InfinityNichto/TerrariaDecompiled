namespace System;

internal sealed class Empty
{
	public static readonly Empty Value = new Empty();

	private Empty()
	{
	}

	public override string ToString()
	{
		return string.Empty;
	}
}

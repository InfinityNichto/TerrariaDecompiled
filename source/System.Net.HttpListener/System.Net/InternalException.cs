namespace System.Net;

internal sealed class InternalException : Exception
{
	private readonly object _unexpectedValue;

	public override string Message
	{
		get
		{
			if (_unexpectedValue == null)
			{
				return base.Message;
			}
			return base.Message + " " + _unexpectedValue;
		}
	}

	internal InternalException(object unexpectedValue)
	{
		_unexpectedValue = unexpectedValue;
	}
}

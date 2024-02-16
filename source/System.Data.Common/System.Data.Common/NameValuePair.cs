namespace System.Data.Common;

internal sealed class NameValuePair
{
	private readonly string _name;

	private readonly string _value;

	private readonly int _length;

	private NameValuePair _next;

	internal string Name => _name;

	internal string Value => _value;

	internal NameValuePair Next
	{
		get
		{
			return _next;
		}
		set
		{
			if (_next != null || value == null)
			{
				throw ADP.InternalError(ADP.InternalErrorCode.NameValuePairNext);
			}
			_next = value;
		}
	}

	internal NameValuePair(string name, string value, int length)
	{
		_name = name;
		_value = value;
		_length = length;
	}
}

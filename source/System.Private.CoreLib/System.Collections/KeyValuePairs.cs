using System.Diagnostics;

namespace System.Collections;

[DebuggerDisplay("{_value}", Name = "[{_key}]")]
internal sealed class KeyValuePairs
{
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private readonly object _key;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private readonly object _value;

	public KeyValuePairs(object key, object value)
	{
		_value = value;
		_key = key;
	}
}

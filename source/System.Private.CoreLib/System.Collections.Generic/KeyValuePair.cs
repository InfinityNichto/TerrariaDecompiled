using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Collections.Generic;

public static class KeyValuePair
{
	public static KeyValuePair<TKey, TValue> Create<TKey, TValue>(TKey key, TValue value)
	{
		return new KeyValuePair<TKey, TValue>(key, value);
	}

	internal static string PairToString(object key, object value)
	{
		IFormatProvider formatProvider = null;
		IFormatProvider provider = formatProvider;
		Span<char> initialBuffer = stackalloc char[256];
		DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(4, 2, formatProvider, initialBuffer);
		handler.AppendLiteral("[");
		handler.AppendFormatted<object>(key);
		handler.AppendLiteral(", ");
		handler.AppendFormatted<object>(value);
		handler.AppendLiteral("]");
		return string.Create(provider, initialBuffer, ref handler);
	}
}
[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public readonly struct KeyValuePair<TKey, TValue>
{
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private readonly TKey key;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private readonly TValue value;

	public TKey Key => key;

	public TValue Value => value;

	public KeyValuePair(TKey key, TValue value)
	{
		this.key = key;
		this.value = value;
	}

	public override string ToString()
	{
		return KeyValuePair.PairToString(Key, Value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public void Deconstruct(out TKey key, out TValue value)
	{
		key = Key;
		value = Value;
	}
}

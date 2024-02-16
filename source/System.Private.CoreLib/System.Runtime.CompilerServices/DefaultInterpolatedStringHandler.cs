using System.Buffers;
using System.Globalization;
using System.Runtime.InteropServices;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.CompilerServices;

[InterpolatedStringHandler]
public ref struct DefaultInterpolatedStringHandler
{
	private readonly IFormatProvider _provider;

	private char[] _arrayToReturnToPool;

	private Span<char> _chars;

	private int _pos;

	private readonly bool _hasCustomFormatter;

	internal ReadOnlySpan<char> Text => _chars.Slice(0, _pos);

	public DefaultInterpolatedStringHandler(int literalLength, int formattedCount)
	{
		_provider = null;
		_chars = (_arrayToReturnToPool = ArrayPool<char>.Shared.Rent(GetDefaultLength(literalLength, formattedCount)));
		_pos = 0;
		_hasCustomFormatter = false;
	}

	public DefaultInterpolatedStringHandler(int literalLength, int formattedCount, IFormatProvider? provider)
	{
		_provider = provider;
		_chars = (_arrayToReturnToPool = ArrayPool<char>.Shared.Rent(GetDefaultLength(literalLength, formattedCount)));
		_pos = 0;
		_hasCustomFormatter = provider != null && HasCustomFormatter(provider);
	}

	public DefaultInterpolatedStringHandler(int literalLength, int formattedCount, IFormatProvider? provider, Span<char> initialBuffer)
	{
		_provider = provider;
		_chars = initialBuffer;
		_arrayToReturnToPool = null;
		_pos = 0;
		_hasCustomFormatter = provider != null && HasCustomFormatter(provider);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int GetDefaultLength(int literalLength, int formattedCount)
	{
		return Math.Max(256, literalLength + formattedCount * 11);
	}

	public override string ToString()
	{
		return new string(Text);
	}

	public string ToStringAndClear()
	{
		string result = new string(Text);
		Clear();
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void Clear()
	{
		char[] arrayToReturnToPool = _arrayToReturnToPool;
		this = default(DefaultInterpolatedStringHandler);
		if (arrayToReturnToPool != null)
		{
			ArrayPool<char>.Shared.Return(arrayToReturnToPool);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AppendLiteral(string value)
	{
		if (value.Length == 1)
		{
			Span<char> chars = _chars;
			int pos = _pos;
			if ((uint)pos < (uint)chars.Length)
			{
				chars[pos] = value[0];
				_pos = pos + 1;
			}
			else
			{
				GrowThenCopyString(value);
			}
		}
		else if (value.Length == 2)
		{
			Span<char> chars2 = _chars;
			int pos2 = _pos;
			if ((uint)pos2 < chars2.Length - 1)
			{
				Unsafe.WriteUnaligned(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref MemoryMarshal.GetReference(chars2), pos2)), Unsafe.ReadUnaligned<int>(ref Unsafe.As<char, byte>(ref value.GetRawStringData())));
				_pos = pos2 + 2;
			}
			else
			{
				GrowThenCopyString(value);
			}
		}
		else
		{
			AppendStringDirect(value);
		}
	}

	private void AppendStringDirect(string value)
	{
		if (value.TryCopyTo(_chars.Slice(_pos)))
		{
			_pos += value.Length;
		}
		else
		{
			GrowThenCopyString(value);
		}
	}

	public void AppendFormatted<T>(T value)
	{
		if (_hasCustomFormatter)
		{
			AppendCustomFormatter(value, null);
			return;
		}
		string text;
		if (value is IFormattable)
		{
			if (value is ISpanFormattable)
			{
				int charsWritten;
				while (!((ISpanFormattable)(object)value).TryFormat(_chars.Slice(_pos), out charsWritten, default(ReadOnlySpan<char>), _provider))
				{
					Grow();
				}
				_pos += charsWritten;
				return;
			}
			text = ((IFormattable)(object)value).ToString(null, _provider);
		}
		else
		{
			text = value?.ToString();
		}
		if (text != null)
		{
			AppendStringDirect(text);
		}
	}

	public void AppendFormatted<T>(T value, string? format)
	{
		if (_hasCustomFormatter)
		{
			AppendCustomFormatter(value, format);
			return;
		}
		string text;
		if (value is IFormattable)
		{
			if (value is ISpanFormattable)
			{
				int charsWritten;
				while (!((ISpanFormattable)(object)value).TryFormat(_chars.Slice(_pos), out charsWritten, format, _provider))
				{
					Grow();
				}
				_pos += charsWritten;
				return;
			}
			text = ((IFormattable)(object)value).ToString(format, _provider);
		}
		else
		{
			text = value?.ToString();
		}
		if (text != null)
		{
			AppendStringDirect(text);
		}
	}

	public void AppendFormatted<T>(T value, int alignment)
	{
		int pos = _pos;
		AppendFormatted(value);
		if (alignment != 0)
		{
			AppendOrInsertAlignmentIfNeeded(pos, alignment);
		}
	}

	public void AppendFormatted<T>(T value, int alignment, string? format)
	{
		int pos = _pos;
		AppendFormatted(value, format);
		if (alignment != 0)
		{
			AppendOrInsertAlignmentIfNeeded(pos, alignment);
		}
	}

	public void AppendFormatted(ReadOnlySpan<char> value)
	{
		if (value.TryCopyTo(_chars.Slice(_pos)))
		{
			_pos += value.Length;
		}
		else
		{
			GrowThenCopySpan(value);
		}
	}

	public void AppendFormatted(ReadOnlySpan<char> value, int alignment = 0, string? format = null)
	{
		bool flag = false;
		if (alignment < 0)
		{
			flag = true;
			alignment = -alignment;
		}
		int num = alignment - value.Length;
		if (num <= 0)
		{
			AppendFormatted(value);
			return;
		}
		EnsureCapacityForAdditionalChars(value.Length + num);
		if (flag)
		{
			value.CopyTo(_chars.Slice(_pos));
			_pos += value.Length;
			_chars.Slice(_pos, num).Fill(' ');
			_pos += num;
		}
		else
		{
			_chars.Slice(_pos, num).Fill(' ');
			_pos += num;
			value.CopyTo(_chars.Slice(_pos));
			_pos += value.Length;
		}
	}

	public void AppendFormatted(string? value)
	{
		if (!_hasCustomFormatter && value != null && value.TryCopyTo(_chars.Slice(_pos)))
		{
			_pos += value.Length;
		}
		else
		{
			AppendFormattedSlow(value);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void AppendFormattedSlow(string value)
	{
		if (_hasCustomFormatter)
		{
			AppendCustomFormatter(value, null);
		}
		else if (value != null)
		{
			EnsureCapacityForAdditionalChars(value.Length);
			value.CopyTo(_chars.Slice(_pos));
			_pos += value.Length;
		}
	}

	public void AppendFormatted(string? value, int alignment = 0, string? format = null)
	{
		this.AppendFormatted<string>(value, alignment, format);
	}

	public void AppendFormatted(object? value, int alignment = 0, string? format = null)
	{
		this.AppendFormatted<object>(value, alignment, format);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool HasCustomFormatter(IFormatProvider provider)
	{
		if (provider.GetType() != typeof(CultureInfo))
		{
			return provider.GetFormat(typeof(ICustomFormatter)) != null;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void AppendCustomFormatter<T>(T value, string format)
	{
		ICustomFormatter customFormatter = (ICustomFormatter)_provider.GetFormat(typeof(ICustomFormatter));
		if (customFormatter != null)
		{
			string text = customFormatter.Format(format, value, _provider);
			if (text != null)
			{
				AppendStringDirect(text);
			}
		}
	}

	private void AppendOrInsertAlignmentIfNeeded(int startingPos, int alignment)
	{
		int num = _pos - startingPos;
		bool flag = false;
		if (alignment < 0)
		{
			flag = true;
			alignment = -alignment;
		}
		int num2 = alignment - num;
		if (num2 > 0)
		{
			EnsureCapacityForAdditionalChars(num2);
			if (flag)
			{
				_chars.Slice(_pos, num2).Fill(' ');
			}
			else
			{
				_chars.Slice(startingPos, num).CopyTo(_chars.Slice(startingPos + num2));
				_chars.Slice(startingPos, num2).Fill(' ');
			}
			_pos += num2;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void EnsureCapacityForAdditionalChars(int additionalChars)
	{
		if (_chars.Length - _pos < additionalChars)
		{
			Grow(additionalChars);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void GrowThenCopyString(string value)
	{
		Grow(value.Length);
		value.CopyTo(_chars.Slice(_pos));
		_pos += value.Length;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void GrowThenCopySpan(ReadOnlySpan<char> value)
	{
		Grow(value.Length);
		value.CopyTo(_chars.Slice(_pos));
		_pos += value.Length;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void Grow(int additionalChars)
	{
		GrowCore((uint)(_pos + additionalChars));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void Grow()
	{
		GrowCore((uint)(_chars.Length + 1));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void GrowCore(uint requiredMinCapacity)
	{
		uint value = Math.Max(requiredMinCapacity, Math.Min((uint)(_chars.Length * 2), 1073741791u));
		int minimumLength = (int)Math.Clamp(value, 256u, 2147483647u);
		char[] array = ArrayPool<char>.Shared.Rent(minimumLength);
		_chars.Slice(0, _pos).CopyTo(array);
		char[] arrayToReturnToPool = _arrayToReturnToPool;
		_chars = (_arrayToReturnToPool = array);
		if (arrayToReturnToPool != null)
		{
			ArrayPool<char>.Shared.Return(arrayToReturnToPool);
		}
	}
}

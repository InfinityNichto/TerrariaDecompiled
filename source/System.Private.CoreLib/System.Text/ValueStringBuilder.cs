using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Text;

internal ref struct ValueStringBuilder
{
	private char[] _arrayToReturnToPool;

	private Span<char> _chars;

	private int _pos;

	public int Length
	{
		get
		{
			return _pos;
		}
		set
		{
			_pos = value;
		}
	}

	public int Capacity => _chars.Length;

	public ref char this[int index] => ref _chars[index];

	public Span<char> RawChars => _chars;

	internal void AppendSpanFormattable<T>(T value, string format, IFormatProvider provider) where T : ISpanFormattable
	{
		if (value.TryFormat(_chars.Slice(_pos), out var charsWritten, format, provider))
		{
			_pos += charsWritten;
		}
		else
		{
			Append(value.ToString(format, provider));
		}
	}

	internal void AppendFormatHelper(IFormatProvider provider, string format, ParamsArray args)
	{
		if (format == null)
		{
			throw new ArgumentNullException("format");
		}
		int num = 0;
		int length = format.Length;
		char c = '\0';
		ICustomFormatter customFormatter = (ICustomFormatter)(provider?.GetFormat(typeof(ICustomFormatter)));
		while (true)
		{
			if (num < length)
			{
				c = format[num];
				num++;
				if (c == '}')
				{
					if (num < length && format[num] == '}')
					{
						num++;
					}
					else
					{
						ThrowFormatError();
					}
				}
				else if (c == '{')
				{
					if (num >= length || format[num] != '{')
					{
						num--;
						goto IL_008f;
					}
					num++;
				}
				Append(c);
				continue;
			}
			goto IL_008f;
			IL_008f:
			if (num == length)
			{
				break;
			}
			num++;
			if (num == length || (c = format[num]) < '0' || c > '9')
			{
				ThrowFormatError();
			}
			int num2 = 0;
			do
			{
				num2 = num2 * 10 + c - 48;
				num++;
				if (num == length)
				{
					ThrowFormatError();
				}
				c = format[num];
			}
			while (c >= '0' && c <= '9' && num2 < 1000000);
			if (num2 >= args.Length)
			{
				throw new FormatException(SR.Format_IndexOutOfRange);
			}
			for (; num < length; num++)
			{
				if ((c = format[num]) != ' ')
				{
					break;
				}
			}
			bool flag = false;
			int num3 = 0;
			if (c == ',')
			{
				for (num++; num < length && format[num] == ' '; num++)
				{
				}
				if (num == length)
				{
					ThrowFormatError();
				}
				c = format[num];
				if (c == '-')
				{
					flag = true;
					num++;
					if (num == length)
					{
						ThrowFormatError();
					}
					c = format[num];
				}
				if (c < '0' || c > '9')
				{
					ThrowFormatError();
				}
				do
				{
					num3 = num3 * 10 + c - 48;
					num++;
					if (num == length)
					{
						ThrowFormatError();
					}
					c = format[num];
				}
				while (c >= '0' && c <= '9' && num3 < 1000000);
			}
			for (; num < length; num++)
			{
				if ((c = format[num]) != ' ')
				{
					break;
				}
			}
			object obj = args[num2];
			ReadOnlySpan<char> readOnlySpan = default(ReadOnlySpan<char>);
			switch (c)
			{
			case ':':
			{
				num++;
				int num4 = num;
				while (true)
				{
					if (num == length)
					{
						ThrowFormatError();
					}
					c = format[num];
					switch (c)
					{
					case '{':
						ThrowFormatError();
						goto IL_0205;
					default:
						goto IL_0205;
					case '}':
						break;
					}
					break;
					IL_0205:
					num++;
				}
				if (num > num4)
				{
					readOnlySpan = format.AsSpan(num4, num - num4);
				}
				break;
			}
			default:
				ThrowFormatError();
				break;
			case '}':
				break;
			}
			num++;
			string text = null;
			string text2 = null;
			if (customFormatter != null)
			{
				if (readOnlySpan.Length != 0)
				{
					text2 = new string(readOnlySpan);
				}
				text = customFormatter.Format(text2, obj, provider);
			}
			if (text == null)
			{
				if (obj is ISpanFormattable spanFormattable && (flag || num3 == 0) && spanFormattable.TryFormat(_chars.Slice(_pos), out var charsWritten, readOnlySpan, provider))
				{
					_pos += charsWritten;
					int num5 = num3 - charsWritten;
					if (flag && num5 > 0)
					{
						Append(' ', num5);
					}
					continue;
				}
				if (obj is IFormattable formattable)
				{
					if (readOnlySpan.Length != 0 && text2 == null)
					{
						text2 = new string(readOnlySpan);
					}
					text = formattable.ToString(text2, provider);
				}
				else if (obj != null)
				{
					text = obj.ToString();
				}
			}
			if (text == null)
			{
				text = string.Empty;
			}
			int num6 = num3 - text.Length;
			if (!flag && num6 > 0)
			{
				Append(' ', num6);
			}
			Append(text);
			if (flag && num6 > 0)
			{
				Append(' ', num6);
			}
		}
	}

	private static void ThrowFormatError()
	{
		throw new FormatException(SR.Format_InvalidString);
	}

	public ValueStringBuilder(Span<char> initialBuffer)
	{
		_arrayToReturnToPool = null;
		_chars = initialBuffer;
		_pos = 0;
	}

	public ValueStringBuilder(int initialCapacity)
	{
		_arrayToReturnToPool = ArrayPool<char>.Shared.Rent(initialCapacity);
		_chars = _arrayToReturnToPool;
		_pos = 0;
	}

	public void EnsureCapacity(int capacity)
	{
		if ((uint)capacity > (uint)_chars.Length)
		{
			Grow(capacity - _pos);
		}
	}

	public ref char GetPinnableReference()
	{
		return ref MemoryMarshal.GetReference(_chars);
	}

	public ref char GetPinnableReference(bool terminate)
	{
		if (terminate)
		{
			EnsureCapacity(Length + 1);
			_chars[Length] = '\0';
		}
		return ref MemoryMarshal.GetReference(_chars);
	}

	public override string ToString()
	{
		string result = _chars.Slice(0, _pos).ToString();
		Dispose();
		return result;
	}

	public ReadOnlySpan<char> AsSpan(bool terminate)
	{
		if (terminate)
		{
			EnsureCapacity(Length + 1);
			_chars[Length] = '\0';
		}
		return _chars.Slice(0, _pos);
	}

	public ReadOnlySpan<char> AsSpan()
	{
		return _chars.Slice(0, _pos);
	}

	public ReadOnlySpan<char> AsSpan(int start)
	{
		return _chars.Slice(start, _pos - start);
	}

	public ReadOnlySpan<char> AsSpan(int start, int length)
	{
		return _chars.Slice(start, length);
	}

	public bool TryCopyTo(Span<char> destination, out int charsWritten)
	{
		if (_chars.Slice(0, _pos).TryCopyTo(destination))
		{
			charsWritten = _pos;
			Dispose();
			return true;
		}
		charsWritten = 0;
		Dispose();
		return false;
	}

	public void Insert(int index, string s)
	{
		if (s != null)
		{
			int length = s.Length;
			if (_pos > _chars.Length - length)
			{
				Grow(length);
			}
			int length2 = _pos - index;
			_chars.Slice(index, length2).CopyTo(_chars.Slice(index + length));
			s.CopyTo(_chars.Slice(index));
			_pos += length;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Append(char c)
	{
		int pos = _pos;
		if ((uint)pos < (uint)_chars.Length)
		{
			_chars[pos] = c;
			_pos = pos + 1;
		}
		else
		{
			GrowAndAppend(c);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Append(string s)
	{
		if (s != null)
		{
			int pos = _pos;
			if (s.Length == 1 && (uint)pos < (uint)_chars.Length)
			{
				_chars[pos] = s[0];
				_pos = pos + 1;
			}
			else
			{
				AppendSlow(s);
			}
		}
	}

	private void AppendSlow(string s)
	{
		int pos = _pos;
		if (pos > _chars.Length - s.Length)
		{
			Grow(s.Length);
		}
		s.CopyTo(_chars.Slice(pos));
		_pos += s.Length;
	}

	public void Append(char c, int count)
	{
		if (_pos > _chars.Length - count)
		{
			Grow(count);
		}
		Span<char> span = _chars.Slice(_pos, count);
		for (int i = 0; i < span.Length; i++)
		{
			span[i] = c;
		}
		_pos += count;
	}

	public unsafe void Append(char* value, int length)
	{
		int pos = _pos;
		if (pos > _chars.Length - length)
		{
			Grow(length);
		}
		Span<char> span = _chars.Slice(_pos, length);
		for (int i = 0; i < span.Length; i++)
		{
			span[i] = *(value++);
		}
		_pos += length;
	}

	public void Append(ReadOnlySpan<char> value)
	{
		int pos = _pos;
		if (pos > _chars.Length - value.Length)
		{
			Grow(value.Length);
		}
		value.CopyTo(_chars.Slice(_pos));
		_pos += value.Length;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Span<char> AppendSpan(int length)
	{
		int pos = _pos;
		if (pos > _chars.Length - length)
		{
			Grow(length);
		}
		_pos = pos + length;
		return _chars.Slice(pos, length);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void GrowAndAppend(char c)
	{
		Grow(1);
		Append(c);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void Grow(int additionalCapacityBeyondPos)
	{
		char[] array = ArrayPool<char>.Shared.Rent((int)Math.Max((uint)(_pos + additionalCapacityBeyondPos), (uint)(_chars.Length * 2)));
		_chars.Slice(0, _pos).CopyTo(array);
		char[] arrayToReturnToPool = _arrayToReturnToPool;
		_chars = (_arrayToReturnToPool = array);
		if (arrayToReturnToPool != null)
		{
			ArrayPool<char>.Shared.Return(arrayToReturnToPool);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose()
	{
		char[] arrayToReturnToPool = _arrayToReturnToPool;
		this = default(ValueStringBuilder);
		if (arrayToReturnToPool != null)
		{
			ArrayPool<char>.Shared.Return(arrayToReturnToPool);
		}
	}
}

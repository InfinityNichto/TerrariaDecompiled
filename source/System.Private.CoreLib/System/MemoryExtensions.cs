using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Internal.Runtime.CompilerServices;

namespace System;

public static class MemoryExtensions
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	[InterpolatedStringHandler]
	public ref struct TryWriteInterpolatedStringHandler
	{
		private readonly Span<char> _destination;

		private readonly IFormatProvider _provider;

		internal int _pos;

		internal bool _success;

		private readonly bool _hasCustomFormatter;

		public TryWriteInterpolatedStringHandler(int literalLength, int formattedCount, Span<char> destination, out bool shouldAppend)
		{
			_destination = destination;
			_provider = null;
			_pos = 0;
			_success = (shouldAppend = destination.Length >= literalLength);
			_hasCustomFormatter = false;
		}

		public TryWriteInterpolatedStringHandler(int literalLength, int formattedCount, Span<char> destination, IFormatProvider? provider, out bool shouldAppend)
		{
			_destination = destination;
			_provider = provider;
			_pos = 0;
			_success = (shouldAppend = destination.Length >= literalLength);
			_hasCustomFormatter = provider != null && DefaultInterpolatedStringHandler.HasCustomFormatter(provider);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool AppendLiteral(string value)
		{
			if (value.Length == 1)
			{
				Span<char> destination = _destination;
				int pos = _pos;
				if ((uint)pos < (uint)destination.Length)
				{
					destination[pos] = value[0];
					_pos = pos + 1;
					return true;
				}
				return Fail();
			}
			if (value.Length == 2)
			{
				Span<char> destination2 = _destination;
				int pos2 = _pos;
				if ((uint)pos2 < destination2.Length - 1)
				{
					Unsafe.WriteUnaligned(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref MemoryMarshal.GetReference(destination2), pos2)), Unsafe.ReadUnaligned<int>(ref Unsafe.As<char, byte>(ref value.GetRawStringData())));
					_pos = pos2 + 2;
					return true;
				}
				return Fail();
			}
			return AppendStringDirect(value);
		}

		private bool AppendStringDirect(string value)
		{
			if (value.TryCopyTo(_destination.Slice(_pos)))
			{
				_pos += value.Length;
				return true;
			}
			return Fail();
		}

		public bool AppendFormatted<T>(T value)
		{
			if (_hasCustomFormatter)
			{
				return AppendCustomFormatter(value, null);
			}
			string text;
			if (value is IFormattable)
			{
				if (value is ISpanFormattable)
				{
					if (((ISpanFormattable)(object)value).TryFormat(_destination.Slice(_pos), out var charsWritten, default(ReadOnlySpan<char>), _provider))
					{
						_pos += charsWritten;
						return true;
					}
					return Fail();
				}
				text = ((IFormattable)(object)value).ToString(null, _provider);
			}
			else
			{
				text = value?.ToString();
			}
			if (text != null)
			{
				return AppendStringDirect(text);
			}
			return true;
		}

		public bool AppendFormatted<T>(T value, string? format)
		{
			if (_hasCustomFormatter)
			{
				return AppendCustomFormatter(value, format);
			}
			string text;
			if (value is IFormattable)
			{
				if (value is ISpanFormattable)
				{
					if (((ISpanFormattable)(object)value).TryFormat(_destination.Slice(_pos), out var charsWritten, format, _provider))
					{
						_pos += charsWritten;
						return true;
					}
					return Fail();
				}
				text = ((IFormattable)(object)value).ToString(format, _provider);
			}
			else
			{
				text = value?.ToString();
			}
			if (text != null)
			{
				return AppendStringDirect(text);
			}
			return true;
		}

		public bool AppendFormatted<T>(T value, int alignment)
		{
			int pos = _pos;
			if (AppendFormatted(value))
			{
				if (alignment != 0)
				{
					return TryAppendOrInsertAlignmentIfNeeded(pos, alignment);
				}
				return true;
			}
			return Fail();
		}

		public bool AppendFormatted<T>(T value, int alignment, string? format)
		{
			int pos = _pos;
			if (AppendFormatted(value, format))
			{
				if (alignment != 0)
				{
					return TryAppendOrInsertAlignmentIfNeeded(pos, alignment);
				}
				return true;
			}
			return Fail();
		}

		public bool AppendFormatted(ReadOnlySpan<char> value)
		{
			if (value.TryCopyTo(_destination.Slice(_pos)))
			{
				_pos += value.Length;
				return true;
			}
			return Fail();
		}

		public bool AppendFormatted(ReadOnlySpan<char> value, int alignment = 0, string? format = null)
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
				return AppendFormatted(value);
			}
			if (alignment <= _destination.Length - _pos)
			{
				if (flag)
				{
					value.CopyTo(_destination.Slice(_pos));
					_pos += value.Length;
					_destination.Slice(_pos, num).Fill(' ');
					_pos += num;
				}
				else
				{
					_destination.Slice(_pos, num).Fill(' ');
					_pos += num;
					value.CopyTo(_destination.Slice(_pos));
					_pos += value.Length;
				}
				return true;
			}
			return Fail();
		}

		public bool AppendFormatted(string? value)
		{
			if (_hasCustomFormatter)
			{
				return AppendCustomFormatter(value, null);
			}
			if (value == null)
			{
				return true;
			}
			if (value.TryCopyTo(_destination.Slice(_pos)))
			{
				_pos += value.Length;
				return true;
			}
			return Fail();
		}

		public bool AppendFormatted(string? value, int alignment = 0, string? format = null)
		{
			return this.AppendFormatted<string>(value, alignment, format);
		}

		public bool AppendFormatted(object? value, int alignment = 0, string? format = null)
		{
			return this.AppendFormatted<object>(value, alignment, format);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private bool AppendCustomFormatter<T>(T value, string format)
		{
			ICustomFormatter customFormatter = (ICustomFormatter)_provider.GetFormat(typeof(ICustomFormatter));
			if (customFormatter != null)
			{
				string text = customFormatter.Format(format, value, _provider);
				if (text != null)
				{
					return AppendStringDirect(text);
				}
			}
			return true;
		}

		private bool TryAppendOrInsertAlignmentIfNeeded(int startingPos, int alignment)
		{
			int num = _pos - startingPos;
			bool flag = false;
			if (alignment < 0)
			{
				flag = true;
				alignment = -alignment;
			}
			int num2 = alignment - num;
			if (num2 <= 0)
			{
				return true;
			}
			if (num2 <= _destination.Length - _pos)
			{
				if (flag)
				{
					_destination.Slice(_pos, num2).Fill(' ');
				}
				else
				{
					_destination.Slice(startingPos, num).CopyTo(_destination.Slice(startingPos + num2));
					_destination.Slice(startingPos, num2).Fill(' ');
				}
				_pos += num2;
				return true;
			}
			return Fail();
		}

		private bool Fail()
		{
			_success = false;
			return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Span<T> AsSpan<T>(this T[]? array, int start)
	{
		if (array == null)
		{
			if (start != 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException();
			}
			return default(Span<T>);
		}
		if (!typeof(T).IsValueType && array.GetType() != typeof(T[]))
		{
			ThrowHelper.ThrowArrayTypeMismatchException();
		}
		if ((uint)start > (uint)array.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException();
		}
		return new Span<T>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), (nint)(uint)start), array.Length - start);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Span<T> AsSpan<T>(this T[]? array, Index startIndex)
	{
		if (array == null)
		{
			if (!startIndex.Equals(Index.Start))
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
			}
			return default(Span<T>);
		}
		if (!typeof(T).IsValueType && array.GetType() != typeof(T[]))
		{
			ThrowHelper.ThrowArrayTypeMismatchException();
		}
		int offset = startIndex.GetOffset(array.Length);
		if ((uint)offset > (uint)array.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException();
		}
		return new Span<T>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), (nint)(uint)offset), array.Length - offset);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Span<T> AsSpan<T>(this T[]? array, Range range)
	{
		if (array == null)
		{
			Index start = range.Start;
			Index end = range.End;
			if (!start.Equals(Index.Start) || !end.Equals(Index.Start))
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
			}
			return default(Span<T>);
		}
		if (!typeof(T).IsValueType && array.GetType() != typeof(T[]))
		{
			ThrowHelper.ThrowArrayTypeMismatchException();
		}
		var (num, length) = range.GetOffsetAndLength(array.Length);
		return new Span<T>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), (nint)(uint)num), length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<char> AsSpan(this string? text)
	{
		if (text == null)
		{
			return default(ReadOnlySpan<char>);
		}
		return new ReadOnlySpan<char>(ref text.GetRawStringData(), text.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<char> AsSpan(this string? text, int start)
	{
		if (text == null)
		{
			if (start != 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			return default(ReadOnlySpan<char>);
		}
		if ((uint)start > (uint)text.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
		}
		return new ReadOnlySpan<char>(ref Unsafe.Add(ref text.GetRawStringData(), (nint)(uint)start), text.Length - start);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<char> AsSpan(this string? text, int start, int length)
	{
		if (text == null)
		{
			if (start != 0 || length != 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			return default(ReadOnlySpan<char>);
		}
		if ((ulong)((long)(uint)start + (long)(uint)length) > (ulong)(uint)text.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
		}
		return new ReadOnlySpan<char>(ref Unsafe.Add(ref text.GetRawStringData(), (nint)(uint)start), length);
	}

	public static ReadOnlyMemory<char> AsMemory(this string? text)
	{
		if (text == null)
		{
			return default(ReadOnlyMemory<char>);
		}
		return new ReadOnlyMemory<char>(text, 0, text.Length);
	}

	public static ReadOnlyMemory<char> AsMemory(this string? text, int start)
	{
		if (text == null)
		{
			if (start != 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			return default(ReadOnlyMemory<char>);
		}
		if ((uint)start > (uint)text.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
		}
		return new ReadOnlyMemory<char>(text, start, text.Length - start);
	}

	public static ReadOnlyMemory<char> AsMemory(this string? text, Index startIndex)
	{
		if (text == null)
		{
			if (!startIndex.Equals(Index.Start))
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.text);
			}
			return default(ReadOnlyMemory<char>);
		}
		int offset = startIndex.GetOffset(text.Length);
		if ((uint)offset > (uint)text.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException();
		}
		return new ReadOnlyMemory<char>(text, offset, text.Length - offset);
	}

	public static ReadOnlyMemory<char> AsMemory(this string? text, int start, int length)
	{
		if (text == null)
		{
			if (start != 0 || length != 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
			}
			return default(ReadOnlyMemory<char>);
		}
		if ((ulong)((long)(uint)start + (long)(uint)length) > (ulong)(uint)text.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
		}
		return new ReadOnlyMemory<char>(text, start, length);
	}

	public static ReadOnlyMemory<char> AsMemory(this string? text, Range range)
	{
		if (text == null)
		{
			Index start = range.Start;
			Index end = range.End;
			if (!start.Equals(Index.Start) || !end.Equals(Index.Start))
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.text);
			}
			return default(ReadOnlyMemory<char>);
		}
		var (start2, length) = range.GetOffsetAndLength(text.Length);
		return new ReadOnlyMemory<char>(text, start2, length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool Contains<T>(this Span<T> span, T value) where T : IEquatable<T>
	{
		if (RuntimeHelpers.IsBitwiseEquatable<T>())
		{
			if (Unsafe.SizeOf<T>() == 1)
			{
				return SpanHelpers.Contains(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)), Unsafe.As<T, byte>(ref value), span.Length);
			}
			if (Unsafe.SizeOf<T>() == 2)
			{
				return SpanHelpers.Contains(ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)), Unsafe.As<T, char>(ref value), span.Length);
			}
		}
		return SpanHelpers.Contains(ref MemoryMarshal.GetReference(span), value, span.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool Contains<T>(this ReadOnlySpan<T> span, T value) where T : IEquatable<T>
	{
		if (RuntimeHelpers.IsBitwiseEquatable<T>())
		{
			if (Unsafe.SizeOf<T>() == 1)
			{
				return SpanHelpers.Contains(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)), Unsafe.As<T, byte>(ref value), span.Length);
			}
			if (Unsafe.SizeOf<T>() == 2)
			{
				return SpanHelpers.Contains(ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)), Unsafe.As<T, char>(ref value), span.Length);
			}
		}
		return SpanHelpers.Contains(ref MemoryMarshal.GetReference(span), value, span.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int IndexOf<T>(this Span<T> span, T value) where T : IEquatable<T>
	{
		if (RuntimeHelpers.IsBitwiseEquatable<T>())
		{
			if (Unsafe.SizeOf<T>() == 1)
			{
				return SpanHelpers.IndexOf(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)), Unsafe.As<T, byte>(ref value), span.Length);
			}
			if (Unsafe.SizeOf<T>() == 2)
			{
				return SpanHelpers.IndexOf(ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)), Unsafe.As<T, char>(ref value), span.Length);
			}
		}
		return SpanHelpers.IndexOf(ref MemoryMarshal.GetReference(span), value, span.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int IndexOf<T>(this Span<T> span, ReadOnlySpan<T> value) where T : IEquatable<T>
	{
		if (RuntimeHelpers.IsBitwiseEquatable<T>())
		{
			if (Unsafe.SizeOf<T>() == 1)
			{
				return SpanHelpers.IndexOf(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)), span.Length, ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(value)), value.Length);
			}
			if (Unsafe.SizeOf<T>() == 2)
			{
				return SpanHelpers.IndexOf(ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)), span.Length, ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(value)), value.Length);
			}
		}
		return SpanHelpers.IndexOf(ref MemoryMarshal.GetReference(span), span.Length, ref MemoryMarshal.GetReference(value), value.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int LastIndexOf<T>(this Span<T> span, T value) where T : IEquatable<T>
	{
		if (RuntimeHelpers.IsBitwiseEquatable<T>())
		{
			if (Unsafe.SizeOf<T>() == 1)
			{
				return SpanHelpers.LastIndexOf(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)), Unsafe.As<T, byte>(ref value), span.Length);
			}
			if (Unsafe.SizeOf<T>() == 2)
			{
				return SpanHelpers.LastIndexOf(ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)), Unsafe.As<T, char>(ref value), span.Length);
			}
		}
		return SpanHelpers.LastIndexOf(ref MemoryMarshal.GetReference(span), value, span.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int LastIndexOf<T>(this Span<T> span, ReadOnlySpan<T> value) where T : IEquatable<T>
	{
		if (Unsafe.SizeOf<T>() == 1 && RuntimeHelpers.IsBitwiseEquatable<T>())
		{
			return SpanHelpers.LastIndexOf(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)), span.Length, ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(value)), value.Length);
		}
		return SpanHelpers.LastIndexOf(ref MemoryMarshal.GetReference(span), span.Length, ref MemoryMarshal.GetReference(value), value.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool SequenceEqual<T>(this Span<T> span, ReadOnlySpan<T> other) where T : IEquatable<T>
	{
		int length = span.Length;
		if (RuntimeHelpers.IsBitwiseEquatable<T>())
		{
			nuint num = (nuint)Unsafe.SizeOf<T>();
			if (length == other.Length)
			{
				return SpanHelpers.SequenceEqual(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)), ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(other)), (uint)length * num);
			}
			return false;
		}
		if (length == other.Length)
		{
			return SpanHelpers.SequenceEqual(ref MemoryMarshal.GetReference(span), ref MemoryMarshal.GetReference(other), length);
		}
		return false;
	}

	public static int SequenceCompareTo<T>(this Span<T> span, ReadOnlySpan<T> other) where T : IComparable<T>
	{
		if (typeof(T) == typeof(byte))
		{
			return SpanHelpers.SequenceCompareTo(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)), span.Length, ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(other)), other.Length);
		}
		if (typeof(T) == typeof(char))
		{
			return SpanHelpers.SequenceCompareTo(ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)), span.Length, ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(other)), other.Length);
		}
		return SpanHelpers.SequenceCompareTo(ref MemoryMarshal.GetReference(span), span.Length, ref MemoryMarshal.GetReference(other), other.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int IndexOf<T>(this ReadOnlySpan<T> span, T value) where T : IEquatable<T>
	{
		if (RuntimeHelpers.IsBitwiseEquatable<T>())
		{
			if (Unsafe.SizeOf<T>() == 1)
			{
				return SpanHelpers.IndexOf(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)), Unsafe.As<T, byte>(ref value), span.Length);
			}
			if (Unsafe.SizeOf<T>() == 2)
			{
				return SpanHelpers.IndexOf(ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)), Unsafe.As<T, char>(ref value), span.Length);
			}
		}
		return SpanHelpers.IndexOf(ref MemoryMarshal.GetReference(span), value, span.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int IndexOf<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> value) where T : IEquatable<T>
	{
		if (RuntimeHelpers.IsBitwiseEquatable<T>())
		{
			if (Unsafe.SizeOf<T>() == 1)
			{
				return SpanHelpers.IndexOf(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)), span.Length, ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(value)), value.Length);
			}
			if (Unsafe.SizeOf<T>() == 2)
			{
				return SpanHelpers.IndexOf(ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)), span.Length, ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(value)), value.Length);
			}
		}
		return SpanHelpers.IndexOf(ref MemoryMarshal.GetReference(span), span.Length, ref MemoryMarshal.GetReference(value), value.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int LastIndexOf<T>(this ReadOnlySpan<T> span, T value) where T : IEquatable<T>
	{
		if (RuntimeHelpers.IsBitwiseEquatable<T>())
		{
			if (Unsafe.SizeOf<T>() == 1)
			{
				return SpanHelpers.LastIndexOf(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)), Unsafe.As<T, byte>(ref value), span.Length);
			}
			if (Unsafe.SizeOf<T>() == 2)
			{
				return SpanHelpers.LastIndexOf(ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)), Unsafe.As<T, char>(ref value), span.Length);
			}
		}
		return SpanHelpers.LastIndexOf(ref MemoryMarshal.GetReference(span), value, span.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int LastIndexOf<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> value) where T : IEquatable<T>
	{
		if (Unsafe.SizeOf<T>() == 1 && RuntimeHelpers.IsBitwiseEquatable<T>())
		{
			return SpanHelpers.LastIndexOf(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)), span.Length, ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(value)), value.Length);
		}
		return SpanHelpers.LastIndexOf(ref MemoryMarshal.GetReference(span), span.Length, ref MemoryMarshal.GetReference(value), value.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int IndexOfAny<T>(this Span<T> span, T value0, T value1) where T : IEquatable<T>
	{
		if (RuntimeHelpers.IsBitwiseEquatable<T>())
		{
			if (Unsafe.SizeOf<T>() == 1)
			{
				return SpanHelpers.IndexOfAny(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)), Unsafe.As<T, byte>(ref value0), Unsafe.As<T, byte>(ref value1), span.Length);
			}
			if (Unsafe.SizeOf<T>() == 2)
			{
				return SpanHelpers.IndexOfAny(ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)), Unsafe.As<T, char>(ref value0), Unsafe.As<T, char>(ref value1), span.Length);
			}
		}
		return SpanHelpers.IndexOfAny(ref MemoryMarshal.GetReference(span), value0, value1, span.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int IndexOfAny<T>(this Span<T> span, T value0, T value1, T value2) where T : IEquatable<T>
	{
		if (RuntimeHelpers.IsBitwiseEquatable<T>())
		{
			if (Unsafe.SizeOf<T>() == 1)
			{
				return SpanHelpers.IndexOfAny(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)), Unsafe.As<T, byte>(ref value0), Unsafe.As<T, byte>(ref value1), Unsafe.As<T, byte>(ref value2), span.Length);
			}
			if (Unsafe.SizeOf<T>() == 2)
			{
				return SpanHelpers.IndexOfAny(ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)), Unsafe.As<T, char>(ref value0), Unsafe.As<T, char>(ref value1), Unsafe.As<T, char>(ref value2), span.Length);
			}
		}
		return SpanHelpers.IndexOfAny(ref MemoryMarshal.GetReference(span), value0, value1, value2, span.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int IndexOfAny<T>(this Span<T> span, ReadOnlySpan<T> values) where T : IEquatable<T>
	{
		return ((ReadOnlySpan<T>)span).IndexOfAny(values);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int IndexOfAny<T>(this ReadOnlySpan<T> span, T value0, T value1) where T : IEquatable<T>
	{
		if (RuntimeHelpers.IsBitwiseEquatable<T>())
		{
			if (Unsafe.SizeOf<T>() == 1)
			{
				return SpanHelpers.IndexOfAny(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)), Unsafe.As<T, byte>(ref value0), Unsafe.As<T, byte>(ref value1), span.Length);
			}
			if (Unsafe.SizeOf<T>() == 2)
			{
				return SpanHelpers.IndexOfAny(ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)), Unsafe.As<T, char>(ref value0), Unsafe.As<T, char>(ref value1), span.Length);
			}
		}
		return SpanHelpers.IndexOfAny(ref MemoryMarshal.GetReference(span), value0, value1, span.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int IndexOfAny<T>(this ReadOnlySpan<T> span, T value0, T value1, T value2) where T : IEquatable<T>
	{
		if (RuntimeHelpers.IsBitwiseEquatable<T>())
		{
			if (Unsafe.SizeOf<T>() == 1)
			{
				return SpanHelpers.IndexOfAny(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)), Unsafe.As<T, byte>(ref value0), Unsafe.As<T, byte>(ref value1), Unsafe.As<T, byte>(ref value2), span.Length);
			}
			if (Unsafe.SizeOf<T>() == 2)
			{
				return SpanHelpers.IndexOfAny(ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)), Unsafe.As<T, char>(ref value0), Unsafe.As<T, char>(ref value1), Unsafe.As<T, char>(ref value2), span.Length);
			}
		}
		return SpanHelpers.IndexOfAny(ref MemoryMarshal.GetReference(span), value0, value1, value2, span.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int IndexOfAny<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> values) where T : IEquatable<T>
	{
		if (RuntimeHelpers.IsBitwiseEquatable<T>())
		{
			if (Unsafe.SizeOf<T>() == 1)
			{
				ref byte reference = ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(values));
				if (values.Length == 2)
				{
					return SpanHelpers.IndexOfAny(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)), reference, Unsafe.Add(ref reference, 1), span.Length);
				}
				if (values.Length == 3)
				{
					return SpanHelpers.IndexOfAny(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)), reference, Unsafe.Add(ref reference, 1), Unsafe.Add(ref reference, 2), span.Length);
				}
			}
			if (Unsafe.SizeOf<T>() == 2)
			{
				ref char reference2 = ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(values));
				if (values.Length == 5)
				{
					return SpanHelpers.IndexOfAny(ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)), reference2, Unsafe.Add(ref reference2, 1), Unsafe.Add(ref reference2, 2), Unsafe.Add(ref reference2, 3), Unsafe.Add(ref reference2, 4), span.Length);
				}
				if (values.Length == 2)
				{
					return SpanHelpers.IndexOfAny(ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)), reference2, Unsafe.Add(ref reference2, 1), span.Length);
				}
				if (values.Length == 4)
				{
					return SpanHelpers.IndexOfAny(ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)), reference2, Unsafe.Add(ref reference2, 1), Unsafe.Add(ref reference2, 2), Unsafe.Add(ref reference2, 3), span.Length);
				}
				if (values.Length == 3)
				{
					return SpanHelpers.IndexOfAny(ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)), reference2, Unsafe.Add(ref reference2, 1), Unsafe.Add(ref reference2, 2), span.Length);
				}
				if (values.Length == 1)
				{
					return SpanHelpers.IndexOf(ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)), reference2, span.Length);
				}
			}
		}
		return SpanHelpers.IndexOfAny(ref MemoryMarshal.GetReference(span), span.Length, ref MemoryMarshal.GetReference(values), values.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int LastIndexOfAny<T>(this Span<T> span, T value0, T value1) where T : IEquatable<T>
	{
		if (Unsafe.SizeOf<T>() == 1 && RuntimeHelpers.IsBitwiseEquatable<T>())
		{
			return SpanHelpers.LastIndexOfAny(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)), Unsafe.As<T, byte>(ref value0), Unsafe.As<T, byte>(ref value1), span.Length);
		}
		return SpanHelpers.LastIndexOfAny(ref MemoryMarshal.GetReference(span), value0, value1, span.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int LastIndexOfAny<T>(this Span<T> span, T value0, T value1, T value2) where T : IEquatable<T>
	{
		if (Unsafe.SizeOf<T>() == 1 && RuntimeHelpers.IsBitwiseEquatable<T>())
		{
			return SpanHelpers.LastIndexOfAny(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)), Unsafe.As<T, byte>(ref value0), Unsafe.As<T, byte>(ref value1), Unsafe.As<T, byte>(ref value2), span.Length);
		}
		return SpanHelpers.LastIndexOfAny(ref MemoryMarshal.GetReference(span), value0, value1, value2, span.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int LastIndexOfAny<T>(this Span<T> span, ReadOnlySpan<T> values) where T : IEquatable<T>
	{
		if (Unsafe.SizeOf<T>() == 1 && RuntimeHelpers.IsBitwiseEquatable<T>())
		{
			return SpanHelpers.LastIndexOfAny(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)), span.Length, ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(values)), values.Length);
		}
		return SpanHelpers.LastIndexOfAny(ref MemoryMarshal.GetReference(span), span.Length, ref MemoryMarshal.GetReference(values), values.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int LastIndexOfAny<T>(this ReadOnlySpan<T> span, T value0, T value1) where T : IEquatable<T>
	{
		if (Unsafe.SizeOf<T>() == 1 && RuntimeHelpers.IsBitwiseEquatable<T>())
		{
			return SpanHelpers.LastIndexOfAny(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)), Unsafe.As<T, byte>(ref value0), Unsafe.As<T, byte>(ref value1), span.Length);
		}
		return SpanHelpers.LastIndexOfAny(ref MemoryMarshal.GetReference(span), value0, value1, span.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int LastIndexOfAny<T>(this ReadOnlySpan<T> span, T value0, T value1, T value2) where T : IEquatable<T>
	{
		if (Unsafe.SizeOf<T>() == 1 && RuntimeHelpers.IsBitwiseEquatable<T>())
		{
			return SpanHelpers.LastIndexOfAny(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)), Unsafe.As<T, byte>(ref value0), Unsafe.As<T, byte>(ref value1), Unsafe.As<T, byte>(ref value2), span.Length);
		}
		return SpanHelpers.LastIndexOfAny(ref MemoryMarshal.GetReference(span), value0, value1, value2, span.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int LastIndexOfAny<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> values) where T : IEquatable<T>
	{
		return SpanHelpers.LastIndexOfAny(ref MemoryMarshal.GetReference(span), span.Length, ref MemoryMarshal.GetReference(values), values.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool SequenceEqual<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> other) where T : IEquatable<T>
	{
		int length = span.Length;
		if (RuntimeHelpers.IsBitwiseEquatable<T>())
		{
			nuint num = (nuint)Unsafe.SizeOf<T>();
			if (length == other.Length)
			{
				return SpanHelpers.SequenceEqual(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)), ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(other)), (uint)length * num);
			}
			return false;
		}
		if (length == other.Length)
		{
			return SpanHelpers.SequenceEqual(ref MemoryMarshal.GetReference(span), ref MemoryMarshal.GetReference(other), length);
		}
		return false;
	}

	public static bool SequenceEqual<T>(this Span<T> span, ReadOnlySpan<T> other, IEqualityComparer<T>? comparer = null)
	{
		return ((ReadOnlySpan<T>)span).SequenceEqual(other, comparer);
	}

	public static bool SequenceEqual<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> other, IEqualityComparer<T>? comparer = null)
	{
		if (span.Length != other.Length)
		{
			return false;
		}
		if (typeof(T).IsValueType && (comparer == null || comparer == EqualityComparer<T>.Default))
		{
			if (RuntimeHelpers.IsBitwiseEquatable<T>())
			{
				nuint num = (nuint)Unsafe.SizeOf<T>();
				return SpanHelpers.SequenceEqual(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)), ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(other)), (uint)span.Length * num);
			}
			for (int i = 0; i < span.Length; i++)
			{
				if (!EqualityComparer<T>.Default.Equals(span[i], other[i]))
				{
					return false;
				}
			}
			return true;
		}
		if (comparer == null)
		{
			comparer = EqualityComparer<T>.Default;
		}
		for (int j = 0; j < span.Length; j++)
		{
			if (!comparer.Equals(span[j], other[j]))
			{
				return false;
			}
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int SequenceCompareTo<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> other) where T : IComparable<T>
	{
		if (typeof(T) == typeof(byte))
		{
			return SpanHelpers.SequenceCompareTo(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)), span.Length, ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(other)), other.Length);
		}
		if (typeof(T) == typeof(char))
		{
			return SpanHelpers.SequenceCompareTo(ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(span)), span.Length, ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(other)), other.Length);
		}
		return SpanHelpers.SequenceCompareTo(ref MemoryMarshal.GetReference(span), span.Length, ref MemoryMarshal.GetReference(other), other.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool StartsWith<T>(this Span<T> span, ReadOnlySpan<T> value) where T : IEquatable<T>
	{
		int length = value.Length;
		if (RuntimeHelpers.IsBitwiseEquatable<T>())
		{
			nuint num = (nuint)Unsafe.SizeOf<T>();
			if (length <= span.Length)
			{
				return SpanHelpers.SequenceEqual(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)), ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(value)), (uint)length * num);
			}
			return false;
		}
		if (length <= span.Length)
		{
			return SpanHelpers.SequenceEqual(ref MemoryMarshal.GetReference(span), ref MemoryMarshal.GetReference(value), length);
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool StartsWith<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> value) where T : IEquatable<T>
	{
		int length = value.Length;
		if (RuntimeHelpers.IsBitwiseEquatable<T>())
		{
			nuint num = (nuint)Unsafe.SizeOf<T>();
			if (length <= span.Length)
			{
				return SpanHelpers.SequenceEqual(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)), ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(value)), (uint)length * num);
			}
			return false;
		}
		if (length <= span.Length)
		{
			return SpanHelpers.SequenceEqual(ref MemoryMarshal.GetReference(span), ref MemoryMarshal.GetReference(value), length);
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool EndsWith<T>(this Span<T> span, ReadOnlySpan<T> value) where T : IEquatable<T>
	{
		int length = span.Length;
		int length2 = value.Length;
		if (RuntimeHelpers.IsBitwiseEquatable<T>())
		{
			nuint num = (nuint)Unsafe.SizeOf<T>();
			if (length2 <= length)
			{
				return SpanHelpers.SequenceEqual(ref Unsafe.As<T, byte>(ref Unsafe.Add(ref MemoryMarshal.GetReference(span), (nint)(uint)(length - length2))), ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(value)), (uint)length2 * num);
			}
			return false;
		}
		if (length2 <= length)
		{
			return SpanHelpers.SequenceEqual(ref Unsafe.Add(ref MemoryMarshal.GetReference(span), (nint)(uint)(length - length2)), ref MemoryMarshal.GetReference(value), length2);
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool EndsWith<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> value) where T : IEquatable<T>
	{
		int length = span.Length;
		int length2 = value.Length;
		if (RuntimeHelpers.IsBitwiseEquatable<T>())
		{
			nuint num = (nuint)Unsafe.SizeOf<T>();
			if (length2 <= length)
			{
				return SpanHelpers.SequenceEqual(ref Unsafe.As<T, byte>(ref Unsafe.Add(ref MemoryMarshal.GetReference(span), (nint)(uint)(length - length2))), ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(value)), (uint)length2 * num);
			}
			return false;
		}
		if (length2 <= length)
		{
			return SpanHelpers.SequenceEqual(ref Unsafe.Add(ref MemoryMarshal.GetReference(span), (nint)(uint)(length - length2)), ref MemoryMarshal.GetReference(value), length2);
		}
		return false;
	}

	public static void Reverse<T>(this Span<T> span)
	{
		if (span.Length > 1)
		{
			ref T reference = ref MemoryMarshal.GetReference(span);
			ref T reference2 = ref Unsafe.Add(ref Unsafe.Add(ref reference, span.Length), -1);
			do
			{
				T val = reference;
				reference = reference2;
				reference2 = val;
				reference = ref Unsafe.Add(ref reference, 1);
				reference2 = ref Unsafe.Add(ref reference2, -1);
			}
			while (Unsafe.IsAddressLessThan(ref reference, ref reference2));
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Span<T> AsSpan<T>(this T[]? array)
	{
		return new Span<T>(array);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Span<T> AsSpan<T>(this T[]? array, int start, int length)
	{
		return new Span<T>(array, start, length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Span<T> AsSpan<T>(this ArraySegment<T> segment)
	{
		return new Span<T>(segment.Array, segment.Offset, segment.Count);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Span<T> AsSpan<T>(this ArraySegment<T> segment, int start)
	{
		if ((uint)start > (uint)segment.Count)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
		}
		return new Span<T>(segment.Array, segment.Offset + start, segment.Count - start);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Span<T> AsSpan<T>(this ArraySegment<T> segment, Index startIndex)
	{
		int offset = startIndex.GetOffset(segment.Count);
		return segment.AsSpan(offset);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Span<T> AsSpan<T>(this ArraySegment<T> segment, int start, int length)
	{
		if ((uint)start > (uint)segment.Count)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
		}
		if ((uint)length > (uint)(segment.Count - start))
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length);
		}
		return new Span<T>(segment.Array, segment.Offset + start, length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Span<T> AsSpan<T>(this ArraySegment<T> segment, Range range)
	{
		var (num, length) = range.GetOffsetAndLength(segment.Count);
		return new Span<T>(segment.Array, segment.Offset + num, length);
	}

	public static Memory<T> AsMemory<T>(this T[]? array)
	{
		return new Memory<T>(array);
	}

	public static Memory<T> AsMemory<T>(this T[]? array, int start)
	{
		return new Memory<T>(array, start);
	}

	public static Memory<T> AsMemory<T>(this T[]? array, Index startIndex)
	{
		if (array == null)
		{
			if (!startIndex.Equals(Index.Start))
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
			}
			return default(Memory<T>);
		}
		int offset = startIndex.GetOffset(array.Length);
		return new Memory<T>(array, offset);
	}

	public static Memory<T> AsMemory<T>(this T[]? array, int start, int length)
	{
		return new Memory<T>(array, start, length);
	}

	public static Memory<T> AsMemory<T>(this T[]? array, Range range)
	{
		if (array == null)
		{
			Index start = range.Start;
			Index end = range.End;
			if (!start.Equals(Index.Start) || !end.Equals(Index.Start))
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
			}
			return default(Memory<T>);
		}
		var (start2, length) = range.GetOffsetAndLength(array.Length);
		return new Memory<T>(array, start2, length);
	}

	public static Memory<T> AsMemory<T>(this ArraySegment<T> segment)
	{
		return new Memory<T>(segment.Array, segment.Offset, segment.Count);
	}

	public static Memory<T> AsMemory<T>(this ArraySegment<T> segment, int start)
	{
		if ((uint)start > (uint)segment.Count)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
		}
		return new Memory<T>(segment.Array, segment.Offset + start, segment.Count - start);
	}

	public static Memory<T> AsMemory<T>(this ArraySegment<T> segment, int start, int length)
	{
		if ((uint)start > (uint)segment.Count)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
		}
		if ((uint)length > (uint)(segment.Count - start))
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length);
		}
		return new Memory<T>(segment.Array, segment.Offset + start, length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void CopyTo<T>(this T[]? source, Span<T> destination)
	{
		new ReadOnlySpan<T>(source).CopyTo(destination);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void CopyTo<T>(this T[]? source, Memory<T> destination)
	{
		source.CopyTo(destination.Span);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool Overlaps<T>(this Span<T> span, ReadOnlySpan<T> other)
	{
		return ((ReadOnlySpan<T>)span).Overlaps(other);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool Overlaps<T>(this Span<T> span, ReadOnlySpan<T> other, out int elementOffset)
	{
		return ((ReadOnlySpan<T>)span).Overlaps(other, out elementOffset);
	}

	public static bool Overlaps<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> other)
	{
		if (span.IsEmpty || other.IsEmpty)
		{
			return false;
		}
		IntPtr intPtr = Unsafe.ByteOffset(ref MemoryMarshal.GetReference(span), ref MemoryMarshal.GetReference(other));
		if (Unsafe.SizeOf<IntPtr>() == 4)
		{
			if ((uint)(int)intPtr >= (uint)(span.Length * Unsafe.SizeOf<T>()))
			{
				return (uint)(int)intPtr > (uint)(-(other.Length * Unsafe.SizeOf<T>()));
			}
			return true;
		}
		if ((ulong)(long)intPtr >= (ulong)((long)span.Length * (long)Unsafe.SizeOf<T>()))
		{
			return (ulong)(long)intPtr > (ulong)(-((long)other.Length * (long)Unsafe.SizeOf<T>()));
		}
		return true;
	}

	public static bool Overlaps<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> other, out int elementOffset)
	{
		if (span.IsEmpty || other.IsEmpty)
		{
			elementOffset = 0;
			return false;
		}
		IntPtr intPtr = Unsafe.ByteOffset(ref MemoryMarshal.GetReference(span), ref MemoryMarshal.GetReference(other));
		if (Unsafe.SizeOf<IntPtr>() == 4)
		{
			if ((uint)(int)intPtr < (uint)(span.Length * Unsafe.SizeOf<T>()) || (uint)(int)intPtr > (uint)(-(other.Length * Unsafe.SizeOf<T>())))
			{
				if ((int)intPtr % Unsafe.SizeOf<T>() != 0)
				{
					ThrowHelper.ThrowArgumentException_OverlapAlignmentMismatch();
				}
				elementOffset = (int)intPtr / Unsafe.SizeOf<T>();
				return true;
			}
			elementOffset = 0;
			return false;
		}
		if ((ulong)(long)intPtr < (ulong)((long)span.Length * (long)Unsafe.SizeOf<T>()) || (ulong)(long)intPtr > (ulong)(-((long)other.Length * (long)Unsafe.SizeOf<T>())))
		{
			if ((long)intPtr % Unsafe.SizeOf<T>() != 0L)
			{
				ThrowHelper.ThrowArgumentException_OverlapAlignmentMismatch();
			}
			elementOffset = (int)((long)intPtr / Unsafe.SizeOf<T>());
			return true;
		}
		elementOffset = 0;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int BinarySearch<T>(this Span<T> span, IComparable<T> comparable)
	{
		return span.BinarySearch<T, IComparable<T>>(comparable);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int BinarySearch<T, TComparable>(this Span<T> span, TComparable comparable) where TComparable : IComparable<T>
	{
		return BinarySearch((ReadOnlySpan<T>)span, comparable);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int BinarySearch<T, TComparer>(this Span<T> span, T value, TComparer comparer) where TComparer : IComparer<T>
	{
		return ((ReadOnlySpan<T>)span).BinarySearch(value, comparer);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int BinarySearch<T>(this ReadOnlySpan<T> span, IComparable<T> comparable)
	{
		return MemoryExtensions.BinarySearch<T, IComparable<T>>(span, comparable);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int BinarySearch<T, TComparable>(this ReadOnlySpan<T> span, TComparable comparable) where TComparable : IComparable<T>
	{
		return SpanHelpers.BinarySearch(span, comparable);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int BinarySearch<T, TComparer>(this ReadOnlySpan<T> span, T value, TComparer comparer) where TComparer : IComparer<T>
	{
		if (comparer == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.comparer);
		}
		SpanHelpers.ComparerComparable<T, TComparer> comparable = new SpanHelpers.ComparerComparable<T, TComparer>(value, comparer);
		return BinarySearch(span, comparable);
	}

	public static void Sort<T>(this Span<T> span)
	{
		span.Sort((IComparer<T>)null);
	}

	public static void Sort<T, TComparer>(this Span<T> span, TComparer comparer) where TComparer : IComparer<T>?
	{
		if (span.Length > 1)
		{
			ArraySortHelper<T>.Default.Sort(span, comparer);
		}
	}

	public static void Sort<T>(this Span<T> span, Comparison<T> comparison)
	{
		if (comparison == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.comparison);
		}
		if (span.Length > 1)
		{
			ArraySortHelper<T>.Sort(span, comparison);
		}
	}

	public static void Sort<TKey, TValue>(this Span<TKey> keys, Span<TValue> items)
	{
		keys.Sort<TKey, TValue, IComparer<TKey>>(items, null);
	}

	public static void Sort<TKey, TValue, TComparer>(this Span<TKey> keys, Span<TValue> items, TComparer comparer) where TComparer : IComparer<TKey>?
	{
		if (keys.Length != items.Length)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_SpansMustHaveSameLength);
		}
		if (keys.Length > 1)
		{
			ArraySortHelper<TKey, TValue>.Default.Sort(keys, items, comparer);
		}
	}

	public static void Sort<TKey, TValue>(this Span<TKey> keys, Span<TValue> items, Comparison<TKey> comparison)
	{
		if (comparison == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.comparison);
		}
		if (keys.Length != items.Length)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_SpansMustHaveSameLength);
		}
		if (keys.Length > 1)
		{
			ArraySortHelper<TKey, TValue>.Default.Sort(keys, items, new ComparisonComparer<TKey>(comparison));
		}
	}

	public static bool TryWrite(this Span<char> destination, [InterpolatedStringHandlerArgument("destination")] ref TryWriteInterpolatedStringHandler handler, out int charsWritten)
	{
		if (handler._success)
		{
			charsWritten = handler._pos;
			return true;
		}
		charsWritten = 0;
		return false;
	}

	public static bool TryWrite(this Span<char> destination, IFormatProvider? provider, [InterpolatedStringHandlerArgument(new string[] { "destination", "provider" })] ref TryWriteInterpolatedStringHandler handler, out int charsWritten)
	{
		return destination.TryWrite(ref handler, out charsWritten);
	}

	public static bool IsWhiteSpace(this ReadOnlySpan<char> span)
	{
		for (int i = 0; i < span.Length; i++)
		{
			if (!char.IsWhiteSpace(span[i]))
			{
				return false;
			}
		}
		return true;
	}

	public static bool Contains(this ReadOnlySpan<char> span, ReadOnlySpan<char> value, StringComparison comparisonType)
	{
		return span.IndexOf(value, comparisonType) >= 0;
	}

	public static bool Equals(this ReadOnlySpan<char> span, ReadOnlySpan<char> other, StringComparison comparisonType)
	{
		string.CheckStringComparison(comparisonType);
		switch (comparisonType)
		{
		case StringComparison.CurrentCulture:
		case StringComparison.CurrentCultureIgnoreCase:
			return CultureInfo.CurrentCulture.CompareInfo.Compare(span, other, string.GetCaseCompareOfComparisonCulture(comparisonType)) == 0;
		case StringComparison.InvariantCulture:
		case StringComparison.InvariantCultureIgnoreCase:
			return CompareInfo.Invariant.Compare(span, other, string.GetCaseCompareOfComparisonCulture(comparisonType)) == 0;
		case StringComparison.Ordinal:
			return span.EqualsOrdinal(other);
		default:
			return span.EqualsOrdinalIgnoreCase(other);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool EqualsOrdinal(this ReadOnlySpan<char> span, ReadOnlySpan<char> value)
	{
		if (span.Length != value.Length)
		{
			return false;
		}
		if (value.Length == 0)
		{
			return true;
		}
		return span.SequenceEqual(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool EqualsOrdinalIgnoreCase(this ReadOnlySpan<char> span, ReadOnlySpan<char> value)
	{
		if (span.Length != value.Length)
		{
			return false;
		}
		if (value.Length == 0)
		{
			return true;
		}
		return Ordinal.EqualsIgnoreCase(ref MemoryMarshal.GetReference(span), ref MemoryMarshal.GetReference(value), span.Length);
	}

	public static int CompareTo(this ReadOnlySpan<char> span, ReadOnlySpan<char> other, StringComparison comparisonType)
	{
		string.CheckStringComparison(comparisonType);
		switch (comparisonType)
		{
		case StringComparison.CurrentCulture:
		case StringComparison.CurrentCultureIgnoreCase:
			return CultureInfo.CurrentCulture.CompareInfo.Compare(span, other, string.GetCaseCompareOfComparisonCulture(comparisonType));
		case StringComparison.InvariantCulture:
		case StringComparison.InvariantCultureIgnoreCase:
			return CompareInfo.Invariant.Compare(span, other, string.GetCaseCompareOfComparisonCulture(comparisonType));
		case StringComparison.Ordinal:
			if (span.Length == 0 || other.Length == 0)
			{
				return span.Length - other.Length;
			}
			return string.CompareOrdinal(span, other);
		default:
			return Ordinal.CompareStringIgnoreCase(ref MemoryMarshal.GetReference(span), span.Length, ref MemoryMarshal.GetReference(other), other.Length);
		}
	}

	public static int IndexOf(this ReadOnlySpan<char> span, ReadOnlySpan<char> value, StringComparison comparisonType)
	{
		string.CheckStringComparison(comparisonType);
		switch (comparisonType)
		{
		case StringComparison.Ordinal:
			return SpanHelpers.IndexOf(ref MemoryMarshal.GetReference(span), span.Length, ref MemoryMarshal.GetReference(value), value.Length);
		case StringComparison.CurrentCulture:
		case StringComparison.CurrentCultureIgnoreCase:
			return CultureInfo.CurrentCulture.CompareInfo.IndexOf(span, value, string.GetCaseCompareOfComparisonCulture(comparisonType));
		case StringComparison.InvariantCulture:
		case StringComparison.InvariantCultureIgnoreCase:
			return CompareInfo.Invariant.IndexOf(span, value, string.GetCaseCompareOfComparisonCulture(comparisonType));
		default:
			return Ordinal.IndexOfOrdinalIgnoreCase(span, value);
		}
	}

	public static int LastIndexOf(this ReadOnlySpan<char> span, ReadOnlySpan<char> value, StringComparison comparisonType)
	{
		string.CheckStringComparison(comparisonType);
		switch (comparisonType)
		{
		case StringComparison.Ordinal:
			return SpanHelpers.LastIndexOf(ref MemoryMarshal.GetReference(span), span.Length, ref MemoryMarshal.GetReference(value), value.Length);
		case StringComparison.CurrentCulture:
		case StringComparison.CurrentCultureIgnoreCase:
			return CultureInfo.CurrentCulture.CompareInfo.LastIndexOf(span, value, string.GetCaseCompareOfComparisonCulture(comparisonType));
		case StringComparison.InvariantCulture:
		case StringComparison.InvariantCultureIgnoreCase:
			return CompareInfo.Invariant.LastIndexOf(span, value, string.GetCaseCompareOfComparisonCulture(comparisonType));
		default:
			return Ordinal.LastIndexOfOrdinalIgnoreCase(span, value);
		}
	}

	public static int ToLower(this ReadOnlySpan<char> source, Span<char> destination, CultureInfo? culture)
	{
		if (source.Overlaps(destination))
		{
			throw new InvalidOperationException(SR.InvalidOperation_SpanOverlappedOperation);
		}
		if (culture == null)
		{
			culture = CultureInfo.CurrentCulture;
		}
		if (destination.Length < source.Length)
		{
			return -1;
		}
		if (GlobalizationMode.Invariant)
		{
			InvariantModeCasing.ToLower(source, destination);
		}
		else
		{
			culture.TextInfo.ChangeCaseToLower(source, destination);
		}
		return source.Length;
	}

	public static int ToLowerInvariant(this ReadOnlySpan<char> source, Span<char> destination)
	{
		if (source.Overlaps(destination))
		{
			throw new InvalidOperationException(SR.InvalidOperation_SpanOverlappedOperation);
		}
		if (destination.Length < source.Length)
		{
			return -1;
		}
		if (GlobalizationMode.Invariant)
		{
			InvariantModeCasing.ToLower(source, destination);
		}
		else
		{
			TextInfo.Invariant.ChangeCaseToLower(source, destination);
		}
		return source.Length;
	}

	public static int ToUpper(this ReadOnlySpan<char> source, Span<char> destination, CultureInfo? culture)
	{
		if (source.Overlaps(destination))
		{
			throw new InvalidOperationException(SR.InvalidOperation_SpanOverlappedOperation);
		}
		if (culture == null)
		{
			culture = CultureInfo.CurrentCulture;
		}
		if (destination.Length < source.Length)
		{
			return -1;
		}
		if (GlobalizationMode.Invariant)
		{
			InvariantModeCasing.ToUpper(source, destination);
		}
		else
		{
			culture.TextInfo.ChangeCaseToUpper(source, destination);
		}
		return source.Length;
	}

	public static int ToUpperInvariant(this ReadOnlySpan<char> source, Span<char> destination)
	{
		if (source.Overlaps(destination))
		{
			throw new InvalidOperationException(SR.InvalidOperation_SpanOverlappedOperation);
		}
		if (destination.Length < source.Length)
		{
			return -1;
		}
		if (GlobalizationMode.Invariant)
		{
			InvariantModeCasing.ToUpper(source, destination);
		}
		else
		{
			TextInfo.Invariant.ChangeCaseToUpper(source, destination);
		}
		return source.Length;
	}

	public static bool EndsWith(this ReadOnlySpan<char> span, ReadOnlySpan<char> value, StringComparison comparisonType)
	{
		string.CheckStringComparison(comparisonType);
		switch (comparisonType)
		{
		case StringComparison.CurrentCulture:
		case StringComparison.CurrentCultureIgnoreCase:
			return CultureInfo.CurrentCulture.CompareInfo.IsSuffix(span, value, string.GetCaseCompareOfComparisonCulture(comparisonType));
		case StringComparison.InvariantCulture:
		case StringComparison.InvariantCultureIgnoreCase:
			return CompareInfo.Invariant.IsSuffix(span, value, string.GetCaseCompareOfComparisonCulture(comparisonType));
		case StringComparison.Ordinal:
			return span.EndsWith(value);
		default:
			return span.EndsWithOrdinalIgnoreCase(value);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool EndsWithOrdinalIgnoreCase(this ReadOnlySpan<char> span, ReadOnlySpan<char> value)
	{
		if (value.Length <= span.Length)
		{
			return Ordinal.EqualsIgnoreCase(ref Unsafe.Add(ref MemoryMarshal.GetReference(span), span.Length - value.Length), ref MemoryMarshal.GetReference(value), value.Length);
		}
		return false;
	}

	public static bool StartsWith(this ReadOnlySpan<char> span, ReadOnlySpan<char> value, StringComparison comparisonType)
	{
		string.CheckStringComparison(comparisonType);
		switch (comparisonType)
		{
		case StringComparison.CurrentCulture:
		case StringComparison.CurrentCultureIgnoreCase:
			return CultureInfo.CurrentCulture.CompareInfo.IsPrefix(span, value, string.GetCaseCompareOfComparisonCulture(comparisonType));
		case StringComparison.InvariantCulture:
		case StringComparison.InvariantCultureIgnoreCase:
			return CompareInfo.Invariant.IsPrefix(span, value, string.GetCaseCompareOfComparisonCulture(comparisonType));
		case StringComparison.Ordinal:
			return span.StartsWith(value);
		default:
			return span.StartsWithOrdinalIgnoreCase(value);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool StartsWithOrdinalIgnoreCase(this ReadOnlySpan<char> span, ReadOnlySpan<char> value)
	{
		if (value.Length <= span.Length)
		{
			return Ordinal.EqualsIgnoreCase(ref MemoryMarshal.GetReference(span), ref MemoryMarshal.GetReference(value), value.Length);
		}
		return false;
	}

	public static SpanRuneEnumerator EnumerateRunes(this ReadOnlySpan<char> span)
	{
		return new SpanRuneEnumerator(span);
	}

	public static SpanRuneEnumerator EnumerateRunes(this Span<char> span)
	{
		return new SpanRuneEnumerator(span);
	}

	public static SpanLineEnumerator EnumerateLines(this ReadOnlySpan<char> span)
	{
		return new SpanLineEnumerator(span);
	}

	public static SpanLineEnumerator EnumerateLines(this Span<char> span)
	{
		return new SpanLineEnumerator(span);
	}

	public static Memory<T> Trim<T>(this Memory<T> memory, T trimElement) where T : IEquatable<T>
	{
		ReadOnlySpan<T> span = memory.Span;
		int start = ClampStart(span, trimElement);
		int length = ClampEnd(span, start, trimElement);
		return memory.Slice(start, length);
	}

	public static Memory<T> TrimStart<T>(this Memory<T> memory, T trimElement) where T : IEquatable<T>
	{
		return memory.Slice(ClampStart(memory.Span, trimElement));
	}

	public static Memory<T> TrimEnd<T>(this Memory<T> memory, T trimElement) where T : IEquatable<T>
	{
		return memory.Slice(0, ClampEnd(memory.Span, 0, trimElement));
	}

	public static ReadOnlyMemory<T> Trim<T>(this ReadOnlyMemory<T> memory, T trimElement) where T : IEquatable<T>
	{
		ReadOnlySpan<T> span = memory.Span;
		int start = ClampStart(span, trimElement);
		int length = ClampEnd(span, start, trimElement);
		return memory.Slice(start, length);
	}

	public static ReadOnlyMemory<T> TrimStart<T>(this ReadOnlyMemory<T> memory, T trimElement) where T : IEquatable<T>
	{
		return memory.Slice(ClampStart(memory.Span, trimElement));
	}

	public static ReadOnlyMemory<T> TrimEnd<T>(this ReadOnlyMemory<T> memory, T trimElement) where T : IEquatable<T>
	{
		return memory.Slice(0, ClampEnd(memory.Span, 0, trimElement));
	}

	public static Span<T> Trim<T>(this Span<T> span, T trimElement) where T : IEquatable<T>
	{
		int start = ClampStart(span, trimElement);
		int length = ClampEnd(span, start, trimElement);
		return span.Slice(start, length);
	}

	public static Span<T> TrimStart<T>(this Span<T> span, T trimElement) where T : IEquatable<T>
	{
		return span.Slice(ClampStart(span, trimElement));
	}

	public static Span<T> TrimEnd<T>(this Span<T> span, T trimElement) where T : IEquatable<T>
	{
		return span.Slice(0, ClampEnd(span, 0, trimElement));
	}

	public static ReadOnlySpan<T> Trim<T>(this ReadOnlySpan<T> span, T trimElement) where T : IEquatable<T>
	{
		int start = ClampStart(span, trimElement);
		int length = ClampEnd(span, start, trimElement);
		return span.Slice(start, length);
	}

	public static ReadOnlySpan<T> TrimStart<T>(this ReadOnlySpan<T> span, T trimElement) where T : IEquatable<T>
	{
		return span.Slice(ClampStart(span, trimElement));
	}

	public static ReadOnlySpan<T> TrimEnd<T>(this ReadOnlySpan<T> span, T trimElement) where T : IEquatable<T>
	{
		return span.Slice(0, ClampEnd(span, 0, trimElement));
	}

	private static int ClampStart<T>(ReadOnlySpan<T> span, T trimElement) where T : IEquatable<T>
	{
		int i = 0;
		if (trimElement != null)
		{
			for (; i < span.Length && trimElement.Equals(span[i]); i++)
			{
			}
		}
		else
		{
			for (; i < span.Length && span[i] == null; i++)
			{
			}
		}
		return i;
	}

	private static int ClampEnd<T>(ReadOnlySpan<T> span, int start, T trimElement) where T : IEquatable<T>
	{
		int num = span.Length - 1;
		if (trimElement != null)
		{
			while (num >= start && trimElement.Equals(span[num]))
			{
				num--;
			}
		}
		else
		{
			while (num >= start && span[num] == null)
			{
				num--;
			}
		}
		return num - start + 1;
	}

	public static Memory<T> Trim<T>(this Memory<T> memory, ReadOnlySpan<T> trimElements) where T : IEquatable<T>
	{
		if (trimElements.Length > 1)
		{
			ReadOnlySpan<T> span = memory.Span;
			int start = ClampStart<T>(span, trimElements);
			int length = ClampEnd<T>(span, start, trimElements);
			return memory.Slice(start, length);
		}
		if (trimElements.Length == 1)
		{
			return memory.Trim(trimElements[0]);
		}
		return memory;
	}

	public static Memory<T> TrimStart<T>(this Memory<T> memory, ReadOnlySpan<T> trimElements) where T : IEquatable<T>
	{
		if (trimElements.Length > 1)
		{
			return memory.Slice(ClampStart(memory.Span, trimElements));
		}
		if (trimElements.Length == 1)
		{
			return memory.TrimStart(trimElements[0]);
		}
		return memory;
	}

	public static Memory<T> TrimEnd<T>(this Memory<T> memory, ReadOnlySpan<T> trimElements) where T : IEquatable<T>
	{
		if (trimElements.Length > 1)
		{
			return memory.Slice(0, ClampEnd(memory.Span, 0, trimElements));
		}
		if (trimElements.Length == 1)
		{
			return memory.TrimEnd(trimElements[0]);
		}
		return memory;
	}

	public static ReadOnlyMemory<T> Trim<T>(this ReadOnlyMemory<T> memory, ReadOnlySpan<T> trimElements) where T : IEquatable<T>
	{
		if (trimElements.Length > 1)
		{
			ReadOnlySpan<T> span = memory.Span;
			int start = ClampStart<T>(span, trimElements);
			int length = ClampEnd<T>(span, start, trimElements);
			return memory.Slice(start, length);
		}
		if (trimElements.Length == 1)
		{
			return memory.Trim(trimElements[0]);
		}
		return memory;
	}

	public static ReadOnlyMemory<T> TrimStart<T>(this ReadOnlyMemory<T> memory, ReadOnlySpan<T> trimElements) where T : IEquatable<T>
	{
		if (trimElements.Length > 1)
		{
			return memory.Slice(ClampStart(memory.Span, trimElements));
		}
		if (trimElements.Length == 1)
		{
			return memory.TrimStart(trimElements[0]);
		}
		return memory;
	}

	public static ReadOnlyMemory<T> TrimEnd<T>(this ReadOnlyMemory<T> memory, ReadOnlySpan<T> trimElements) where T : IEquatable<T>
	{
		if (trimElements.Length > 1)
		{
			return memory.Slice(0, ClampEnd(memory.Span, 0, trimElements));
		}
		if (trimElements.Length == 1)
		{
			return memory.TrimEnd(trimElements[0]);
		}
		return memory;
	}

	public static Span<T> Trim<T>(this Span<T> span, ReadOnlySpan<T> trimElements) where T : IEquatable<T>
	{
		if (trimElements.Length > 1)
		{
			int start = ClampStart(span, trimElements);
			int length = ClampEnd(span, start, trimElements);
			return span.Slice(start, length);
		}
		if (trimElements.Length == 1)
		{
			return span.Trim(trimElements[0]);
		}
		return span;
	}

	public static Span<T> TrimStart<T>(this Span<T> span, ReadOnlySpan<T> trimElements) where T : IEquatable<T>
	{
		if (trimElements.Length > 1)
		{
			return span.Slice(ClampStart(span, trimElements));
		}
		if (trimElements.Length == 1)
		{
			return span.TrimStart(trimElements[0]);
		}
		return span;
	}

	public static Span<T> TrimEnd<T>(this Span<T> span, ReadOnlySpan<T> trimElements) where T : IEquatable<T>
	{
		if (trimElements.Length > 1)
		{
			return span.Slice(0, ClampEnd(span, 0, trimElements));
		}
		if (trimElements.Length == 1)
		{
			return span.TrimEnd(trimElements[0]);
		}
		return span;
	}

	public static ReadOnlySpan<T> Trim<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> trimElements) where T : IEquatable<T>
	{
		if (trimElements.Length > 1)
		{
			int start = ClampStart(span, trimElements);
			int length = ClampEnd(span, start, trimElements);
			return span.Slice(start, length);
		}
		if (trimElements.Length == 1)
		{
			return span.Trim(trimElements[0]);
		}
		return span;
	}

	public static ReadOnlySpan<T> TrimStart<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> trimElements) where T : IEquatable<T>
	{
		if (trimElements.Length > 1)
		{
			return span.Slice(ClampStart(span, trimElements));
		}
		if (trimElements.Length == 1)
		{
			return span.TrimStart(trimElements[0]);
		}
		return span;
	}

	public static ReadOnlySpan<T> TrimEnd<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> trimElements) where T : IEquatable<T>
	{
		if (trimElements.Length > 1)
		{
			return span.Slice(0, ClampEnd(span, 0, trimElements));
		}
		if (trimElements.Length == 1)
		{
			return span.TrimEnd(trimElements[0]);
		}
		return span;
	}

	private static int ClampStart<T>(ReadOnlySpan<T> span, ReadOnlySpan<T> trimElements) where T : IEquatable<T>
	{
		int i;
		for (i = 0; i < span.Length && trimElements.Contains(span[i]); i++)
		{
		}
		return i;
	}

	private static int ClampEnd<T>(ReadOnlySpan<T> span, int start, ReadOnlySpan<T> trimElements) where T : IEquatable<T>
	{
		int num = span.Length - 1;
		while (num >= start && trimElements.Contains(span[num]))
		{
			num--;
		}
		return num - start + 1;
	}

	public static Memory<char> Trim(this Memory<char> memory)
	{
		ReadOnlySpan<char> span = memory.Span;
		int start = ClampStart(span);
		int length = ClampEnd(span, start);
		return memory.Slice(start, length);
	}

	public static Memory<char> TrimStart(this Memory<char> memory)
	{
		return memory.Slice(ClampStart(memory.Span));
	}

	public static Memory<char> TrimEnd(this Memory<char> memory)
	{
		return memory.Slice(0, ClampEnd(memory.Span, 0));
	}

	public static ReadOnlyMemory<char> Trim(this ReadOnlyMemory<char> memory)
	{
		ReadOnlySpan<char> span = memory.Span;
		int start = ClampStart(span);
		int length = ClampEnd(span, start);
		return memory.Slice(start, length);
	}

	public static ReadOnlyMemory<char> TrimStart(this ReadOnlyMemory<char> memory)
	{
		return memory.Slice(ClampStart(memory.Span));
	}

	public static ReadOnlyMemory<char> TrimEnd(this ReadOnlyMemory<char> memory)
	{
		return memory.Slice(0, ClampEnd(memory.Span, 0));
	}

	public static ReadOnlySpan<char> Trim(this ReadOnlySpan<char> span)
	{
		int i;
		for (i = 0; i < span.Length && char.IsWhiteSpace(span[i]); i++)
		{
		}
		int num = span.Length - 1;
		while (num > i && char.IsWhiteSpace(span[num]))
		{
			num--;
		}
		return span.Slice(i, num - i + 1);
	}

	public static ReadOnlySpan<char> TrimStart(this ReadOnlySpan<char> span)
	{
		int i;
		for (i = 0; i < span.Length && char.IsWhiteSpace(span[i]); i++)
		{
		}
		return span.Slice(i);
	}

	public static ReadOnlySpan<char> TrimEnd(this ReadOnlySpan<char> span)
	{
		int num = span.Length - 1;
		while (num >= 0 && char.IsWhiteSpace(span[num]))
		{
			num--;
		}
		return span.Slice(0, num + 1);
	}

	public static ReadOnlySpan<char> Trim(this ReadOnlySpan<char> span, char trimChar)
	{
		int i;
		for (i = 0; i < span.Length && span[i] == trimChar; i++)
		{
		}
		int num = span.Length - 1;
		while (num > i && span[num] == trimChar)
		{
			num--;
		}
		return span.Slice(i, num - i + 1);
	}

	public static ReadOnlySpan<char> TrimStart(this ReadOnlySpan<char> span, char trimChar)
	{
		int i;
		for (i = 0; i < span.Length && span[i] == trimChar; i++)
		{
		}
		return span.Slice(i);
	}

	public static ReadOnlySpan<char> TrimEnd(this ReadOnlySpan<char> span, char trimChar)
	{
		int num = span.Length - 1;
		while (num >= 0 && span[num] == trimChar)
		{
			num--;
		}
		return span.Slice(0, num + 1);
	}

	public static ReadOnlySpan<char> Trim(this ReadOnlySpan<char> span, ReadOnlySpan<char> trimChars)
	{
		return span.TrimStart(trimChars).TrimEnd(trimChars);
	}

	public static ReadOnlySpan<char> TrimStart(this ReadOnlySpan<char> span, ReadOnlySpan<char> trimChars)
	{
		if (trimChars.IsEmpty)
		{
			return span.TrimStart();
		}
		int i;
		for (i = 0; i < span.Length; i++)
		{
			int num = 0;
			while (num < trimChars.Length)
			{
				if (span[i] != trimChars[num])
				{
					num++;
					continue;
				}
				goto IL_003c;
			}
			break;
			IL_003c:;
		}
		return span.Slice(i);
	}

	public static ReadOnlySpan<char> TrimEnd(this ReadOnlySpan<char> span, ReadOnlySpan<char> trimChars)
	{
		if (trimChars.IsEmpty)
		{
			return span.TrimEnd();
		}
		int num;
		for (num = span.Length - 1; num >= 0; num--)
		{
			int num2 = 0;
			while (num2 < trimChars.Length)
			{
				if (span[num] != trimChars[num2])
				{
					num2++;
					continue;
				}
				goto IL_0044;
			}
			break;
			IL_0044:;
		}
		return span.Slice(0, num + 1);
	}

	public static Span<char> Trim(this Span<char> span)
	{
		int start = ClampStart(span);
		int length = ClampEnd(span, start);
		return span.Slice(start, length);
	}

	public static Span<char> TrimStart(this Span<char> span)
	{
		return span.Slice(ClampStart(span));
	}

	public static Span<char> TrimEnd(this Span<char> span)
	{
		return span.Slice(0, ClampEnd(span, 0));
	}

	private static int ClampStart(ReadOnlySpan<char> span)
	{
		int i;
		for (i = 0; i < span.Length && char.IsWhiteSpace(span[i]); i++)
		{
		}
		return i;
	}

	private static int ClampEnd(ReadOnlySpan<char> span, int start)
	{
		int num = span.Length - 1;
		while (num >= start && char.IsWhiteSpace(span[num]))
		{
			num--;
		}
		return num - start + 1;
	}
}

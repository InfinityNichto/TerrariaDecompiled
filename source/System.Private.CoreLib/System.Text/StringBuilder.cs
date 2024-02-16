using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Internal.Runtime.CompilerServices;

namespace System.Text;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class StringBuilder : ISerializable
{
	public struct ChunkEnumerator
	{
		private sealed class ManyChunkInfo
		{
			private readonly StringBuilder[] _chunks;

			private int _chunkPos;

			public bool MoveNext(ref StringBuilder current)
			{
				int num = ++_chunkPos;
				if (_chunks.Length <= num)
				{
					return false;
				}
				current = _chunks[num];
				return true;
			}

			public ManyChunkInfo(StringBuilder stringBuilder, int chunkCount)
			{
				_chunks = new StringBuilder[chunkCount];
				while (0 <= --chunkCount)
				{
					_chunks[chunkCount] = stringBuilder;
					stringBuilder = stringBuilder.m_ChunkPrevious;
				}
				_chunkPos = -1;
			}
		}

		private readonly StringBuilder _firstChunk;

		private StringBuilder _currentChunk;

		private readonly ManyChunkInfo _manyChunks;

		public ReadOnlyMemory<char> Current
		{
			get
			{
				if (_currentChunk == null)
				{
					ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumOpCantHappen();
				}
				return new ReadOnlyMemory<char>(_currentChunk.m_ChunkChars, 0, _currentChunk.m_ChunkLength);
			}
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public ChunkEnumerator GetEnumerator()
		{
			return this;
		}

		public bool MoveNext()
		{
			if (_currentChunk == _firstChunk)
			{
				return false;
			}
			if (_manyChunks != null)
			{
				return _manyChunks.MoveNext(ref _currentChunk);
			}
			StringBuilder stringBuilder = _firstChunk;
			while (stringBuilder.m_ChunkPrevious != _currentChunk)
			{
				stringBuilder = stringBuilder.m_ChunkPrevious;
			}
			_currentChunk = stringBuilder;
			return true;
		}

		internal ChunkEnumerator(StringBuilder stringBuilder)
		{
			_firstChunk = stringBuilder;
			_currentChunk = null;
			_manyChunks = null;
			int num = ChunkCount(stringBuilder);
			if (8 < num)
			{
				_manyChunks = new ManyChunkInfo(stringBuilder, num);
			}
		}

		private static int ChunkCount(StringBuilder stringBuilder)
		{
			int num = 0;
			while (stringBuilder != null)
			{
				num++;
				stringBuilder = stringBuilder.m_ChunkPrevious;
			}
			return num;
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	[InterpolatedStringHandler]
	public struct AppendInterpolatedStringHandler
	{
		internal readonly StringBuilder _stringBuilder;

		private readonly IFormatProvider _provider;

		private readonly bool _hasCustomFormatter;

		public AppendInterpolatedStringHandler(int literalLength, int formattedCount, StringBuilder stringBuilder)
		{
			_stringBuilder = stringBuilder;
			_provider = null;
			_hasCustomFormatter = false;
		}

		public AppendInterpolatedStringHandler(int literalLength, int formattedCount, StringBuilder stringBuilder, IFormatProvider? provider)
		{
			_stringBuilder = stringBuilder;
			_provider = provider;
			_hasCustomFormatter = provider != null && DefaultInterpolatedStringHandler.HasCustomFormatter(provider);
		}

		public void AppendLiteral(string value)
		{
			_stringBuilder.Append(value);
		}

		public void AppendFormatted<T>(T value)
		{
			if (_hasCustomFormatter)
			{
				AppendCustomFormatter(value, null);
			}
			else if (value is IFormattable)
			{
				if (value is ISpanFormattable)
				{
					Span<char> remainingCurrentChunk = _stringBuilder.RemainingCurrentChunk;
					if (((ISpanFormattable)(object)value).TryFormat(remainingCurrentChunk, out var charsWritten, default(ReadOnlySpan<char>), _provider))
					{
						if ((uint)charsWritten > (uint)remainingCurrentChunk.Length)
						{
							FormatError();
						}
						_stringBuilder.m_ChunkLength += charsWritten;
					}
					else
					{
						AppendFormattedWithTempSpace(value, 0, null);
					}
				}
				else
				{
					_stringBuilder.Append(((IFormattable)(object)value).ToString(null, _provider));
				}
			}
			else if (value != null)
			{
				_stringBuilder.Append(value.ToString());
			}
		}

		public void AppendFormatted<T>(T value, string? format)
		{
			if (_hasCustomFormatter)
			{
				AppendCustomFormatter(value, format);
			}
			else if (value is IFormattable)
			{
				if (value is ISpanFormattable)
				{
					Span<char> remainingCurrentChunk = _stringBuilder.RemainingCurrentChunk;
					if (((ISpanFormattable)(object)value).TryFormat(remainingCurrentChunk, out var charsWritten, format, _provider))
					{
						if ((uint)charsWritten > (uint)remainingCurrentChunk.Length)
						{
							FormatError();
						}
						_stringBuilder.m_ChunkLength += charsWritten;
					}
					else
					{
						AppendFormattedWithTempSpace(value, 0, format);
					}
				}
				else
				{
					_stringBuilder.Append(((IFormattable)(object)value).ToString(format, _provider));
				}
			}
			else if (value != null)
			{
				_stringBuilder.Append(value.ToString());
			}
		}

		public void AppendFormatted<T>(T value, int alignment)
		{
			AppendFormatted(value, alignment, null);
		}

		public void AppendFormatted<T>(T value, int alignment, string? format)
		{
			if (alignment == 0)
			{
				AppendFormatted(value, format);
			}
			else if (alignment < 0)
			{
				int length = _stringBuilder.Length;
				AppendFormatted(value, format);
				int num = -alignment - (_stringBuilder.Length - length);
				if (num > 0)
				{
					_stringBuilder.Append(' ', num);
				}
			}
			else
			{
				AppendFormattedWithTempSpace(value, alignment, format);
			}
		}

		private void AppendFormattedWithTempSpace<T>(T value, int alignment, string format)
		{
			IFormatProvider provider = _provider;
			Span<char> initialBuffer = stackalloc char[256];
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 0, provider, initialBuffer);
			defaultInterpolatedStringHandler.AppendFormatted(value, format);
			AppendFormatted(defaultInterpolatedStringHandler.Text, alignment);
			defaultInterpolatedStringHandler.Clear();
		}

		public void AppendFormatted(ReadOnlySpan<char> value)
		{
			_stringBuilder.Append(value);
		}

		public void AppendFormatted(ReadOnlySpan<char> value, int alignment = 0, string? format = null)
		{
			if (alignment == 0)
			{
				_stringBuilder.Append(value);
				return;
			}
			bool flag = false;
			if (alignment < 0)
			{
				flag = true;
				alignment = -alignment;
			}
			int num = alignment - value.Length;
			if (num <= 0)
			{
				_stringBuilder.Append(value);
			}
			else if (flag)
			{
				_stringBuilder.Append(value);
				_stringBuilder.Append(' ', num);
			}
			else
			{
				_stringBuilder.Append(' ', num);
				_stringBuilder.Append(value);
			}
		}

		public void AppendFormatted(string? value)
		{
			if (!_hasCustomFormatter)
			{
				_stringBuilder.Append(value);
			}
			else
			{
				this.AppendFormatted<string>(value);
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

		[MethodImpl(MethodImplOptions.NoInlining)]
		private void AppendCustomFormatter<T>(T value, string format)
		{
			ICustomFormatter customFormatter = (ICustomFormatter)_provider.GetFormat(typeof(ICustomFormatter));
			if (customFormatter != null)
			{
				_stringBuilder.Append(customFormatter.Format(format, value, _provider));
			}
		}
	}

	internal char[] m_ChunkChars;

	internal StringBuilder m_ChunkPrevious;

	internal int m_ChunkLength;

	internal int m_ChunkOffset;

	internal int m_MaxCapacity;

	public int Capacity
	{
		get
		{
			return m_ChunkChars.Length + m_ChunkOffset;
		}
		set
		{
			if (value < 0)
			{
				throw new ArgumentOutOfRangeException("value", SR.ArgumentOutOfRange_NegativeCapacity);
			}
			if (value > MaxCapacity)
			{
				throw new ArgumentOutOfRangeException("value", SR.ArgumentOutOfRange_Capacity);
			}
			if (value < Length)
			{
				throw new ArgumentOutOfRangeException("value", SR.ArgumentOutOfRange_SmallCapacity);
			}
			if (Capacity != value)
			{
				int length = value - m_ChunkOffset;
				char[] array = GC.AllocateUninitializedArray<char>(length);
				Array.Copy(m_ChunkChars, array, m_ChunkLength);
				m_ChunkChars = array;
			}
		}
	}

	public int MaxCapacity => m_MaxCapacity;

	public int Length
	{
		get
		{
			return m_ChunkOffset + m_ChunkLength;
		}
		set
		{
			if (value < 0)
			{
				throw new ArgumentOutOfRangeException("value", SR.ArgumentOutOfRange_NegativeLength);
			}
			if (value > MaxCapacity)
			{
				throw new ArgumentOutOfRangeException("value", SR.ArgumentOutOfRange_SmallCapacity);
			}
			if (value == 0 && m_ChunkPrevious == null)
			{
				m_ChunkLength = 0;
				m_ChunkOffset = 0;
				return;
			}
			int num = value - Length;
			if (num > 0)
			{
				Append('\0', num);
				return;
			}
			StringBuilder stringBuilder = FindChunkForIndex(value);
			if (stringBuilder != this)
			{
				int num2 = Math.Min(Capacity, Math.Max(Length * 6 / 5, m_ChunkChars.Length));
				int num3 = num2 - stringBuilder.m_ChunkOffset;
				if (num3 > stringBuilder.m_ChunkChars.Length)
				{
					char[] array = GC.AllocateUninitializedArray<char>(num3);
					Array.Copy(stringBuilder.m_ChunkChars, array, stringBuilder.m_ChunkLength);
					m_ChunkChars = array;
				}
				else
				{
					m_ChunkChars = stringBuilder.m_ChunkChars;
				}
				m_ChunkPrevious = stringBuilder.m_ChunkPrevious;
				m_ChunkOffset = stringBuilder.m_ChunkOffset;
			}
			m_ChunkLength = value - stringBuilder.m_ChunkOffset;
		}
	}

	[IndexerName("Chars")]
	public char this[int index]
	{
		get
		{
			StringBuilder stringBuilder = this;
			do
			{
				int num = index - stringBuilder.m_ChunkOffset;
				if (num >= 0)
				{
					if (num >= stringBuilder.m_ChunkLength)
					{
						throw new IndexOutOfRangeException();
					}
					return stringBuilder.m_ChunkChars[num];
				}
				stringBuilder = stringBuilder.m_ChunkPrevious;
			}
			while (stringBuilder != null);
			throw new IndexOutOfRangeException();
		}
		set
		{
			StringBuilder stringBuilder = this;
			do
			{
				int num = index - stringBuilder.m_ChunkOffset;
				if (num >= 0)
				{
					if (num >= stringBuilder.m_ChunkLength)
					{
						throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_Index);
					}
					stringBuilder.m_ChunkChars[num] = value;
					return;
				}
				stringBuilder = stringBuilder.m_ChunkPrevious;
			}
			while (stringBuilder != null);
			throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_Index);
		}
	}

	private Span<char> RemainingCurrentChunk
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return new Span<char>(m_ChunkChars, m_ChunkLength, m_ChunkChars.Length - m_ChunkLength);
		}
	}

	private int GetReplaceBufferCapacity(int requiredCapacity)
	{
		int num = Capacity;
		if (num < requiredCapacity)
		{
			num = (requiredCapacity + 1) & -2;
		}
		return num;
	}

	internal unsafe void ReplaceBufferInternal(char* newBuffer, int newLength)
	{
		if (newLength > m_MaxCapacity)
		{
			throw new ArgumentOutOfRangeException("capacity", SR.ArgumentOutOfRange_Capacity);
		}
		if (newLength > m_ChunkChars.Length)
		{
			m_ChunkChars = new char[GetReplaceBufferCapacity(newLength)];
		}
		new Span<char>(newBuffer, newLength).CopyTo(m_ChunkChars);
		m_ChunkLength = newLength;
		m_ChunkPrevious = null;
		m_ChunkOffset = 0;
	}

	internal void ReplaceBufferUtf8Internal(ReadOnlySpan<byte> source)
	{
		if (source.Length > m_MaxCapacity)
		{
			throw new ArgumentOutOfRangeException("capacity", SR.ArgumentOutOfRange_Capacity);
		}
		int charCount = Encoding.UTF8.GetCharCount(source);
		if (charCount > m_ChunkChars.Length)
		{
			m_ChunkChars = new char[GetReplaceBufferCapacity(charCount)];
		}
		m_ChunkLength = Encoding.UTF8.GetChars(source, m_ChunkChars);
		m_ChunkPrevious = null;
		m_ChunkOffset = 0;
	}

	internal unsafe void ReplaceBufferAnsiInternal(sbyte* newBuffer, int newLength)
	{
		if (newLength > m_MaxCapacity)
		{
			throw new ArgumentOutOfRangeException("capacity", SR.ArgumentOutOfRange_Capacity);
		}
		if (newLength > m_ChunkChars.Length)
		{
			m_ChunkChars = new char[GetReplaceBufferCapacity(newLength)];
		}
		int chunkLength;
		fixed (char* lpWideCharStr = m_ChunkChars)
		{
			chunkLength = Interop.Kernel32.MultiByteToWideChar(0u, 1u, (byte*)newBuffer, newLength, lpWideCharStr, newLength);
		}
		m_ChunkOffset = 0;
		m_ChunkLength = chunkLength;
		m_ChunkPrevious = null;
	}

	internal unsafe void InternalCopy(IntPtr dest, int charLen)
	{
		CopyTo(0, new Span<char>((void*)dest, charLen), charLen);
	}

	public StringBuilder()
	{
		m_MaxCapacity = int.MaxValue;
		m_ChunkChars = new char[16];
	}

	public StringBuilder(int capacity)
		: this(capacity, int.MaxValue)
	{
	}

	public StringBuilder(string? value)
		: this(value, 16)
	{
	}

	public StringBuilder(string? value, int capacity)
		: this(value, 0, value?.Length ?? 0, capacity)
	{
	}

	public StringBuilder(string? value, int startIndex, int length, int capacity)
	{
		if (capacity < 0)
		{
			throw new ArgumentOutOfRangeException("capacity", SR.Format(SR.ArgumentOutOfRange_MustBePositive, "capacity"));
		}
		if (length < 0)
		{
			throw new ArgumentOutOfRangeException("length", SR.Format(SR.ArgumentOutOfRange_MustBeNonNegNum, "length"));
		}
		if (startIndex < 0)
		{
			throw new ArgumentOutOfRangeException("startIndex", SR.ArgumentOutOfRange_StartIndex);
		}
		if (value == null)
		{
			value = string.Empty;
		}
		if (startIndex > value.Length - length)
		{
			throw new ArgumentOutOfRangeException("length", SR.ArgumentOutOfRange_IndexLength);
		}
		m_MaxCapacity = int.MaxValue;
		if (capacity == 0)
		{
			capacity = 16;
		}
		capacity = Math.Max(capacity, length);
		m_ChunkChars = GC.AllocateUninitializedArray<char>(capacity);
		m_ChunkLength = length;
		value.AsSpan(startIndex, length).CopyTo(m_ChunkChars);
	}

	public StringBuilder(int capacity, int maxCapacity)
	{
		if (capacity > maxCapacity)
		{
			throw new ArgumentOutOfRangeException("capacity", SR.ArgumentOutOfRange_Capacity);
		}
		if (maxCapacity < 1)
		{
			throw new ArgumentOutOfRangeException("maxCapacity", SR.ArgumentOutOfRange_SmallMaxCapacity);
		}
		if (capacity < 0)
		{
			throw new ArgumentOutOfRangeException("capacity", SR.Format(SR.ArgumentOutOfRange_MustBePositive, "capacity"));
		}
		if (capacity == 0)
		{
			capacity = Math.Min(16, maxCapacity);
		}
		m_MaxCapacity = maxCapacity;
		m_ChunkChars = GC.AllocateUninitializedArray<char>(capacity);
	}

	private StringBuilder(SerializationInfo info, StreamingContext context)
	{
		if (info == null)
		{
			throw new ArgumentNullException("info");
		}
		int num = 0;
		string text = null;
		int num2 = int.MaxValue;
		bool flag = false;
		SerializationInfoEnumerator enumerator = info.GetEnumerator();
		while (enumerator.MoveNext())
		{
			switch (enumerator.Name)
			{
			case "m_MaxCapacity":
				num2 = info.GetInt32("m_MaxCapacity");
				break;
			case "m_StringValue":
				text = info.GetString("m_StringValue");
				break;
			case "Capacity":
				num = info.GetInt32("Capacity");
				flag = true;
				break;
			}
		}
		if (text == null)
		{
			text = string.Empty;
		}
		if (num2 < 1 || text.Length > num2)
		{
			throw new SerializationException(SR.Serialization_StringBuilderMaxCapacity);
		}
		if (!flag)
		{
			num = Math.Min(Math.Max(16, text.Length), num2);
		}
		if (num < 0 || num < text.Length || num > num2)
		{
			throw new SerializationException(SR.Serialization_StringBuilderCapacity);
		}
		m_MaxCapacity = num2;
		m_ChunkChars = GC.AllocateUninitializedArray<char>(num);
		text.CopyTo(0, m_ChunkChars, 0, text.Length);
		m_ChunkLength = text.Length;
	}

	void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
	{
		if (info == null)
		{
			throw new ArgumentNullException("info");
		}
		info.AddValue("m_MaxCapacity", m_MaxCapacity);
		info.AddValue("Capacity", Capacity);
		info.AddValue("m_StringValue", ToString());
		info.AddValue("m_currentThread", 0);
	}

	public int EnsureCapacity(int capacity)
	{
		if (capacity < 0)
		{
			throw new ArgumentOutOfRangeException("capacity", SR.ArgumentOutOfRange_NegativeCapacity);
		}
		if (Capacity < capacity)
		{
			Capacity = capacity;
		}
		return Capacity;
	}

	public override string ToString()
	{
		if (Length == 0)
		{
			return string.Empty;
		}
		string text = string.FastAllocateString(Length);
		StringBuilder stringBuilder = this;
		do
		{
			if (stringBuilder.m_ChunkLength > 0)
			{
				char[] chunkChars = stringBuilder.m_ChunkChars;
				int chunkOffset = stringBuilder.m_ChunkOffset;
				int chunkLength = stringBuilder.m_ChunkLength;
				if ((uint)(chunkLength + chunkOffset) > (uint)text.Length || (uint)chunkLength > (uint)chunkChars.Length)
				{
					throw new ArgumentOutOfRangeException("chunkLength", SR.ArgumentOutOfRange_Index);
				}
				Buffer.Memmove(ref Unsafe.Add(ref text.GetRawStringData(), chunkOffset), ref MemoryMarshal.GetArrayDataReference(chunkChars), (nuint)chunkLength);
			}
			stringBuilder = stringBuilder.m_ChunkPrevious;
		}
		while (stringBuilder != null);
		return text;
	}

	public unsafe string ToString(int startIndex, int length)
	{
		int length2 = Length;
		if (startIndex < 0)
		{
			throw new ArgumentOutOfRangeException("startIndex", SR.ArgumentOutOfRange_StartIndex);
		}
		if (startIndex > length2)
		{
			throw new ArgumentOutOfRangeException("startIndex", SR.ArgumentOutOfRange_StartIndexLargerThanLength);
		}
		if (length < 0)
		{
			throw new ArgumentOutOfRangeException("length", SR.ArgumentOutOfRange_NegativeLength);
		}
		if (startIndex > length2 - length)
		{
			throw new ArgumentOutOfRangeException("length", SR.ArgumentOutOfRange_IndexLength);
		}
		string text = string.FastAllocateString(length);
		fixed (char* pointer = text)
		{
			CopyTo(startIndex, new Span<char>(pointer, length), length);
			return text;
		}
	}

	public StringBuilder Clear()
	{
		Length = 0;
		return this;
	}

	public ChunkEnumerator GetChunks()
	{
		return new ChunkEnumerator(this);
	}

	public StringBuilder Append(char value, int repeatCount)
	{
		if (repeatCount < 0)
		{
			throw new ArgumentOutOfRangeException("repeatCount", SR.ArgumentOutOfRange_NegativeCount);
		}
		if (repeatCount == 0)
		{
			return this;
		}
		int num = Length + repeatCount;
		if (num > m_MaxCapacity || num < repeatCount)
		{
			throw new ArgumentOutOfRangeException("repeatCount", SR.ArgumentOutOfRange_LengthGreaterThanCapacity);
		}
		int num2 = m_ChunkLength;
		while (repeatCount > 0)
		{
			if (num2 < m_ChunkChars.Length)
			{
				m_ChunkChars[num2++] = value;
				repeatCount--;
			}
			else
			{
				m_ChunkLength = num2;
				ExpandByABlock(repeatCount);
				num2 = 0;
			}
		}
		m_ChunkLength = num2;
		return this;
	}

	public unsafe StringBuilder Append(char[]? value, int startIndex, int charCount)
	{
		if (startIndex < 0)
		{
			throw new ArgumentOutOfRangeException("startIndex", SR.ArgumentOutOfRange_GenericPositive);
		}
		if (charCount < 0)
		{
			throw new ArgumentOutOfRangeException("charCount", SR.ArgumentOutOfRange_GenericPositive);
		}
		if (value == null)
		{
			if (startIndex == 0 && charCount == 0)
			{
				return this;
			}
			throw new ArgumentNullException("value");
		}
		if (charCount > value.Length - startIndex)
		{
			throw new ArgumentOutOfRangeException("charCount", SR.ArgumentOutOfRange_Index);
		}
		if (charCount == 0)
		{
			return this;
		}
		fixed (char* value2 = &value[startIndex])
		{
			Append(value2, charCount);
			return this;
		}
	}

	public StringBuilder Append(string? value)
	{
		if (value != null)
		{
			char[] chunkChars = m_ChunkChars;
			int chunkLength = m_ChunkLength;
			int length = value.Length;
			if ((uint)(chunkLength + length) < (uint)chunkChars.Length)
			{
				if (length <= 2)
				{
					if (length > 0)
					{
						chunkChars[chunkLength] = value[0];
					}
					if (length > 1)
					{
						chunkChars[chunkLength + 1] = value[1];
					}
				}
				else
				{
					Buffer.Memmove(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(chunkChars), chunkLength), ref value.GetRawStringData(), (nuint)length);
				}
				m_ChunkLength = chunkLength + length;
			}
			else
			{
				AppendHelper(value);
			}
		}
		return this;
	}

	private unsafe void AppendHelper(string value)
	{
		fixed (char* value2 = value)
		{
			Append(value2, value.Length);
		}
	}

	public unsafe StringBuilder Append(string? value, int startIndex, int count)
	{
		if (startIndex < 0)
		{
			throw new ArgumentOutOfRangeException("startIndex", SR.ArgumentOutOfRange_Index);
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_GenericPositive);
		}
		if (value == null)
		{
			if (startIndex == 0 && count == 0)
			{
				return this;
			}
			throw new ArgumentNullException("value");
		}
		if (count == 0)
		{
			return this;
		}
		if (startIndex > value.Length - count)
		{
			throw new ArgumentOutOfRangeException("startIndex", SR.ArgumentOutOfRange_Index);
		}
		fixed (char* ptr = value)
		{
			Append(ptr + startIndex, count);
			return this;
		}
	}

	public StringBuilder Append(StringBuilder? value)
	{
		if (value != null && value.Length != 0)
		{
			return AppendCore(value, 0, value.Length);
		}
		return this;
	}

	public StringBuilder Append(StringBuilder? value, int startIndex, int count)
	{
		if (startIndex < 0)
		{
			throw new ArgumentOutOfRangeException("startIndex", SR.ArgumentOutOfRange_Index);
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_GenericPositive);
		}
		if (value == null)
		{
			if (startIndex == 0 && count == 0)
			{
				return this;
			}
			throw new ArgumentNullException("value");
		}
		if (count == 0)
		{
			return this;
		}
		if (count > value.Length - startIndex)
		{
			throw new ArgumentOutOfRangeException("startIndex", SR.ArgumentOutOfRange_Index);
		}
		return AppendCore(value, startIndex, count);
	}

	private StringBuilder AppendCore(StringBuilder value, int startIndex, int count)
	{
		if (value == this)
		{
			return Append(value.ToString(startIndex, count));
		}
		int num = Length + count;
		if ((uint)num > (uint)m_MaxCapacity)
		{
			throw new ArgumentOutOfRangeException("Capacity", SR.ArgumentOutOfRange_Capacity);
		}
		while (count > 0)
		{
			int num2 = Math.Min(m_ChunkChars.Length - m_ChunkLength, count);
			if (num2 == 0)
			{
				ExpandByABlock(count);
				num2 = Math.Min(m_ChunkChars.Length - m_ChunkLength, count);
			}
			value.CopyTo(startIndex, new Span<char>(m_ChunkChars, m_ChunkLength, num2), num2);
			m_ChunkLength += num2;
			startIndex += num2;
			count -= num2;
		}
		return this;
	}

	public StringBuilder AppendLine()
	{
		return Append(Environment.NewLine);
	}

	public StringBuilder AppendLine(string? value)
	{
		Append(value);
		return Append(Environment.NewLine);
	}

	public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
	{
		if (destination == null)
		{
			throw new ArgumentNullException("destination");
		}
		if (destinationIndex < 0)
		{
			throw new ArgumentOutOfRangeException("destinationIndex", SR.Format(SR.ArgumentOutOfRange_MustBeNonNegNum, "destinationIndex"));
		}
		if (destinationIndex > destination.Length - count)
		{
			throw new ArgumentException(SR.ArgumentOutOfRange_OffsetOut);
		}
		CopyTo(sourceIndex, new Span<char>(destination).Slice(destinationIndex), count);
	}

	public void CopyTo(int sourceIndex, Span<char> destination, int count)
	{
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", SR.Arg_NegativeArgCount);
		}
		if ((uint)sourceIndex > (uint)Length)
		{
			throw new ArgumentOutOfRangeException("sourceIndex", SR.ArgumentOutOfRange_Index);
		}
		if (sourceIndex > Length - count)
		{
			throw new ArgumentException(SR.Arg_LongerThanSrcString);
		}
		StringBuilder stringBuilder = this;
		int num = sourceIndex + count;
		int num2 = count;
		while (count > 0)
		{
			int num3 = num - stringBuilder.m_ChunkOffset;
			if (num3 >= 0)
			{
				num3 = Math.Min(num3, stringBuilder.m_ChunkLength);
				int num4 = count;
				int num5 = num3 - count;
				if (num5 < 0)
				{
					num4 += num5;
					num5 = 0;
				}
				num2 -= num4;
				count -= num4;
				new ReadOnlySpan<char>(stringBuilder.m_ChunkChars, num5, num4).CopyTo(destination.Slice(num2));
			}
			stringBuilder = stringBuilder.m_ChunkPrevious;
		}
	}

	public unsafe StringBuilder Insert(int index, string? value, int count)
	{
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		int length = Length;
		if ((uint)index > (uint)length)
		{
			throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_Index);
		}
		if (string.IsNullOrEmpty(value) || count == 0)
		{
			return this;
		}
		long num = (long)value.Length * (long)count;
		if (num > MaxCapacity - Length)
		{
			throw new OutOfMemoryException();
		}
		MakeRoom(index, (int)num, out var chunk, out var indexInChunk, doNotMoveFollowingChars: false);
		fixed (char* value2 = value)
		{
			while (count > 0)
			{
				ReplaceInPlaceAtChunk(ref chunk, ref indexInChunk, value2, value.Length);
				count--;
			}
			return this;
		}
	}

	public StringBuilder Remove(int startIndex, int length)
	{
		if (length < 0)
		{
			throw new ArgumentOutOfRangeException("length", SR.ArgumentOutOfRange_NegativeLength);
		}
		if (startIndex < 0)
		{
			throw new ArgumentOutOfRangeException("startIndex", SR.ArgumentOutOfRange_StartIndex);
		}
		if (length > Length - startIndex)
		{
			throw new ArgumentOutOfRangeException("length", SR.ArgumentOutOfRange_Index);
		}
		if (Length == length && startIndex == 0)
		{
			Length = 0;
			return this;
		}
		if (length > 0)
		{
			Remove(startIndex, length, out var _, out var _);
		}
		return this;
	}

	public StringBuilder Append(bool value)
	{
		return Append(value.ToString());
	}

	public StringBuilder Append(char value)
	{
		int chunkLength = m_ChunkLength;
		char[] chunkChars = m_ChunkChars;
		if ((uint)chunkChars.Length > (uint)chunkLength)
		{
			chunkChars[chunkLength] = value;
			m_ChunkLength++;
		}
		else
		{
			Append(value, 1);
		}
		return this;
	}

	[CLSCompliant(false)]
	public StringBuilder Append(sbyte value)
	{
		return AppendSpanFormattable(value);
	}

	public StringBuilder Append(byte value)
	{
		return AppendSpanFormattable(value);
	}

	public StringBuilder Append(short value)
	{
		return AppendSpanFormattable(value);
	}

	public StringBuilder Append(int value)
	{
		return AppendSpanFormattable(value);
	}

	public StringBuilder Append(long value)
	{
		return AppendSpanFormattable(value);
	}

	public StringBuilder Append(float value)
	{
		return AppendSpanFormattable(value);
	}

	public StringBuilder Append(double value)
	{
		return AppendSpanFormattable(value);
	}

	public StringBuilder Append(decimal value)
	{
		return AppendSpanFormattable(value);
	}

	[CLSCompliant(false)]
	public StringBuilder Append(ushort value)
	{
		return AppendSpanFormattable(value);
	}

	[CLSCompliant(false)]
	public StringBuilder Append(uint value)
	{
		return AppendSpanFormattable(value);
	}

	[CLSCompliant(false)]
	public StringBuilder Append(ulong value)
	{
		return AppendSpanFormattable(value);
	}

	private StringBuilder AppendSpanFormattable<T>(T value) where T : ISpanFormattable
	{
		if (value.TryFormat(RemainingCurrentChunk, out var charsWritten, default(ReadOnlySpan<char>), null))
		{
			m_ChunkLength += charsWritten;
			return this;
		}
		return Append(value.ToString());
	}

	internal StringBuilder AppendSpanFormattable<T>(T value, string format, IFormatProvider provider) where T : ISpanFormattable
	{
		if (value.TryFormat(RemainingCurrentChunk, out var charsWritten, format, provider))
		{
			m_ChunkLength += charsWritten;
			return this;
		}
		return Append(value.ToString(format, provider));
	}

	public StringBuilder Append(object? value)
	{
		if (value != null)
		{
			return Append(value.ToString());
		}
		return this;
	}

	public unsafe StringBuilder Append(char[]? value)
	{
		if (value != null && value.Length != 0)
		{
			fixed (char* value2 = &value[0])
			{
				Append(value2, value.Length);
			}
		}
		return this;
	}

	public unsafe StringBuilder Append(ReadOnlySpan<char> value)
	{
		if (value.Length > 0)
		{
			fixed (char* value2 = &MemoryMarshal.GetReference(value))
			{
				Append(value2, value.Length);
			}
		}
		return this;
	}

	public StringBuilder Append(ReadOnlyMemory<char> value)
	{
		return Append(value.Span);
	}

	public StringBuilder Append([InterpolatedStringHandlerArgument("")] ref AppendInterpolatedStringHandler handler)
	{
		return this;
	}

	public StringBuilder Append(IFormatProvider? provider, [InterpolatedStringHandlerArgument(new string[] { "", "provider" })] ref AppendInterpolatedStringHandler handler)
	{
		return this;
	}

	public StringBuilder AppendLine([InterpolatedStringHandlerArgument("")] ref AppendInterpolatedStringHandler handler)
	{
		return AppendLine();
	}

	public StringBuilder AppendLine(IFormatProvider? provider, [InterpolatedStringHandlerArgument(new string[] { "", "provider" })] ref AppendInterpolatedStringHandler handler)
	{
		return AppendLine();
	}

	public unsafe StringBuilder AppendJoin(string? separator, params object?[] values)
	{
		if (separator == null)
		{
			separator = string.Empty;
		}
		fixed (char* separator2 = separator)
		{
			return AppendJoinCore(separator2, separator.Length, values);
		}
	}

	public unsafe StringBuilder AppendJoin<T>(string? separator, IEnumerable<T> values)
	{
		if (separator == null)
		{
			separator = string.Empty;
		}
		fixed (char* separator2 = separator)
		{
			return AppendJoinCore(separator2, separator.Length, values);
		}
	}

	public unsafe StringBuilder AppendJoin(string? separator, params string?[] values)
	{
		if (separator == null)
		{
			separator = string.Empty;
		}
		fixed (char* separator2 = separator)
		{
			return AppendJoinCore(separator2, separator.Length, values);
		}
	}

	public unsafe StringBuilder AppendJoin(char separator, params object?[] values)
	{
		return AppendJoinCore(&separator, 1, values);
	}

	public unsafe StringBuilder AppendJoin<T>(char separator, IEnumerable<T> values)
	{
		return AppendJoinCore(&separator, 1, values);
	}

	public unsafe StringBuilder AppendJoin(char separator, params string?[] values)
	{
		return AppendJoinCore(&separator, 1, values);
	}

	private unsafe StringBuilder AppendJoinCore<T>(char* separator, int separatorLength, IEnumerable<T> values)
	{
		if (values == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.values);
		}
		using IEnumerator<T> enumerator = values.GetEnumerator();
		if (!enumerator.MoveNext())
		{
			return this;
		}
		T current = enumerator.Current;
		if (current != null)
		{
			Append(current.ToString());
		}
		while (enumerator.MoveNext())
		{
			Append(separator, separatorLength);
			current = enumerator.Current;
			if (current != null)
			{
				Append(current.ToString());
			}
		}
		return this;
	}

	private unsafe StringBuilder AppendJoinCore<T>(char* separator, int separatorLength, T[] values)
	{
		if (values == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.values);
		}
		if (values.Length == 0)
		{
			return this;
		}
		if (values[0] != null)
		{
			Append(values[0].ToString());
		}
		for (int i = 1; i < values.Length; i++)
		{
			Append(separator, separatorLength);
			if (values[i] != null)
			{
				Append(values[i].ToString());
			}
		}
		return this;
	}

	public unsafe StringBuilder Insert(int index, string? value)
	{
		if ((uint)index > (uint)Length)
		{
			throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_Index);
		}
		if (value != null)
		{
			fixed (char* value2 = value)
			{
				Insert(index, value2, value.Length);
			}
		}
		return this;
	}

	public StringBuilder Insert(int index, bool value)
	{
		return Insert(index, value.ToString(), 1);
	}

	[CLSCompliant(false)]
	public StringBuilder Insert(int index, sbyte value)
	{
		return Insert(index, value.ToString(), 1);
	}

	public StringBuilder Insert(int index, byte value)
	{
		return Insert(index, value.ToString(), 1);
	}

	public StringBuilder Insert(int index, short value)
	{
		return Insert(index, value.ToString(), 1);
	}

	public unsafe StringBuilder Insert(int index, char value)
	{
		Insert(index, &value, 1);
		return this;
	}

	public StringBuilder Insert(int index, char[]? value)
	{
		if ((uint)index > (uint)Length)
		{
			throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_Index);
		}
		if (value != null)
		{
			Insert(index, value, 0, value.Length);
		}
		return this;
	}

	public unsafe StringBuilder Insert(int index, char[]? value, int startIndex, int charCount)
	{
		int length = Length;
		if ((uint)index > (uint)length)
		{
			throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_Index);
		}
		if (value == null)
		{
			if (startIndex == 0 && charCount == 0)
			{
				return this;
			}
			throw new ArgumentNullException("value", SR.ArgumentNull_String);
		}
		if (startIndex < 0)
		{
			throw new ArgumentOutOfRangeException("startIndex", SR.ArgumentOutOfRange_StartIndex);
		}
		if (charCount < 0)
		{
			throw new ArgumentOutOfRangeException("charCount", SR.ArgumentOutOfRange_GenericPositive);
		}
		if (startIndex > value.Length - charCount)
		{
			throw new ArgumentOutOfRangeException("startIndex", SR.ArgumentOutOfRange_Index);
		}
		if (charCount > 0)
		{
			fixed (char* value2 = &value[startIndex])
			{
				Insert(index, value2, charCount);
			}
		}
		return this;
	}

	public StringBuilder Insert(int index, int value)
	{
		return Insert(index, value.ToString(), 1);
	}

	public StringBuilder Insert(int index, long value)
	{
		return Insert(index, value.ToString(), 1);
	}

	public StringBuilder Insert(int index, float value)
	{
		return Insert(index, value.ToString(), 1);
	}

	public StringBuilder Insert(int index, double value)
	{
		return Insert(index, value.ToString(), 1);
	}

	public StringBuilder Insert(int index, decimal value)
	{
		return Insert(index, value.ToString(), 1);
	}

	[CLSCompliant(false)]
	public StringBuilder Insert(int index, ushort value)
	{
		return Insert(index, value.ToString(), 1);
	}

	[CLSCompliant(false)]
	public StringBuilder Insert(int index, uint value)
	{
		return Insert(index, value.ToString(), 1);
	}

	[CLSCompliant(false)]
	public StringBuilder Insert(int index, ulong value)
	{
		return Insert(index, value.ToString(), 1);
	}

	public StringBuilder Insert(int index, object? value)
	{
		if (value != null)
		{
			return Insert(index, value.ToString(), 1);
		}
		return this;
	}

	public unsafe StringBuilder Insert(int index, ReadOnlySpan<char> value)
	{
		if ((uint)index > (uint)Length)
		{
			throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_Index);
		}
		if (value.Length > 0)
		{
			fixed (char* value2 = &MemoryMarshal.GetReference(value))
			{
				Insert(index, value2, value.Length);
			}
		}
		return this;
	}

	public StringBuilder AppendFormat(string format, object? arg0)
	{
		return AppendFormatHelper(null, format, new ParamsArray(arg0));
	}

	public StringBuilder AppendFormat(string format, object? arg0, object? arg1)
	{
		return AppendFormatHelper(null, format, new ParamsArray(arg0, arg1));
	}

	public StringBuilder AppendFormat(string format, object? arg0, object? arg1, object? arg2)
	{
		return AppendFormatHelper(null, format, new ParamsArray(arg0, arg1, arg2));
	}

	public StringBuilder AppendFormat(string format, params object?[] args)
	{
		if (args == null)
		{
			string paramName = ((format == null) ? "format" : "args");
			throw new ArgumentNullException(paramName);
		}
		return AppendFormatHelper(null, format, new ParamsArray(args));
	}

	public StringBuilder AppendFormat(IFormatProvider? provider, string format, object? arg0)
	{
		return AppendFormatHelper(provider, format, new ParamsArray(arg0));
	}

	public StringBuilder AppendFormat(IFormatProvider? provider, string format, object? arg0, object? arg1)
	{
		return AppendFormatHelper(provider, format, new ParamsArray(arg0, arg1));
	}

	public StringBuilder AppendFormat(IFormatProvider? provider, string format, object? arg0, object? arg1, object? arg2)
	{
		return AppendFormatHelper(provider, format, new ParamsArray(arg0, arg1, arg2));
	}

	public StringBuilder AppendFormat(IFormatProvider? provider, string format, params object?[] args)
	{
		if (args == null)
		{
			string paramName = ((format == null) ? "format" : "args");
			throw new ArgumentNullException(paramName);
		}
		return AppendFormatHelper(provider, format, new ParamsArray(args));
	}

	private static void FormatError()
	{
		throw new FormatException(SR.Format_InvalidString);
	}

	internal StringBuilder AppendFormatHelper(IFormatProvider provider, string format, ParamsArray args)
	{
		if (format == null)
		{
			throw new ArgumentNullException("format");
		}
		int num = 0;
		int length = format.Length;
		char c = '\0';
		ICustomFormatter customFormatter = null;
		if (provider != null)
		{
			customFormatter = (ICustomFormatter)provider.GetFormat(typeof(ICustomFormatter));
		}
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
						FormatError();
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
				FormatError();
			}
			int num2 = 0;
			do
			{
				num2 = num2 * 10 + c - 48;
				num++;
				if (num == length)
				{
					FormatError();
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
					FormatError();
				}
				c = format[num];
				if (c == '-')
				{
					flag = true;
					num++;
					if (num == length)
					{
						FormatError();
					}
					c = format[num];
				}
				if (c < '0' || c > '9')
				{
					FormatError();
				}
				do
				{
					num3 = num3 * 10 + c - 48;
					num++;
					if (num == length)
					{
						FormatError();
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
						FormatError();
					}
					c = format[num];
					switch (c)
					{
					case '{':
						FormatError();
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
				FormatError();
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
				if (obj is ISpanFormattable spanFormattable && (flag || num3 == 0) && spanFormattable.TryFormat(RemainingCurrentChunk, out var charsWritten, readOnlySpan, provider))
				{
					if ((uint)charsWritten > (uint)RemainingCurrentChunk.Length)
					{
						FormatError();
					}
					m_ChunkLength += charsWritten;
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
		return this;
	}

	public StringBuilder Replace(string oldValue, string? newValue)
	{
		return Replace(oldValue, newValue, 0, Length);
	}

	public bool Equals([NotNullWhen(true)] StringBuilder? sb)
	{
		if (sb == null)
		{
			return false;
		}
		if (Length != sb.Length)
		{
			return false;
		}
		if (sb == this)
		{
			return true;
		}
		StringBuilder stringBuilder = this;
		int num = stringBuilder.m_ChunkLength;
		StringBuilder stringBuilder2 = sb;
		int num2 = stringBuilder2.m_ChunkLength;
		do
		{
			num--;
			num2--;
			while (num < 0)
			{
				stringBuilder = stringBuilder.m_ChunkPrevious;
				if (stringBuilder == null)
				{
					break;
				}
				num = stringBuilder.m_ChunkLength + num;
			}
			while (num2 < 0)
			{
				stringBuilder2 = stringBuilder2.m_ChunkPrevious;
				if (stringBuilder2 == null)
				{
					break;
				}
				num2 = stringBuilder2.m_ChunkLength + num2;
			}
			if (num < 0)
			{
				return num2 < 0;
			}
			if (num2 < 0)
			{
				return false;
			}
		}
		while (stringBuilder.m_ChunkChars[num] == stringBuilder2.m_ChunkChars[num2]);
		return false;
	}

	public bool Equals(ReadOnlySpan<char> span)
	{
		if (span.Length != Length)
		{
			return false;
		}
		StringBuilder stringBuilder = this;
		int num = 0;
		do
		{
			int chunkLength = stringBuilder.m_ChunkLength;
			num += chunkLength;
			ReadOnlySpan<char> span2 = new ReadOnlySpan<char>(stringBuilder.m_ChunkChars, 0, chunkLength);
			if (!span2.EqualsOrdinal(span.Slice(span.Length - num, chunkLength)))
			{
				return false;
			}
			stringBuilder = stringBuilder.m_ChunkPrevious;
		}
		while (stringBuilder != null);
		return true;
	}

	public StringBuilder Replace(string oldValue, string? newValue, int startIndex, int count)
	{
		int length = Length;
		if ((uint)startIndex > (uint)length)
		{
			throw new ArgumentOutOfRangeException("startIndex", SR.ArgumentOutOfRange_Index);
		}
		if (count < 0 || startIndex > length - count)
		{
			throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_Index);
		}
		if (oldValue == null)
		{
			throw new ArgumentNullException("oldValue");
		}
		if (oldValue.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyName, "oldValue");
		}
		if (newValue == null)
		{
			newValue = string.Empty;
		}
		int[] array = null;
		int num = 0;
		StringBuilder stringBuilder = FindChunkForIndex(startIndex);
		int num2 = startIndex - stringBuilder.m_ChunkOffset;
		while (count > 0)
		{
			if (StartsWith(stringBuilder, num2, count, oldValue))
			{
				if (array == null)
				{
					array = new int[5];
				}
				else if (num >= array.Length)
				{
					Array.Resize(ref array, array.Length * 3 / 2 + 4);
				}
				array[num++] = num2;
				num2 += oldValue.Length;
				count -= oldValue.Length;
			}
			else
			{
				num2++;
				count--;
			}
			if (num2 >= stringBuilder.m_ChunkLength || count == 0)
			{
				int num3 = num2 + stringBuilder.m_ChunkOffset;
				ReplaceAllInChunk(array, num, stringBuilder, oldValue.Length, newValue);
				num3 += (newValue.Length - oldValue.Length) * num;
				num = 0;
				stringBuilder = FindChunkForIndex(num3);
				num2 = num3 - stringBuilder.m_ChunkOffset;
			}
		}
		return this;
	}

	public StringBuilder Replace(char oldChar, char newChar)
	{
		return Replace(oldChar, newChar, 0, Length);
	}

	public StringBuilder Replace(char oldChar, char newChar, int startIndex, int count)
	{
		int length = Length;
		if ((uint)startIndex > (uint)length)
		{
			throw new ArgumentOutOfRangeException("startIndex", SR.ArgumentOutOfRange_Index);
		}
		if (count < 0 || startIndex > length - count)
		{
			throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_Index);
		}
		int num = startIndex + count;
		StringBuilder stringBuilder = this;
		while (true)
		{
			int num2 = num - stringBuilder.m_ChunkOffset;
			int num3 = startIndex - stringBuilder.m_ChunkOffset;
			if (num2 >= 0)
			{
				int i = Math.Max(num3, 0);
				for (int num4 = Math.Min(stringBuilder.m_ChunkLength, num2); i < num4; i++)
				{
					if (stringBuilder.m_ChunkChars[i] == oldChar)
					{
						stringBuilder.m_ChunkChars[i] = newChar;
					}
				}
			}
			if (num3 >= 0)
			{
				break;
			}
			stringBuilder = stringBuilder.m_ChunkPrevious;
		}
		return this;
	}

	[CLSCompliant(false)]
	public unsafe StringBuilder Append(char* value, int valueCount)
	{
		if (valueCount < 0)
		{
			throw new ArgumentOutOfRangeException("valueCount", SR.ArgumentOutOfRange_NegativeCount);
		}
		int num = Length + valueCount;
		if (num > m_MaxCapacity || num < valueCount)
		{
			throw new ArgumentOutOfRangeException("valueCount", SR.ArgumentOutOfRange_LengthGreaterThanCapacity);
		}
		int num2 = valueCount + m_ChunkLength;
		if (num2 <= m_ChunkChars.Length)
		{
			new ReadOnlySpan<char>(value, valueCount).CopyTo(m_ChunkChars.AsSpan(m_ChunkLength));
			m_ChunkLength = num2;
		}
		else
		{
			int num3 = m_ChunkChars.Length - m_ChunkLength;
			if (num3 > 0)
			{
				new ReadOnlySpan<char>(value, num3).CopyTo(m_ChunkChars.AsSpan(m_ChunkLength));
				m_ChunkLength = m_ChunkChars.Length;
			}
			int num4 = valueCount - num3;
			ExpandByABlock(num4);
			new ReadOnlySpan<char>(value + num3, num4).CopyTo(m_ChunkChars);
			m_ChunkLength = num4;
		}
		return this;
	}

	private unsafe void Insert(int index, char* value, int valueCount)
	{
		if ((uint)index > (uint)Length)
		{
			throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_Index);
		}
		if (valueCount > 0)
		{
			MakeRoom(index, valueCount, out var chunk, out var indexInChunk, doNotMoveFollowingChars: false);
			ReplaceInPlaceAtChunk(ref chunk, ref indexInChunk, value, valueCount);
		}
	}

	private unsafe void ReplaceAllInChunk(int[] replacements, int replacementsCount, StringBuilder sourceChunk, int removeCount, string value)
	{
		if (replacementsCount <= 0)
		{
			return;
		}
		fixed (char* value2 = value)
		{
			long num = (long)(value.Length - removeCount) * (long)replacementsCount;
			int num2 = (int)num;
			if (num2 != num)
			{
				throw new OutOfMemoryException();
			}
			StringBuilder chunk = sourceChunk;
			int indexInChunk = replacements[0];
			if (num2 > 0)
			{
				MakeRoom(chunk.m_ChunkOffset + indexInChunk, num2, out chunk, out indexInChunk, doNotMoveFollowingChars: true);
			}
			int num3 = 0;
			while (true)
			{
				ReplaceInPlaceAtChunk(ref chunk, ref indexInChunk, value2, value.Length);
				int num4 = replacements[num3] + removeCount;
				num3++;
				if (num3 >= replacementsCount)
				{
					break;
				}
				int num5 = replacements[num3];
				if (num2 != 0)
				{
					fixed (char* value3 = &sourceChunk.m_ChunkChars[num4])
					{
						ReplaceInPlaceAtChunk(ref chunk, ref indexInChunk, value3, num5 - num4);
					}
				}
				else
				{
					indexInChunk += num5 - num4;
				}
			}
			if (num2 < 0)
			{
				Remove(chunk.m_ChunkOffset + indexInChunk, -num2, out chunk, out indexInChunk);
			}
		}
	}

	private bool StartsWith(StringBuilder chunk, int indexInChunk, int count, string value)
	{
		for (int i = 0; i < value.Length; i++)
		{
			if (count == 0)
			{
				return false;
			}
			if (indexInChunk >= chunk.m_ChunkLength)
			{
				chunk = Next(chunk);
				if (chunk == null)
				{
					return false;
				}
				indexInChunk = 0;
			}
			if (value[i] != chunk.m_ChunkChars[indexInChunk])
			{
				return false;
			}
			indexInChunk++;
			count--;
		}
		return true;
	}

	private unsafe void ReplaceInPlaceAtChunk(ref StringBuilder chunk, ref int indexInChunk, char* value, int count)
	{
		if (count == 0)
		{
			return;
		}
		while (true)
		{
			int val = chunk.m_ChunkLength - indexInChunk;
			int num = Math.Min(val, count);
			new ReadOnlySpan<char>(value, num).CopyTo(chunk.m_ChunkChars.AsSpan(indexInChunk));
			indexInChunk += num;
			if (indexInChunk >= chunk.m_ChunkLength)
			{
				chunk = Next(chunk);
				indexInChunk = 0;
			}
			count -= num;
			if (count != 0)
			{
				value += num;
				continue;
			}
			break;
		}
	}

	private StringBuilder FindChunkForIndex(int index)
	{
		StringBuilder stringBuilder = this;
		while (stringBuilder.m_ChunkOffset > index)
		{
			stringBuilder = stringBuilder.m_ChunkPrevious;
		}
		return stringBuilder;
	}

	private StringBuilder Next(StringBuilder chunk)
	{
		if (chunk != this)
		{
			return FindChunkForIndex(chunk.m_ChunkOffset + chunk.m_ChunkLength);
		}
		return null;
	}

	private void ExpandByABlock(int minBlockCharCount)
	{
		if (minBlockCharCount + Length > m_MaxCapacity || minBlockCharCount + Length < minBlockCharCount)
		{
			throw new ArgumentOutOfRangeException("requiredLength", SR.ArgumentOutOfRange_SmallCapacity);
		}
		int num = Math.Max(minBlockCharCount, Math.Min(Length, 8000));
		if (m_ChunkOffset + m_ChunkLength + num < num)
		{
			throw new OutOfMemoryException();
		}
		char[] chunkChars = GC.AllocateUninitializedArray<char>(num);
		m_ChunkPrevious = new StringBuilder(this);
		m_ChunkOffset += m_ChunkLength;
		m_ChunkLength = 0;
		m_ChunkChars = chunkChars;
	}

	private StringBuilder(StringBuilder from)
	{
		m_ChunkLength = from.m_ChunkLength;
		m_ChunkOffset = from.m_ChunkOffset;
		m_ChunkChars = from.m_ChunkChars;
		m_ChunkPrevious = from.m_ChunkPrevious;
		m_MaxCapacity = from.m_MaxCapacity;
	}

	private void MakeRoom(int index, int count, out StringBuilder chunk, out int indexInChunk, bool doNotMoveFollowingChars)
	{
		if (count + Length > m_MaxCapacity || count + Length < count)
		{
			throw new ArgumentOutOfRangeException("requiredLength", SR.ArgumentOutOfRange_SmallCapacity);
		}
		chunk = this;
		while (chunk.m_ChunkOffset > index)
		{
			chunk.m_ChunkOffset += count;
			chunk = chunk.m_ChunkPrevious;
		}
		indexInChunk = index - chunk.m_ChunkOffset;
		if (!doNotMoveFollowingChars && chunk.m_ChunkLength <= 32 && chunk.m_ChunkChars.Length - chunk.m_ChunkLength >= count)
		{
			int num = chunk.m_ChunkLength;
			while (num > indexInChunk)
			{
				num--;
				chunk.m_ChunkChars[num + count] = chunk.m_ChunkChars[num];
			}
			chunk.m_ChunkLength += count;
			return;
		}
		StringBuilder stringBuilder = new StringBuilder(Math.Max(count, 16), chunk.m_MaxCapacity, chunk.m_ChunkPrevious);
		stringBuilder.m_ChunkLength = count;
		int num2 = Math.Min(count, indexInChunk);
		if (num2 > 0)
		{
			new ReadOnlySpan<char>(chunk.m_ChunkChars, 0, num2).CopyTo(stringBuilder.m_ChunkChars);
			int num3 = indexInChunk - num2;
			if (num3 >= 0)
			{
				new ReadOnlySpan<char>(chunk.m_ChunkChars, num2, num3).CopyTo(chunk.m_ChunkChars);
				indexInChunk = num3;
			}
		}
		chunk.m_ChunkPrevious = stringBuilder;
		chunk.m_ChunkOffset += count;
		if (num2 < count)
		{
			chunk = stringBuilder;
			indexInChunk = num2;
		}
	}

	private StringBuilder(int size, int maxCapacity, StringBuilder previousBlock)
	{
		m_ChunkChars = GC.AllocateUninitializedArray<char>(size);
		m_MaxCapacity = maxCapacity;
		m_ChunkPrevious = previousBlock;
		if (previousBlock != null)
		{
			m_ChunkOffset = previousBlock.m_ChunkOffset + previousBlock.m_ChunkLength;
		}
	}

	private void Remove(int startIndex, int count, out StringBuilder chunk, out int indexInChunk)
	{
		int num = startIndex + count;
		chunk = this;
		StringBuilder stringBuilder = null;
		int num2 = 0;
		while (true)
		{
			if (num - chunk.m_ChunkOffset >= 0)
			{
				if (stringBuilder == null)
				{
					stringBuilder = chunk;
					num2 = num - stringBuilder.m_ChunkOffset;
				}
				if (startIndex - chunk.m_ChunkOffset >= 0)
				{
					break;
				}
			}
			else
			{
				chunk.m_ChunkOffset -= count;
			}
			chunk = chunk.m_ChunkPrevious;
		}
		indexInChunk = startIndex - chunk.m_ChunkOffset;
		int num3 = indexInChunk;
		int length = stringBuilder.m_ChunkLength - num2;
		if (stringBuilder != chunk)
		{
			num3 = 0;
			chunk.m_ChunkLength = indexInChunk;
			stringBuilder.m_ChunkPrevious = chunk;
			stringBuilder.m_ChunkOffset = chunk.m_ChunkOffset + chunk.m_ChunkLength;
			if (indexInChunk == 0)
			{
				stringBuilder.m_ChunkPrevious = chunk.m_ChunkPrevious;
				chunk = stringBuilder;
			}
		}
		stringBuilder.m_ChunkLength -= num2 - num3;
		if (num3 != num2)
		{
			new ReadOnlySpan<char>(stringBuilder.m_ChunkChars, num2, length).CopyTo(stringBuilder.m_ChunkChars.AsSpan(num3));
		}
	}
}

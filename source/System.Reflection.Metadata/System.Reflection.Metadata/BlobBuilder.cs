using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection.Internal;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection.Metadata;

[DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
public class BlobBuilder
{
	internal struct Chunks : IEnumerable<BlobBuilder>, IEnumerable, IEnumerator<BlobBuilder>, IEnumerator, IDisposable
	{
		private readonly BlobBuilder _head;

		private BlobBuilder _next;

		private BlobBuilder _currentOpt;

		object IEnumerator.Current => Current;

		public BlobBuilder Current => _currentOpt;

		internal Chunks(BlobBuilder builder)
		{
			_head = builder;
			_next = builder.FirstChunk;
			_currentOpt = null;
		}

		public bool MoveNext()
		{
			if (_currentOpt == _head)
			{
				return false;
			}
			if (_currentOpt == _head._nextOrPrevious)
			{
				_currentOpt = _head;
				return true;
			}
			_currentOpt = _next;
			_next = _next._nextOrPrevious;
			return true;
		}

		public void Reset()
		{
			_currentOpt = null;
			_next = _head.FirstChunk;
		}

		void IDisposable.Dispose()
		{
		}

		public Chunks GetEnumerator()
		{
			return this;
		}

		IEnumerator<BlobBuilder> IEnumerable<BlobBuilder>.GetEnumerator()
		{
			return GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	public struct Blobs : IEnumerable<Blob>, IEnumerable, IEnumerator<Blob>, IEnumerator, IDisposable
	{
		private Chunks _chunks;

		object IEnumerator.Current => Current;

		public Blob Current
		{
			get
			{
				BlobBuilder current = _chunks.Current;
				if (current != null)
				{
					return new Blob(current._buffer, 0, current.Length);
				}
				return default(Blob);
			}
		}

		internal Blobs(BlobBuilder builder)
		{
			_chunks = new Chunks(builder);
		}

		public bool MoveNext()
		{
			return _chunks.MoveNext();
		}

		public void Reset()
		{
			_chunks.Reset();
		}

		void IDisposable.Dispose()
		{
		}

		public Blobs GetEnumerator()
		{
			return this;
		}

		IEnumerator<Blob> IEnumerable<Blob>.GetEnumerator()
		{
			return GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	internal const int DefaultChunkSize = 256;

	internal const int MinChunkSize = 16;

	private BlobBuilder _nextOrPrevious;

	private int _previousLengthOrFrozenSuffixLengthDelta;

	private byte[] _buffer;

	private uint _length;

	private BlobBuilder FirstChunk => _nextOrPrevious._nextOrPrevious;

	private bool IsHead => (_length & 0x80000000u) == 0;

	private int Length => (int)(_length & 0x7FFFFFFF);

	private uint FrozenLength => _length | 0x80000000u;

	public int Count => _previousLengthOrFrozenSuffixLengthDelta + Length;

	private int PreviousLength
	{
		get
		{
			return _previousLengthOrFrozenSuffixLengthDelta;
		}
		set
		{
			_previousLengthOrFrozenSuffixLengthDelta = value;
		}
	}

	protected int FreeBytes => _buffer.Length - Length;

	protected internal int ChunkCapacity => _buffer.Length;

	public BlobBuilder(int capacity = 256)
	{
		if (capacity < 0)
		{
			Throw.ArgumentOutOfRange("capacity");
		}
		_nextOrPrevious = this;
		_buffer = new byte[Math.Max(16, capacity)];
	}

	protected virtual BlobBuilder AllocateChunk(int minimalSize)
	{
		return new BlobBuilder(Math.Max(_buffer.Length, minimalSize));
	}

	protected virtual void FreeChunk()
	{
	}

	public void Clear()
	{
		if (!IsHead)
		{
			Throw.InvalidOperationBuilderAlreadyLinked();
		}
		BlobBuilder firstChunk = FirstChunk;
		if (firstChunk != this)
		{
			byte[] buffer = firstChunk._buffer;
			firstChunk._length = FrozenLength;
			firstChunk._buffer = _buffer;
			_buffer = buffer;
		}
		foreach (BlobBuilder chunk in GetChunks())
		{
			if (chunk != this)
			{
				chunk.ClearChunk();
				chunk.FreeChunk();
			}
		}
		ClearChunk();
	}

	protected void Free()
	{
		Clear();
		FreeChunk();
	}

	internal void ClearChunk()
	{
		_length = 0u;
		_previousLengthOrFrozenSuffixLengthDelta = 0;
		_nextOrPrevious = this;
	}

	private void CheckInvariants()
	{
	}

	internal Chunks GetChunks()
	{
		if (!IsHead)
		{
			Throw.InvalidOperationBuilderAlreadyLinked();
		}
		return new Chunks(this);
	}

	public Blobs GetBlobs()
	{
		if (!IsHead)
		{
			Throw.InvalidOperationBuilderAlreadyLinked();
		}
		return new Blobs(this);
	}

	public bool ContentEquals(BlobBuilder other)
	{
		if (!IsHead)
		{
			Throw.InvalidOperationBuilderAlreadyLinked();
		}
		if (this == other)
		{
			return true;
		}
		if (other == null)
		{
			return false;
		}
		if (!other.IsHead)
		{
			Throw.InvalidOperationBuilderAlreadyLinked();
		}
		if (Count != other.Count)
		{
			return false;
		}
		Chunks chunks = GetChunks();
		Chunks chunks2 = other.GetChunks();
		int num = 0;
		int num2 = 0;
		bool flag = chunks.MoveNext();
		bool flag2 = chunks2.MoveNext();
		while (flag && flag2)
		{
			BlobBuilder current = chunks.Current;
			BlobBuilder current2 = chunks2.Current;
			int num3 = Math.Min(current.Length - num, current2.Length - num2);
			if (!ByteSequenceComparer.Equals(current._buffer, num, current2._buffer, num2, num3))
			{
				return false;
			}
			num += num3;
			num2 += num3;
			if (num == current.Length)
			{
				flag = chunks.MoveNext();
				num = 0;
			}
			if (num2 == current2.Length)
			{
				flag2 = chunks2.MoveNext();
				num2 = 0;
			}
		}
		return flag == flag2;
	}

	public byte[] ToArray()
	{
		return ToArray(0, Count);
	}

	public byte[] ToArray(int start, int byteCount)
	{
		BlobUtilities.ValidateRange(Count, start, byteCount, "byteCount");
		byte[] array = new byte[byteCount];
		int num = 0;
		int num2 = start;
		int num3 = start + byteCount;
		foreach (BlobBuilder chunk in GetChunks())
		{
			int num4 = num + chunk.Length;
			if (num4 > num2)
			{
				int num5 = Math.Min(num3, num4) - num2;
				Array.Copy(chunk._buffer, num2 - num, array, num2 - start, num5);
				num2 += num5;
				if (num2 == num3)
				{
					break;
				}
			}
			num = num4;
		}
		return array;
	}

	public ImmutableArray<byte> ToImmutableArray()
	{
		return ToImmutableArray(0, Count);
	}

	public ImmutableArray<byte> ToImmutableArray(int start, int byteCount)
	{
		byte[] array = ToArray(start, byteCount);
		return ImmutableByteArrayInterop.DangerousCreateFromUnderlyingArray(ref array);
	}

	public void WriteContentTo(Stream destination)
	{
		if (destination == null)
		{
			Throw.ArgumentNull("destination");
		}
		foreach (BlobBuilder chunk in GetChunks())
		{
			destination.Write(chunk._buffer, 0, chunk.Length);
		}
	}

	public void WriteContentTo(ref BlobWriter destination)
	{
		if (destination.IsDefault)
		{
			Throw.ArgumentNull("destination");
		}
		foreach (BlobBuilder chunk in GetChunks())
		{
			destination.WriteBytes(chunk._buffer, 0, chunk.Length);
		}
	}

	public void WriteContentTo(BlobBuilder destination)
	{
		if (destination == null)
		{
			Throw.ArgumentNull("destination");
		}
		foreach (BlobBuilder chunk in GetChunks())
		{
			destination.WriteBytes(chunk._buffer, 0, chunk.Length);
		}
	}

	public void LinkPrefix(BlobBuilder prefix)
	{
		if (prefix == null)
		{
			Throw.ArgumentNull("prefix");
		}
		if (!prefix.IsHead || !IsHead)
		{
			Throw.InvalidOperationBuilderAlreadyLinked();
		}
		if (prefix.Count != 0)
		{
			PreviousLength += prefix.Count;
			prefix._length = prefix.FrozenLength;
			BlobBuilder firstChunk = FirstChunk;
			BlobBuilder firstChunk2 = prefix.FirstChunk;
			BlobBuilder nextOrPrevious = _nextOrPrevious;
			BlobBuilder nextOrPrevious2 = prefix._nextOrPrevious;
			_nextOrPrevious = ((nextOrPrevious != this) ? nextOrPrevious : prefix);
			prefix._nextOrPrevious = ((firstChunk != this) ? firstChunk : ((firstChunk2 != prefix) ? firstChunk2 : prefix));
			if (nextOrPrevious != this)
			{
				nextOrPrevious._nextOrPrevious = ((firstChunk2 != prefix) ? firstChunk2 : prefix);
			}
			if (nextOrPrevious2 != prefix)
			{
				nextOrPrevious2._nextOrPrevious = prefix;
			}
			prefix.CheckInvariants();
			CheckInvariants();
		}
	}

	public void LinkSuffix(BlobBuilder suffix)
	{
		if (suffix == null)
		{
			throw new ArgumentNullException("suffix");
		}
		if (!IsHead || !suffix.IsHead)
		{
			Throw.InvalidOperationBuilderAlreadyLinked();
		}
		if (suffix.Count == 0)
		{
			return;
		}
		bool flag = Count == 0;
		byte[] buffer = suffix._buffer;
		uint length = suffix._length;
		int previousLength = suffix.PreviousLength;
		int length2 = suffix.Length;
		suffix._buffer = _buffer;
		suffix._length = FrozenLength;
		_buffer = buffer;
		_length = length;
		PreviousLength += suffix.Length + previousLength;
		suffix._previousLengthOrFrozenSuffixLengthDelta = previousLength + length2 - suffix.Length;
		if (!flag)
		{
			BlobBuilder firstChunk = FirstChunk;
			BlobBuilder firstChunk2 = suffix.FirstChunk;
			BlobBuilder nextOrPrevious = _nextOrPrevious;
			BlobBuilder blobBuilder = (_nextOrPrevious = suffix._nextOrPrevious);
			suffix._nextOrPrevious = ((firstChunk2 != suffix) ? firstChunk2 : ((firstChunk != this) ? firstChunk : suffix));
			if (nextOrPrevious != this)
			{
				nextOrPrevious._nextOrPrevious = suffix;
			}
			if (blobBuilder != suffix)
			{
				blobBuilder._nextOrPrevious = ((firstChunk != this) ? firstChunk : suffix);
			}
		}
		CheckInvariants();
		suffix.CheckInvariants();
	}

	private void AddLength(int value)
	{
		_length += (uint)value;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void Expand(int newLength)
	{
		if (!IsHead)
		{
			Throw.InvalidOperationBuilderAlreadyLinked();
		}
		BlobBuilder blobBuilder = AllocateChunk(Math.Max(newLength, 16));
		if (blobBuilder.ChunkCapacity < newLength)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.ReturnedBuilderSizeTooSmall, GetType(), "AllocateChunk"));
		}
		byte[] buffer = blobBuilder._buffer;
		if (_length == 0)
		{
			blobBuilder._buffer = _buffer;
			_buffer = buffer;
		}
		else
		{
			BlobBuilder nextOrPrevious = _nextOrPrevious;
			BlobBuilder firstChunk = FirstChunk;
			if (nextOrPrevious == this)
			{
				_nextOrPrevious = blobBuilder;
			}
			else
			{
				blobBuilder._nextOrPrevious = firstChunk;
				nextOrPrevious._nextOrPrevious = blobBuilder;
				_nextOrPrevious = blobBuilder;
			}
			blobBuilder._buffer = _buffer;
			blobBuilder._length = FrozenLength;
			blobBuilder._previousLengthOrFrozenSuffixLengthDelta = PreviousLength;
			_buffer = buffer;
			PreviousLength += Length;
			_length = 0u;
		}
		CheckInvariants();
	}

	public Blob ReserveBytes(int byteCount)
	{
		if (byteCount < 0)
		{
			Throw.ArgumentOutOfRange("byteCount");
		}
		int start = ReserveBytesImpl(byteCount);
		return new Blob(_buffer, start, byteCount);
	}

	private int ReserveBytesImpl(int byteCount)
	{
		uint num = _length;
		if (num > _buffer.Length - byteCount)
		{
			Expand(byteCount);
			num = 0u;
		}
		_length = num + (uint)byteCount;
		return (int)num;
	}

	private int ReserveBytesPrimitive(int byteCount)
	{
		return ReserveBytesImpl(byteCount);
	}

	public void WriteBytes(byte value, int byteCount)
	{
		if (byteCount < 0)
		{
			Throw.ArgumentOutOfRange("byteCount");
		}
		if (!IsHead)
		{
			Throw.InvalidOperationBuilderAlreadyLinked();
		}
		int num = Math.Min(FreeBytes, byteCount);
		_buffer.WriteBytes(Length, value, num);
		AddLength(num);
		int num2 = byteCount - num;
		if (num2 > 0)
		{
			Expand(num2);
			_buffer.WriteBytes(0, value, num2);
			AddLength(num2);
		}
	}

	public unsafe void WriteBytes(byte* buffer, int byteCount)
	{
		if (buffer == null)
		{
			Throw.ArgumentNull("buffer");
		}
		if (byteCount < 0)
		{
			Throw.ArgumentOutOfRange("byteCount");
		}
		if (!IsHead)
		{
			Throw.InvalidOperationBuilderAlreadyLinked();
		}
		WriteBytesUnchecked(buffer, byteCount);
	}

	private unsafe void WriteBytesUnchecked(byte* buffer, int byteCount)
	{
		int num = Math.Min(FreeBytes, byteCount);
		Marshal.Copy((IntPtr)buffer, _buffer, Length, num);
		AddLength(num);
		int num2 = byteCount - num;
		if (num2 > 0)
		{
			Expand(num2);
			Marshal.Copy((IntPtr)(buffer + num), _buffer, 0, num2);
			AddLength(num2);
		}
	}

	public int TryWriteBytes(Stream source, int byteCount)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (byteCount < 0)
		{
			throw new ArgumentOutOfRangeException("byteCount");
		}
		if (byteCount == 0)
		{
			return 0;
		}
		int num = 0;
		int num2 = Math.Min(FreeBytes, byteCount);
		if (num2 > 0)
		{
			num = source.TryReadAll(_buffer, Length, num2);
			AddLength(num);
			if (num != num2)
			{
				return num;
			}
		}
		int num3 = byteCount - num2;
		if (num3 > 0)
		{
			Expand(num3);
			num = source.TryReadAll(_buffer, 0, num3);
			AddLength(num);
			num += num2;
		}
		return num;
	}

	public void WriteBytes(ImmutableArray<byte> buffer)
	{
		WriteBytes(buffer, 0, (!buffer.IsDefault) ? buffer.Length : 0);
	}

	public void WriteBytes(ImmutableArray<byte> buffer, int start, int byteCount)
	{
		WriteBytes(ImmutableByteArrayInterop.DangerousGetUnderlyingArray(buffer), start, byteCount);
	}

	public void WriteBytes(byte[] buffer)
	{
		WriteBytes(buffer, 0, (buffer != null) ? buffer.Length : 0);
	}

	public unsafe void WriteBytes(byte[] buffer, int start, int byteCount)
	{
		if (buffer == null)
		{
			Throw.ArgumentNull("buffer");
		}
		BlobUtilities.ValidateRange(buffer.Length, start, byteCount, "byteCount");
		if (!IsHead)
		{
			Throw.InvalidOperationBuilderAlreadyLinked();
		}
		if (buffer.Length != 0)
		{
			fixed (byte* ptr = &buffer[0])
			{
				WriteBytesUnchecked(ptr + start, byteCount);
			}
		}
	}

	public void PadTo(int position)
	{
		WriteBytes(0, position - Count);
	}

	public void Align(int alignment)
	{
		int count = Count;
		WriteBytes(0, BitArithmetic.Align(count, alignment) - count);
	}

	public void WriteBoolean(bool value)
	{
		WriteByte((byte)(value ? 1u : 0u));
	}

	public void WriteByte(byte value)
	{
		int start = ReserveBytesPrimitive(1);
		_buffer.WriteByte(start, value);
	}

	public void WriteSByte(sbyte value)
	{
		WriteByte((byte)value);
	}

	public void WriteDouble(double value)
	{
		int start = ReserveBytesPrimitive(8);
		_buffer.WriteDouble(start, value);
	}

	public void WriteSingle(float value)
	{
		int start = ReserveBytesPrimitive(4);
		_buffer.WriteSingle(start, value);
	}

	public void WriteInt16(short value)
	{
		WriteUInt16((ushort)value);
	}

	public void WriteUInt16(ushort value)
	{
		int start = ReserveBytesPrimitive(2);
		_buffer.WriteUInt16(start, value);
	}

	public void WriteInt16BE(short value)
	{
		WriteUInt16BE((ushort)value);
	}

	public void WriteUInt16BE(ushort value)
	{
		int start = ReserveBytesPrimitive(2);
		_buffer.WriteUInt16BE(start, value);
	}

	public void WriteInt32BE(int value)
	{
		WriteUInt32BE((uint)value);
	}

	public void WriteUInt32BE(uint value)
	{
		int start = ReserveBytesPrimitive(4);
		_buffer.WriteUInt32BE(start, value);
	}

	public void WriteInt32(int value)
	{
		WriteUInt32((uint)value);
	}

	public void WriteUInt32(uint value)
	{
		int start = ReserveBytesPrimitive(4);
		_buffer.WriteUInt32(start, value);
	}

	public void WriteInt64(long value)
	{
		WriteUInt64((ulong)value);
	}

	public void WriteUInt64(ulong value)
	{
		int start = ReserveBytesPrimitive(8);
		_buffer.WriteUInt64(start, value);
	}

	public void WriteDecimal(decimal value)
	{
		int start = ReserveBytesPrimitive(13);
		_buffer.WriteDecimal(start, value);
	}

	public void WriteGuid(Guid value)
	{
		int start = ReserveBytesPrimitive(16);
		_buffer.WriteGuid(start, value);
	}

	public void WriteDateTime(DateTime value)
	{
		WriteInt64(value.Ticks);
	}

	public void WriteReference(int reference, bool isSmall)
	{
		if (isSmall)
		{
			WriteUInt16((ushort)reference);
		}
		else
		{
			WriteInt32(reference);
		}
	}

	public unsafe void WriteUTF16(char[] value)
	{
		if (value == null)
		{
			Throw.ArgumentNull("value");
		}
		if (!IsHead)
		{
			Throw.InvalidOperationBuilderAlreadyLinked();
		}
		if (value.Length == 0)
		{
			return;
		}
		if (BitConverter.IsLittleEndian)
		{
			fixed (char* buffer = &value[0])
			{
				WriteBytesUnchecked((byte*)buffer, value.Length * 2);
			}
			return;
		}
		for (int i = 0; i < value.Length; i++)
		{
			WriteUInt16(value[i]);
		}
	}

	public unsafe void WriteUTF16(string value)
	{
		if (value == null)
		{
			Throw.ArgumentNull("value");
		}
		if (!IsHead)
		{
			Throw.InvalidOperationBuilderAlreadyLinked();
		}
		if (BitConverter.IsLittleEndian)
		{
			fixed (char* buffer = value)
			{
				WriteBytesUnchecked((byte*)buffer, value.Length * 2);
			}
			return;
		}
		for (int i = 0; i < value.Length; i++)
		{
			WriteUInt16(value[i]);
		}
	}

	public void WriteSerializedString(string? value)
	{
		if (value == null)
		{
			WriteByte(byte.MaxValue);
		}
		else
		{
			WriteUTF8(value, 0, value.Length, allowUnpairedSurrogates: true, prependSize: true);
		}
	}

	public void WriteUserString(string value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		WriteCompressedInteger(BlobUtilities.GetUserStringByteLength(value.Length));
		WriteUTF16(value);
		WriteByte(BlobUtilities.GetUserStringTrailingByte(value));
	}

	public void WriteUTF8(string value, bool allowUnpairedSurrogates = true)
	{
		if (value == null)
		{
			Throw.ArgumentNull("value");
		}
		WriteUTF8(value, 0, value.Length, allowUnpairedSurrogates, prependSize: false);
	}

	internal unsafe void WriteUTF8(string str, int start, int length, bool allowUnpairedSurrogates, bool prependSize)
	{
		if (!IsHead)
		{
			Throw.InvalidOperationBuilderAlreadyLinked();
		}
		fixed (char* ptr = str)
		{
			char* ptr2 = ptr + start;
			int byteLimit = FreeBytes - (prependSize ? 4 : 0);
			char* remainder;
			int uTF8ByteCount = BlobUtilities.GetUTF8ByteCount(ptr2, length, byteLimit, out remainder);
			int num = (int)(remainder - ptr2);
			int charCount = length - num;
			int uTF8ByteCount2 = BlobUtilities.GetUTF8ByteCount(remainder, charCount);
			if (prependSize)
			{
				WriteCompressedInteger(uTF8ByteCount + uTF8ByteCount2);
			}
			_buffer.WriteUTF8(Length, ptr2, num, uTF8ByteCount, allowUnpairedSurrogates);
			AddLength(uTF8ByteCount);
			if (uTF8ByteCount2 > 0)
			{
				Expand(uTF8ByteCount2);
				_buffer.WriteUTF8(0, remainder, charCount, uTF8ByteCount2, allowUnpairedSurrogates);
				AddLength(uTF8ByteCount2);
			}
		}
	}

	public void WriteCompressedSignedInteger(int value)
	{
		BlobWriterImpl.WriteCompressedSignedInteger(this, value);
	}

	public void WriteCompressedInteger(int value)
	{
		BlobWriterImpl.WriteCompressedInteger(this, (uint)value);
	}

	public void WriteConstant(object? value)
	{
		BlobWriterImpl.WriteConstant(this, value);
	}

	internal string GetDebuggerDisplay()
	{
		if (!IsHead)
		{
			return "<" + Display(_buffer, Length) + ">";
		}
		return string.Join("->", from chunk in GetChunks()
			select "[" + Display(chunk._buffer, chunk.Length) + "]");
	}

	private static string Display(byte[] bytes, int length)
	{
		if (length > 64)
		{
			return BitConverter.ToString(bytes, 0, 32) + "-...-" + BitConverter.ToString(bytes, length - 32, 32);
		}
		return BitConverter.ToString(bytes, 0, length);
	}
}

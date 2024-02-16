using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace System.IO;

public class BinaryWriter : IDisposable, IAsyncDisposable
{
	public static readonly BinaryWriter Null = new BinaryWriter();

	protected Stream OutStream;

	private readonly Encoding _encoding;

	private readonly bool _leaveOpen;

	private readonly bool _useFastUtf8;

	public virtual Stream BaseStream
	{
		get
		{
			Flush();
			return OutStream;
		}
	}

	protected BinaryWriter()
	{
		OutStream = Stream.Null;
		_encoding = Encoding.UTF8;
		_useFastUtf8 = true;
	}

	public BinaryWriter(Stream output)
		: this(output, Encoding.UTF8, leaveOpen: false)
	{
	}

	public BinaryWriter(Stream output, Encoding encoding)
		: this(output, encoding, leaveOpen: false)
	{
	}

	public BinaryWriter(Stream output, Encoding encoding, bool leaveOpen)
	{
		if (output == null)
		{
			throw new ArgumentNullException("output");
		}
		if (encoding == null)
		{
			throw new ArgumentNullException("encoding");
		}
		if (!output.CanWrite)
		{
			throw new ArgumentException(SR.Argument_StreamNotWritable);
		}
		OutStream = output;
		_encoding = encoding;
		_leaveOpen = leaveOpen;
		_useFastUtf8 = encoding.IsUTF8CodePage && encoding.EncoderFallback.MaxCharCount <= 1;
	}

	public virtual void Close()
	{
		Dispose(disposing: true);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			if (_leaveOpen)
			{
				OutStream.Flush();
			}
			else
			{
				OutStream.Close();
			}
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
	}

	public virtual ValueTask DisposeAsync()
	{
		try
		{
			if (GetType() == typeof(BinaryWriter))
			{
				if (_leaveOpen)
				{
					return new ValueTask(OutStream.FlushAsync());
				}
				OutStream.Close();
			}
			else
			{
				Dispose();
			}
			return default(ValueTask);
		}
		catch (Exception exception)
		{
			return ValueTask.FromException(exception);
		}
	}

	public virtual void Flush()
	{
		OutStream.Flush();
	}

	public virtual long Seek(int offset, SeekOrigin origin)
	{
		return OutStream.Seek(offset, origin);
	}

	public virtual void Write(bool value)
	{
		OutStream.WriteByte((byte)(value ? 1u : 0u));
	}

	public virtual void Write(byte value)
	{
		OutStream.WriteByte(value);
	}

	[CLSCompliant(false)]
	public virtual void Write(sbyte value)
	{
		OutStream.WriteByte((byte)value);
	}

	public virtual void Write(byte[] buffer)
	{
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer");
		}
		OutStream.Write(buffer, 0, buffer.Length);
	}

	public virtual void Write(byte[] buffer, int index, int count)
	{
		OutStream.Write(buffer, index, count);
	}

	public virtual void Write(char ch)
	{
		if (!Rune.TryCreate(ch, out var result))
		{
			throw new ArgumentException(SR.Arg_SurrogatesNotAllowedAsSingleChar);
		}
		Span<byte> span = stackalloc byte[8];
		if (_useFastUtf8)
		{
			int length = result.EncodeToUtf8(span);
			OutStream.Write(span.Slice(0, length));
			return;
		}
		byte[] array = null;
		int maxByteCount = _encoding.GetMaxByteCount(1);
		if (maxByteCount > span.Length)
		{
			array = ArrayPool<byte>.Shared.Rent(maxByteCount);
			span = array;
		}
		int bytes = _encoding.GetBytes(MemoryMarshal.CreateReadOnlySpan(ref ch, 1), span);
		OutStream.Write(span.Slice(0, bytes));
		if (array != null)
		{
			ArrayPool<byte>.Shared.Return(array);
		}
	}

	public virtual void Write(char[] chars)
	{
		if (chars == null)
		{
			throw new ArgumentNullException("chars");
		}
		WriteCharsCommonWithoutLengthPrefix(chars, useThisWriteOverride: false);
	}

	public virtual void Write(char[] chars, int index, int count)
	{
		if (chars == null)
		{
			throw new ArgumentNullException("chars");
		}
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (index > chars.Length - count)
		{
			throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_IndexCount);
		}
		WriteCharsCommonWithoutLengthPrefix(chars.AsSpan(index, count), useThisWriteOverride: false);
	}

	public virtual void Write(double value)
	{
		Span<byte> span = stackalloc byte[8];
		BinaryPrimitives.WriteDoubleLittleEndian(span, value);
		OutStream.Write(span);
	}

	public virtual void Write(decimal value)
	{
		Span<byte> span = stackalloc byte[16];
		decimal.GetBytes(in value, span);
		OutStream.Write(span);
	}

	public virtual void Write(short value)
	{
		Span<byte> span = stackalloc byte[2];
		BinaryPrimitives.WriteInt16LittleEndian(span, value);
		OutStream.Write(span);
	}

	[CLSCompliant(false)]
	public virtual void Write(ushort value)
	{
		Span<byte> span = stackalloc byte[2];
		BinaryPrimitives.WriteUInt16LittleEndian(span, value);
		OutStream.Write(span);
	}

	public virtual void Write(int value)
	{
		Span<byte> span = stackalloc byte[4];
		BinaryPrimitives.WriteInt32LittleEndian(span, value);
		OutStream.Write(span);
	}

	[CLSCompliant(false)]
	public virtual void Write(uint value)
	{
		Span<byte> span = stackalloc byte[4];
		BinaryPrimitives.WriteUInt32LittleEndian(span, value);
		OutStream.Write(span);
	}

	public virtual void Write(long value)
	{
		Span<byte> span = stackalloc byte[8];
		BinaryPrimitives.WriteInt64LittleEndian(span, value);
		OutStream.Write(span);
	}

	[CLSCompliant(false)]
	public virtual void Write(ulong value)
	{
		Span<byte> span = stackalloc byte[8];
		BinaryPrimitives.WriteUInt64LittleEndian(span, value);
		OutStream.Write(span);
	}

	public virtual void Write(float value)
	{
		Span<byte> span = stackalloc byte[4];
		BinaryPrimitives.WriteSingleLittleEndian(span, value);
		OutStream.Write(span);
	}

	public virtual void Write(Half value)
	{
		Span<byte> span = stackalloc byte[2];
		BinaryPrimitives.WriteHalfLittleEndian(span, value);
		OutStream.Write(span);
	}

	public virtual void Write(string value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		if (_useFastUtf8)
		{
			if (value.Length <= 42)
			{
				Span<byte> span = stackalloc byte[128];
				int bytes = _encoding.GetBytes(value, span.Slice(1));
				span[0] = (byte)bytes;
				OutStream.Write(span.Slice(0, bytes + 1));
				return;
			}
			if (value.Length <= 21845)
			{
				byte[] array = ArrayPool<byte>.Shared.Rent(value.Length * 3);
				int bytes2 = _encoding.GetBytes(value, array);
				Write7BitEncodedInt(bytes2);
				OutStream.Write(array, 0, bytes2);
				ArrayPool<byte>.Shared.Return(array);
				return;
			}
		}
		int byteCount = _encoding.GetByteCount(value);
		Write7BitEncodedInt(byteCount);
		WriteCharsCommonWithoutLengthPrefix(value, useThisWriteOverride: false);
	}

	public virtual void Write(ReadOnlySpan<byte> buffer)
	{
		if (GetType() == typeof(BinaryWriter))
		{
			OutStream.Write(buffer);
			return;
		}
		byte[] array = ArrayPool<byte>.Shared.Rent(buffer.Length);
		try
		{
			buffer.CopyTo(array);
			Write(array, 0, buffer.Length);
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(array);
		}
	}

	public virtual void Write(ReadOnlySpan<char> chars)
	{
		WriteCharsCommonWithoutLengthPrefix(chars, useThisWriteOverride: true);
	}

	private void WriteCharsCommonWithoutLengthPrefix(ReadOnlySpan<char> chars, bool useThisWriteOverride)
	{
		byte[] array;
		if (chars.Length <= 65536)
		{
			int maxByteCount = _encoding.GetMaxByteCount(chars.Length);
			if (maxByteCount <= 65536)
			{
				array = ArrayPool<byte>.Shared.Rent(maxByteCount);
				int bytes = _encoding.GetBytes(chars, array);
				WriteToOutStream(array, 0, bytes, useThisWriteOverride);
				ArrayPool<byte>.Shared.Return(array);
				return;
			}
		}
		array = ArrayPool<byte>.Shared.Rent(65536);
		Encoder encoder = _encoding.GetEncoder();
		bool completed;
		do
		{
			encoder.Convert(chars, array, flush: true, out var charsUsed, out var bytesUsed, out completed);
			if (bytesUsed != 0)
			{
				WriteToOutStream(array, 0, bytesUsed, useThisWriteOverride);
			}
			chars = chars.Slice(charsUsed);
		}
		while (!completed);
		ArrayPool<byte>.Shared.Return(array);
		void WriteToOutStream(byte[] buffer, int offset, int count, bool useThisWriteOverride)
		{
			if (useThisWriteOverride)
			{
				Write(buffer, offset, count);
			}
			else
			{
				OutStream.Write(buffer, offset, count);
			}
		}
	}

	public void Write7BitEncodedInt(int value)
	{
		uint num;
		for (num = (uint)value; num > 127; num >>= 7)
		{
			Write((byte)(num | 0xFFFFFF80u));
		}
		Write((byte)num);
	}

	public void Write7BitEncodedInt64(long value)
	{
		ulong num;
		for (num = (ulong)value; num > 127; num >>= 7)
		{
			Write((byte)((uint)(int)num | 0xFFFFFF80u));
		}
		Write((byte)num);
	}
}

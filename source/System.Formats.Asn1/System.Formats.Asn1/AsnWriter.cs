using System.Buffers;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace System.Formats.Asn1;

public sealed class AsnWriter
{
	private sealed class ArrayIndexSetOfValueComparer : IComparer<(int, int)>
	{
		private readonly byte[] _data;

		public ArrayIndexSetOfValueComparer(byte[] data)
		{
			_data = data;
		}

		public int Compare((int, int) x, (int, int) y)
		{
			(int, int) tuple = x;
			int item = tuple.Item1;
			int item2 = tuple.Item2;
			(int, int) tuple2 = y;
			int item3 = tuple2.Item1;
			int item4 = tuple2.Item2;
			int num = SetOfValueComparer.Instance.Compare(new ReadOnlyMemory<byte>(_data, item, item2), new ReadOnlyMemory<byte>(_data, item3, item4));
			if (num == 0)
			{
				return item - item3;
			}
			return num;
		}
	}

	private readonly struct StackFrame : IEquatable<StackFrame>
	{
		public Asn1Tag Tag { get; }

		public int Offset { get; }

		public UniversalTagNumber ItemType { get; }

		internal StackFrame(Asn1Tag tag, int offset, UniversalTagNumber itemType)
		{
			Tag = tag;
			Offset = offset;
			ItemType = itemType;
		}

		public void Deconstruct(out Asn1Tag tag, out int offset, out UniversalTagNumber itemType)
		{
			tag = Tag;
			offset = Offset;
			itemType = ItemType;
		}

		public bool Equals(StackFrame other)
		{
			if (Tag.Equals(other.Tag) && Offset == other.Offset)
			{
				return ItemType == other.ItemType;
			}
			return false;
		}

		public override bool Equals([NotNullWhen(true)] object obj)
		{
			if (obj is StackFrame other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (Tag, Offset, ItemType).GetHashCode();
		}

		public static bool operator ==(StackFrame left, StackFrame right)
		{
			return left.Equals(right);
		}
	}

	public readonly struct Scope : IDisposable
	{
		private readonly AsnWriter _writer;

		private readonly StackFrame _frame;

		private readonly int _depth;

		internal Scope(AsnWriter writer)
		{
			_writer = writer;
			_frame = _writer._nestingStack.Peek();
			_depth = _writer._nestingStack.Count;
		}

		public void Dispose()
		{
			if (_writer == null || _writer._nestingStack.Count == 0)
			{
				return;
			}
			if (_writer._nestingStack.Peek() == _frame)
			{
				switch (_frame.ItemType)
				{
				case UniversalTagNumber.Set:
					_writer.PopSetOf(_frame.Tag);
					break;
				case UniversalTagNumber.Sequence:
					_writer.PopSequence(_frame.Tag);
					break;
				case UniversalTagNumber.OctetString:
					_writer.PopOctetString(_frame.Tag);
					break;
				default:
					throw new InvalidOperationException();
				}
			}
			else if (_writer._nestingStack.Count > _depth && _writer._nestingStack.Contains(_frame))
			{
				throw new InvalidOperationException(System.SR.AsnWriter_PopWrongTag);
			}
		}
	}

	private byte[] _buffer;

	private int _offset;

	private Stack<StackFrame> _nestingStack;

	public AsnEncodingRules RuleSet { get; }

	public AsnWriter(AsnEncodingRules ruleSet)
	{
		if (ruleSet != 0 && ruleSet != AsnEncodingRules.CER && ruleSet != AsnEncodingRules.DER)
		{
			throw new ArgumentOutOfRangeException("ruleSet");
		}
		RuleSet = ruleSet;
	}

	public void Reset()
	{
		if (_offset > 0)
		{
			Array.Clear(_buffer, 0, _offset);
			_offset = 0;
			_nestingStack?.Clear();
		}
	}

	public int GetEncodedLength()
	{
		Stack<StackFrame> nestingStack = _nestingStack;
		if (nestingStack != null && nestingStack.Count != 0)
		{
			throw new InvalidOperationException(System.SR.AsnWriter_EncodeUnbalancedStack);
		}
		return _offset;
	}

	public bool TryEncode(Span<byte> destination, out int bytesWritten)
	{
		Stack<StackFrame> nestingStack = _nestingStack;
		if (nestingStack != null && nestingStack.Count != 0)
		{
			throw new InvalidOperationException(System.SR.AsnWriter_EncodeUnbalancedStack);
		}
		if (destination.Length < _offset)
		{
			bytesWritten = 0;
			return false;
		}
		if (_offset == 0)
		{
			bytesWritten = 0;
			return true;
		}
		bytesWritten = _offset;
		_buffer.AsSpan(0, _offset).CopyTo(destination);
		return true;
	}

	public int Encode(Span<byte> destination)
	{
		if (!TryEncode(destination, out var bytesWritten))
		{
			throw new ArgumentException(System.SR.Argument_DestinationTooShort, "destination");
		}
		return bytesWritten;
	}

	public byte[] Encode()
	{
		Stack<StackFrame> nestingStack = _nestingStack;
		if (nestingStack != null && nestingStack.Count != 0)
		{
			throw new InvalidOperationException(System.SR.AsnWriter_EncodeUnbalancedStack);
		}
		if (_offset == 0)
		{
			return Array.Empty<byte>();
		}
		return _buffer.AsSpan(0, _offset).ToArray();
	}

	private ReadOnlySpan<byte> EncodeAsSpan()
	{
		Stack<StackFrame> nestingStack = _nestingStack;
		if (nestingStack != null && nestingStack.Count != 0)
		{
			throw new InvalidOperationException(System.SR.AsnWriter_EncodeUnbalancedStack);
		}
		if (_offset == 0)
		{
			return ReadOnlySpan<byte>.Empty;
		}
		return new ReadOnlySpan<byte>(_buffer, 0, _offset);
	}

	public bool EncodedValueEquals(ReadOnlySpan<byte> other)
	{
		return EncodeAsSpan().SequenceEqual(other);
	}

	public bool EncodedValueEquals(AsnWriter other)
	{
		if (other == null)
		{
			throw new ArgumentNullException("other");
		}
		return EncodeAsSpan().SequenceEqual(other.EncodeAsSpan());
	}

	private void EnsureWriteCapacity(int pendingCount)
	{
		if (pendingCount < 0)
		{
			throw new OverflowException();
		}
		if (_buffer == null || _buffer.Length - _offset < pendingCount)
		{
			int num = checked(_offset + pendingCount + 1023) / 1024;
			byte[] buffer = _buffer;
			Array.Resize(ref _buffer, 1024 * num);
			buffer?.AsSpan(0, _offset).Clear();
		}
	}

	private void WriteTag(Asn1Tag tag)
	{
		int num = tag.CalculateEncodedSize();
		EnsureWriteCapacity(num);
		if (!tag.TryEncode(_buffer.AsSpan(_offset, num), out var bytesWritten) || bytesWritten != num)
		{
			throw new InvalidOperationException();
		}
		_offset += num;
	}

	private void WriteLength(int length)
	{
		if (length == -1)
		{
			EnsureWriteCapacity(1);
			_buffer[_offset] = 128;
			_offset++;
			return;
		}
		if (length < 128)
		{
			EnsureWriteCapacity(1 + length);
			_buffer[_offset] = (byte)length;
			_offset++;
			return;
		}
		int encodedLengthSubsequentByteCount = GetEncodedLengthSubsequentByteCount(length);
		EnsureWriteCapacity(encodedLengthSubsequentByteCount + 1 + length);
		_buffer[_offset] = (byte)(0x80u | (uint)encodedLengthSubsequentByteCount);
		int num = _offset + encodedLengthSubsequentByteCount;
		int num2 = length;
		do
		{
			_buffer[num] = (byte)num2;
			num2 >>= 8;
			num--;
		}
		while (num2 > 0);
		_offset += encodedLengthSubsequentByteCount + 1;
	}

	private static int GetEncodedLengthSubsequentByteCount(int length)
	{
		if (length < 0)
		{
			throw new OverflowException();
		}
		if (length <= 127)
		{
			return 0;
		}
		if (length <= 255)
		{
			return 1;
		}
		if (length <= 65535)
		{
			return 2;
		}
		if (length <= 16777215)
		{
			return 3;
		}
		return 4;
	}

	public void CopyTo(AsnWriter destination)
	{
		if (destination == null)
		{
			throw new ArgumentNullException("destination");
		}
		try
		{
			destination.WriteEncodedValue(EncodeAsSpan());
		}
		catch (ArgumentException innerException)
		{
			throw new InvalidOperationException(new InvalidOperationException().Message, innerException);
		}
	}

	public void WriteEncodedValue(ReadOnlySpan<byte> value)
	{
		if (!AsnDecoder.TryReadEncodedValue(value, RuleSet, out var _, out var _, out var _, out var bytesConsumed) || bytesConsumed != value.Length)
		{
			throw new ArgumentException(System.SR.Argument_WriteEncodedValue_OneValueAtATime, "value");
		}
		EnsureWriteCapacity(value.Length);
		value.CopyTo(_buffer.AsSpan(_offset));
		_offset += value.Length;
	}

	private void WriteEndOfContents()
	{
		EnsureWriteCapacity(2);
		_buffer[_offset++] = 0;
		_buffer[_offset++] = 0;
	}

	private Scope PushTag(Asn1Tag tag, UniversalTagNumber tagType)
	{
		if (_nestingStack == null)
		{
			_nestingStack = new Stack<StackFrame>();
		}
		WriteTag(tag);
		_nestingStack.Push(new StackFrame(tag, _offset, tagType));
		WriteLength(-1);
		return new Scope(this);
	}

	private void PopTag(Asn1Tag tag, UniversalTagNumber tagType, bool sortContents = false)
	{
		if (_nestingStack == null || _nestingStack.Count == 0)
		{
			throw new InvalidOperationException(System.SR.AsnWriter_PopWrongTag);
		}
		var (asn1Tag2, num2, universalTagNumber2) = (StackFrame)(ref _nestingStack.Peek());
		if (asn1Tag2 != tag || universalTagNumber2 != tagType)
		{
			throw new InvalidOperationException(System.SR.AsnWriter_PopWrongTag);
		}
		_nestingStack.Pop();
		if (sortContents)
		{
			SortContents(_buffer, num2 + 1, _offset);
		}
		if (RuleSet == AsnEncodingRules.CER && tagType != UniversalTagNumber.OctetString)
		{
			WriteEndOfContents();
			return;
		}
		int num3 = _offset - 1 - num2;
		int num4 = num2 + 1;
		if (tagType == UniversalTagNumber.OctetString)
		{
			if (RuleSet == AsnEncodingRules.CER && num3 > 1000)
			{
				int result;
				int num5 = Math.DivRem(num3, 1000, out result);
				int num6 = 4 * num5 + 2 + GetEncodedLengthSubsequentByteCount(result);
				EnsureWriteCapacity(num6 + 2);
				ReadOnlySpan<byte> readOnlySpan = _buffer.AsSpan(num4, num3);
				Span<byte> span = _buffer.AsSpan(num4 + num6, num3);
				readOnlySpan.CopyTo(span);
				int num7 = num4 + num3 + num6 + 2;
				_offset = num2 - tag.CalculateEncodedSize();
				WriteConstructedCerOctetString(tag, span);
				return;
			}
			int num8 = tag.CalculateEncodedSize();
			tag.AsPrimitive().Encode(_buffer.AsSpan(num2 - num8, num8));
		}
		int encodedLengthSubsequentByteCount = GetEncodedLengthSubsequentByteCount(num3);
		if (encodedLengthSubsequentByteCount == 0)
		{
			_buffer[num2] = (byte)num3;
			return;
		}
		EnsureWriteCapacity(encodedLengthSubsequentByteCount);
		Buffer.BlockCopy(_buffer, num4, _buffer, num4 + encodedLengthSubsequentByteCount, num3);
		int offset = _offset;
		_offset = num2;
		WriteLength(num3);
		_offset = offset + encodedLengthSubsequentByteCount;
	}

	private static void SortContents(byte[] buffer, int start, int end)
	{
		int num = end - start;
		if (num == 0)
		{
			return;
		}
		AsnReader asnReader = new AsnReader(new ReadOnlyMemory<byte>(buffer, start, num), AsnEncodingRules.BER);
		List<(int, int)> list = new List<(int, int)>();
		int num2 = start;
		while (asnReader.HasData)
		{
			ReadOnlyMemory<byte> readOnlyMemory = asnReader.ReadEncodedValue();
			list.Add((num2, readOnlyMemory.Length));
			num2 += readOnlyMemory.Length;
		}
		ArrayIndexSetOfValueComparer comparer = new ArrayIndexSetOfValueComparer(buffer);
		list.Sort(comparer);
		byte[] array = System.Security.Cryptography.CryptoPool.Rent(num);
		num2 = 0;
		foreach (var (srcOffset, num3) in list)
		{
			Buffer.BlockCopy(buffer, srcOffset, array, num2, num3);
			num2 += num3;
		}
		Buffer.BlockCopy(array, 0, buffer, start, num);
		System.Security.Cryptography.CryptoPool.Return(array, num);
	}

	internal static void Reverse(Span<byte> span)
	{
		int num = 0;
		int num2 = span.Length - 1;
		while (num < num2)
		{
			byte b = span[num];
			span[num] = span[num2];
			span[num2] = b;
			num++;
			num2--;
		}
	}

	private static void CheckUniversalTag(Asn1Tag? tag, UniversalTagNumber universalTagNumber)
	{
		if (tag.HasValue)
		{
			Asn1Tag value = tag.Value;
			if (value.TagClass == TagClass.Universal && value.TagValue != (int)universalTagNumber)
			{
				throw new ArgumentException(System.SR.Argument_UniversalValueIsFixed, "tag");
			}
		}
	}

	public void WriteBitString(ReadOnlySpan<byte> value, int unusedBitCount = 0, Asn1Tag? tag = null)
	{
		CheckUniversalTag(tag, UniversalTagNumber.BitString);
		WriteBitStringCore(tag ?? Asn1Tag.PrimitiveBitString, value, unusedBitCount);
	}

	private void WriteBitStringCore(Asn1Tag tag, ReadOnlySpan<byte> bitString, int unusedBitCount)
	{
		if (unusedBitCount < 0 || unusedBitCount > 7)
		{
			throw new ArgumentOutOfRangeException("unusedBitCount", unusedBitCount, System.SR.Argument_UnusedBitCountRange);
		}
		if (bitString.Length == 0 && unusedBitCount != 0)
		{
			throw new ArgumentException(System.SR.Argument_UnusedBitCountMustBeZero, "unusedBitCount");
		}
		byte lastByte = (byte)((!bitString.IsEmpty) ? bitString[bitString.Length - 1] : 0);
		if (!CheckValidLastByte(lastByte, unusedBitCount))
		{
			throw new ArgumentException(System.SR.Argument_UnusedBitWasSet, "unusedBitCount");
		}
		if (RuleSet == AsnEncodingRules.CER && bitString.Length >= 1000)
		{
			WriteConstructedCerBitString(tag, bitString, unusedBitCount);
			return;
		}
		WriteTag(tag.AsPrimitive());
		WriteLength(bitString.Length + 1);
		_buffer[_offset] = (byte)unusedBitCount;
		_offset++;
		bitString.CopyTo(_buffer.AsSpan(_offset));
		_offset += bitString.Length;
	}

	private static bool CheckValidLastByte(byte lastByte, int unusedBitCount)
	{
		int num = (1 << unusedBitCount) - 1;
		return (lastByte & num) == 0;
	}

	private static int DetermineCerBitStringTotalLength(Asn1Tag tag, int contentLength)
	{
		int result;
		int num = Math.DivRem(contentLength, 999, out result);
		int num2 = ((result != 0) ? (3 + result + GetEncodedLengthSubsequentByteCount(result)) : 0);
		return num * 1004 + num2 + 3 + tag.CalculateEncodedSize();
	}

	private void WriteConstructedCerBitString(Asn1Tag tag, ReadOnlySpan<byte> payload, int unusedBitCount)
	{
		int pendingCount = DetermineCerBitStringTotalLength(tag, payload.Length);
		EnsureWriteCapacity(pendingCount);
		int offset = _offset;
		WriteTag(tag.AsConstructed());
		WriteLength(-1);
		byte[] buffer = _buffer;
		ReadOnlySpan<byte> readOnlySpan = payload;
		Asn1Tag primitiveBitString = Asn1Tag.PrimitiveBitString;
		Span<byte> destination;
		while (readOnlySpan.Length > 999)
		{
			WriteTag(primitiveBitString);
			WriteLength(1000);
			_buffer[_offset] = 0;
			_offset++;
			destination = _buffer.AsSpan(_offset);
			readOnlySpan.Slice(0, 999).CopyTo(destination);
			readOnlySpan = readOnlySpan.Slice(999);
			_offset += 999;
		}
		WriteTag(primitiveBitString);
		WriteLength(readOnlySpan.Length + 1);
		_buffer[_offset] = (byte)unusedBitCount;
		_offset++;
		destination = _buffer.AsSpan(_offset);
		readOnlySpan.CopyTo(destination);
		_offset += readOnlySpan.Length;
		WriteEndOfContents();
	}

	public void WriteBoolean(bool value, Asn1Tag? tag = null)
	{
		CheckUniversalTag(tag, UniversalTagNumber.Boolean);
		WriteBooleanCore(tag?.AsPrimitive() ?? Asn1Tag.Boolean, value);
	}

	private void WriteBooleanCore(Asn1Tag tag, bool value)
	{
		WriteTag(tag);
		WriteLength(1);
		_buffer[_offset] = (byte)(value ? 255u : 0u);
		_offset++;
	}

	public void WriteEnumeratedValue(Enum value, Asn1Tag? tag = null)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		WriteEnumeratedValue(tag?.AsPrimitive() ?? Asn1Tag.Enumerated, value.GetType(), value);
	}

	public void WriteEnumeratedValue<TEnum>(TEnum value, Asn1Tag? tag = null) where TEnum : Enum
	{
		WriteEnumeratedValue(tag?.AsPrimitive() ?? Asn1Tag.Enumerated, typeof(TEnum), value);
	}

	private void WriteEnumeratedValue(Asn1Tag tag, Type tEnum, object value)
	{
		CheckUniversalTag(tag, UniversalTagNumber.Enumerated);
		Type enumUnderlyingType = tEnum.GetEnumUnderlyingType();
		if (tEnum.IsDefined(typeof(FlagsAttribute), inherit: false))
		{
			throw new ArgumentException(System.SR.Argument_EnumeratedValueRequiresNonFlagsEnum, "tEnum");
		}
		if (enumUnderlyingType == typeof(ulong))
		{
			ulong value2 = Convert.ToUInt64(value);
			WriteNonNegativeIntegerCore(tag, value2);
		}
		else
		{
			long value3 = Convert.ToInt64(value);
			WriteIntegerCore(tag, value3);
		}
	}

	public void WriteGeneralizedTime(DateTimeOffset value, bool omitFractionalSeconds = false, Asn1Tag? tag = null)
	{
		CheckUniversalTag(tag, UniversalTagNumber.GeneralizedTime);
		WriteGeneralizedTimeCore(tag?.AsPrimitive() ?? Asn1Tag.GeneralizedTime, value, omitFractionalSeconds);
	}

	private void WriteGeneralizedTimeCore(Asn1Tag tag, DateTimeOffset value, bool omitFractionalSeconds)
	{
		DateTimeOffset dateTimeOffset = value.ToUniversalTime();
		if (dateTimeOffset.Year > 9999)
		{
			throw new ArgumentOutOfRangeException("value");
		}
		Span<byte> destination = default(Span<byte>);
		if (!omitFractionalSeconds)
		{
			long num = dateTimeOffset.Ticks % 10000000;
			if (num != 0L)
			{
				destination = stackalloc byte[9];
				decimal value2 = num;
				value2 /= 10000000m;
				if (!Utf8Formatter.TryFormat(value2, destination, out var bytesWritten, new StandardFormat('G')))
				{
					throw new InvalidOperationException();
				}
				destination = destination.Slice(1, bytesWritten - 1);
			}
		}
		int length = 15 + destination.Length;
		WriteTag(tag);
		WriteLength(length);
		int year = dateTimeOffset.Year;
		int month = dateTimeOffset.Month;
		int day = dateTimeOffset.Day;
		int hour = dateTimeOffset.Hour;
		int minute = dateTimeOffset.Minute;
		int second = dateTimeOffset.Second;
		Span<byte> span = _buffer.AsSpan(_offset);
		StandardFormat format = new StandardFormat('D', 4);
		StandardFormat format2 = new StandardFormat('D', 2);
		if (!Utf8Formatter.TryFormat(year, span.Slice(0, 4), out var bytesWritten2, format) || !Utf8Formatter.TryFormat(month, span.Slice(4, 2), out bytesWritten2, format2) || !Utf8Formatter.TryFormat(day, span.Slice(6, 2), out bytesWritten2, format2) || !Utf8Formatter.TryFormat(hour, span.Slice(8, 2), out bytesWritten2, format2) || !Utf8Formatter.TryFormat(minute, span.Slice(10, 2), out bytesWritten2, format2) || !Utf8Formatter.TryFormat(second, span.Slice(12, 2), out bytesWritten2, format2))
		{
			throw new InvalidOperationException();
		}
		_offset += 14;
		destination.CopyTo(span.Slice(14));
		_offset += destination.Length;
		_buffer[_offset] = 90;
		_offset++;
	}

	public void WriteInteger(long value, Asn1Tag? tag = null)
	{
		CheckUniversalTag(tag, UniversalTagNumber.Integer);
		WriteIntegerCore(tag?.AsPrimitive() ?? Asn1Tag.Integer, value);
	}

	[CLSCompliant(false)]
	public void WriteInteger(ulong value, Asn1Tag? tag = null)
	{
		CheckUniversalTag(tag, UniversalTagNumber.Integer);
		WriteNonNegativeIntegerCore(tag?.AsPrimitive() ?? Asn1Tag.Integer, value);
	}

	public void WriteInteger(BigInteger value, Asn1Tag? tag = null)
	{
		CheckUniversalTag(tag, UniversalTagNumber.Integer);
		WriteIntegerCore(tag?.AsPrimitive() ?? Asn1Tag.Integer, value);
	}

	public void WriteInteger(ReadOnlySpan<byte> value, Asn1Tag? tag = null)
	{
		CheckUniversalTag(tag, UniversalTagNumber.Integer);
		WriteIntegerCore(tag?.AsPrimitive() ?? Asn1Tag.Integer, value);
	}

	public void WriteIntegerUnsigned(ReadOnlySpan<byte> value, Asn1Tag? tag = null)
	{
		CheckUniversalTag(tag, UniversalTagNumber.Integer);
		WriteIntegerUnsignedCore(tag?.AsPrimitive() ?? Asn1Tag.Integer, value);
	}

	private void WriteIntegerCore(Asn1Tag tag, long value)
	{
		if (value >= 0)
		{
			WriteNonNegativeIntegerCore(tag, (ulong)value);
			return;
		}
		int num = ((value >= -128) ? 1 : ((value >= -32768) ? 2 : ((value >= -8388608) ? 3 : ((value >= int.MinValue) ? 4 : ((value >= -549755813888L) ? 5 : ((value >= -140737488355328L) ? 6 : ((value < -36028797018963968L) ? 8 : 7)))))));
		WriteTag(tag);
		WriteLength(num);
		long num2 = value;
		int num3 = _offset + num - 1;
		do
		{
			_buffer[num3] = (byte)num2;
			num2 >>= 8;
			num3--;
		}
		while (num3 >= _offset);
		_offset += num;
	}

	private void WriteNonNegativeIntegerCore(Asn1Tag tag, ulong value)
	{
		int num = ((value < 128) ? 1 : ((value < 32768) ? 2 : ((value < 8388608) ? 3 : ((value < 2147483648u) ? 4 : ((value < 549755813888L) ? 5 : ((value < 140737488355328L) ? 6 : ((value < 36028797018963968L) ? 7 : ((value >= 9223372036854775808uL) ? 9 : 8))))))));
		WriteTag(tag);
		WriteLength(num);
		ulong num2 = value;
		int num3 = _offset + num - 1;
		do
		{
			_buffer[num3] = (byte)num2;
			num2 >>= 8;
			num3--;
		}
		while (num3 >= _offset);
		_offset += num;
	}

	private void WriteIntegerUnsignedCore(Asn1Tag tag, ReadOnlySpan<byte> value)
	{
		if (value.IsEmpty)
		{
			throw new ArgumentException(System.SR.Argument_IntegerCannotBeEmpty, "value");
		}
		if (value.Length > 1 && value[0] == 0 && value[1] < 128)
		{
			throw new ArgumentException(System.SR.Argument_IntegerRedundantByte, "value");
		}
		WriteTag(tag);
		if (value[0] >= 128)
		{
			WriteLength(checked(value.Length + 1));
			_buffer[_offset] = 0;
			_offset++;
		}
		else
		{
			WriteLength(value.Length);
		}
		value.CopyTo(_buffer.AsSpan(_offset));
		_offset += value.Length;
	}

	private void WriteIntegerCore(Asn1Tag tag, ReadOnlySpan<byte> value)
	{
		if (value.IsEmpty)
		{
			throw new ArgumentException(System.SR.Argument_IntegerCannotBeEmpty, "value");
		}
		if (value.Length > 1)
		{
			ushort num = (ushort)((value[0] << 8) | value[1]);
			ushort num2 = (ushort)(num & 0xFF80u);
			if (num2 == 0 || num2 == 65408)
			{
				throw new ArgumentException(System.SR.Argument_IntegerRedundantByte, "value");
			}
		}
		WriteTag(tag);
		WriteLength(value.Length);
		value.CopyTo(_buffer.AsSpan(_offset));
		_offset += value.Length;
	}

	private void WriteIntegerCore(Asn1Tag tag, BigInteger value)
	{
		byte[] array = value.ToByteArray();
		Array.Reverse(array);
		WriteTag(tag);
		WriteLength(array.Length);
		Buffer.BlockCopy(array, 0, _buffer, _offset, array.Length);
		_offset += array.Length;
	}

	public void WriteNamedBitList(Enum value, Asn1Tag? tag = null)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		CheckUniversalTag(tag, UniversalTagNumber.BitString);
		WriteNamedBitList(tag, value.GetType(), value);
	}

	public void WriteNamedBitList<TEnum>(TEnum value, Asn1Tag? tag = null) where TEnum : Enum
	{
		CheckUniversalTag(tag, UniversalTagNumber.BitString);
		WriteNamedBitList(tag, typeof(TEnum), value);
	}

	public void WriteNamedBitList(BitArray value, Asn1Tag? tag = null)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		CheckUniversalTag(tag, UniversalTagNumber.BitString);
		WriteBitArray(value, tag);
	}

	private void WriteNamedBitList(Asn1Tag? tag, Type tEnum, Enum value)
	{
		Type enumUnderlyingType = tEnum.GetEnumUnderlyingType();
		if (!tEnum.IsDefined(typeof(FlagsAttribute), inherit: false))
		{
			throw new ArgumentException(System.SR.Argument_NamedBitListRequiresFlagsEnum, "tEnum");
		}
		ulong integralValue;
		if (enumUnderlyingType == typeof(ulong))
		{
			integralValue = Convert.ToUInt64(value);
		}
		else
		{
			long num = Convert.ToInt64(value);
			integralValue = (ulong)num;
		}
		WriteNamedBitList(tag, integralValue);
	}

	private void WriteNamedBitList(Asn1Tag? tag, ulong integralValue)
	{
		Span<byte> span = stackalloc byte[8];
		span.Clear();
		int num = -1;
		int num2 = 0;
		while (integralValue != 0L)
		{
			if ((integralValue & 1) != 0L)
			{
				span[num2 / 8] |= (byte)(128 >> num2 % 8);
				num = num2;
			}
			integralValue >>= 1;
			num2++;
		}
		if (num < 0)
		{
			WriteBitString(ReadOnlySpan<byte>.Empty, 0, tag);
			return;
		}
		int length = num / 8 + 1;
		int unusedBitCount = 7 - num % 8;
		WriteBitString(span.Slice(0, length), unusedBitCount, tag);
	}

	private void WriteBitArray(BitArray value, Asn1Tag? tag)
	{
		if (value.Count == 0)
		{
			WriteBitString(ReadOnlySpan<byte>.Empty, 0, tag);
			return;
		}
		int num = checked(value.Count + 7) / 8;
		int unusedBitCount = num * 8 - value.Count;
		byte[] array = System.Security.Cryptography.CryptoPool.Rent(num);
		value.CopyTo(array, 0);
		Span<byte> span = array.AsSpan(0, num);
		AsnDecoder.ReverseBitsPerByte(span);
		WriteBitString(span, unusedBitCount, tag);
		System.Security.Cryptography.CryptoPool.Return(array, num);
	}

	public void WriteNull(Asn1Tag? tag = null)
	{
		CheckUniversalTag(tag, UniversalTagNumber.Null);
		WriteNullCore(tag?.AsPrimitive() ?? Asn1Tag.Null);
	}

	private void WriteNullCore(Asn1Tag tag)
	{
		WriteTag(tag);
		WriteLength(0);
	}

	public Scope PushOctetString(Asn1Tag? tag = null)
	{
		CheckUniversalTag(tag, UniversalTagNumber.OctetString);
		return PushTag(tag?.AsConstructed() ?? Asn1Tag.ConstructedOctetString, UniversalTagNumber.OctetString);
	}

	public void PopOctetString(Asn1Tag? tag = null)
	{
		CheckUniversalTag(tag, UniversalTagNumber.OctetString);
		PopTag(tag?.AsConstructed() ?? Asn1Tag.ConstructedOctetString, UniversalTagNumber.OctetString);
	}

	public void WriteOctetString(ReadOnlySpan<byte> value, Asn1Tag? tag = null)
	{
		CheckUniversalTag(tag, UniversalTagNumber.OctetString);
		WriteOctetStringCore(tag ?? Asn1Tag.PrimitiveOctetString, value);
	}

	private void WriteOctetStringCore(Asn1Tag tag, ReadOnlySpan<byte> octetString)
	{
		if (RuleSet == AsnEncodingRules.CER && octetString.Length > 1000)
		{
			WriteConstructedCerOctetString(tag, octetString);
			return;
		}
		WriteTag(tag.AsPrimitive());
		WriteLength(octetString.Length);
		octetString.CopyTo(_buffer.AsSpan(_offset));
		_offset += octetString.Length;
	}

	private void WriteConstructedCerOctetString(Asn1Tag tag, ReadOnlySpan<byte> payload)
	{
		WriteTag(tag.AsConstructed());
		WriteLength(-1);
		int result;
		int num = Math.DivRem(payload.Length, 1000, out result);
		int num2 = ((result != 0) ? (2 + result + GetEncodedLengthSubsequentByteCount(result)) : 0);
		int pendingCount = num * 1004 + num2 + 2;
		EnsureWriteCapacity(pendingCount);
		byte[] buffer = _buffer;
		int offset = _offset;
		ReadOnlySpan<byte> readOnlySpan = payload;
		Asn1Tag primitiveOctetString = Asn1Tag.PrimitiveOctetString;
		Span<byte> destination;
		while (readOnlySpan.Length > 1000)
		{
			WriteTag(primitiveOctetString);
			WriteLength(1000);
			destination = _buffer.AsSpan(_offset);
			readOnlySpan.Slice(0, 1000).CopyTo(destination);
			_offset += 1000;
			readOnlySpan = readOnlySpan.Slice(1000);
		}
		WriteTag(primitiveOctetString);
		WriteLength(readOnlySpan.Length);
		destination = _buffer.AsSpan(_offset);
		readOnlySpan.CopyTo(destination);
		_offset += readOnlySpan.Length;
		WriteEndOfContents();
	}

	public void WriteObjectIdentifier(string oidValue, Asn1Tag? tag = null)
	{
		if (oidValue == null)
		{
			throw new ArgumentNullException("oidValue");
		}
		WriteObjectIdentifier(oidValue.AsSpan(), tag);
	}

	public void WriteObjectIdentifier(ReadOnlySpan<char> oidValue, Asn1Tag? tag = null)
	{
		CheckUniversalTag(tag, UniversalTagNumber.ObjectIdentifier);
		WriteObjectIdentifierCore(tag?.AsPrimitive() ?? Asn1Tag.ObjectIdentifier, oidValue);
	}

	private void WriteObjectIdentifierCore(Asn1Tag tag, ReadOnlySpan<char> oidValue)
	{
		if (oidValue.Length < 3)
		{
			throw new ArgumentException(System.SR.Argument_InvalidOidValue, "oidValue");
		}
		if (oidValue[1] != '.')
		{
			throw new ArgumentException(System.SR.Argument_InvalidOidValue, "oidValue");
		}
		byte[] array = System.Security.Cryptography.CryptoPool.Rent(oidValue.Length / 2);
		int num = 0;
		try
		{
			int num2 = oidValue[0] switch
			{
				'0' => 0, 
				'1' => 1, 
				'2' => 2, 
				_ => throw new ArgumentException(System.SR.Argument_InvalidOidValue, "oidValue"), 
			};
			ReadOnlySpan<char> oidValue2 = oidValue.Slice(2);
			BigInteger subIdentifier = ParseSubIdentifier(ref oidValue2);
			subIdentifier += (BigInteger)(40 * num2);
			int num3 = EncodeSubIdentifier(array.AsSpan(num), ref subIdentifier);
			num += num3;
			while (!oidValue2.IsEmpty)
			{
				subIdentifier = ParseSubIdentifier(ref oidValue2);
				num3 = EncodeSubIdentifier(array.AsSpan(num), ref subIdentifier);
				num += num3;
			}
			WriteTag(tag);
			WriteLength(num);
			Buffer.BlockCopy(array, 0, _buffer, _offset, num);
			_offset += num;
		}
		finally
		{
			System.Security.Cryptography.CryptoPool.Return(array, num);
		}
	}

	private static BigInteger ParseSubIdentifier(ref ReadOnlySpan<char> oidValue)
	{
		int num = oidValue.IndexOf('.');
		if (num == -1)
		{
			num = oidValue.Length;
		}
		else if (num == 0 || num == oidValue.Length - 1)
		{
			throw new ArgumentException(System.SR.Argument_InvalidOidValue, "oidValue");
		}
		BigInteger zero = BigInteger.Zero;
		for (int i = 0; i < num; i++)
		{
			if (i > 0 && zero == 0L)
			{
				throw new ArgumentException(System.SR.Argument_InvalidOidValue, "oidValue");
			}
			zero *= (BigInteger)10;
			zero += (BigInteger)AtoI(oidValue[i]);
		}
		oidValue = oidValue.Slice(Math.Min(oidValue.Length, num + 1));
		return zero;
	}

	private static int AtoI(char c)
	{
		if (c >= '0' && c <= '9')
		{
			return c - 48;
		}
		throw new ArgumentException(System.SR.Argument_InvalidOidValue, "oidValue");
	}

	private static int EncodeSubIdentifier(Span<byte> dest, ref BigInteger subIdentifier)
	{
		if (subIdentifier.IsZero)
		{
			dest[0] = 0;
			return 1;
		}
		BigInteger bigInteger = subIdentifier;
		int num = 0;
		do
		{
			BigInteger bigInteger2 = bigInteger & 127;
			byte b = (byte)bigInteger2;
			if (subIdentifier != bigInteger)
			{
				b = (byte)(b | 0x80u);
			}
			bigInteger >>= 7;
			dest[num] = b;
			num++;
		}
		while (bigInteger != BigInteger.Zero);
		Reverse(dest.Slice(0, num));
		return num;
	}

	public Scope PushSequence(Asn1Tag? tag = null)
	{
		CheckUniversalTag(tag, UniversalTagNumber.Sequence);
		return PushSequenceCore(tag?.AsConstructed() ?? Asn1Tag.Sequence);
	}

	public void PopSequence(Asn1Tag? tag = null)
	{
		CheckUniversalTag(tag, UniversalTagNumber.Sequence);
		PopSequenceCore(tag?.AsConstructed() ?? Asn1Tag.Sequence);
	}

	private Scope PushSequenceCore(Asn1Tag tag)
	{
		return PushTag(tag, UniversalTagNumber.Sequence);
	}

	private void PopSequenceCore(Asn1Tag tag)
	{
		PopTag(tag, UniversalTagNumber.Sequence);
	}

	public Scope PushSetOf(Asn1Tag? tag = null)
	{
		CheckUniversalTag(tag, UniversalTagNumber.Set);
		return PushSetOfCore(tag?.AsConstructed() ?? Asn1Tag.SetOf);
	}

	public void PopSetOf(Asn1Tag? tag = null)
	{
		CheckUniversalTag(tag, UniversalTagNumber.Set);
		PopSetOfCore(tag?.AsConstructed() ?? Asn1Tag.SetOf);
	}

	private Scope PushSetOfCore(Asn1Tag tag)
	{
		return PushTag(tag, UniversalTagNumber.Set);
	}

	private void PopSetOfCore(Asn1Tag tag)
	{
		bool sortContents = RuleSet == AsnEncodingRules.CER || RuleSet == AsnEncodingRules.DER;
		PopTag(tag, UniversalTagNumber.Set, sortContents);
	}

	public void WriteCharacterString(UniversalTagNumber encodingType, string value, Asn1Tag? tag = null)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		WriteCharacterString(encodingType, value.AsSpan(), tag);
	}

	public void WriteCharacterString(UniversalTagNumber encodingType, ReadOnlySpan<char> str, Asn1Tag? tag = null)
	{
		CheckUniversalTag(tag, encodingType);
		Encoding encoding = AsnCharacterStringEncodings.GetEncoding(encodingType);
		WriteCharacterStringCore(tag ?? new Asn1Tag(encodingType), encoding, str);
	}

	private void WriteCharacterStringCore(Asn1Tag tag, Encoding encoding, ReadOnlySpan<char> str)
	{
		int byteCount = encoding.GetByteCount(str);
		if (RuleSet == AsnEncodingRules.CER && byteCount > 1000)
		{
			WriteConstructedCerCharacterString(tag, encoding, str, byteCount);
			return;
		}
		WriteTag(tag.AsPrimitive());
		WriteLength(byteCount);
		Span<byte> bytes = _buffer.AsSpan(_offset, byteCount);
		int bytes2 = encoding.GetBytes(str, bytes);
		if (bytes2 != byteCount)
		{
			throw new InvalidOperationException();
		}
		_offset += byteCount;
	}

	private void WriteConstructedCerCharacterString(Asn1Tag tag, Encoding encoding, ReadOnlySpan<char> str, int size)
	{
		byte[] array = System.Security.Cryptography.CryptoPool.Rent(size);
		int bytes = encoding.GetBytes(str, array);
		if (bytes != size)
		{
			throw new InvalidOperationException();
		}
		WriteConstructedCerOctetString(tag, array.AsSpan(0, size));
		System.Security.Cryptography.CryptoPool.Return(array, size);
	}

	public void WriteUtcTime(DateTimeOffset value, Asn1Tag? tag = null)
	{
		CheckUniversalTag(tag, UniversalTagNumber.UtcTime);
		WriteUtcTimeCore(tag?.AsPrimitive() ?? Asn1Tag.UtcTime, value);
	}

	public void WriteUtcTime(DateTimeOffset value, int twoDigitYearMax, Asn1Tag? tag = null)
	{
		CheckUniversalTag(tag, UniversalTagNumber.UtcTime);
		value = value.ToUniversalTime();
		if (value.Year > twoDigitYearMax || value.Year <= twoDigitYearMax - 100)
		{
			throw new ArgumentOutOfRangeException("value");
		}
		WriteUtcTimeCore(tag?.AsPrimitive() ?? Asn1Tag.UtcTime, value);
	}

	private void WriteUtcTimeCore(Asn1Tag tag, DateTimeOffset value)
	{
		WriteTag(tag);
		WriteLength(13);
		DateTimeOffset dateTimeOffset = value.ToUniversalTime();
		int year = dateTimeOffset.Year;
		int month = dateTimeOffset.Month;
		int day = dateTimeOffset.Day;
		int hour = dateTimeOffset.Hour;
		int minute = dateTimeOffset.Minute;
		int second = dateTimeOffset.Second;
		Span<byte> span = _buffer.AsSpan(_offset);
		StandardFormat format = new StandardFormat('D', 2);
		if (!Utf8Formatter.TryFormat(year % 100, span.Slice(0, 2), out var bytesWritten, format) || !Utf8Formatter.TryFormat(month, span.Slice(2, 2), out bytesWritten, format) || !Utf8Formatter.TryFormat(day, span.Slice(4, 2), out bytesWritten, format) || !Utf8Formatter.TryFormat(hour, span.Slice(6, 2), out bytesWritten, format) || !Utf8Formatter.TryFormat(minute, span.Slice(8, 2), out bytesWritten, format) || !Utf8Formatter.TryFormat(second, span.Slice(10, 2), out bytesWritten, format))
		{
			throw new InvalidOperationException();
		}
		_buffer[_offset + 12] = 90;
		_offset += 13;
	}
}

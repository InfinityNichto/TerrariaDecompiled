using System.Collections;
using System.Numerics;

namespace System.Formats.Asn1;

public class AsnReader
{
	private ReadOnlyMemory<byte> _data;

	private readonly AsnReaderOptions _options;

	public AsnEncodingRules RuleSet { get; }

	public bool HasData => !_data.IsEmpty;

	public AsnReader(ReadOnlyMemory<byte> data, AsnEncodingRules ruleSet, AsnReaderOptions options = default(AsnReaderOptions))
	{
		AsnDecoder.CheckEncodingRules(ruleSet);
		_data = data;
		RuleSet = ruleSet;
		_options = options;
	}

	public void ThrowIfNotEmpty()
	{
		if (HasData)
		{
			throw new AsnContentException(System.SR.ContentException_TooMuchData);
		}
	}

	public Asn1Tag PeekTag()
	{
		int bytesConsumed;
		return Asn1Tag.Decode(_data.Span, out bytesConsumed);
	}

	public ReadOnlyMemory<byte> PeekEncodedValue()
	{
		AsnDecoder.ReadEncodedValue(_data.Span, RuleSet, out var _, out var _, out var bytesConsumed);
		return _data.Slice(0, bytesConsumed);
	}

	public ReadOnlyMemory<byte> PeekContentBytes()
	{
		AsnDecoder.ReadEncodedValue(_data.Span, RuleSet, out var contentOffset, out var contentLength, out var _);
		return _data.Slice(contentOffset, contentLength);
	}

	public ReadOnlyMemory<byte> ReadEncodedValue()
	{
		ReadOnlyMemory<byte> result = PeekEncodedValue();
		_data = _data.Slice(result.Length);
		return result;
	}

	private AsnReader CloneAtSlice(int start, int length)
	{
		return new AsnReader(_data.Slice(start, length), RuleSet, _options);
	}

	public bool TryReadPrimitiveBitString(out int unusedBitCount, out ReadOnlyMemory<byte> value, Asn1Tag? expectedTag = null)
	{
		ReadOnlySpan<byte> value2;
		int bytesConsumed;
		bool flag = AsnDecoder.TryReadPrimitiveBitString(_data.Span, RuleSet, out unusedBitCount, out value2, out bytesConsumed, expectedTag);
		if (flag)
		{
			value = AsnDecoder.Slice(_data, value2);
			_data = _data.Slice(bytesConsumed);
		}
		else
		{
			value = default(ReadOnlyMemory<byte>);
		}
		return flag;
	}

	public bool TryReadBitString(Span<byte> destination, out int unusedBitCount, out int bytesWritten, Asn1Tag? expectedTag = null)
	{
		int bytesConsumed;
		bool flag = AsnDecoder.TryReadBitString(_data.Span, destination, RuleSet, out unusedBitCount, out bytesConsumed, out bytesWritten, expectedTag);
		if (flag)
		{
			_data = _data.Slice(bytesConsumed);
		}
		return flag;
	}

	public byte[] ReadBitString(out int unusedBitCount, Asn1Tag? expectedTag = null)
	{
		int bytesConsumed;
		byte[] result = AsnDecoder.ReadBitString(_data.Span, RuleSet, out unusedBitCount, out bytesConsumed, expectedTag);
		_data = _data.Slice(bytesConsumed);
		return result;
	}

	public bool ReadBoolean(Asn1Tag? expectedTag = null)
	{
		int bytesConsumed;
		bool result = AsnDecoder.ReadBoolean(_data.Span, RuleSet, out bytesConsumed, expectedTag);
		_data = _data.Slice(bytesConsumed);
		return result;
	}

	public ReadOnlyMemory<byte> ReadEnumeratedBytes(Asn1Tag? expectedTag = null)
	{
		int bytesConsumed;
		ReadOnlySpan<byte> smaller = AsnDecoder.ReadEnumeratedBytes(_data.Span, RuleSet, out bytesConsumed, expectedTag);
		ReadOnlyMemory<byte> result = AsnDecoder.Slice(_data, smaller);
		_data = _data.Slice(bytesConsumed);
		return result;
	}

	public TEnum ReadEnumeratedValue<TEnum>(Asn1Tag? expectedTag = null) where TEnum : Enum
	{
		int bytesConsumed;
		TEnum result = AsnDecoder.ReadEnumeratedValue<TEnum>(_data.Span, RuleSet, out bytesConsumed, expectedTag);
		_data = _data.Slice(bytesConsumed);
		return result;
	}

	public Enum ReadEnumeratedValue(Type enumType, Asn1Tag? expectedTag = null)
	{
		int bytesConsumed;
		Enum result = AsnDecoder.ReadEnumeratedValue(_data.Span, RuleSet, enumType, out bytesConsumed, expectedTag);
		_data = _data.Slice(bytesConsumed);
		return result;
	}

	public DateTimeOffset ReadGeneralizedTime(Asn1Tag? expectedTag = null)
	{
		int bytesConsumed;
		DateTimeOffset result = AsnDecoder.ReadGeneralizedTime(_data.Span, RuleSet, out bytesConsumed, expectedTag);
		_data = _data.Slice(bytesConsumed);
		return result;
	}

	public ReadOnlyMemory<byte> ReadIntegerBytes(Asn1Tag? expectedTag = null)
	{
		int bytesConsumed;
		ReadOnlySpan<byte> smaller = AsnDecoder.ReadIntegerBytes(_data.Span, RuleSet, out bytesConsumed, expectedTag);
		ReadOnlyMemory<byte> result = AsnDecoder.Slice(_data, smaller);
		_data = _data.Slice(bytesConsumed);
		return result;
	}

	public BigInteger ReadInteger(Asn1Tag? expectedTag = null)
	{
		int bytesConsumed;
		BigInteger result = AsnDecoder.ReadInteger(_data.Span, RuleSet, out bytesConsumed, expectedTag);
		_data = _data.Slice(bytesConsumed);
		return result;
	}

	public bool TryReadInt32(out int value, Asn1Tag? expectedTag = null)
	{
		int bytesConsumed;
		bool result = AsnDecoder.TryReadInt32(_data.Span, RuleSet, out value, out bytesConsumed, expectedTag);
		_data = _data.Slice(bytesConsumed);
		return result;
	}

	[CLSCompliant(false)]
	public bool TryReadUInt32(out uint value, Asn1Tag? expectedTag = null)
	{
		int bytesConsumed;
		bool result = AsnDecoder.TryReadUInt32(_data.Span, RuleSet, out value, out bytesConsumed, expectedTag);
		_data = _data.Slice(bytesConsumed);
		return result;
	}

	public bool TryReadInt64(out long value, Asn1Tag? expectedTag = null)
	{
		int bytesConsumed;
		bool result = AsnDecoder.TryReadInt64(_data.Span, RuleSet, out value, out bytesConsumed, expectedTag);
		_data = _data.Slice(bytesConsumed);
		return result;
	}

	[CLSCompliant(false)]
	public bool TryReadUInt64(out ulong value, Asn1Tag? expectedTag = null)
	{
		int bytesConsumed;
		bool result = AsnDecoder.TryReadUInt64(_data.Span, RuleSet, out value, out bytesConsumed, expectedTag);
		_data = _data.Slice(bytesConsumed);
		return result;
	}

	public TFlagsEnum ReadNamedBitListValue<TFlagsEnum>(Asn1Tag? expectedTag = null) where TFlagsEnum : Enum
	{
		int bytesConsumed;
		TFlagsEnum result = AsnDecoder.ReadNamedBitListValue<TFlagsEnum>(_data.Span, RuleSet, out bytesConsumed, expectedTag);
		_data = _data.Slice(bytesConsumed);
		return result;
	}

	public Enum ReadNamedBitListValue(Type flagsEnumType, Asn1Tag? expectedTag = null)
	{
		int bytesConsumed;
		Enum result = AsnDecoder.ReadNamedBitListValue(_data.Span, RuleSet, flagsEnumType, out bytesConsumed, expectedTag);
		_data = _data.Slice(bytesConsumed);
		return result;
	}

	public BitArray ReadNamedBitList(Asn1Tag? expectedTag = null)
	{
		int bytesConsumed;
		BitArray result = AsnDecoder.ReadNamedBitList(_data.Span, RuleSet, out bytesConsumed, expectedTag);
		_data = _data.Slice(bytesConsumed);
		return result;
	}

	public void ReadNull(Asn1Tag? expectedTag = null)
	{
		AsnDecoder.ReadNull(_data.Span, RuleSet, out var bytesConsumed, expectedTag);
		_data = _data.Slice(bytesConsumed);
	}

	public bool TryReadOctetString(Span<byte> destination, out int bytesWritten, Asn1Tag? expectedTag = null)
	{
		int bytesConsumed;
		bool flag = AsnDecoder.TryReadOctetString(_data.Span, destination, RuleSet, out bytesConsumed, out bytesWritten, expectedTag);
		if (flag)
		{
			_data = _data.Slice(bytesConsumed);
		}
		return flag;
	}

	public byte[] ReadOctetString(Asn1Tag? expectedTag = null)
	{
		int bytesConsumed;
		byte[] result = AsnDecoder.ReadOctetString(_data.Span, RuleSet, out bytesConsumed, expectedTag);
		_data = _data.Slice(bytesConsumed);
		return result;
	}

	public bool TryReadPrimitiveOctetString(out ReadOnlyMemory<byte> contents, Asn1Tag? expectedTag = null)
	{
		ReadOnlySpan<byte> value;
		int bytesConsumed;
		bool flag = AsnDecoder.TryReadPrimitiveOctetString(_data.Span, RuleSet, out value, out bytesConsumed, expectedTag);
		if (flag)
		{
			contents = AsnDecoder.Slice(_data, value);
			_data = _data.Slice(bytesConsumed);
		}
		else
		{
			contents = default(ReadOnlyMemory<byte>);
		}
		return flag;
	}

	public string ReadObjectIdentifier(Asn1Tag? expectedTag = null)
	{
		int bytesConsumed;
		string result = AsnDecoder.ReadObjectIdentifier(_data.Span, RuleSet, out bytesConsumed, expectedTag);
		_data = _data.Slice(bytesConsumed);
		return result;
	}

	public AsnReader ReadSequence(Asn1Tag? expectedTag = null)
	{
		AsnDecoder.ReadSequence(_data.Span, RuleSet, out var contentOffset, out var contentLength, out var bytesConsumed, expectedTag);
		AsnReader result = CloneAtSlice(contentOffset, contentLength);
		_data = _data.Slice(bytesConsumed);
		return result;
	}

	public AsnReader ReadSetOf(Asn1Tag? expectedTag = null)
	{
		return ReadSetOf(_options.SkipSetSortOrderVerification, expectedTag);
	}

	public AsnReader ReadSetOf(bool skipSortOrderValidation, Asn1Tag? expectedTag = null)
	{
		AsnDecoder.ReadSetOf(_data.Span, RuleSet, out var contentOffset, out var contentLength, out var bytesConsumed, skipSortOrderValidation, expectedTag);
		AsnReader result = CloneAtSlice(contentOffset, contentLength);
		_data = _data.Slice(bytesConsumed);
		return result;
	}

	public bool TryReadPrimitiveCharacterStringBytes(Asn1Tag expectedTag, out ReadOnlyMemory<byte> contents)
	{
		ReadOnlySpan<byte> value;
		int bytesConsumed;
		bool flag = AsnDecoder.TryReadPrimitiveCharacterStringBytes(_data.Span, RuleSet, expectedTag, out value, out bytesConsumed);
		if (flag)
		{
			contents = AsnDecoder.Slice(_data, value);
			_data = _data.Slice(bytesConsumed);
		}
		else
		{
			contents = default(ReadOnlyMemory<byte>);
		}
		return flag;
	}

	public bool TryReadCharacterStringBytes(Span<byte> destination, Asn1Tag expectedTag, out int bytesWritten)
	{
		int bytesConsumed;
		bool flag = AsnDecoder.TryReadCharacterStringBytes(_data.Span, destination, RuleSet, expectedTag, out bytesConsumed, out bytesWritten);
		if (flag)
		{
			_data = _data.Slice(bytesConsumed);
		}
		return flag;
	}

	public bool TryReadCharacterString(Span<char> destination, UniversalTagNumber encodingType, out int charsWritten, Asn1Tag? expectedTag = null)
	{
		int bytesConsumed;
		bool result = AsnDecoder.TryReadCharacterString(_data.Span, destination, RuleSet, encodingType, out bytesConsumed, out charsWritten, expectedTag);
		_data = _data.Slice(bytesConsumed);
		return result;
	}

	public string ReadCharacterString(UniversalTagNumber encodingType, Asn1Tag? expectedTag = null)
	{
		int bytesConsumed;
		string result = AsnDecoder.ReadCharacterString(_data.Span, RuleSet, encodingType, out bytesConsumed, expectedTag);
		_data = _data.Slice(bytesConsumed);
		return result;
	}

	public DateTimeOffset ReadUtcTime(Asn1Tag? expectedTag = null)
	{
		int bytesConsumed;
		DateTimeOffset result = AsnDecoder.ReadUtcTime(_data.Span, RuleSet, out bytesConsumed, _options.UtcTimeTwoDigitYearMax, expectedTag);
		_data = _data.Slice(bytesConsumed);
		return result;
	}

	public DateTimeOffset ReadUtcTime(int twoDigitYearMax, Asn1Tag? expectedTag = null)
	{
		int bytesConsumed;
		DateTimeOffset result = AsnDecoder.ReadUtcTime(_data.Span, RuleSet, out bytesConsumed, twoDigitYearMax, expectedTag);
		_data = _data.Slice(bytesConsumed);
		return result;
	}
}

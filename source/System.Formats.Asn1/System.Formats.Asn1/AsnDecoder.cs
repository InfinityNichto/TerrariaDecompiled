using System.Buffers.Binary;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace System.Formats.Asn1;

public static class AsnDecoder
{
	private enum LengthDecodeStatus
	{
		NeedMoreData,
		DerIndefinite,
		ReservedValue,
		LengthTooBig,
		LaxEncodingProhibited,
		Success
	}

	private enum LengthValidity
	{
		CerRequiresIndefinite,
		PrimitiveEncodingRequiresDefinite,
		LengthExceedsInput,
		Valid
	}

	private delegate void BitStringCopyAction(ReadOnlySpan<byte> value, byte normalizedLastByte, Span<byte> destination);

	public static bool TryReadEncodedValue(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, out Asn1Tag tag, out int contentOffset, out int contentLength, out int bytesConsumed)
	{
		CheckEncodingRules(ruleSet);
		if (Asn1Tag.TryDecode(source, out var tag2, out var bytesConsumed2) && TryReadLength(source.Slice(bytesConsumed2), ruleSet, out var length, out var bytesRead))
		{
			int num = bytesConsumed2 + bytesRead;
			int actualLength;
			int bytesConsumed3;
			LengthValidity lengthValidity = ValidateLength(source.Slice(num), ruleSet, tag2, length, out actualLength, out bytesConsumed3);
			if (lengthValidity == LengthValidity.Valid)
			{
				tag = tag2;
				contentOffset = num;
				contentLength = actualLength;
				bytesConsumed = num + bytesConsumed3;
				return true;
			}
		}
		tag = default(Asn1Tag);
		contentOffset = (contentLength = (bytesConsumed = 0));
		return false;
	}

	public static Asn1Tag ReadEncodedValue(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, out int contentOffset, out int contentLength, out int bytesConsumed)
	{
		CheckEncodingRules(ruleSet);
		int bytesConsumed2;
		Asn1Tag asn1Tag = Asn1Tag.Decode(source, out bytesConsumed2);
		int bytesConsumed3;
		int? encodedLength = ReadLength(source.Slice(bytesConsumed2), ruleSet, out bytesConsumed3);
		int num = bytesConsumed2 + bytesConsumed3;
		int actualLength;
		int bytesConsumed4;
		LengthValidity lengthValidity = ValidateLength(source.Slice(num), ruleSet, asn1Tag, encodedLength, out actualLength, out bytesConsumed4);
		if (lengthValidity == LengthValidity.Valid)
		{
			contentOffset = num;
			contentLength = actualLength;
			bytesConsumed = num + bytesConsumed4;
			return asn1Tag;
		}
		throw GetValidityException(lengthValidity);
	}

	private static ReadOnlySpan<byte> GetPrimitiveContentSpan(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, Asn1Tag expectedTag, UniversalTagNumber tagNumber, out int bytesConsumed)
	{
		CheckEncodingRules(ruleSet);
		int bytesConsumed2;
		Asn1Tag tag = Asn1Tag.Decode(source, out bytesConsumed2);
		int bytesConsumed3;
		int? num = ReadLength(source.Slice(bytesConsumed2), ruleSet, out bytesConsumed3);
		int num2 = bytesConsumed2 + bytesConsumed3;
		CheckExpectedTag(tag, expectedTag, tagNumber);
		if (tag.IsConstructed)
		{
			throw new AsnContentException(System.SR.Format(System.SR.ContentException_PrimitiveEncodingRequired, tagNumber));
		}
		if (!num.HasValue)
		{
			throw new AsnContentException();
		}
		ReadOnlySpan<byte> result = Slice(source, num2, num.Value);
		bytesConsumed = num2 + result.Length;
		return result;
	}

	private static bool TryReadLength(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, out int? length, out int bytesRead)
	{
		return DecodeLength(source, ruleSet, out length, out bytesRead) == LengthDecodeStatus.Success;
	}

	private static int? ReadLength(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, out int bytesConsumed)
	{
		int? length;
		switch (DecodeLength(source, ruleSet, out length, out bytesConsumed))
		{
		case LengthDecodeStatus.Success:
			return length;
		case LengthDecodeStatus.LengthTooBig:
			throw new AsnContentException(System.SR.ContentException_LengthTooBig);
		case LengthDecodeStatus.DerIndefinite:
		case LengthDecodeStatus.LaxEncodingProhibited:
			throw new AsnContentException(System.SR.ContentException_LengthRuleSetConstraint);
		default:
			throw new AsnContentException();
		}
	}

	private static LengthDecodeStatus DecodeLength(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, out int? length, out int bytesRead)
	{
		length = null;
		bytesRead = 0;
		if (source.IsEmpty)
		{
			return LengthDecodeStatus.NeedMoreData;
		}
		byte b = source[bytesRead];
		bytesRead++;
		if (b == 128)
		{
			if (ruleSet == AsnEncodingRules.DER)
			{
				bytesRead = 0;
				return LengthDecodeStatus.DerIndefinite;
			}
			return LengthDecodeStatus.Success;
		}
		if (b < 128)
		{
			length = b;
			return LengthDecodeStatus.Success;
		}
		if (b == byte.MaxValue)
		{
			bytesRead = 0;
			return LengthDecodeStatus.ReservedValue;
		}
		byte b2 = (byte)(b & 0xFFFFFF7Fu);
		if (b2 + 1 > source.Length)
		{
			bytesRead = 0;
			return LengthDecodeStatus.NeedMoreData;
		}
		bool flag = ruleSet == AsnEncodingRules.DER || ruleSet == AsnEncodingRules.CER;
		if (flag && b2 > 4)
		{
			bytesRead = 0;
			return LengthDecodeStatus.LengthTooBig;
		}
		uint num = 0u;
		for (int i = 0; i < b2; i++)
		{
			byte b3 = source[bytesRead];
			bytesRead++;
			if (num == 0)
			{
				if (flag && b3 == 0)
				{
					bytesRead = 0;
					return LengthDecodeStatus.LaxEncodingProhibited;
				}
				if (!flag && b3 != 0 && b2 - i > 4)
				{
					bytesRead = 0;
					return LengthDecodeStatus.LengthTooBig;
				}
			}
			num <<= 8;
			num |= b3;
		}
		if (num > int.MaxValue)
		{
			bytesRead = 0;
			return LengthDecodeStatus.LengthTooBig;
		}
		if (flag && num < 128)
		{
			bytesRead = 0;
			return LengthDecodeStatus.LaxEncodingProhibited;
		}
		length = (int)num;
		return LengthDecodeStatus.Success;
	}

	private static Asn1Tag ReadTagAndLength(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, out int? contentsLength, out int bytesRead)
	{
		int bytesConsumed;
		Asn1Tag result = Asn1Tag.Decode(source, out bytesConsumed);
		int bytesConsumed2;
		int? num = ReadLength(source.Slice(bytesConsumed), ruleSet, out bytesConsumed2);
		int num2 = bytesConsumed + bytesConsumed2;
		if (result.IsConstructed)
		{
			if (ruleSet == AsnEncodingRules.CER && num.HasValue)
			{
				throw GetValidityException(LengthValidity.CerRequiresIndefinite);
			}
		}
		else if (!num.HasValue)
		{
			throw GetValidityException(LengthValidity.PrimitiveEncodingRequiresDefinite);
		}
		bytesRead = num2;
		contentsLength = num;
		return result;
	}

	private static void ValidateEndOfContents(Asn1Tag tag, int? length, int headerLength)
	{
		if (tag.IsConstructed || length != 0 || headerLength != 2)
		{
			throw new AsnContentException();
		}
	}

	private static LengthValidity ValidateLength(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, Asn1Tag localTag, int? encodedLength, out int actualLength, out int bytesConsumed)
	{
		if (localTag.IsConstructed)
		{
			if (ruleSet == AsnEncodingRules.CER && encodedLength.HasValue)
			{
				actualLength = (bytesConsumed = 0);
				return LengthValidity.CerRequiresIndefinite;
			}
		}
		else if (!encodedLength.HasValue)
		{
			actualLength = (bytesConsumed = 0);
			return LengthValidity.PrimitiveEncodingRequiresDefinite;
		}
		if (encodedLength.HasValue)
		{
			int value = encodedLength.Value;
			int num = value;
			if (num > source.Length)
			{
				actualLength = (bytesConsumed = 0);
				return LengthValidity.LengthExceedsInput;
			}
			actualLength = value;
			bytesConsumed = value;
			return LengthValidity.Valid;
		}
		actualLength = SeekEndOfContents(source, ruleSet);
		bytesConsumed = actualLength + 2;
		return LengthValidity.Valid;
	}

	private static AsnContentException GetValidityException(LengthValidity validity)
	{
		return validity switch
		{
			LengthValidity.CerRequiresIndefinite => new AsnContentException(System.SR.ContentException_CerRequiresIndefiniteLength), 
			LengthValidity.LengthExceedsInput => new AsnContentException(System.SR.ContentException_LengthExceedsPayload), 
			_ => new AsnContentException(), 
		};
	}

	private static int SeekEndOfContents(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet)
	{
		ReadOnlySpan<byte> source2 = source;
		int num = 0;
		int num2 = 1;
		while (!source2.IsEmpty)
		{
			int? contentsLength;
			int bytesRead;
			Asn1Tag asn1Tag = ReadTagAndLength(source2, ruleSet, out contentsLength, out bytesRead);
			if (asn1Tag == Asn1Tag.EndOfContents)
			{
				ValidateEndOfContents(asn1Tag, contentsLength, bytesRead);
				num2--;
				if (num2 == 0)
				{
					return num;
				}
			}
			if (!contentsLength.HasValue)
			{
				num2++;
				source2 = source2.Slice(bytesRead);
				num += bytesRead;
			}
			else
			{
				ReadOnlySpan<byte> readOnlySpan = Slice(source2, 0, bytesRead + contentsLength.Value);
				source2 = source2.Slice(readOnlySpan.Length);
				num += readOnlySpan.Length;
			}
		}
		throw new AsnContentException();
	}

	private static int ParseNonNegativeIntAndSlice(ref ReadOnlySpan<byte> data, int bytesToRead)
	{
		int result = ParseNonNegativeInt(Slice(data, 0, bytesToRead));
		data = data.Slice(bytesToRead);
		return result;
	}

	private static int ParseNonNegativeInt(ReadOnlySpan<byte> data)
	{
		if (Utf8Parser.TryParse(data, out uint value, out int bytesConsumed, '\0') && value <= int.MaxValue && bytesConsumed == data.Length)
		{
			return (int)value;
		}
		throw new AsnContentException();
	}

	private static ReadOnlySpan<byte> SliceAtMost(ReadOnlySpan<byte> source, int longestPermitted)
	{
		return source[..Math.Min(longestPermitted, source.Length)];
	}

	private static ReadOnlySpan<byte> Slice(ReadOnlySpan<byte> source, int offset, int length)
	{
		if (length < 0 || source.Length - offset < length)
		{
			throw new AsnContentException(System.SR.ContentException_LengthExceedsPayload);
		}
		return source.Slice(offset, length);
	}

	private static ReadOnlySpan<byte> Slice(ReadOnlySpan<byte> source, int offset, int? length)
	{
		if (!length.HasValue)
		{
			return source.Slice(offset);
		}
		int value = length.Value;
		if (value < 0 || source.Length - offset < value)
		{
			throw new AsnContentException(System.SR.ContentException_LengthExceedsPayload);
		}
		return source.Slice(offset, value);
	}

	internal static ReadOnlyMemory<byte> Slice(ReadOnlyMemory<byte> bigger, ReadOnlySpan<byte> smaller)
	{
		if (smaller.IsEmpty)
		{
			return default(ReadOnlyMemory<byte>);
		}
		if (bigger.Span.Overlaps(smaller, out var elementOffset))
		{
			return bigger.Slice(elementOffset, smaller.Length);
		}
		throw new AsnContentException();
	}

	internal static void CheckEncodingRules(AsnEncodingRules ruleSet)
	{
		if (ruleSet != 0 && ruleSet != AsnEncodingRules.CER && ruleSet != AsnEncodingRules.DER)
		{
			throw new ArgumentOutOfRangeException("ruleSet");
		}
	}

	private static void CheckExpectedTag(Asn1Tag tag, Asn1Tag expectedTag, UniversalTagNumber tagNumber)
	{
		if (expectedTag.TagClass == TagClass.Universal && expectedTag.TagValue != (int)tagNumber)
		{
			throw new ArgumentException(System.SR.Argument_UniversalValueIsFixed, "expectedTag");
		}
		if (expectedTag.TagClass != tag.TagClass || expectedTag.TagValue != tag.TagValue)
		{
			throw new AsnContentException(System.SR.Format(System.SR.ContentException_WrongTag, tag.TagClass, tag.TagValue, expectedTag.TagClass, expectedTag.TagValue));
		}
	}

	public static bool TryReadPrimitiveBitString(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, out int unusedBitCount, out ReadOnlySpan<byte> value, out int bytesConsumed, Asn1Tag? expectedTag = null)
	{
		if (TryReadPrimitiveBitStringCore(source, ruleSet, expectedTag ?? Asn1Tag.PrimitiveBitString, out var _, out var _, out var unusedBitCount2, out var value2, out var bytesConsumed2, out var normalizedLastByte) && (value2.Length == 0 || normalizedLastByte == value2[value2.Length - 1]))
		{
			unusedBitCount = unusedBitCount2;
			value = value2;
			bytesConsumed = bytesConsumed2;
			return true;
		}
		unusedBitCount = 0;
		value = default(ReadOnlySpan<byte>);
		bytesConsumed = 0;
		return false;
	}

	public static bool TryReadBitString(ReadOnlySpan<byte> source, Span<byte> destination, AsnEncodingRules ruleSet, out int unusedBitCount, out int bytesConsumed, out int bytesWritten, Asn1Tag? expectedTag = null)
	{
		if (source.Overlaps(destination))
		{
			throw new ArgumentException(System.SR.Argument_SourceOverlapsDestination, "destination");
		}
		if (TryReadPrimitiveBitStringCore(source, ruleSet, expectedTag ?? Asn1Tag.PrimitiveBitString, out var contentsLength, out var headerLength, out var unusedBitCount2, out var value, out var bytesConsumed2, out var normalizedLastByte))
		{
			if (value.Length > destination.Length)
			{
				bytesConsumed = 0;
				bytesWritten = 0;
				unusedBitCount = 0;
				return false;
			}
			CopyBitStringValue(value, normalizedLastByte, destination);
			bytesWritten = value.Length;
			bytesConsumed = bytesConsumed2;
			unusedBitCount = unusedBitCount2;
			return true;
		}
		if (TryCopyConstructedBitStringValue(Slice(source, headerLength, contentsLength), ruleSet, destination, !contentsLength.HasValue, out unusedBitCount2, out var bytesRead, out var bytesWritten2))
		{
			unusedBitCount = unusedBitCount2;
			bytesConsumed = headerLength + bytesRead;
			bytesWritten = bytesWritten2;
			return true;
		}
		bytesWritten = (bytesConsumed = (unusedBitCount = 0));
		return false;
	}

	public static byte[] ReadBitString(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, out int unusedBitCount, out int bytesConsumed, Asn1Tag? expectedTag = null)
	{
		if (TryReadPrimitiveBitStringCore(source, ruleSet, expectedTag ?? Asn1Tag.PrimitiveBitString, out var contentsLength, out var headerLength, out var unusedBitCount2, out var value, out var bytesConsumed2, out var normalizedLastByte))
		{
			byte[] array = value.ToArray();
			if (value.Length > 0)
			{
				array[^1] = normalizedLastByte;
			}
			unusedBitCount = unusedBitCount2;
			bytesConsumed = bytesConsumed2;
			return array;
		}
		int minimumLength = contentsLength ?? SeekEndOfContents(source.Slice(headerLength), ruleSet);
		byte[] array2 = System.Security.Cryptography.CryptoPool.Rent(minimumLength);
		if (TryCopyConstructedBitStringValue(Slice(source, headerLength, contentsLength), ruleSet, array2, !contentsLength.HasValue, out unusedBitCount2, out var bytesRead, out var bytesWritten))
		{
			byte[] result = array2.AsSpan(0, bytesWritten).ToArray();
			System.Security.Cryptography.CryptoPool.Return(array2, bytesWritten);
			unusedBitCount = unusedBitCount2;
			bytesConsumed = headerLength + bytesRead;
			return result;
		}
		throw new AsnContentException();
	}

	private static void ParsePrimitiveBitStringContents(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, out int unusedBitCount, out ReadOnlySpan<byte> value, out byte normalizedLastByte)
	{
		if (ruleSet == AsnEncodingRules.CER && source.Length > 1000)
		{
			throw new AsnContentException(System.SR.ContentException_InvalidUnderCer_TryBerOrDer);
		}
		if (source.Length == 0)
		{
			throw new AsnContentException();
		}
		unusedBitCount = source[0];
		if (unusedBitCount > 7)
		{
			throw new AsnContentException();
		}
		if (source.Length == 1)
		{
			if (unusedBitCount > 0)
			{
				throw new AsnContentException();
			}
			value = ReadOnlySpan<byte>.Empty;
			normalizedLastByte = 0;
			return;
		}
		int num = -1 << unusedBitCount;
		byte b = source[source.Length - 1];
		byte b2 = (byte)(b & num);
		if (b2 != b && (ruleSet == AsnEncodingRules.DER || ruleSet == AsnEncodingRules.CER))
		{
			throw new AsnContentException(System.SR.ContentException_InvalidUnderCerOrDer_TryBer);
		}
		normalizedLastByte = b2;
		value = source.Slice(1);
	}

	private static void CopyBitStringValue(ReadOnlySpan<byte> value, byte normalizedLastByte, Span<byte> destination)
	{
		if (value.Length != 0)
		{
			value.CopyTo(destination);
			destination[value.Length - 1] = normalizedLastByte;
		}
	}

	private static int CountConstructedBitString(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, bool isIndefinite)
	{
		Span<byte> empty = Span<byte>.Empty;
		int lastUnusedBitCount;
		int bytesRead;
		return ProcessConstructedBitString(source, ruleSet, empty, null, isIndefinite, out lastUnusedBitCount, out bytesRead);
	}

	private static void CopyConstructedBitString(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, Span<byte> destination, bool isIndefinite, out int unusedBitCount, out int bytesRead, out int bytesWritten)
	{
		bytesWritten = ProcessConstructedBitString(source, ruleSet, destination, delegate(ReadOnlySpan<byte> value, byte lastByte, Span<byte> dest)
		{
			CopyBitStringValue(value, lastByte, dest);
		}, isIndefinite, out unusedBitCount, out bytesRead);
	}

	private static int ProcessConstructedBitString(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, Span<byte> destination, BitStringCopyAction copyAction, bool isIndefinite, out int lastUnusedBitCount, out int bytesRead)
	{
		lastUnusedBitCount = 0;
		bytesRead = 0;
		int num = 1000;
		ReadOnlySpan<byte> readOnlySpan = source;
		Stack<(int, int, bool, int)> stack = null;
		int num2 = 0;
		Asn1Tag asn1Tag = Asn1Tag.ConstructedBitString;
		Span<byte> destination2 = destination;
		while (true)
		{
			if (!readOnlySpan.IsEmpty)
			{
				asn1Tag = ReadTagAndLength(readOnlySpan, ruleSet, out var contentsLength, out var bytesRead2);
				if (asn1Tag == Asn1Tag.PrimitiveBitString)
				{
					if (lastUnusedBitCount != 0)
					{
						throw new AsnContentException();
					}
					if (ruleSet == AsnEncodingRules.CER && num != 1000)
					{
						throw new AsnContentException(System.SR.ContentException_InvalidUnderCer_TryBerOrDer);
					}
					ReadOnlySpan<byte> source2 = Slice(readOnlySpan, bytesRead2, contentsLength.Value);
					ParsePrimitiveBitStringContents(source2, ruleSet, out lastUnusedBitCount, out var value, out var normalizedLastByte);
					int num3 = bytesRead2 + source2.Length;
					readOnlySpan = readOnlySpan.Slice(num3);
					bytesRead += num3;
					num2 += value.Length;
					num = source2.Length;
					if (copyAction != null)
					{
						copyAction(value, normalizedLastByte, destination2);
						destination2 = destination2.Slice(value.Length);
					}
					continue;
				}
				if (!(asn1Tag == Asn1Tag.EndOfContents && isIndefinite))
				{
					if (asn1Tag == Asn1Tag.ConstructedBitString)
					{
						if (ruleSet == AsnEncodingRules.CER)
						{
							throw new AsnContentException(System.SR.ContentException_InvalidUnderCerOrDer_TryBer);
						}
						if (stack == null)
						{
							stack = new Stack<(int, int, bool, int)>();
						}
						if (!source.Overlaps(readOnlySpan, out var elementOffset))
						{
							throw new AsnContentException();
						}
						stack.Push((elementOffset, readOnlySpan.Length, isIndefinite, bytesRead));
						readOnlySpan = Slice(readOnlySpan, bytesRead2, contentsLength);
						bytesRead = bytesRead2;
						isIndefinite = !contentsLength.HasValue;
						continue;
					}
					throw new AsnContentException();
				}
				ValidateEndOfContents(asn1Tag, contentsLength, bytesRead2);
				bytesRead += bytesRead2;
				if (stack != null && stack.Count > 0)
				{
					(int, int, bool, int) tuple = stack.Pop();
					int item = tuple.Item1;
					int item2 = tuple.Item2;
					bool item3 = tuple.Item3;
					int item4 = tuple.Item4;
					readOnlySpan = source.Slice(item, item2).Slice(bytesRead);
					bytesRead += item4;
					isIndefinite = item3;
					continue;
				}
			}
			if (isIndefinite && asn1Tag != Asn1Tag.EndOfContents)
			{
				throw new AsnContentException();
			}
			if (stack == null || stack.Count <= 0)
			{
				break;
			}
			(int, int, bool, int) tuple2 = stack.Pop();
			int item5 = tuple2.Item1;
			int item6 = tuple2.Item2;
			bool item7 = tuple2.Item3;
			int item8 = tuple2.Item4;
			readOnlySpan = source.Slice(item5, item6).Slice(bytesRead);
			isIndefinite = item7;
			bytesRead += item8;
		}
		return num2;
	}

	private static bool TryCopyConstructedBitStringValue(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, Span<byte> dest, bool isIndefinite, out int unusedBitCount, out int bytesRead, out int bytesWritten)
	{
		int num = CountConstructedBitString(source, ruleSet, isIndefinite);
		if (ruleSet == AsnEncodingRules.CER && num < 1000)
		{
			throw new AsnContentException(System.SR.ContentException_InvalidUnderCerOrDer_TryBer);
		}
		if (dest.Length < num)
		{
			unusedBitCount = 0;
			bytesRead = 0;
			bytesWritten = 0;
			return false;
		}
		CopyConstructedBitString(source, ruleSet, dest, isIndefinite, out unusedBitCount, out bytesRead, out bytesWritten);
		return true;
	}

	private static bool TryReadPrimitiveBitStringCore(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, Asn1Tag expectedTag, out int? contentsLength, out int headerLength, out int unusedBitCount, out ReadOnlySpan<byte> value, out int bytesConsumed, out byte normalizedLastByte)
	{
		Asn1Tag tag = ReadTagAndLength(source, ruleSet, out contentsLength, out headerLength);
		CheckExpectedTag(tag, expectedTag, UniversalTagNumber.BitString);
		ReadOnlySpan<byte> source2 = Slice(source, headerLength, contentsLength);
		if (tag.IsConstructed)
		{
			if (ruleSet == AsnEncodingRules.DER)
			{
				throw new AsnContentException(System.SR.ContentException_InvalidUnderDer_TryBerOrCer);
			}
			unusedBitCount = 0;
			value = default(ReadOnlySpan<byte>);
			normalizedLastByte = 0;
			bytesConsumed = 0;
			return false;
		}
		ParsePrimitiveBitStringContents(source2, ruleSet, out unusedBitCount, out value, out normalizedLastByte);
		bytesConsumed = headerLength + source2.Length;
		return true;
	}

	public static bool ReadBoolean(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, out int bytesConsumed, Asn1Tag? expectedTag = null)
	{
		int bytesConsumed2;
		ReadOnlySpan<byte> primitiveContentSpan = GetPrimitiveContentSpan(source, ruleSet, expectedTag ?? Asn1Tag.Boolean, UniversalTagNumber.Boolean, out bytesConsumed2);
		if (primitiveContentSpan.Length != 1)
		{
			throw new AsnContentException();
		}
		switch (primitiveContentSpan[0])
		{
		case 0:
			bytesConsumed = bytesConsumed2;
			return false;
		default:
			if (ruleSet == AsnEncodingRules.DER || ruleSet == AsnEncodingRules.CER)
			{
				throw new AsnContentException(System.SR.ContentException_InvalidUnderCerOrDer_TryBer);
			}
			break;
		case byte.MaxValue:
			break;
		}
		bytesConsumed = bytesConsumed2;
		return true;
	}

	public static ReadOnlySpan<byte> ReadEnumeratedBytes(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, out int bytesConsumed, Asn1Tag? expectedTag = null)
	{
		return GetIntegerContents(source, ruleSet, expectedTag ?? Asn1Tag.Enumerated, UniversalTagNumber.Enumerated, out bytesConsumed);
	}

	public static TEnum ReadEnumeratedValue<TEnum>(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, out int bytesConsumed, Asn1Tag? expectedTag = null) where TEnum : Enum
	{
		Type typeFromHandle = typeof(TEnum);
		return (TEnum)Enum.ToObject(typeFromHandle, ReadEnumeratedValue(source, ruleSet, typeFromHandle, out bytesConsumed, expectedTag));
	}

	public static Enum ReadEnumeratedValue(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, Type enumType, out int bytesConsumed, Asn1Tag? expectedTag = null)
	{
		if (enumType == null)
		{
			throw new ArgumentNullException("enumType");
		}
		Asn1Tag expectedTag2 = expectedTag ?? Asn1Tag.Enumerated;
		Type enumUnderlyingType = enumType.GetEnumUnderlyingType();
		if (enumType.IsDefined(typeof(FlagsAttribute), inherit: false))
		{
			throw new ArgumentException(System.SR.Argument_EnumeratedValueRequiresNonFlagsEnum, "enumType");
		}
		int sizeLimit = Marshal.SizeOf(enumUnderlyingType);
		if (enumUnderlyingType == typeof(int) || enumUnderlyingType == typeof(long) || enumUnderlyingType == typeof(short) || enumUnderlyingType == typeof(sbyte))
		{
			if (!TryReadSignedInteger(source, ruleSet, sizeLimit, expectedTag2, UniversalTagNumber.Enumerated, out var value, out var bytesConsumed2))
			{
				throw new AsnContentException(System.SR.ContentException_EnumeratedValueTooBig);
			}
			bytesConsumed = bytesConsumed2;
			return (Enum)Enum.ToObject(enumType, value);
		}
		if (enumUnderlyingType == typeof(uint) || enumUnderlyingType == typeof(ulong) || enumUnderlyingType == typeof(ushort) || enumUnderlyingType == typeof(byte))
		{
			if (!TryReadUnsignedInteger(source, ruleSet, sizeLimit, expectedTag2, UniversalTagNumber.Enumerated, out var value2, out var bytesConsumed3))
			{
				throw new AsnContentException(System.SR.ContentException_EnumeratedValueTooBig);
			}
			bytesConsumed = bytesConsumed3;
			return (Enum)Enum.ToObject(enumType, value2);
		}
		throw new AsnContentException(System.SR.Format(System.SR.Argument_EnumeratedValueBackingTypeNotSupported, enumUnderlyingType.FullName));
	}

	public static DateTimeOffset ReadGeneralizedTime(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, out int bytesConsumed, Asn1Tag? expectedTag = null)
	{
		byte[] rented = null;
		Span<byte> tmpSpace = stackalloc byte[64];
		int bytesConsumed2;
		ReadOnlySpan<byte> octetStringContents = GetOctetStringContents(source, ruleSet, expectedTag ?? Asn1Tag.GeneralizedTime, UniversalTagNumber.GeneralizedTime, out bytesConsumed2, ref rented, tmpSpace);
		DateTimeOffset result = ParseGeneralizedTime(ruleSet, octetStringContents);
		if (rented != null)
		{
			System.Security.Cryptography.CryptoPool.Return(rented, octetStringContents.Length);
		}
		bytesConsumed = bytesConsumed2;
		return result;
	}

	private static DateTimeOffset ParseGeneralizedTime(AsnEncodingRules ruleSet, ReadOnlySpan<byte> contentOctets)
	{
		bool flag = ruleSet == AsnEncodingRules.DER || ruleSet == AsnEncodingRules.CER;
		if (flag && contentOctets.Length < 15)
		{
			throw new AsnContentException(System.SR.ContentException_InvalidUnderCerOrDer_TryBer);
		}
		if (contentOctets.Length < 10)
		{
			throw new AsnContentException();
		}
		ReadOnlySpan<byte> data = contentOctets;
		int year = ParseNonNegativeIntAndSlice(ref data, 4);
		int month = ParseNonNegativeIntAndSlice(ref data, 2);
		int day = ParseNonNegativeIntAndSlice(ref data, 2);
		int hour = ParseNonNegativeIntAndSlice(ref data, 2);
		int? num = null;
		int? num2 = null;
		ulong value = 0uL;
		ulong num3 = 1uL;
		byte b = byte.MaxValue;
		TimeSpan? timeSpan = null;
		bool flag2 = false;
		byte b2 = 0;
		while (b2 == 0 && data.Length != 0)
		{
			byte? b3 = GetNextState(data[0]);
			if (!b3.HasValue)
			{
				if (!num.HasValue)
				{
					num = ParseNonNegativeIntAndSlice(ref data, 2);
					continue;
				}
				if (num2.HasValue)
				{
					throw new AsnContentException();
				}
				num2 = ParseNonNegativeIntAndSlice(ref data, 2);
			}
			else
			{
				b2 = b3.Value;
			}
		}
		if (b2 == 1)
		{
			switch (data[0])
			{
			case 44:
				if (flag)
				{
					throw new AsnContentException(System.SR.ContentException_InvalidUnderCerOrDer_TryBer);
				}
				break;
			default:
				throw new AsnContentException();
			case 46:
				break;
			}
			data = data.Slice(1);
			if (data.IsEmpty)
			{
				throw new AsnContentException();
			}
			if (!Utf8Parser.TryParse(SliceAtMost(data, 12), out value, out int bytesConsumed, '\0') || bytesConsumed == 0)
			{
				throw new AsnContentException();
			}
			b = (byte)(value % 10);
			for (int i = 0; i < bytesConsumed; i++)
			{
				num3 *= 10;
			}
			data = data.Slice(bytesConsumed);
			uint value2;
			while (Utf8Parser.TryParse(SliceAtMost(data, 9), out value2, out bytesConsumed, '\0'))
			{
				data = data.Slice(bytesConsumed);
				b = (byte)(value2 % 10);
			}
			if (data.Length != 0)
			{
				byte? b4 = GetNextState(data[0]);
				if (!b4.HasValue)
				{
					throw new AsnContentException();
				}
				b2 = b4.Value;
			}
		}
		if (b2 == 2)
		{
			byte b5 = data[0];
			data = data.Slice(1);
			if (b5 == 90)
			{
				timeSpan = TimeSpan.Zero;
				flag2 = true;
			}
			else
			{
				bool flag3 = b5 switch
				{
					43 => false, 
					45 => true, 
					_ => throw new AsnContentException(), 
				};
				if (data.IsEmpty)
				{
					throw new AsnContentException();
				}
				int hours = ParseNonNegativeIntAndSlice(ref data, 2);
				int num4 = 0;
				if (data.Length != 0)
				{
					num4 = ParseNonNegativeIntAndSlice(ref data, 2);
				}
				if (num4 > 59)
				{
					throw new AsnContentException();
				}
				TimeSpan timeSpan2 = new TimeSpan(hours, num4, 0);
				if (flag3)
				{
					timeSpan2 = -timeSpan2;
				}
				timeSpan = timeSpan2;
			}
		}
		if (!data.IsEmpty)
		{
			throw new AsnContentException();
		}
		if (flag)
		{
			if (!flag2 || !num2.HasValue)
			{
				throw new AsnContentException(System.SR.ContentException_InvalidUnderCerOrDer_TryBer);
			}
			if (b == 0)
			{
				throw new AsnContentException(System.SR.ContentException_InvalidUnderCerOrDer_TryBer);
			}
		}
		double num5 = (double)value / (double)num3;
		TimeSpan timeSpan3 = TimeSpan.Zero;
		if (!num.HasValue)
		{
			num = 0;
			num2 = 0;
			if (value != 0L)
			{
				timeSpan3 = new TimeSpan((long)(num5 * 36000000000.0));
			}
		}
		else if (!num2.HasValue)
		{
			num2 = 0;
			if (value != 0L)
			{
				timeSpan3 = new TimeSpan((long)(num5 * 600000000.0));
			}
		}
		else if (value != 0L)
		{
			timeSpan3 = new TimeSpan((long)(num5 * 10000000.0));
		}
		try
		{
			DateTimeOffset dateTimeOffset = (timeSpan.HasValue ? new DateTimeOffset(year, month, day, hour, num.Value, num2.Value, timeSpan.Value) : new DateTimeOffset(new DateTime(year, month, day, hour, num.Value, num2.Value)));
			return dateTimeOffset + timeSpan3;
		}
		catch (Exception inner)
		{
			throw new AsnContentException(System.SR.ContentException_DefaultMessage, inner);
		}
		static byte? GetNextState(byte octet)
		{
			switch (octet)
			{
			case 43:
			case 45:
			case 90:
				return 2;
			case 44:
			case 46:
				return 1;
			default:
				return null;
			}
		}
	}

	public static ReadOnlySpan<byte> ReadIntegerBytes(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, out int bytesConsumed, Asn1Tag? expectedTag = null)
	{
		return GetIntegerContents(source, ruleSet, expectedTag ?? Asn1Tag.Integer, UniversalTagNumber.Integer, out bytesConsumed);
	}

	public static BigInteger ReadInteger(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, out int bytesConsumed, Asn1Tag? expectedTag = null)
	{
		int bytesConsumed2;
		ReadOnlySpan<byte> readOnlySpan = ReadIntegerBytes(source, ruleSet, out bytesConsumed2, expectedTag);
		byte[] array = System.Security.Cryptography.CryptoPool.Rent(readOnlySpan.Length);
		BigInteger result;
		try
		{
			byte value = (byte)(((readOnlySpan[0] & 0x80u) != 0) ? byte.MaxValue : 0);
			new Span<byte>(array, readOnlySpan.Length, array.Length - readOnlySpan.Length).Fill(value);
			readOnlySpan.CopyTo(array);
			AsnWriter.Reverse(new Span<byte>(array, 0, readOnlySpan.Length));
			result = new BigInteger(array);
		}
		finally
		{
			System.Security.Cryptography.CryptoPool.Return(array);
		}
		bytesConsumed = bytesConsumed2;
		return result;
	}

	public static bool TryReadInt32(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, out int value, out int bytesConsumed, Asn1Tag? expectedTag = null)
	{
		if (TryReadSignedInteger(source, ruleSet, 4, expectedTag ?? Asn1Tag.Integer, UniversalTagNumber.Integer, out var value2, out bytesConsumed))
		{
			value = (int)value2;
			return true;
		}
		value = 0;
		return false;
	}

	[CLSCompliant(false)]
	public static bool TryReadUInt32(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, out uint value, out int bytesConsumed, Asn1Tag? expectedTag = null)
	{
		if (TryReadUnsignedInteger(source, ruleSet, 4, expectedTag ?? Asn1Tag.Integer, UniversalTagNumber.Integer, out var value2, out bytesConsumed))
		{
			value = (uint)value2;
			return true;
		}
		value = 0u;
		return false;
	}

	public static bool TryReadInt64(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, out long value, out int bytesConsumed, Asn1Tag? expectedTag = null)
	{
		return TryReadSignedInteger(source, ruleSet, 8, expectedTag ?? Asn1Tag.Integer, UniversalTagNumber.Integer, out value, out bytesConsumed);
	}

	[CLSCompliant(false)]
	public static bool TryReadUInt64(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, out ulong value, out int bytesConsumed, Asn1Tag? expectedTag = null)
	{
		return TryReadUnsignedInteger(source, ruleSet, 8, expectedTag ?? Asn1Tag.Integer, UniversalTagNumber.Integer, out value, out bytesConsumed);
	}

	private static ReadOnlySpan<byte> GetIntegerContents(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, Asn1Tag expectedTag, UniversalTagNumber tagNumber, out int bytesConsumed)
	{
		int bytesConsumed2;
		ReadOnlySpan<byte> primitiveContentSpan = GetPrimitiveContentSpan(source, ruleSet, expectedTag, tagNumber, out bytesConsumed2);
		if (primitiveContentSpan.IsEmpty)
		{
			throw new AsnContentException();
		}
		if (BinaryPrimitives.TryReadUInt16BigEndian(primitiveContentSpan, out var value))
		{
			ushort num = (ushort)(value & 0xFF80u);
			if (num == 0 || num == 65408)
			{
				throw new AsnContentException();
			}
		}
		bytesConsumed = bytesConsumed2;
		return primitiveContentSpan;
	}

	private static bool TryReadSignedInteger(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, int sizeLimit, Asn1Tag expectedTag, UniversalTagNumber tagNumber, out long value, out int bytesConsumed)
	{
		int bytesConsumed2;
		ReadOnlySpan<byte> integerContents = GetIntegerContents(source, ruleSet, expectedTag, tagNumber, out bytesConsumed2);
		if (integerContents.Length > sizeLimit)
		{
			value = 0L;
			bytesConsumed = 0;
			return false;
		}
		long num = (((integerContents[0] & 0x80u) != 0) ? (-1) : 0);
		for (int i = 0; i < integerContents.Length; i++)
		{
			num <<= 8;
			num |= integerContents[i];
		}
		bytesConsumed = bytesConsumed2;
		value = num;
		return true;
	}

	private static bool TryReadUnsignedInteger(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, int sizeLimit, Asn1Tag expectedTag, UniversalTagNumber tagNumber, out ulong value, out int bytesConsumed)
	{
		int bytesConsumed2;
		ReadOnlySpan<byte> readOnlySpan = GetIntegerContents(source, ruleSet, expectedTag, tagNumber, out bytesConsumed2);
		if ((readOnlySpan[0] & 0x80u) != 0)
		{
			bytesConsumed = 0;
			value = 0uL;
			return false;
		}
		if (readOnlySpan.Length > 1 && readOnlySpan[0] == 0)
		{
			readOnlySpan = readOnlySpan.Slice(1);
		}
		if (readOnlySpan.Length > sizeLimit)
		{
			bytesConsumed = 0;
			value = 0uL;
			return false;
		}
		ulong num = 0uL;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			num <<= 8;
			num |= readOnlySpan[i];
		}
		bytesConsumed = bytesConsumed2;
		value = num;
		return true;
	}

	public static TFlagsEnum ReadNamedBitListValue<TFlagsEnum>(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, out int bytesConsumed, Asn1Tag? expectedTag = null) where TFlagsEnum : Enum
	{
		Type typeFromHandle = typeof(TFlagsEnum);
		int bytesConsumed2;
		TFlagsEnum result = (TFlagsEnum)Enum.ToObject(typeFromHandle, ReadNamedBitListValue(source, ruleSet, typeFromHandle, out bytesConsumed2, expectedTag));
		bytesConsumed = bytesConsumed2;
		return result;
	}

	public static Enum ReadNamedBitListValue(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, Type flagsEnumType, out int bytesConsumed, Asn1Tag? expectedTag = null)
	{
		if (flagsEnumType == null)
		{
			throw new ArgumentNullException("flagsEnumType");
		}
		Type enumUnderlyingType = flagsEnumType.GetEnumUnderlyingType();
		if (!flagsEnumType.IsDefined(typeof(FlagsAttribute), inherit: false))
		{
			throw new ArgumentException(System.SR.Argument_NamedBitListRequiresFlagsEnum, "flagsEnumType");
		}
		Span<byte> destination = stackalloc byte[8];
		destination = destination[..Marshal.SizeOf(enumUnderlyingType)];
		if (!TryReadBitString(source, destination, ruleSet, out var unusedBitCount, out var bytesConsumed2, out var bytesWritten, expectedTag))
		{
			throw new AsnContentException(System.SR.Format(System.SR.ContentException_NamedBitListValueTooBig, flagsEnumType.Name));
		}
		Enum result;
		if (bytesWritten == 0)
		{
			result = (Enum)Enum.ToObject(flagsEnumType, 0);
			bytesConsumed = bytesConsumed2;
			return result;
		}
		ReadOnlySpan<byte> valueSpan = destination.Slice(0, bytesWritten);
		if (ruleSet == AsnEncodingRules.DER || ruleSet == AsnEncodingRules.CER)
		{
			byte b = valueSpan[bytesWritten - 1];
			byte b2 = (byte)(1 << unusedBitCount);
			if ((b & b2) == 0)
			{
				throw new AsnContentException(System.SR.ContentException_InvalidUnderCerOrDer_TryBer);
			}
		}
		result = (Enum)Enum.ToObject(flagsEnumType, InterpretNamedBitListReversed(valueSpan));
		bytesConsumed = bytesConsumed2;
		return result;
	}

	public static BitArray ReadNamedBitList(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, out int bytesConsumed, Asn1Tag? expectedTag = null)
	{
		int contentOffset;
		int contentLength;
		int bytesConsumed2;
		Asn1Tag tag = ReadEncodedValue(source, ruleSet, out contentOffset, out contentLength, out bytesConsumed2);
		if (expectedTag.HasValue)
		{
			CheckExpectedTag(tag, expectedTag.Value, UniversalTagNumber.BitString);
		}
		byte[] array = System.Security.Cryptography.CryptoPool.Rent(contentLength);
		if (!TryReadBitString(source, array, ruleSet, out var unusedBitCount, out var bytesConsumed3, out var bytesWritten, expectedTag))
		{
			throw new InvalidOperationException();
		}
		int length = checked(bytesWritten * 8 - unusedBitCount);
		Span<byte> value = array.AsSpan(0, bytesWritten);
		ReverseBitsPerByte(value);
		BitArray bitArray = new BitArray(array);
		System.Security.Cryptography.CryptoPool.Return(array, bytesWritten);
		bitArray.Length = length;
		bytesConsumed = bytesConsumed3;
		return bitArray;
	}

	private static long InterpretNamedBitListReversed(ReadOnlySpan<byte> valueSpan)
	{
		long num = 0L;
		long num2 = 1L;
		for (int i = 0; i < valueSpan.Length; i++)
		{
			byte b = valueSpan[i];
			for (int num3 = 7; num3 >= 0; num3--)
			{
				int num4 = 1 << num3;
				if ((b & num4) != 0)
				{
					num |= num2;
				}
				num2 <<= 1;
			}
		}
		return num;
	}

	internal static void ReverseBitsPerByte(Span<byte> value)
	{
		for (int i = 0; i < value.Length; i++)
		{
			byte b = value[i];
			byte b2 = 128;
			byte b3 = 0;
			while (b != 0)
			{
				b3 |= (byte)((b & 1) * b2);
				b >>= 1;
				b2 >>= 1;
			}
			value[i] = b3;
		}
	}

	public static void ReadNull(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, out int bytesConsumed, Asn1Tag? expectedTag = null)
	{
		if (GetPrimitiveContentSpan(source, ruleSet, expectedTag ?? Asn1Tag.Null, UniversalTagNumber.Null, out var bytesConsumed2).Length != 0)
		{
			throw new AsnContentException();
		}
		bytesConsumed = bytesConsumed2;
	}

	public static bool TryReadOctetString(ReadOnlySpan<byte> source, Span<byte> destination, AsnEncodingRules ruleSet, out int bytesConsumed, out int bytesWritten, Asn1Tag? expectedTag = null)
	{
		if (source.Overlaps(destination))
		{
			throw new ArgumentException(System.SR.Argument_SourceOverlapsDestination, "destination");
		}
		if (TryReadPrimitiveOctetStringCore(source, ruleSet, expectedTag ?? Asn1Tag.PrimitiveOctetString, UniversalTagNumber.OctetString, out var contentLength, out var headerLength, out var contents, out var bytesConsumed2))
		{
			if (contents.Length > destination.Length)
			{
				bytesWritten = 0;
				bytesConsumed = 0;
				return false;
			}
			contents.CopyTo(destination);
			bytesWritten = contents.Length;
			bytesConsumed = bytesConsumed2;
			return true;
		}
		int bytesRead;
		bool flag = TryCopyConstructedOctetStringContents(Slice(source, headerLength, contentLength), ruleSet, destination, !contentLength.HasValue, out bytesRead, out bytesWritten);
		if (flag)
		{
			bytesConsumed = headerLength + bytesRead;
		}
		else
		{
			bytesConsumed = 0;
		}
		return flag;
	}

	public static byte[] ReadOctetString(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, out int bytesConsumed, Asn1Tag? expectedTag = null)
	{
		byte[] rented = null;
		int bytesConsumed2;
		ReadOnlySpan<byte> octetStringContents = GetOctetStringContents(source, ruleSet, expectedTag ?? Asn1Tag.PrimitiveOctetString, UniversalTagNumber.OctetString, out bytesConsumed2, ref rented);
		byte[] result = octetStringContents.ToArray();
		if (rented != null)
		{
			System.Security.Cryptography.CryptoPool.Return(rented, octetStringContents.Length);
		}
		bytesConsumed = bytesConsumed2;
		return result;
	}

	private static bool TryReadPrimitiveOctetStringCore(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, Asn1Tag expectedTag, UniversalTagNumber universalTagNumber, out int? contentLength, out int headerLength, out ReadOnlySpan<byte> contents, out int bytesConsumed)
	{
		Asn1Tag tag = ReadTagAndLength(source, ruleSet, out contentLength, out headerLength);
		CheckExpectedTag(tag, expectedTag, universalTagNumber);
		ReadOnlySpan<byte> readOnlySpan = Slice(source, headerLength, contentLength);
		if (tag.IsConstructed)
		{
			if (ruleSet == AsnEncodingRules.DER)
			{
				throw new AsnContentException(System.SR.ContentException_InvalidUnderDer_TryBerOrCer);
			}
			contents = default(ReadOnlySpan<byte>);
			bytesConsumed = 0;
			return false;
		}
		if (ruleSet == AsnEncodingRules.CER && readOnlySpan.Length > 1000)
		{
			throw new AsnContentException(System.SR.ContentException_InvalidUnderCer_TryBerOrDer);
		}
		contents = readOnlySpan;
		bytesConsumed = headerLength + readOnlySpan.Length;
		return true;
	}

	public static bool TryReadPrimitiveOctetString(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, out ReadOnlySpan<byte> value, out int bytesConsumed, Asn1Tag? expectedTag = null)
	{
		int? contentLength;
		int headerLength;
		return TryReadPrimitiveOctetStringCore(source, ruleSet, expectedTag ?? Asn1Tag.PrimitiveOctetString, UniversalTagNumber.OctetString, out contentLength, out headerLength, out value, out bytesConsumed);
	}

	private static int CountConstructedOctetString(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, bool isIndefinite)
	{
		int bytesRead;
		int num = CopyConstructedOctetString(source, ruleSet, Span<byte>.Empty, write: false, isIndefinite, out bytesRead);
		if (ruleSet == AsnEncodingRules.CER && num <= 1000)
		{
			throw new AsnContentException(System.SR.ContentException_InvalidUnderCerOrDer_TryBer);
		}
		return num;
	}

	private static void CopyConstructedOctetString(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, Span<byte> destination, bool isIndefinite, out int bytesRead, out int bytesWritten)
	{
		bytesWritten = CopyConstructedOctetString(source, ruleSet, destination, write: true, isIndefinite, out bytesRead);
	}

	private static int CopyConstructedOctetString(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, Span<byte> destination, bool write, bool isIndefinite, out int bytesRead)
	{
		bytesRead = 0;
		int num = 1000;
		ReadOnlySpan<byte> readOnlySpan = source;
		Stack<(int, int, bool, int)> stack = null;
		int num2 = 0;
		Asn1Tag asn1Tag = Asn1Tag.ConstructedBitString;
		Span<byte> destination2 = destination;
		while (true)
		{
			if (!readOnlySpan.IsEmpty)
			{
				asn1Tag = ReadTagAndLength(readOnlySpan, ruleSet, out var contentsLength, out var bytesRead2);
				if (asn1Tag == Asn1Tag.PrimitiveOctetString)
				{
					if (ruleSet == AsnEncodingRules.CER && num != 1000)
					{
						throw new AsnContentException(System.SR.ContentException_InvalidUnderCerOrDer_TryBer);
					}
					ReadOnlySpan<byte> readOnlySpan2 = Slice(readOnlySpan, bytesRead2, contentsLength.Value);
					int num3 = bytesRead2 + readOnlySpan2.Length;
					readOnlySpan = readOnlySpan.Slice(num3);
					bytesRead += num3;
					num2 += readOnlySpan2.Length;
					num = readOnlySpan2.Length;
					if (ruleSet == AsnEncodingRules.CER && num > 1000)
					{
						throw new AsnContentException(System.SR.ContentException_InvalidUnderCerOrDer_TryBer);
					}
					if (write)
					{
						readOnlySpan2.CopyTo(destination2);
						destination2 = destination2.Slice(readOnlySpan2.Length);
					}
					continue;
				}
				if (!(asn1Tag == Asn1Tag.EndOfContents && isIndefinite))
				{
					if (asn1Tag == Asn1Tag.ConstructedOctetString)
					{
						if (ruleSet == AsnEncodingRules.CER)
						{
							throw new AsnContentException(System.SR.ContentException_InvalidUnderCerOrDer_TryBer);
						}
						if (stack == null)
						{
							stack = new Stack<(int, int, bool, int)>();
						}
						if (!source.Overlaps(readOnlySpan, out var elementOffset))
						{
							throw new AsnContentException();
						}
						stack.Push((elementOffset, readOnlySpan.Length, isIndefinite, bytesRead));
						readOnlySpan = Slice(readOnlySpan, bytesRead2, contentsLength);
						bytesRead = bytesRead2;
						isIndefinite = !contentsLength.HasValue;
						continue;
					}
					throw new AsnContentException();
				}
				ValidateEndOfContents(asn1Tag, contentsLength, bytesRead2);
				bytesRead += bytesRead2;
				if (stack != null && stack.Count > 0)
				{
					(int, int, bool, int) tuple = stack.Pop();
					int item = tuple.Item1;
					int item2 = tuple.Item2;
					bool item3 = tuple.Item3;
					int item4 = tuple.Item4;
					readOnlySpan = source.Slice(item, item2).Slice(bytesRead);
					bytesRead += item4;
					isIndefinite = item3;
					continue;
				}
			}
			if (isIndefinite && asn1Tag != Asn1Tag.EndOfContents)
			{
				throw new AsnContentException();
			}
			if (stack == null || stack.Count <= 0)
			{
				break;
			}
			(int, int, bool, int) tuple2 = stack.Pop();
			int item5 = tuple2.Item1;
			int item6 = tuple2.Item2;
			bool item7 = tuple2.Item3;
			int item8 = tuple2.Item4;
			readOnlySpan = source.Slice(item5, item6).Slice(bytesRead);
			isIndefinite = item7;
			bytesRead += item8;
		}
		return num2;
	}

	private static bool TryCopyConstructedOctetStringContents(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, Span<byte> dest, bool isIndefinite, out int bytesRead, out int bytesWritten)
	{
		bytesRead = 0;
		int num = CountConstructedOctetString(source, ruleSet, isIndefinite);
		if (dest.Length < num)
		{
			bytesWritten = 0;
			return false;
		}
		CopyConstructedOctetString(source, ruleSet, dest, isIndefinite, out bytesRead, out bytesWritten);
		return true;
	}

	private static ReadOnlySpan<byte> GetOctetStringContents(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, Asn1Tag expectedTag, UniversalTagNumber universalTagNumber, out int bytesConsumed, ref byte[] rented, Span<byte> tmpSpace = default(Span<byte>))
	{
		if (TryReadPrimitiveOctetStringCore(source, ruleSet, expectedTag, universalTagNumber, out var contentLength, out var headerLength, out var contents, out bytesConsumed))
		{
			return contents;
		}
		contents = source.Slice(headerLength);
		int num = contentLength ?? SeekEndOfContents(contents, ruleSet);
		if (tmpSpace.Length > 0 && num > tmpSpace.Length)
		{
			bool isIndefinite = !contentLength.HasValue;
			num = CountConstructedOctetString(contents, ruleSet, isIndefinite);
		}
		if (num > tmpSpace.Length)
		{
			rented = System.Security.Cryptography.CryptoPool.Rent(num);
			tmpSpace = rented;
		}
		if (TryCopyConstructedOctetStringContents(Slice(source, headerLength, contentLength), ruleSet, tmpSpace, !contentLength.HasValue, out var bytesRead, out var bytesWritten))
		{
			bytesConsumed = headerLength + bytesRead;
			return tmpSpace.Slice(0, bytesWritten);
		}
		throw new AsnContentException();
	}

	public static string ReadObjectIdentifier(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, out int bytesConsumed, Asn1Tag? expectedTag = null)
	{
		return ReadObjectIdentifier(source, ruleSet, expectedTag, out bytesConsumed);
	}

	private static void ReadSubIdentifier(ReadOnlySpan<byte> source, out int bytesRead, out long? smallValue, out BigInteger? largeValue)
	{
		if (source[0] == 128)
		{
			throw new AsnContentException();
		}
		int num = -1;
		int i;
		for (i = 0; i < source.Length; i++)
		{
			if ((source[i] & 0x80) == 0)
			{
				num = i;
				break;
			}
		}
		if (num < 0)
		{
			throw new AsnContentException();
		}
		bytesRead = num + 1;
		long num2 = 0L;
		if (bytesRead <= 9)
		{
			for (i = 0; i < bytesRead; i++)
			{
				byte b = source[i];
				num2 <<= 7;
				num2 |= (byte)(b & 0x7F);
			}
			largeValue = null;
			smallValue = num2;
			return;
		}
		int num3 = (bytesRead / 8 + 1) * 7;
		byte[] array = System.Security.Cryptography.CryptoPool.Rent(num3);
		Array.Clear(array, 0, array.Length);
		Span<byte> destination = array;
		Span<byte> destination2 = stackalloc byte[8];
		int num4 = bytesRead;
		i = bytesRead - 8;
		while (num4 > 0)
		{
			byte b2 = source[i];
			num2 <<= 7;
			num2 |= (byte)(b2 & 0x7F);
			i++;
			if (i >= num4)
			{
				BinaryPrimitives.WriteInt64LittleEndian(destination2, num2);
				destination2.Slice(0, 7).CopyTo(destination);
				destination = destination.Slice(7);
				num2 = 0L;
				num4 -= 8;
				i = Math.Max(0, num4 - 8);
			}
		}
		int num5 = array.Length - destination.Length;
		int num6 = num3 - num5;
		largeValue = new BigInteger(array);
		smallValue = null;
		System.Security.Cryptography.CryptoPool.Return(array, num5);
	}

	private static string ReadObjectIdentifier(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, Asn1Tag? expectedTag, out int totalBytesRead)
	{
		int bytesConsumed;
		ReadOnlySpan<byte> source2 = GetPrimitiveContentSpan(source, ruleSet, expectedTag ?? Asn1Tag.ObjectIdentifier, UniversalTagNumber.ObjectIdentifier, out bytesConsumed);
		if (source2.Length < 1)
		{
			throw new AsnContentException();
		}
		StringBuilder stringBuilder = new StringBuilder((byte)source2.Length * 4);
		ReadSubIdentifier(source2, out var bytesRead, out var smallValue, out var largeValue);
		if (smallValue.HasValue)
		{
			long num = smallValue.Value;
			byte value;
			if (num < 40)
			{
				value = 0;
			}
			else if (num < 80)
			{
				value = 1;
				num -= 40;
			}
			else
			{
				value = 2;
				num -= 80;
			}
			stringBuilder.Append(value);
			stringBuilder.Append('.');
			stringBuilder.Append(num);
		}
		else
		{
			BigInteger value2 = largeValue.Value;
			byte value = 2;
			value2 -= (BigInteger)80;
			stringBuilder.Append(value);
			stringBuilder.Append('.');
			stringBuilder.Append(value2.ToString());
		}
		source2 = source2.Slice(bytesRead);
		while (!source2.IsEmpty)
		{
			ReadSubIdentifier(source2, out bytesRead, out smallValue, out largeValue);
			stringBuilder.Append('.');
			if (smallValue.HasValue)
			{
				stringBuilder.Append(smallValue.Value);
			}
			else
			{
				stringBuilder.Append(largeValue.Value.ToString());
			}
			source2 = source2.Slice(bytesRead);
		}
		totalBytesRead = bytesConsumed;
		return stringBuilder.ToString();
	}

	public static void ReadSequence(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, out int contentOffset, out int contentLength, out int bytesConsumed, Asn1Tag? expectedTag = null)
	{
		int? contentsLength;
		int bytesRead;
		Asn1Tag tag = ReadTagAndLength(source, ruleSet, out contentsLength, out bytesRead);
		CheckExpectedTag(tag, expectedTag ?? Asn1Tag.Sequence, UniversalTagNumber.Sequence);
		if (!tag.IsConstructed)
		{
			throw new AsnContentException(System.SR.Format(System.SR.ContentException_ConstructedEncodingRequired, UniversalTagNumber.Sequence));
		}
		if (contentsLength.HasValue)
		{
			if (contentsLength.Value + bytesRead > source.Length)
			{
				throw GetValidityException(LengthValidity.LengthExceedsInput);
			}
			contentLength = contentsLength.Value;
			contentOffset = bytesRead;
			bytesConsumed = contentLength + bytesRead;
		}
		else
		{
			int num = (contentLength = SeekEndOfContents(source.Slice(bytesRead), ruleSet));
			contentOffset = bytesRead;
			bytesConsumed = num + bytesRead + 2;
		}
	}

	public static void ReadSetOf(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, out int contentOffset, out int contentLength, out int bytesConsumed, bool skipSortOrderValidation = false, Asn1Tag? expectedTag = null)
	{
		int? contentsLength;
		int bytesRead;
		Asn1Tag tag = ReadTagAndLength(source, ruleSet, out contentsLength, out bytesRead);
		CheckExpectedTag(tag, expectedTag ?? Asn1Tag.SetOf, UniversalTagNumber.Set);
		if (!tag.IsConstructed)
		{
			throw new AsnContentException(System.SR.Format(System.SR.ContentException_ConstructedEncodingRequired, UniversalTagNumber.Set));
		}
		int num;
		ReadOnlySpan<byte> readOnlySpan;
		if (contentsLength.HasValue)
		{
			num = 0;
			readOnlySpan = Slice(source, bytesRead, contentsLength.Value);
		}
		else
		{
			int length = SeekEndOfContents(source.Slice(bytesRead), ruleSet);
			readOnlySpan = Slice(source, bytesRead, length);
			num = 2;
		}
		if (!skipSortOrderValidation && (ruleSet == AsnEncodingRules.DER || ruleSet == AsnEncodingRules.CER))
		{
			ReadOnlySpan<byte> source2 = readOnlySpan;
			ReadOnlySpan<byte> y = default(ReadOnlySpan<byte>);
			while (!source2.IsEmpty)
			{
				ReadEncodedValue(source2, ruleSet, out var _, out var _, out var bytesConsumed2);
				ReadOnlySpan<byte> readOnlySpan2 = source2.Slice(0, bytesConsumed2);
				source2 = source2.Slice(bytesConsumed2);
				if (SetOfValueComparer.Compare(readOnlySpan2, y) < 0)
				{
					throw new AsnContentException(System.SR.ContentException_SetOfNotSorted);
				}
				y = readOnlySpan2;
			}
		}
		contentOffset = bytesRead;
		contentLength = readOnlySpan.Length;
		bytesConsumed = bytesRead + readOnlySpan.Length + num;
	}

	public static bool TryReadPrimitiveCharacterStringBytes(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, Asn1Tag expectedTag, out ReadOnlySpan<byte> value, out int bytesConsumed)
	{
		UniversalTagNumber universalTagNumber = UniversalTagNumber.IA5String;
		if (expectedTag.TagClass == TagClass.Universal)
		{
			universalTagNumber = (UniversalTagNumber)expectedTag.TagValue;
			if (!IsCharacterStringEncodingType(universalTagNumber))
			{
				throw new ArgumentException(System.SR.Argument_Tag_NotCharacterString, "expectedTag");
			}
		}
		int? contentLength;
		int headerLength;
		return TryReadPrimitiveOctetStringCore(source, ruleSet, expectedTag, universalTagNumber, out contentLength, out headerLength, out value, out bytesConsumed);
	}

	public static bool TryReadCharacterStringBytes(ReadOnlySpan<byte> source, Span<byte> destination, AsnEncodingRules ruleSet, Asn1Tag expectedTag, out int bytesConsumed, out int bytesWritten)
	{
		if (source.Overlaps(destination))
		{
			throw new ArgumentException(System.SR.Argument_SourceOverlapsDestination, "destination");
		}
		UniversalTagNumber universalTagNumber = UniversalTagNumber.IA5String;
		if (expectedTag.TagClass == TagClass.Universal)
		{
			universalTagNumber = (UniversalTagNumber)expectedTag.TagValue;
			if (!IsCharacterStringEncodingType(universalTagNumber))
			{
				throw new ArgumentException(System.SR.Argument_Tag_NotCharacterString, "expectedTag");
			}
		}
		return TryReadCharacterStringBytesCore(source, ruleSet, expectedTag, universalTagNumber, destination, out bytesConsumed, out bytesWritten);
	}

	public static bool TryReadCharacterString(ReadOnlySpan<byte> source, Span<char> destination, AsnEncodingRules ruleSet, UniversalTagNumber encodingType, out int bytesConsumed, out int charsWritten, Asn1Tag? expectedTag = null)
	{
		Encoding encoding = AsnCharacterStringEncodings.GetEncoding(encodingType);
		return TryReadCharacterStringCore(source, ruleSet, expectedTag ?? new Asn1Tag(encodingType), encodingType, encoding, destination, out bytesConsumed, out charsWritten);
	}

	public static string ReadCharacterString(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, UniversalTagNumber encodingType, out int bytesConsumed, Asn1Tag? expectedTag = null)
	{
		Encoding encoding = AsnCharacterStringEncodings.GetEncoding(encodingType);
		return ReadCharacterStringCore(source, ruleSet, expectedTag ?? new Asn1Tag(encodingType), encodingType, encoding, out bytesConsumed);
	}

	private static bool TryReadCharacterStringBytesCore(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, Asn1Tag expectedTag, UniversalTagNumber universalTagNumber, Span<byte> destination, out int bytesConsumed, out int bytesWritten)
	{
		if (TryReadPrimitiveOctetStringCore(source, ruleSet, expectedTag, universalTagNumber, out var contentLength, out var headerLength, out var contents, out var bytesConsumed2))
		{
			if (contents.Length > destination.Length)
			{
				bytesWritten = 0;
				bytesConsumed = 0;
				return false;
			}
			contents.CopyTo(destination);
			bytesWritten = contents.Length;
			bytesConsumed = bytesConsumed2;
			return true;
		}
		int bytesRead;
		bool flag = TryCopyConstructedOctetStringContents(Slice(source, headerLength, contentLength), ruleSet, destination, !contentLength.HasValue, out bytesRead, out bytesWritten);
		if (flag)
		{
			bytesConsumed = headerLength + bytesRead;
		}
		else
		{
			bytesConsumed = 0;
		}
		return flag;
	}

	private unsafe static bool TryReadCharacterStringCore(ReadOnlySpan<byte> source, Span<char> destination, Encoding encoding, out int charsWritten)
	{
		if (source.Length == 0)
		{
			charsWritten = 0;
			return true;
		}
		fixed (byte* bytes = &MemoryMarshal.GetReference(source))
		{
			fixed (char* chars = &MemoryMarshal.GetReference(destination))
			{
				try
				{
					int charCount = encoding.GetCharCount(bytes, source.Length);
					if (charCount > destination.Length)
					{
						charsWritten = 0;
						return false;
					}
					charsWritten = encoding.GetChars(bytes, source.Length, chars, destination.Length);
				}
				catch (DecoderFallbackException inner)
				{
					throw new AsnContentException(System.SR.ContentException_DefaultMessage, inner);
				}
				return true;
			}
		}
	}

	private unsafe static string ReadCharacterStringCore(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, Asn1Tag expectedTag, UniversalTagNumber universalTagNumber, Encoding encoding, out int bytesConsumed)
	{
		byte[] rented = null;
		int bytesConsumed2;
		ReadOnlySpan<byte> octetStringContents = GetOctetStringContents(source, ruleSet, expectedTag, universalTagNumber, out bytesConsumed2, ref rented);
		string result;
		if (octetStringContents.Length == 0)
		{
			result = string.Empty;
		}
		else
		{
			fixed (byte* bytes = &MemoryMarshal.GetReference(octetStringContents))
			{
				try
				{
					result = encoding.GetString(bytes, octetStringContents.Length);
				}
				catch (DecoderFallbackException inner)
				{
					throw new AsnContentException(System.SR.ContentException_DefaultMessage, inner);
				}
			}
		}
		if (rented != null)
		{
			System.Security.Cryptography.CryptoPool.Return(rented, octetStringContents.Length);
		}
		bytesConsumed = bytesConsumed2;
		return result;
	}

	private static bool TryReadCharacterStringCore(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, Asn1Tag expectedTag, UniversalTagNumber universalTagNumber, Encoding encoding, Span<char> destination, out int bytesConsumed, out int charsWritten)
	{
		byte[] rented = null;
		int bytesConsumed2;
		ReadOnlySpan<byte> octetStringContents = GetOctetStringContents(source, ruleSet, expectedTag, universalTagNumber, out bytesConsumed2, ref rented);
		bool flag = TryReadCharacterStringCore(octetStringContents, destination, encoding, out charsWritten);
		if (rented != null)
		{
			System.Security.Cryptography.CryptoPool.Return(rented, octetStringContents.Length);
		}
		if (flag)
		{
			bytesConsumed = bytesConsumed2;
		}
		else
		{
			bytesConsumed = 0;
		}
		return flag;
	}

	private static bool IsCharacterStringEncodingType(UniversalTagNumber encodingType)
	{
		switch (encodingType)
		{
		case UniversalTagNumber.UTF8String:
		case UniversalTagNumber.NumericString:
		case UniversalTagNumber.PrintableString:
		case UniversalTagNumber.TeletexString:
		case UniversalTagNumber.VideotexString:
		case UniversalTagNumber.IA5String:
		case UniversalTagNumber.GraphicString:
		case UniversalTagNumber.VisibleString:
		case UniversalTagNumber.GeneralString:
		case UniversalTagNumber.UniversalString:
		case UniversalTagNumber.BMPString:
			return true;
		default:
			return false;
		}
	}

	public static DateTimeOffset ReadUtcTime(ReadOnlySpan<byte> source, AsnEncodingRules ruleSet, out int bytesConsumed, int twoDigitYearMax = 2049, Asn1Tag? expectedTag = null)
	{
		if (twoDigitYearMax < 1 || twoDigitYearMax > 9999)
		{
			throw new ArgumentOutOfRangeException("twoDigitYearMax");
		}
		Span<byte> tmpSpace = stackalloc byte[17];
		byte[] rented = null;
		int bytesConsumed2;
		ReadOnlySpan<byte> octetStringContents = GetOctetStringContents(source, ruleSet, expectedTag ?? Asn1Tag.UtcTime, UniversalTagNumber.UtcTime, out bytesConsumed2, ref rented, tmpSpace);
		DateTimeOffset result = ParseUtcTime(octetStringContents, ruleSet, twoDigitYearMax);
		if (rented != null)
		{
			System.Security.Cryptography.CryptoPool.Return(rented, octetStringContents.Length);
		}
		bytesConsumed = bytesConsumed2;
		return result;
	}

	private static DateTimeOffset ParseUtcTime(ReadOnlySpan<byte> contentOctets, AsnEncodingRules ruleSet, int twoDigitYearMax)
	{
		if ((ruleSet == AsnEncodingRules.DER || ruleSet == AsnEncodingRules.CER) && contentOctets.Length != 13)
		{
			throw new AsnContentException(System.SR.ContentException_InvalidUnderCerOrDer_TryBer);
		}
		if (contentOctets.Length < 11 || contentOctets.Length > 17 || (contentOctets.Length & 1) != 1)
		{
			throw new AsnContentException();
		}
		ReadOnlySpan<byte> data = contentOctets;
		int num = ParseNonNegativeIntAndSlice(ref data, 2);
		int month = ParseNonNegativeIntAndSlice(ref data, 2);
		int day = ParseNonNegativeIntAndSlice(ref data, 2);
		int hour = ParseNonNegativeIntAndSlice(ref data, 2);
		int minute = ParseNonNegativeIntAndSlice(ref data, 2);
		int second = 0;
		int hours = 0;
		int num2 = 0;
		bool flag = false;
		if (contentOctets.Length == 17 || contentOctets.Length == 13)
		{
			second = ParseNonNegativeIntAndSlice(ref data, 2);
		}
		if (contentOctets.Length == 11 || contentOctets.Length == 13)
		{
			if (data[0] != 90)
			{
				throw new AsnContentException();
			}
		}
		else
		{
			if (data[0] == 45)
			{
				flag = true;
			}
			else if (data[0] != 43)
			{
				throw new AsnContentException();
			}
			data = data.Slice(1);
			hours = ParseNonNegativeIntAndSlice(ref data, 2);
			num2 = ParseNonNegativeIntAndSlice(ref data, 2);
		}
		if (num2 > 59)
		{
			throw new AsnContentException();
		}
		TimeSpan timeSpan = new TimeSpan(hours, num2, 0);
		if (flag)
		{
			timeSpan = -timeSpan;
		}
		int num3 = twoDigitYearMax / 100;
		if (num > twoDigitYearMax % 100)
		{
			num3--;
		}
		int year = num3 * 100 + num;
		try
		{
			return new DateTimeOffset(year, month, day, hour, minute, second, timeSpan);
		}
		catch (Exception inner)
		{
			throw new AsnContentException(System.SR.ContentException_DefaultMessage, inner);
		}
	}
}

using System.Diagnostics.CodeAnalysis;

namespace System.Formats.Asn1;

public readonly struct Asn1Tag : IEquatable<Asn1Tag>
{
	private readonly byte _controlFlags;

	internal static readonly Asn1Tag EndOfContents = new Asn1Tag((byte)0, 0);

	public static readonly Asn1Tag Boolean = new Asn1Tag((byte)0, 1);

	public static readonly Asn1Tag Integer = new Asn1Tag((byte)0, 2);

	public static readonly Asn1Tag PrimitiveBitString = new Asn1Tag((byte)0, 3);

	public static readonly Asn1Tag ConstructedBitString = new Asn1Tag(32, 3);

	public static readonly Asn1Tag PrimitiveOctetString = new Asn1Tag((byte)0, 4);

	public static readonly Asn1Tag ConstructedOctetString = new Asn1Tag(32, 4);

	public static readonly Asn1Tag Null = new Asn1Tag((byte)0, 5);

	public static readonly Asn1Tag ObjectIdentifier = new Asn1Tag((byte)0, 6);

	public static readonly Asn1Tag Enumerated = new Asn1Tag((byte)0, 10);

	public static readonly Asn1Tag Sequence = new Asn1Tag(32, 16);

	public static readonly Asn1Tag SetOf = new Asn1Tag(32, 17);

	public static readonly Asn1Tag UtcTime = new Asn1Tag((byte)0, 23);

	public static readonly Asn1Tag GeneralizedTime = new Asn1Tag((byte)0, 24);

	public TagClass TagClass => (TagClass)(_controlFlags & 0xC0);

	public bool IsConstructed => (_controlFlags & 0x20) != 0;

	public int TagValue { get; }

	private Asn1Tag(byte controlFlags, int tagValue)
	{
		_controlFlags = (byte)(controlFlags & 0xE0u);
		TagValue = tagValue;
	}

	public Asn1Tag(UniversalTagNumber universalTagNumber, bool isConstructed = false)
		: this((byte)(isConstructed ? 32 : 0), (int)universalTagNumber)
	{
		if (universalTagNumber < UniversalTagNumber.EndOfContents || universalTagNumber > UniversalTagNumber.RelativeObjectIdentifierIRI || universalTagNumber == (UniversalTagNumber)15)
		{
			throw new ArgumentOutOfRangeException("universalTagNumber");
		}
	}

	public Asn1Tag(TagClass tagClass, int tagValue, bool isConstructed = false)
		: this((byte)((byte)tagClass | (isConstructed ? 32u : 0u)), tagValue)
	{
		switch (tagClass)
		{
		default:
			throw new ArgumentOutOfRangeException("tagClass");
		case TagClass.Universal:
		case TagClass.Application:
		case TagClass.ContextSpecific:
		case TagClass.Private:
			if (tagValue < 0)
			{
				throw new ArgumentOutOfRangeException("tagValue");
			}
			break;
		}
	}

	public Asn1Tag AsConstructed()
	{
		return new Asn1Tag((byte)(_controlFlags | 0x20u), TagValue);
	}

	public Asn1Tag AsPrimitive()
	{
		return new Asn1Tag((byte)(_controlFlags & 0xFFFFFFDFu), TagValue);
	}

	public static bool TryDecode(ReadOnlySpan<byte> source, out Asn1Tag tag, out int bytesConsumed)
	{
		tag = default(Asn1Tag);
		bytesConsumed = 0;
		if (source.IsEmpty)
		{
			return false;
		}
		byte b = source[bytesConsumed];
		bytesConsumed++;
		uint num = b & 0x1Fu;
		if (num == 31)
		{
			num = 0u;
			byte b2;
			do
			{
				if (source.Length <= bytesConsumed)
				{
					bytesConsumed = 0;
					return false;
				}
				b2 = source[bytesConsumed];
				byte b3 = (byte)(b2 & 0x7Fu);
				bytesConsumed++;
				if (num >= 33554432)
				{
					bytesConsumed = 0;
					return false;
				}
				num <<= 7;
				num |= b3;
				if (num == 0)
				{
					bytesConsumed = 0;
					return false;
				}
			}
			while ((b2 & 0x80) == 128);
			if (num <= 30)
			{
				bytesConsumed = 0;
				return false;
			}
			if (num > int.MaxValue)
			{
				bytesConsumed = 0;
				return false;
			}
		}
		tag = new Asn1Tag(b, (int)num);
		return true;
	}

	public static Asn1Tag Decode(ReadOnlySpan<byte> source, out int bytesConsumed)
	{
		if (TryDecode(source, out var tag, out bytesConsumed))
		{
			return tag;
		}
		throw new AsnContentException(System.SR.ContentException_InvalidTag);
	}

	public int CalculateEncodedSize()
	{
		if (TagValue < 31)
		{
			return 1;
		}
		if (TagValue <= 127)
		{
			return 2;
		}
		if (TagValue <= 16383)
		{
			return 3;
		}
		if (TagValue <= 2097151)
		{
			return 4;
		}
		if (TagValue <= 268435455)
		{
			return 5;
		}
		return 6;
	}

	public bool TryEncode(Span<byte> destination, out int bytesWritten)
	{
		int num = CalculateEncodedSize();
		if (destination.Length < num)
		{
			bytesWritten = 0;
			return false;
		}
		if (num == 1)
		{
			byte b = (byte)(_controlFlags | TagValue);
			destination[0] = b;
			bytesWritten = 1;
			return true;
		}
		byte b2 = (byte)(_controlFlags | 0x1Fu);
		destination[0] = b2;
		int num2 = TagValue;
		int num3 = num - 1;
		while (num2 > 0)
		{
			int num4 = num2 & 0x7F;
			if (num2 != TagValue)
			{
				num4 |= 0x80;
			}
			destination[num3] = (byte)num4;
			num2 >>= 7;
			num3--;
		}
		bytesWritten = num;
		return true;
	}

	public int Encode(Span<byte> destination)
	{
		if (TryEncode(destination, out var bytesWritten))
		{
			return bytesWritten;
		}
		throw new ArgumentException(System.SR.Argument_DestinationTooShort, "destination");
	}

	public bool Equals(Asn1Tag other)
	{
		if (_controlFlags == other._controlFlags)
		{
			return TagValue == other.TagValue;
		}
		return false;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is Asn1Tag other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (_controlFlags << 24) ^ TagValue;
	}

	public static bool operator ==(Asn1Tag left, Asn1Tag right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(Asn1Tag left, Asn1Tag right)
	{
		return !left.Equals(right);
	}

	public bool HasSameClassAndValue(Asn1Tag other)
	{
		if (TagValue == other.TagValue)
		{
			return TagClass == other.TagClass;
		}
		return false;
	}

	public override string ToString()
	{
		string text = ((TagClass != 0) ? (TagClass.ToString() + "-" + TagValue) : ((UniversalTagNumber)TagValue).ToString());
		if (IsConstructed)
		{
			return "Constructed " + text;
		}
		return text;
	}
}

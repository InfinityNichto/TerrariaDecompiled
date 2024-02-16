using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace System.Reflection.Metadata;

public struct SignatureHeader : IEquatable<SignatureHeader>
{
	private readonly byte _rawValue;

	public const byte CallingConventionOrKindMask = 15;

	public byte RawValue => _rawValue;

	public SignatureCallingConvention CallingConvention
	{
		get
		{
			int num = _rawValue & 0xF;
			if (num > 5 && num != 9)
			{
				return SignatureCallingConvention.Default;
			}
			return (SignatureCallingConvention)num;
		}
	}

	public SignatureKind Kind
	{
		get
		{
			int num = _rawValue & 0xF;
			if (num <= 5 || num == 9)
			{
				return SignatureKind.Method;
			}
			return (SignatureKind)num;
		}
	}

	public SignatureAttributes Attributes => (SignatureAttributes)(_rawValue & 0xFFFFFFF0u);

	public bool HasExplicitThis => (_rawValue & 0x40) != 0;

	public bool IsInstance => (_rawValue & 0x20) != 0;

	public bool IsGeneric => (_rawValue & 0x10) != 0;

	public SignatureHeader(byte rawValue)
	{
		_rawValue = rawValue;
	}

	public SignatureHeader(SignatureKind kind, SignatureCallingConvention convention, SignatureAttributes attributes)
		: this((byte)((uint)kind | (uint)convention | (uint)attributes))
	{
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is SignatureHeader other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(SignatureHeader other)
	{
		return _rawValue == other._rawValue;
	}

	public override int GetHashCode()
	{
		return _rawValue;
	}

	public static bool operator ==(SignatureHeader left, SignatureHeader right)
	{
		return left._rawValue == right._rawValue;
	}

	public static bool operator !=(SignatureHeader left, SignatureHeader right)
	{
		return left._rawValue != right._rawValue;
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(Kind.ToString());
		if (Kind == SignatureKind.Method)
		{
			stringBuilder.Append(',');
			stringBuilder.Append(CallingConvention.ToString());
		}
		if (Attributes != 0)
		{
			stringBuilder.Append(',');
			stringBuilder.Append(Attributes.ToString());
		}
		return stringBuilder.ToString();
	}
}

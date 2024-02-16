using Internal.Cryptography.Pal;

namespace System.Security.Cryptography.X509Certificates;

public sealed class X500DistinguishedName : AsnEncodedData
{
	private volatile string _lazyDistinguishedName;

	public string Name
	{
		get
		{
			string text = _lazyDistinguishedName;
			if (text == null)
			{
				text = (_lazyDistinguishedName = Decode(X500DistinguishedNameFlags.Reversed));
			}
			return text;
		}
	}

	public X500DistinguishedName(byte[] encodedDistinguishedName)
		: base(new Oid(null, null), encodedDistinguishedName)
	{
	}

	public X500DistinguishedName(ReadOnlySpan<byte> encodedDistinguishedName)
		: base(new Oid(null, null), encodedDistinguishedName)
	{
	}

	public X500DistinguishedName(AsnEncodedData encodedDistinguishedName)
		: base(encodedDistinguishedName)
	{
	}

	public X500DistinguishedName(X500DistinguishedName distinguishedName)
		: base(distinguishedName)
	{
		_lazyDistinguishedName = distinguishedName.Name;
	}

	public X500DistinguishedName(string distinguishedName)
		: this(distinguishedName, X500DistinguishedNameFlags.Reversed)
	{
	}

	public X500DistinguishedName(string distinguishedName, X500DistinguishedNameFlags flag)
		: base(new Oid(null, null), Encode(distinguishedName, flag))
	{
		_lazyDistinguishedName = distinguishedName;
	}

	public string Decode(X500DistinguishedNameFlags flag)
	{
		ThrowIfInvalid(flag);
		return X509Pal.Instance.X500DistinguishedNameDecode(base.RawData, flag);
	}

	public override string Format(bool multiLine)
	{
		return X509Pal.Instance.X500DistinguishedNameFormat(base.RawData, multiLine);
	}

	private static byte[] Encode(string distinguishedName, X500DistinguishedNameFlags flags)
	{
		if (distinguishedName == null)
		{
			throw new ArgumentNullException("distinguishedName");
		}
		ThrowIfInvalid(flags);
		return X509Pal.Instance.X500DistinguishedNameEncode(distinguishedName, flags);
	}

	private static void ThrowIfInvalid(X500DistinguishedNameFlags flags)
	{
		uint num = 29169u;
		if (((uint)flags & ~num) != 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Arg_EnumIllegalVal, "flag"));
		}
	}
}

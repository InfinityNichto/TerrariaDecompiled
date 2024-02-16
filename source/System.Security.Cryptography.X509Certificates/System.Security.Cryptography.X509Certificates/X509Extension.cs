using Internal.Cryptography;

namespace System.Security.Cryptography.X509Certificates;

public class X509Extension : AsnEncodedData
{
	public bool Critical { get; set; }

	protected X509Extension()
	{
	}

	public X509Extension(AsnEncodedData encodedExtension, bool critical)
		: this(encodedExtension.Oid, encodedExtension.RawData, critical)
	{
	}

	public X509Extension(Oid oid, byte[] rawData, bool critical)
		: this(oid, rawData.AsSpanParameter("rawData"), critical)
	{
	}

	public X509Extension(Oid oid, ReadOnlySpan<byte> rawData, bool critical)
		: base(oid, rawData)
	{
		if (base.Oid == null || base.Oid.Value == null)
		{
			throw new ArgumentNullException("oid");
		}
		if (base.Oid.Value.Length == 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Arg_EmptyOrNullString_Named, "oid.Value"), "oid");
		}
		Critical = critical;
	}

	public X509Extension(string oid, byte[] rawData, bool critical)
		: this(new Oid(oid), rawData, critical)
	{
	}

	public X509Extension(string oid, ReadOnlySpan<byte> rawData, bool critical)
		: this(new Oid(oid), rawData, critical)
	{
	}

	public override void CopyFrom(AsnEncodedData asnEncodedData)
	{
		if (asnEncodedData == null)
		{
			throw new ArgumentNullException("asnEncodedData");
		}
		if (!(asnEncodedData is X509Extension x509Extension))
		{
			throw new ArgumentException(System.SR.Cryptography_X509_ExtensionMismatch);
		}
		base.CopyFrom(asnEncodedData);
		Critical = x509Extension.Critical;
	}

	internal X509Extension(Oid oid)
	{
		base.Oid = oid;
	}
}

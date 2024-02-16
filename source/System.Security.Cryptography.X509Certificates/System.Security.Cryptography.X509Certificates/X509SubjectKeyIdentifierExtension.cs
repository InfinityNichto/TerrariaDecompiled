using Internal.Cryptography;
using Internal.Cryptography.Pal;

namespace System.Security.Cryptography.X509Certificates;

public sealed class X509SubjectKeyIdentifierExtension : X509Extension
{
	private string _subjectKeyIdentifier;

	private bool _decoded;

	public string? SubjectKeyIdentifier
	{
		get
		{
			if (!_decoded)
			{
				X509Pal.Instance.DecodeX509SubjectKeyIdentifierExtension(base.RawData, out var subjectKeyIdentifier);
				_subjectKeyIdentifier = subjectKeyIdentifier.ToHexStringUpper();
				_decoded = true;
			}
			return _subjectKeyIdentifier;
		}
	}

	public X509SubjectKeyIdentifierExtension()
		: base(System.Security.Cryptography.Oids.SubjectKeyIdentifierOid)
	{
		_subjectKeyIdentifier = null;
		_decoded = true;
	}

	public X509SubjectKeyIdentifierExtension(AsnEncodedData encodedSubjectKeyIdentifier, bool critical)
		: base(System.Security.Cryptography.Oids.SubjectKeyIdentifierOid, encodedSubjectKeyIdentifier.RawData, critical)
	{
	}

	public X509SubjectKeyIdentifierExtension(byte[] subjectKeyIdentifier, bool critical)
		: this(subjectKeyIdentifier.AsSpanParameter("subjectKeyIdentifier"), critical)
	{
	}

	public X509SubjectKeyIdentifierExtension(ReadOnlySpan<byte> subjectKeyIdentifier, bool critical)
		: base(System.Security.Cryptography.Oids.SubjectKeyIdentifierOid, EncodeExtension(subjectKeyIdentifier), critical)
	{
	}

	public X509SubjectKeyIdentifierExtension(PublicKey key, bool critical)
		: this(key, X509SubjectKeyIdentifierHashAlgorithm.Sha1, critical)
	{
	}

	public X509SubjectKeyIdentifierExtension(PublicKey key, X509SubjectKeyIdentifierHashAlgorithm algorithm, bool critical)
		: base(System.Security.Cryptography.Oids.SubjectKeyIdentifierOid, EncodeExtension(key, algorithm), critical)
	{
	}

	public X509SubjectKeyIdentifierExtension(string subjectKeyIdentifier, bool critical)
		: base(System.Security.Cryptography.Oids.SubjectKeyIdentifierOid, EncodeExtension(subjectKeyIdentifier), critical)
	{
	}

	public override void CopyFrom(AsnEncodedData asnEncodedData)
	{
		base.CopyFrom(asnEncodedData);
		_decoded = false;
	}

	private static byte[] EncodeExtension(ReadOnlySpan<byte> subjectKeyIdentifier)
	{
		if (subjectKeyIdentifier.Length == 0)
		{
			throw new ArgumentException(System.SR.Arg_EmptyOrNullArray, "subjectKeyIdentifier");
		}
		return X509Pal.Instance.EncodeX509SubjectKeyIdentifierExtension(subjectKeyIdentifier);
	}

	private static byte[] EncodeExtension(string subjectKeyIdentifier)
	{
		if (subjectKeyIdentifier == null)
		{
			throw new ArgumentNullException("subjectKeyIdentifier");
		}
		byte[] array = subjectKeyIdentifier.DecodeHexString();
		return EncodeExtension(array);
	}

	private static byte[] EncodeExtension(PublicKey key, X509SubjectKeyIdentifierHashAlgorithm algorithm)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		byte[] array = GenerateSubjectKeyIdentifierFromPublicKey(key, algorithm);
		return EncodeExtension(array);
	}

	private static byte[] GenerateSubjectKeyIdentifierFromPublicKey(PublicKey key, X509SubjectKeyIdentifierHashAlgorithm algorithm)
	{
		switch (algorithm)
		{
		case X509SubjectKeyIdentifierHashAlgorithm.Sha1:
			return SHA1.HashData(key.EncodedKeyValue.RawData);
		case X509SubjectKeyIdentifierHashAlgorithm.ShortSha1:
		{
			byte[] array = SHA1.HashData(key.EncodedKeyValue.RawData);
			byte[] array2 = new byte[8];
			Buffer.BlockCopy(array, array.Length - 8, array2, 0, array2.Length);
			array2[0] &= 15;
			array2[0] |= 64;
			return array2;
		}
		case X509SubjectKeyIdentifierHashAlgorithm.CapiSha1:
			return X509Pal.Instance.ComputeCapiSha1OfPublicKey(key);
		default:
			throw new ArgumentException(System.SR.Format(System.SR.Arg_EnumIllegalVal, algorithm), "algorithm");
		}
	}
}

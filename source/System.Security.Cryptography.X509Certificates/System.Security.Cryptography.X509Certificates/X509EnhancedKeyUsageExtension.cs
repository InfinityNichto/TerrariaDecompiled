using Internal.Cryptography.Pal;

namespace System.Security.Cryptography.X509Certificates;

public sealed class X509EnhancedKeyUsageExtension : X509Extension
{
	private OidCollection _enhancedKeyUsages;

	private bool _decoded;

	public OidCollection EnhancedKeyUsages
	{
		get
		{
			if (!_decoded)
			{
				X509Pal.Instance.DecodeX509EnhancedKeyUsageExtension(base.RawData, out _enhancedKeyUsages);
				_decoded = true;
			}
			OidCollection oidCollection = new OidCollection();
			OidEnumerator enumerator = _enhancedKeyUsages.GetEnumerator();
			while (enumerator.MoveNext())
			{
				Oid current = enumerator.Current;
				oidCollection.Add(current);
			}
			return oidCollection;
		}
	}

	public X509EnhancedKeyUsageExtension()
		: base(System.Security.Cryptography.Oids.EnhancedKeyUsageOid)
	{
		_enhancedKeyUsages = new OidCollection();
		_decoded = true;
	}

	public X509EnhancedKeyUsageExtension(AsnEncodedData encodedEnhancedKeyUsages, bool critical)
		: base(System.Security.Cryptography.Oids.EnhancedKeyUsageOid, encodedEnhancedKeyUsages.RawData, critical)
	{
	}

	public X509EnhancedKeyUsageExtension(OidCollection enhancedKeyUsages, bool critical)
		: base(System.Security.Cryptography.Oids.EnhancedKeyUsageOid, EncodeExtension(enhancedKeyUsages), critical)
	{
	}

	public override void CopyFrom(AsnEncodedData asnEncodedData)
	{
		base.CopyFrom(asnEncodedData);
		_decoded = false;
	}

	private static byte[] EncodeExtension(OidCollection enhancedKeyUsages)
	{
		if (enhancedKeyUsages == null)
		{
			throw new ArgumentNullException("enhancedKeyUsages");
		}
		return X509Pal.Instance.EncodeX509EnhancedKeyUsageExtension(enhancedKeyUsages);
	}
}

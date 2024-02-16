namespace System.Security.Cryptography;

public sealed class ECDiffieHellmanCngPublicKey : ECDiffieHellmanPublicKey
{
	private readonly CngKeyBlobFormat _format;

	private readonly string _curveName;

	private bool _disposed;

	public CngKeyBlobFormat BlobFormat => _format;

	internal ECDiffieHellmanCngPublicKey(byte[] keyBlob, string curveName, CngKeyBlobFormat format)
		: base(keyBlob)
	{
		_format = format;
		_curveName = curveName;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_disposed = true;
		}
		base.Dispose(disposing);
	}

	public override string ToXmlString()
	{
		throw new PlatformNotSupportedException();
	}

	public static ECDiffieHellmanCngPublicKey FromXmlString(string xml)
	{
		throw new PlatformNotSupportedException();
	}

	public static ECDiffieHellmanPublicKey FromByteArray(byte[] publicKeyBlob, CngKeyBlobFormat format)
	{
		if (publicKeyBlob == null)
		{
			throw new ArgumentNullException("publicKeyBlob");
		}
		if (format == null)
		{
			throw new ArgumentNullException("format");
		}
		using CngKey cngKey = CngKey.Import(publicKeyBlob, format);
		if (cngKey.AlgorithmGroup != CngAlgorithmGroup.ECDiffieHellman)
		{
			throw new ArgumentException(System.SR.Cryptography_ArgECDHRequiresECDHKey);
		}
		return new ECDiffieHellmanCngPublicKey(publicKeyBlob, null, format);
	}

	internal static ECDiffieHellmanCngPublicKey FromKey(CngKey key)
	{
		CngKeyBlobFormat format;
		string curveName;
		byte[] keyBlob = System.Security.Cryptography.ECCng.ExportKeyBlob(key, includePrivateParameters: false, out format, out curveName);
		return new ECDiffieHellmanCngPublicKey(keyBlob, curveName, format);
	}

	public CngKey Import()
	{
		if (_disposed)
		{
			throw new ObjectDisposedException("ECDiffieHellmanCngPublicKey");
		}
		return CngKey.Import(ToByteArray(), _curveName, BlobFormat);
	}

	public override ECParameters ExportExplicitParameters()
	{
		using CngKey key = Import();
		ECParameters ecParams = default(ECParameters);
		byte[] ecBlob = System.Security.Cryptography.ECCng.ExportFullKeyBlob(key, includePrivateParameters: false);
		System.Security.Cryptography.ECCng.ExportPrimeCurveParameters(ref ecParams, ecBlob, includePrivateParameters: false);
		return ecParams;
	}

	public override ECParameters ExportParameters()
	{
		using CngKey cngKey = Import();
		ECParameters ecParams = default(ECParameters);
		string oidValue;
		string curveName = cngKey.GetCurveName(out oidValue);
		if (string.IsNullOrEmpty(curveName))
		{
			byte[] ecBlob = System.Security.Cryptography.ECCng.ExportFullKeyBlob(cngKey, includePrivateParameters: false);
			System.Security.Cryptography.ECCng.ExportPrimeCurveParameters(ref ecParams, ecBlob, includePrivateParameters: false);
		}
		else
		{
			byte[] ecBlob2 = System.Security.Cryptography.ECCng.ExportKeyBlob(cngKey, includePrivateParameters: false);
			System.Security.Cryptography.ECCng.ExportNamedCurveParameters(ref ecParams, ecBlob2, includePrivateParameters: false);
			ecParams.Curve = ECCurve.CreateFromFriendlyName(curveName);
		}
		return ecParams;
	}
}

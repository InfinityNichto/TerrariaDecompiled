using System.Formats.Asn1;

namespace System.Security.Cryptography;

public abstract class ECDiffieHellmanPublicKey : IDisposable
{
	private readonly byte[] _keyBlob;

	protected ECDiffieHellmanPublicKey()
	{
		_keyBlob = Array.Empty<byte>();
	}

	protected ECDiffieHellmanPublicKey(byte[] keyBlob)
	{
		if (keyBlob == null)
		{
			throw new ArgumentNullException("keyBlob");
		}
		_keyBlob = (byte[])keyBlob.Clone();
	}

	public void Dispose()
	{
		Dispose(disposing: true);
	}

	protected virtual void Dispose(bool disposing)
	{
	}

	public virtual byte[] ToByteArray()
	{
		return (byte[])_keyBlob.Clone();
	}

	public virtual string ToXmlString()
	{
		throw new NotImplementedException(System.SR.NotSupported_SubclassOverride);
	}

	public virtual ECParameters ExportParameters()
	{
		throw new NotSupportedException(System.SR.NotSupported_SubclassOverride);
	}

	public virtual ECParameters ExportExplicitParameters()
	{
		throw new NotSupportedException(System.SR.NotSupported_SubclassOverride);
	}

	public virtual bool TryExportSubjectPublicKeyInfo(Span<byte> destination, out int bytesWritten)
	{
		ECParameters ecParameters = ExportParameters();
		AsnWriter asnWriter = EccKeyFormatHelper.WriteSubjectPublicKeyInfo(ecParameters);
		return asnWriter.TryEncode(destination, out bytesWritten);
	}

	public virtual byte[] ExportSubjectPublicKeyInfo()
	{
		ECParameters ecParameters = ExportParameters();
		AsnWriter asnWriter = EccKeyFormatHelper.WriteSubjectPublicKeyInfo(ecParameters);
		return asnWriter.Encode();
	}
}

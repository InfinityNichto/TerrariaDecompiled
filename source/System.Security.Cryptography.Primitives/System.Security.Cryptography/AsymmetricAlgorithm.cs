using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography;

public abstract class AsymmetricAlgorithm : IDisposable
{
	private delegate bool TryExportPbe<T>(ReadOnlySpan<T> password, PbeParameters pbeParameters, Span<byte> destination, out int bytesWritten);

	private delegate bool TryExport(Span<byte> destination, out int bytesWritten);

	protected int KeySizeValue;

	[MaybeNull]
	protected KeySizes[] LegalKeySizesValue;

	public virtual int KeySize
	{
		get
		{
			return KeySizeValue;
		}
		set
		{
			if (!value.IsLegalSize(LegalKeySizes))
			{
				throw new CryptographicException(System.SR.Cryptography_InvalidKeySize);
			}
			KeySizeValue = value;
		}
	}

	public virtual KeySizes[] LegalKeySizes => (KeySizes[])LegalKeySizesValue.Clone();

	public virtual string? SignatureAlgorithm
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public virtual string? KeyExchangeAlgorithm
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	[Obsolete("The default implementation of this cryptography algorithm is not supported.", DiagnosticId = "SYSLIB0007", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public static AsymmetricAlgorithm Create()
	{
		throw new PlatformNotSupportedException(System.SR.Cryptography_DefaultAlgorithm_NotSupported);
	}

	[RequiresUnreferencedCode("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
	public static AsymmetricAlgorithm? Create(string algName)
	{
		return (AsymmetricAlgorithm)CryptoConfigForwarder.CreateFromName(algName);
	}

	public virtual void FromXmlString(string xmlString)
	{
		throw new NotImplementedException();
	}

	public virtual string ToXmlString(bool includePrivateParameters)
	{
		throw new NotImplementedException();
	}

	public void Clear()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	public void Dispose()
	{
		Clear();
	}

	protected virtual void Dispose(bool disposing)
	{
	}

	public virtual void ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, ReadOnlySpan<byte> source, out int bytesRead)
	{
		throw new NotImplementedException(System.SR.NotSupported_SubclassOverride);
	}

	public virtual void ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, ReadOnlySpan<byte> source, out int bytesRead)
	{
		throw new NotImplementedException(System.SR.NotSupported_SubclassOverride);
	}

	public virtual void ImportPkcs8PrivateKey(ReadOnlySpan<byte> source, out int bytesRead)
	{
		throw new NotImplementedException(System.SR.NotSupported_SubclassOverride);
	}

	public virtual void ImportSubjectPublicKeyInfo(ReadOnlySpan<byte> source, out int bytesRead)
	{
		throw new NotImplementedException(System.SR.NotSupported_SubclassOverride);
	}

	public virtual byte[] ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters)
	{
		return ExportArray(passwordBytes, pbeParameters, delegate(ReadOnlySpan<byte> span, PbeParameters parameters, Span<byte> destination, out int i)
		{
			return TryExportEncryptedPkcs8PrivateKey(span, parameters, destination, out i);
		});
	}

	public virtual byte[] ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, PbeParameters pbeParameters)
	{
		return ExportArray(password, pbeParameters, delegate(ReadOnlySpan<char> span, PbeParameters parameters, Span<byte> destination, out int i)
		{
			return TryExportEncryptedPkcs8PrivateKey(span, parameters, destination, out i);
		});
	}

	public virtual byte[] ExportPkcs8PrivateKey()
	{
		return ExportArray(delegate(Span<byte> destination, out int i)
		{
			return TryExportPkcs8PrivateKey(destination, out i);
		});
	}

	public virtual byte[] ExportSubjectPublicKeyInfo()
	{
		return ExportArray(delegate(Span<byte> destination, out int i)
		{
			return TryExportSubjectPublicKeyInfo(destination, out i);
		});
	}

	public virtual bool TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters, Span<byte> destination, out int bytesWritten)
	{
		throw new NotImplementedException(System.SR.NotSupported_SubclassOverride);
	}

	public virtual bool TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, PbeParameters pbeParameters, Span<byte> destination, out int bytesWritten)
	{
		throw new NotImplementedException(System.SR.NotSupported_SubclassOverride);
	}

	public virtual bool TryExportPkcs8PrivateKey(Span<byte> destination, out int bytesWritten)
	{
		throw new NotImplementedException(System.SR.NotSupported_SubclassOverride);
	}

	public virtual bool TryExportSubjectPublicKeyInfo(Span<byte> destination, out int bytesWritten)
	{
		throw new NotImplementedException(System.SR.NotSupported_SubclassOverride);
	}

	public virtual void ImportFromEncryptedPem(ReadOnlySpan<char> input, ReadOnlySpan<char> password)
	{
		throw new NotImplementedException(System.SR.NotSupported_SubclassOverride);
	}

	public virtual void ImportFromEncryptedPem(ReadOnlySpan<char> input, ReadOnlySpan<byte> passwordBytes)
	{
		throw new NotImplementedException(System.SR.NotSupported_SubclassOverride);
	}

	public virtual void ImportFromPem(ReadOnlySpan<char> input)
	{
		throw new NotImplementedException(System.SR.NotSupported_SubclassOverride);
	}

	private unsafe static byte[] ExportArray<T>(ReadOnlySpan<T> password, PbeParameters pbeParameters, TryExportPbe<T> exporter)
	{
		int minimumLength = 4096;
		while (true)
		{
			byte[] array = CryptoPool.Rent(minimumLength);
			int bytesWritten = 0;
			minimumLength = array.Length;
			fixed (byte* ptr = array)
			{
				try
				{
					if (exporter(password, pbeParameters, array, out bytesWritten))
					{
						return new Span<byte>(array, 0, bytesWritten).ToArray();
					}
				}
				finally
				{
					CryptoPool.Return(array, bytesWritten);
				}
				minimumLength = checked(minimumLength * 2);
			}
		}
	}

	private unsafe static byte[] ExportArray(TryExport exporter)
	{
		int minimumLength = 4096;
		while (true)
		{
			byte[] array = CryptoPool.Rent(minimumLength);
			int bytesWritten = 0;
			minimumLength = array.Length;
			fixed (byte* ptr = array)
			{
				try
				{
					if (exporter(array, out bytesWritten))
					{
						return new Span<byte>(array, 0, bytesWritten).ToArray();
					}
				}
				finally
				{
					CryptoPool.Return(array, bytesWritten);
				}
				minimumLength = checked(minimumLength * 2);
			}
		}
	}
}

using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography;

public abstract class HMAC : KeyedHashAlgorithm
{
	private string _hashName;

	private int _blockSizeValue = 64;

	protected int BlockSizeValue
	{
		get
		{
			return _blockSizeValue;
		}
		set
		{
			_blockSizeValue = value;
		}
	}

	public string HashName
	{
		get
		{
			return _hashName;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("HashName");
			}
			if (_hashName != null && value != _hashName)
			{
				throw new PlatformNotSupportedException(System.SR.HashNameMultipleSetNotSupported);
			}
			_hashName = value;
		}
	}

	public override byte[] Key
	{
		get
		{
			return base.Key;
		}
		set
		{
			base.Key = value;
		}
	}

	[Obsolete("The default implementation of this cryptography algorithm is not supported.", DiagnosticId = "SYSLIB0007", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public new static HMAC Create()
	{
		throw new PlatformNotSupportedException(System.SR.Cryptography_DefaultAlgorithm_NotSupported);
	}

	[RequiresUnreferencedCode("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
	public new static HMAC? Create(string algorithmName)
	{
		return (HMAC)CryptoConfigForwarder.CreateFromName(algorithmName);
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
	}

	protected override void HashCore(byte[] rgb, int ib, int cb)
	{
		throw new PlatformNotSupportedException(System.SR.CryptoConfigNotSupported);
	}

	protected override void HashCore(ReadOnlySpan<byte> source)
	{
		throw new PlatformNotSupportedException(System.SR.CryptoConfigNotSupported);
	}

	protected override byte[] HashFinal()
	{
		throw new PlatformNotSupportedException(System.SR.CryptoConfigNotSupported);
	}

	protected override bool TryHashFinal(Span<byte> destination, out int bytesWritten)
	{
		throw new PlatformNotSupportedException(System.SR.CryptoConfigNotSupported);
	}

	public override void Initialize()
	{
		throw new PlatformNotSupportedException(System.SR.CryptoConfigNotSupported);
	}
}

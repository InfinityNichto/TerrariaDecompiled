using System.ComponentModel;

namespace System.Security.Cryptography;

[Obsolete("Derived cryptographic types are obsolete. Use the Create method on the base type instead.", DiagnosticId = "SYSLIB0021", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class SHA512CryptoServiceProvider : SHA512
{
	private readonly IncrementalHash _incrementalHash;

	private bool _running;

	public SHA512CryptoServiceProvider()
	{
		_incrementalHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA512);
		HashSizeValue = 512;
	}

	public override void Initialize()
	{
		if (_running)
		{
			Span<byte> destination = stackalloc byte[64];
			if (!_incrementalHash.TryGetHashAndReset(destination, out var _))
			{
				throw new CryptographicException();
			}
			_running = false;
		}
	}

	protected override void HashCore(byte[] array, int ibStart, int cbSize)
	{
		_running = true;
		_incrementalHash.AppendData(array, ibStart, cbSize);
	}

	protected override void HashCore(ReadOnlySpan<byte> source)
	{
		_running = true;
		_incrementalHash.AppendData(source);
	}

	protected override byte[] HashFinal()
	{
		_running = false;
		return _incrementalHash.GetHashAndReset();
	}

	protected override bool TryHashFinal(Span<byte> destination, out int bytesWritten)
	{
		_running = false;
		return _incrementalHash.TryGetHashAndReset(destination, out bytesWritten);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_incrementalHash.Dispose();
		}
		base.Dispose(disposing);
	}
}

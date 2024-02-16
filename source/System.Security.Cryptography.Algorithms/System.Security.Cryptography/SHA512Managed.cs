using System.ComponentModel;
using Internal.Cryptography;

namespace System.Security.Cryptography;

[Obsolete("Derived cryptographic types are obsolete. Use the Create method on the base type instead.", DiagnosticId = "SYSLIB0021", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class SHA512Managed : SHA512
{
	private readonly HashProvider _hashProvider;

	public SHA512Managed()
	{
		_hashProvider = HashProviderDispenser.CreateHashProvider("SHA512");
		HashSizeValue = _hashProvider.HashSizeInBytes * 8;
	}

	protected sealed override void HashCore(byte[] array, int ibStart, int cbSize)
	{
		_hashProvider.AppendHashData(array, ibStart, cbSize);
	}

	protected sealed override void HashCore(ReadOnlySpan<byte> source)
	{
		_hashProvider.AppendHashData(source);
	}

	protected sealed override byte[] HashFinal()
	{
		return _hashProvider.FinalizeHashAndReset();
	}

	protected sealed override bool TryHashFinal(Span<byte> destination, out int bytesWritten)
	{
		return _hashProvider.TryFinalizeHashAndReset(destination, out bytesWritten);
	}

	public sealed override void Initialize()
	{
		_hashProvider.Reset();
	}

	protected sealed override void Dispose(bool disposing)
	{
		_hashProvider.Dispose(disposing);
		base.Dispose(disposing);
	}
}

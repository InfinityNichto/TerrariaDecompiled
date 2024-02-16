using System.Runtime.Versioning;
using Internal.Cryptography;

namespace System.Security.Cryptography;

[UnsupportedOSPlatform("browser")]
public class HMACSHA512 : HMAC
{
	private HMACCommon _hMacCommon;

	[Obsolete("ProduceLegacyHmacValues is obsolete. Producing legacy HMAC values is not supported.", DiagnosticId = "SYSLIB0029", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public bool ProduceLegacyHmacValues
	{
		get
		{
			return false;
		}
		set
		{
			if (value)
			{
				throw new PlatformNotSupportedException();
			}
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
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			_hMacCommon.ChangeKey(value);
			base.Key = _hMacCommon.ActualKey;
		}
	}

	public HMACSHA512()
		: this(RandomNumberGenerator.GetBytes(128))
	{
	}

	public HMACSHA512(byte[] key)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		base.HashName = "SHA512";
		_hMacCommon = new HMACCommon("SHA512", key, 128);
		base.Key = _hMacCommon.ActualKey;
		base.BlockSizeValue = 128;
		HashSizeValue = _hMacCommon.HashSizeInBits;
	}

	protected override void HashCore(byte[] rgb, int ib, int cb)
	{
		_hMacCommon.AppendHashData(rgb, ib, cb);
	}

	protected override void HashCore(ReadOnlySpan<byte> source)
	{
		_hMacCommon.AppendHashData(source);
	}

	protected override byte[] HashFinal()
	{
		return _hMacCommon.FinalizeHashAndReset();
	}

	protected override bool TryHashFinal(Span<byte> destination, out int bytesWritten)
	{
		return _hMacCommon.TryFinalizeHashAndReset(destination, out bytesWritten);
	}

	public override void Initialize()
	{
		_hMacCommon.Reset();
	}

	public static byte[] HashData(byte[] key, byte[] source)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return HashData(new ReadOnlySpan<byte>(key), new ReadOnlySpan<byte>(source));
	}

	public static byte[] HashData(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source)
	{
		byte[] array = new byte[64];
		int num = HashData(key, source, array.AsSpan());
		return array;
	}

	public static int HashData(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, Span<byte> destination)
	{
		if (!TryHashData(key, source, destination, out var bytesWritten))
		{
			throw new ArgumentException(System.SR.Argument_DestinationTooShort, "destination");
		}
		return bytesWritten;
	}

	public static bool TryHashData(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length < 64)
		{
			bytesWritten = 0;
			return false;
		}
		bytesWritten = HashProviderDispenser.OneShotHashProvider.MacData("SHA512", key, source, destination);
		return true;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			HMACCommon hMacCommon = _hMacCommon;
			if (hMacCommon != null)
			{
				_hMacCommon = null;
				hMacCommon.Dispose(disposing);
			}
		}
		base.Dispose(disposing);
	}
}

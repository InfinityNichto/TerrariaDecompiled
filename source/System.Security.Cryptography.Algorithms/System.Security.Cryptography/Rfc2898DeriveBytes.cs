using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using System.Text;
using Internal.Cryptography;

namespace System.Security.Cryptography;

[UnsupportedOSPlatform("browser")]
public class Rfc2898DeriveBytes : DeriveBytes
{
	private byte[] _salt;

	private uint _iterations;

	private HMAC _hmac;

	private readonly int _blockSize;

	private byte[] _buffer;

	private uint _block;

	private int _startIndex;

	private int _endIndex;

	private static readonly Encoding s_throwingUtf8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

	public HashAlgorithmName HashAlgorithm { get; }

	public int IterationCount
	{
		get
		{
			return (int)_iterations;
		}
		set
		{
			if (value <= 0)
			{
				throw new ArgumentOutOfRangeException("value", System.SR.ArgumentOutOfRange_NeedPosNum);
			}
			_iterations = (uint)value;
			Initialize();
		}
	}

	public byte[] Salt
	{
		get
		{
			return _salt.AsSpan(0, _salt.Length - 4).ToArray();
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			_salt = new byte[value.Length + 4];
			value.AsSpan().CopyTo(_salt);
			Initialize();
		}
	}

	public Rfc2898DeriveBytes(byte[] password, byte[] salt, int iterations)
		: this(password, salt, iterations, HashAlgorithmName.SHA1)
	{
	}

	public Rfc2898DeriveBytes(byte[] password, byte[] salt, int iterations, HashAlgorithmName hashAlgorithm)
		: this(password, salt, iterations, hashAlgorithm, clearPassword: false)
	{
	}

	public Rfc2898DeriveBytes(string password, byte[] salt)
		: this(password, salt, 1000)
	{
	}

	public Rfc2898DeriveBytes(string password, byte[] salt, int iterations)
		: this(password, salt, iterations, HashAlgorithmName.SHA1)
	{
	}

	public Rfc2898DeriveBytes(string password, byte[] salt, int iterations, HashAlgorithmName hashAlgorithm)
		: this(Encoding.UTF8.GetBytes(password), salt, iterations, hashAlgorithm, clearPassword: true)
	{
	}

	public Rfc2898DeriveBytes(string password, int saltSize)
		: this(password, saltSize, 1000)
	{
	}

	public Rfc2898DeriveBytes(string password, int saltSize, int iterations)
		: this(password, saltSize, iterations, HashAlgorithmName.SHA1)
	{
	}

	public Rfc2898DeriveBytes(string password, int saltSize, int iterations, HashAlgorithmName hashAlgorithm)
	{
		if (saltSize < 0)
		{
			throw new ArgumentOutOfRangeException("saltSize", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (iterations <= 0)
		{
			throw new ArgumentOutOfRangeException("iterations", System.SR.ArgumentOutOfRange_NeedPosNum);
		}
		_salt = new byte[saltSize + 4];
		RandomNumberGenerator.Fill(_salt.AsSpan(0, saltSize));
		_iterations = (uint)iterations;
		byte[] bytes = Encoding.UTF8.GetBytes(password);
		HashAlgorithm = hashAlgorithm;
		_hmac = OpenHmac(bytes);
		CryptographicOperations.ZeroMemory(bytes);
		_blockSize = _hmac.HashSize >> 3;
		Initialize();
	}

	internal Rfc2898DeriveBytes(byte[] password, byte[] salt, int iterations, HashAlgorithmName hashAlgorithm, bool clearPassword)
	{
		if (salt == null)
		{
			throw new ArgumentNullException("salt");
		}
		if (iterations <= 0)
		{
			throw new ArgumentOutOfRangeException("iterations", System.SR.ArgumentOutOfRange_NeedPosNum);
		}
		if (password == null)
		{
			throw new NullReferenceException();
		}
		_salt = new byte[salt.Length + 4];
		salt.AsSpan().CopyTo(_salt);
		_iterations = (uint)iterations;
		HashAlgorithm = hashAlgorithm;
		_hmac = OpenHmac(password);
		if (clearPassword)
		{
			CryptographicOperations.ZeroMemory(password);
		}
		_blockSize = _hmac.HashSize >> 3;
		Initialize();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			if (_hmac != null)
			{
				_hmac.Dispose();
				_hmac = null;
			}
			if (_buffer != null)
			{
				Array.Clear(_buffer);
			}
			if (_salt != null)
			{
				Array.Clear(_salt);
			}
		}
		base.Dispose(disposing);
	}

	public override byte[] GetBytes(int cb)
	{
		if (cb <= 0)
		{
			throw new ArgumentOutOfRangeException("cb", System.SR.ArgumentOutOfRange_NeedPosNum);
		}
		byte[] array = new byte[cb];
		int i = 0;
		int num = _endIndex - _startIndex;
		if (num > 0)
		{
			if (cb < num)
			{
				Buffer.BlockCopy(_buffer, _startIndex, array, 0, cb);
				_startIndex += cb;
				return array;
			}
			Buffer.BlockCopy(_buffer, _startIndex, array, 0, num);
			_startIndex = (_endIndex = 0);
			i += num;
		}
		for (; i < cb; i += _blockSize)
		{
			Func();
			int num2 = cb - i;
			if (num2 >= _blockSize)
			{
				Buffer.BlockCopy(_buffer, 0, array, i, _blockSize);
				continue;
			}
			Buffer.BlockCopy(_buffer, 0, array, i, num2);
			_startIndex = num2;
			_endIndex = _buffer.Length;
			return array;
		}
		return array;
	}

	[Obsolete("Rfc2898DeriveBytes.CryptDeriveKey is obsolete and is not supported. Use PasswordDeriveBytes.CryptDeriveKey instead.", DiagnosticId = "SYSLIB0033", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public byte[] CryptDeriveKey(string algname, string alghashname, int keySize, byte[] rgbIV)
	{
		throw new PlatformNotSupportedException();
	}

	public override void Reset()
	{
		Initialize();
	}

	private HMAC OpenHmac(byte[] password)
	{
		HashAlgorithmName hashAlgorithm = HashAlgorithm;
		if (string.IsNullOrEmpty(hashAlgorithm.Name))
		{
			throw new CryptographicException(System.SR.Cryptography_HashAlgorithmNameNullOrEmpty);
		}
		if (hashAlgorithm == HashAlgorithmName.SHA1)
		{
			return new HMACSHA1(password);
		}
		if (hashAlgorithm == HashAlgorithmName.SHA256)
		{
			return new HMACSHA256(password);
		}
		if (hashAlgorithm == HashAlgorithmName.SHA384)
		{
			return new HMACSHA384(password);
		}
		if (hashAlgorithm == HashAlgorithmName.SHA512)
		{
			return new HMACSHA512(password);
		}
		throw new CryptographicException(System.SR.Format(System.SR.Cryptography_UnknownHashAlgorithm, hashAlgorithm.Name));
	}

	[MemberNotNull("_buffer")]
	private void Initialize()
	{
		if (_buffer != null)
		{
			Array.Clear(_buffer);
		}
		_buffer = new byte[_blockSize];
		_block = 0u;
		_startIndex = (_endIndex = 0);
	}

	private void Func()
	{
		if (_block == uint.MaxValue)
		{
			throw new CryptographicException(System.SR.Cryptography_ExceedKdfExtractLimit);
		}
		BinaryPrimitives.WriteUInt32BigEndian(_salt.AsSpan(_salt.Length - 4), _block + 1);
		Span<byte> span = stackalloc byte[64];
		span = span.Slice(0, _blockSize);
		if (!_hmac.TryComputeHash(_salt, span, out var bytesWritten) || bytesWritten != _blockSize)
		{
			throw new CryptographicException();
		}
		span.CopyTo(_buffer);
		for (int i = 2; i <= _iterations; i++)
		{
			if (!_hmac.TryComputeHash(span, span, out bytesWritten) || bytesWritten != _blockSize)
			{
				throw new CryptographicException();
			}
			for (int num = _buffer.Length - 1; num >= 0; num--)
			{
				_buffer[num] ^= span[num];
			}
		}
		_block++;
	}

	public static byte[] Pbkdf2(byte[] password, byte[] salt, int iterations, HashAlgorithmName hashAlgorithm, int outputLength)
	{
		if (password == null)
		{
			throw new ArgumentNullException("password");
		}
		if (salt == null)
		{
			throw new ArgumentNullException("salt");
		}
		return Pbkdf2(new ReadOnlySpan<byte>(password), new ReadOnlySpan<byte>(salt), iterations, hashAlgorithm, outputLength);
	}

	public static byte[] Pbkdf2(ReadOnlySpan<byte> password, ReadOnlySpan<byte> salt, int iterations, HashAlgorithmName hashAlgorithm, int outputLength)
	{
		if (iterations <= 0)
		{
			throw new ArgumentOutOfRangeException("iterations", System.SR.ArgumentOutOfRange_NeedPosNum);
		}
		if (outputLength < 0)
		{
			throw new ArgumentOutOfRangeException("outputLength", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		ValidateHashAlgorithm(hashAlgorithm);
		byte[] array = new byte[outputLength];
		Pbkdf2Core(password, salt, array, iterations, hashAlgorithm);
		return array;
	}

	public static void Pbkdf2(ReadOnlySpan<byte> password, ReadOnlySpan<byte> salt, Span<byte> destination, int iterations, HashAlgorithmName hashAlgorithm)
	{
		if (iterations <= 0)
		{
			throw new ArgumentOutOfRangeException("iterations", System.SR.ArgumentOutOfRange_NeedPosNum);
		}
		ValidateHashAlgorithm(hashAlgorithm);
		Pbkdf2Core(password, salt, destination, iterations, hashAlgorithm);
	}

	public static byte[] Pbkdf2(string password, byte[] salt, int iterations, HashAlgorithmName hashAlgorithm, int outputLength)
	{
		if (password == null)
		{
			throw new ArgumentNullException("password");
		}
		if (salt == null)
		{
			throw new ArgumentNullException("salt");
		}
		return Pbkdf2(password.AsSpan(), new ReadOnlySpan<byte>(salt), iterations, hashAlgorithm, outputLength);
	}

	public static byte[] Pbkdf2(ReadOnlySpan<char> password, ReadOnlySpan<byte> salt, int iterations, HashAlgorithmName hashAlgorithm, int outputLength)
	{
		if (outputLength < 0)
		{
			throw new ArgumentOutOfRangeException("outputLength", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (iterations <= 0)
		{
			throw new ArgumentOutOfRangeException("iterations", System.SR.ArgumentOutOfRange_NeedPosNum);
		}
		ValidateHashAlgorithm(hashAlgorithm);
		byte[] array = new byte[outputLength];
		Pbkdf2Core(password, salt, array, iterations, hashAlgorithm);
		return array;
	}

	public static void Pbkdf2(ReadOnlySpan<char> password, ReadOnlySpan<byte> salt, Span<byte> destination, int iterations, HashAlgorithmName hashAlgorithm)
	{
		if (iterations <= 0)
		{
			throw new ArgumentOutOfRangeException("iterations", System.SR.ArgumentOutOfRange_NeedPosNum);
		}
		ValidateHashAlgorithm(hashAlgorithm);
		Pbkdf2Core(password, salt, destination, iterations, hashAlgorithm);
	}

	private static void Pbkdf2Core(ReadOnlySpan<char> password, ReadOnlySpan<byte> salt, Span<byte> destination, int iterations, HashAlgorithmName hashAlgorithm)
	{
		if (!destination.IsEmpty)
		{
			byte[] array = null;
			int maxByteCount = s_throwingUtf8Encoding.GetMaxByteCount(password.Length);
			Span<byte> span = ((maxByteCount <= 256) ? stackalloc byte[256] : ((Span<byte>)(array = System.Security.Cryptography.CryptoPool.Rent(maxByteCount))));
			Span<byte> bytes = span;
			Span<byte> span2 = bytes[..s_throwingUtf8Encoding.GetBytes(password, bytes)];
			try
			{
				Pbkdf2Implementation.Fill(span2, salt, iterations, hashAlgorithm, destination);
			}
			finally
			{
				CryptographicOperations.ZeroMemory(span2);
			}
			if (array != null)
			{
				System.Security.Cryptography.CryptoPool.Return(array, 0);
			}
		}
	}

	private static void Pbkdf2Core(ReadOnlySpan<byte> password, ReadOnlySpan<byte> salt, Span<byte> destination, int iterations, HashAlgorithmName hashAlgorithm)
	{
		if (!destination.IsEmpty)
		{
			Pbkdf2Implementation.Fill(password, salt, iterations, hashAlgorithm, destination);
		}
	}

	private static void ValidateHashAlgorithm(HashAlgorithmName hashAlgorithm)
	{
		if (string.IsNullOrEmpty(hashAlgorithm.Name))
		{
			throw new ArgumentException(System.SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");
		}
		string name = hashAlgorithm.Name;
		if (name != HashAlgorithmName.SHA1.Name && name != HashAlgorithmName.SHA256.Name && name != HashAlgorithmName.SHA384.Name && name != HashAlgorithmName.SHA512.Name)
		{
			throw new CryptographicException(System.SR.Format(System.SR.Cryptography_UnknownHashAlgorithm, name));
		}
	}
}

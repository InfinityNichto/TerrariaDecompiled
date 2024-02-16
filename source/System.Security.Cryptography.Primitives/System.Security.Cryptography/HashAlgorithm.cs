using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Security.Cryptography;

public abstract class HashAlgorithm : IDisposable, ICryptoTransform
{
	private bool _disposed;

	protected int HashSizeValue;

	protected internal byte[]? HashValue;

	protected int State;

	public virtual int HashSize => HashSizeValue;

	public virtual byte[]? Hash
	{
		get
		{
			if (_disposed)
			{
				throw new ObjectDisposedException(null);
			}
			if (State != 0)
			{
				throw new CryptographicUnexpectedOperationException(System.SR.Cryptography_HashNotYetFinalized);
			}
			return (byte[])HashValue?.Clone();
		}
	}

	public virtual int InputBlockSize => 1;

	public virtual int OutputBlockSize => 1;

	public virtual bool CanTransformMultipleBlocks => true;

	public virtual bool CanReuseTransform => true;

	[Obsolete("The default implementation of this cryptography algorithm is not supported.", DiagnosticId = "SYSLIB0007", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public static HashAlgorithm Create()
	{
		return CryptoConfigForwarder.CreateDefaultHashAlgorithm();
	}

	[RequiresUnreferencedCode("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
	public static HashAlgorithm? Create(string hashName)
	{
		return (HashAlgorithm)CryptoConfigForwarder.CreateFromName(hashName);
	}

	public byte[] ComputeHash(byte[] buffer)
	{
		if (_disposed)
		{
			throw new ObjectDisposedException(null);
		}
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer");
		}
		HashCore(buffer, 0, buffer.Length);
		return CaptureHashCodeAndReinitialize();
	}

	public bool TryComputeHash(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
	{
		if (_disposed)
		{
			throw new ObjectDisposedException(null);
		}
		if (destination.Length < HashSizeValue / 8)
		{
			bytesWritten = 0;
			return false;
		}
		HashCore(source);
		if (!TryHashFinal(destination, out bytesWritten))
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_IncorrectImplementation);
		}
		HashValue = null;
		Initialize();
		return true;
	}

	public byte[] ComputeHash(byte[] buffer, int offset, int count)
	{
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer");
		}
		if (offset < 0)
		{
			throw new ArgumentOutOfRangeException("offset", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (count < 0 || count > buffer.Length)
		{
			throw new ArgumentException(System.SR.Argument_InvalidValue);
		}
		if (buffer.Length - count < offset)
		{
			throw new ArgumentException(System.SR.Argument_InvalidOffLen);
		}
		if (_disposed)
		{
			throw new ObjectDisposedException(null);
		}
		HashCore(buffer, offset, count);
		return CaptureHashCodeAndReinitialize();
	}

	public byte[] ComputeHash(Stream inputStream)
	{
		if (_disposed)
		{
			throw new ObjectDisposedException(null);
		}
		byte[] array = ArrayPool<byte>.Shared.Rent(4096);
		int num = 0;
		int num2;
		while ((num2 = inputStream.Read(array, 0, array.Length)) > 0)
		{
			if (num2 > num)
			{
				num = num2;
			}
			HashCore(array, 0, num2);
		}
		CryptographicOperations.ZeroMemory(array.AsSpan(0, num));
		ArrayPool<byte>.Shared.Return(array);
		return CaptureHashCodeAndReinitialize();
	}

	public Task<byte[]> ComputeHashAsync(Stream inputStream, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (inputStream == null)
		{
			throw new ArgumentNullException("inputStream");
		}
		if (_disposed)
		{
			throw new ObjectDisposedException(null);
		}
		return ComputeHashAsyncCore(inputStream, cancellationToken);
	}

	private async Task<byte[]> ComputeHashAsyncCore(Stream inputStream, CancellationToken cancellationToken)
	{
		byte[] rented = ArrayPool<byte>.Shared.Rent(4096);
		Memory<byte> buffer = rented;
		int clearLimit = 0;
		int num;
		while ((num = await inputStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false)) > 0)
		{
			if (num > clearLimit)
			{
				clearLimit = num;
			}
			HashCore(rented, 0, num);
		}
		CryptographicOperations.ZeroMemory(rented.AsSpan(0, clearLimit));
		ArrayPool<byte>.Shared.Return(rented);
		return CaptureHashCodeAndReinitialize();
	}

	private byte[] CaptureHashCodeAndReinitialize()
	{
		HashValue = HashFinal();
		byte[] result = (byte[])HashValue.Clone();
		Initialize();
		return result;
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	public void Clear()
	{
		((IDisposable)this).Dispose();
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			_disposed = true;
		}
	}

	public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[]? outputBuffer, int outputOffset)
	{
		ValidateTransformBlock(inputBuffer, inputOffset, inputCount);
		State = 1;
		HashCore(inputBuffer, inputOffset, inputCount);
		if (outputBuffer != null && (inputBuffer != outputBuffer || inputOffset != outputOffset))
		{
			Buffer.BlockCopy(inputBuffer, inputOffset, outputBuffer, outputOffset, inputCount);
		}
		return inputCount;
	}

	public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
	{
		ValidateTransformBlock(inputBuffer, inputOffset, inputCount);
		HashCore(inputBuffer, inputOffset, inputCount);
		HashValue = CaptureHashCodeAndReinitialize();
		byte[] array;
		if (inputCount != 0)
		{
			array = new byte[inputCount];
			Buffer.BlockCopy(inputBuffer, inputOffset, array, 0, inputCount);
		}
		else
		{
			array = Array.Empty<byte>();
		}
		State = 0;
		return array;
	}

	private void ValidateTransformBlock(byte[] inputBuffer, int inputOffset, int inputCount)
	{
		if (inputBuffer == null)
		{
			throw new ArgumentNullException("inputBuffer");
		}
		if (inputOffset < 0)
		{
			throw new ArgumentOutOfRangeException("inputOffset", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (inputCount < 0 || inputCount > inputBuffer.Length)
		{
			throw new ArgumentException(System.SR.Argument_InvalidValue);
		}
		if (inputBuffer.Length - inputCount < inputOffset)
		{
			throw new ArgumentException(System.SR.Argument_InvalidOffLen);
		}
		if (_disposed)
		{
			throw new ObjectDisposedException(null);
		}
	}

	protected abstract void HashCore(byte[] array, int ibStart, int cbSize);

	protected abstract byte[] HashFinal();

	public abstract void Initialize();

	protected virtual void HashCore(ReadOnlySpan<byte> source)
	{
		byte[] array = ArrayPool<byte>.Shared.Rent(source.Length);
		source.CopyTo(array);
		HashCore(array, 0, source.Length);
		Array.Clear(array, 0, source.Length);
		ArrayPool<byte>.Shared.Return(array);
	}

	protected virtual bool TryHashFinal(Span<byte> destination, out int bytesWritten)
	{
		int num = HashSizeValue / 8;
		if (destination.Length >= num)
		{
			byte[] array = HashFinal();
			if (array.Length == num)
			{
				new ReadOnlySpan<byte>(array).CopyTo(destination);
				bytesWritten = array.Length;
				return true;
			}
			throw new InvalidOperationException(System.SR.InvalidOperation_IncorrectImplementation);
		}
		bytesWritten = 0;
		return false;
	}
}

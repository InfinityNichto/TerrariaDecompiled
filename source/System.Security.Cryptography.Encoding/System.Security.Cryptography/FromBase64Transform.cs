using System.Buffers.Text;
using System.Runtime.CompilerServices;

namespace System.Security.Cryptography;

public class FromBase64Transform : ICryptoTransform, IDisposable
{
	private byte[] _inputBuffer = new byte[4];

	private int _inputIndex;

	private readonly FromBase64TransformMode _whitespaces;

	public int InputBlockSize => 4;

	public int OutputBlockSize => 3;

	public bool CanTransformMultipleBlocks => true;

	public virtual bool CanReuseTransform => true;

	public FromBase64Transform()
		: this(FromBase64TransformMode.IgnoreWhiteSpaces)
	{
	}

	public FromBase64Transform(FromBase64TransformMode whitespaces)
	{
		_whitespaces = whitespaces;
	}

	public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
	{
		ThrowHelper.ValidateTransformBlock(inputBuffer, inputOffset, inputCount);
		if (_inputBuffer == null)
		{
			ThrowHelper.ThrowObjectDisposed();
		}
		if (outputBuffer == null)
		{
			ThrowHelper.ThrowArgumentNull(ThrowHelper.ExceptionArgument.outputBuffer);
		}
		byte[] array = null;
		Span<byte> tmpBuffer = stackalloc byte[32];
		if (inputCount > 32)
		{
			tmpBuffer = (array = System.Security.Cryptography.CryptoPool.Rent(inputCount));
		}
		tmpBuffer = GetTempBuffer(inputBuffer.AsSpan(inputOffset, inputCount), tmpBuffer);
		int num = _inputIndex + tmpBuffer.Length;
		if (num < InputBlockSize)
		{
			tmpBuffer.CopyTo(_inputBuffer.AsSpan(_inputIndex));
			_inputIndex = num;
			ReturnToCryptoPool(array, tmpBuffer.Length);
			return 0;
		}
		ConvertFromBase64(tmpBuffer, outputBuffer.AsSpan(outputOffset), out var _, out var written);
		ReturnToCryptoPool(array, tmpBuffer.Length);
		return written;
	}

	public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
	{
		ThrowHelper.ValidateTransformBlock(inputBuffer, inputOffset, inputCount);
		if (_inputBuffer == null)
		{
			ThrowHelper.ThrowObjectDisposed();
		}
		if (inputCount == 0)
		{
			return Array.Empty<byte>();
		}
		byte[] array = null;
		Span<byte> tmpBuffer = stackalloc byte[32];
		if (inputCount > 32)
		{
			tmpBuffer = (array = System.Security.Cryptography.CryptoPool.Rent(inputCount));
		}
		tmpBuffer = GetTempBuffer(inputBuffer.AsSpan(inputOffset, inputCount), tmpBuffer);
		int num = _inputIndex + tmpBuffer.Length;
		if (num < InputBlockSize)
		{
			Reset();
			ReturnToCryptoPool(array, tmpBuffer.Length);
			return Array.Empty<byte>();
		}
		int outputSize = GetOutputSize(num, tmpBuffer);
		byte[] array2 = new byte[outputSize];
		ConvertFromBase64(tmpBuffer, array2, out var _, out var _);
		ReturnToCryptoPool(array, tmpBuffer.Length);
		Reset();
		return array2;
	}

	private Span<byte> GetTempBuffer(Span<byte> inputBuffer, Span<byte> tmpBuffer)
	{
		if (_whitespaces == FromBase64TransformMode.DoNotIgnoreWhiteSpaces)
		{
			return inputBuffer;
		}
		return DiscardWhiteSpaces(inputBuffer, tmpBuffer);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Span<byte> DiscardWhiteSpaces(Span<byte> inputBuffer, Span<byte> tmpBuffer)
	{
		int length = 0;
		for (int i = 0; i < inputBuffer.Length; i++)
		{
			if (!IsWhitespace(inputBuffer[i]))
			{
				tmpBuffer[length++] = inputBuffer[i];
			}
		}
		return tmpBuffer.Slice(0, length);
	}

	private static bool IsWhitespace(byte value)
	{
		if (value != 32)
		{
			return (uint)(value - 9) <= 4u;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int GetOutputSize(int bytesToTransform, Span<byte> tmpBuffer)
	{
		int num = Base64.GetMaxDecodedFromUtf8Length(bytesToTransform);
		int length = tmpBuffer.Length;
		if (tmpBuffer[length - 2] == 61)
		{
			num--;
		}
		if (tmpBuffer[length - 1] == 61)
		{
			num--;
		}
		return num;
	}

	private void ConvertFromBase64(Span<byte> tmpBuffer, Span<byte> outputBuffer, out int consumed, out int written)
	{
		int num = _inputIndex + tmpBuffer.Length;
		byte[] array = null;
		Span<byte> span = stackalloc byte[32];
		if (num > 32)
		{
			span = (array = System.Security.Cryptography.CryptoPool.Rent(num));
		}
		_inputBuffer.AsSpan(0, _inputIndex).CopyTo(span);
		tmpBuffer.CopyTo(span.Slice(_inputIndex));
		_inputIndex = num & 3;
		num -= _inputIndex;
		tmpBuffer.Slice(tmpBuffer.Length - _inputIndex).CopyTo(_inputBuffer);
		span = span.Slice(0, num);
		if (Base64.DecodeFromUtf8(span, outputBuffer, out consumed, out written) != 0)
		{
			ThrowHelper.ThrowBase64FormatException();
		}
		ReturnToCryptoPool(array, span.Length);
	}

	private void ReturnToCryptoPool(byte[] array, int clearSize)
	{
		if (array != null)
		{
			System.Security.Cryptography.CryptoPool.Return(array, clearSize);
		}
	}

	public void Clear()
	{
		Dispose();
	}

	private void Reset()
	{
		_inputIndex = 0;
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			if (_inputBuffer != null)
			{
				CryptographicOperations.ZeroMemory(_inputBuffer);
				_inputBuffer = null;
			}
			Reset();
		}
	}

	~FromBase64Transform()
	{
		Dispose(disposing: false);
	}
}

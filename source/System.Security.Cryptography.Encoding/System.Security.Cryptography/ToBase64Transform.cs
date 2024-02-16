using System.Buffers;
using System.Buffers.Text;

namespace System.Security.Cryptography;

public class ToBase64Transform : ICryptoTransform, IDisposable
{
	public int InputBlockSize => 3;

	public int OutputBlockSize => 4;

	public bool CanTransformMultipleBlocks => true;

	public virtual bool CanReuseTransform => true;

	public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
	{
		ThrowHelper.ValidateTransformBlock(inputBuffer, inputOffset, inputCount);
		int result;
		int num = Math.DivRem(inputCount, InputBlockSize, out result);
		if (num == 0)
		{
			ThrowHelper.ThrowArgumentOutOfRange(ThrowHelper.ExceptionArgument.inputCount);
		}
		if (outputBuffer == null)
		{
			ThrowHelper.ThrowArgumentNull(ThrowHelper.ExceptionArgument.outputBuffer);
		}
		if (result != 0)
		{
			ThrowHelper.ThrowArgumentOutOfRange(ThrowHelper.ExceptionArgument.inputCount);
		}
		int num2 = checked(num * OutputBlockSize);
		if (num2 > outputBuffer.Length - outputOffset)
		{
			ThrowHelper.ThrowArgumentOutOfRange(ThrowHelper.ExceptionArgument.outputBuffer);
		}
		Span<byte> span = inputBuffer.AsSpan(inputOffset, inputCount);
		Span<byte> utf = outputBuffer.AsSpan(outputOffset, num2);
		int bytesConsumed;
		int bytesWritten;
		OperationStatus operationStatus = Base64.EncodeToUtf8(span, utf, out bytesConsumed, out bytesWritten, isFinalBlock: false);
		return bytesWritten;
	}

	public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
	{
		ThrowHelper.ValidateTransformBlock(inputBuffer, inputOffset, inputCount);
		if (inputCount == 0)
		{
			return Array.Empty<byte>();
		}
		Span<byte> span = inputBuffer.AsSpan(inputOffset, inputCount);
		int result;
		int num = Math.DivRem(inputCount, InputBlockSize, out result);
		int num2 = num + ((result != 0) ? 1 : 0);
		byte[] array = new byte[num2 * OutputBlockSize];
		int bytesConsumed;
		int bytesWritten;
		OperationStatus operationStatus = Base64.EncodeToUtf8(span, array, out bytesConsumed, out bytesWritten);
		return array;
	}

	public void Dispose()
	{
		Clear();
	}

	public void Clear()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
	}

	~ToBase64Transform()
	{
		Dispose(disposing: false);
	}
}

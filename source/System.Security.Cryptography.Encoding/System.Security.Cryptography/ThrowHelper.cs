using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Security.Cryptography;

internal static class ThrowHelper
{
	public enum ExceptionArgument
	{
		inputBuffer,
		outputBuffer,
		inputOffset,
		inputCount
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ValidateTransformBlock(byte[] inputBuffer, int inputOffset, int inputCount)
	{
		if (inputBuffer == null)
		{
			ThrowArgumentNull(ExceptionArgument.inputBuffer);
		}
		if ((uint)inputCount > inputBuffer.Length)
		{
			ThrowArgumentOutOfRange(ExceptionArgument.inputCount);
		}
		if (inputOffset < 0)
		{
			ThrowArgumentOutOfRange(ExceptionArgument.inputOffset);
		}
		if (inputBuffer.Length - inputCount < inputOffset)
		{
			ThrowInvalidOffLen();
		}
	}

	[DoesNotReturn]
	public static void ThrowArgumentNull(ExceptionArgument argument)
	{
		throw new ArgumentNullException(argument.ToString());
	}

	[DoesNotReturn]
	public static void ThrowArgumentOutOfRange(ExceptionArgument argument)
	{
		throw new ArgumentOutOfRangeException(argument.ToString(), System.SR.ArgumentOutOfRange_NeedNonNegNum);
	}

	[DoesNotReturn]
	public static void ThrowInvalidOffLen()
	{
		throw new ArgumentException(System.SR.Argument_InvalidOffLen);
	}

	[DoesNotReturn]
	public static void ThrowObjectDisposed()
	{
		throw new ObjectDisposedException(null, System.SR.ObjectDisposed_Generic);
	}

	[DoesNotReturn]
	public static void ThrowBase64FormatException()
	{
		throw new FormatException();
	}
}

namespace System.Security.Cryptography;

public interface ICryptoTransform : IDisposable
{
	int InputBlockSize { get; }

	int OutputBlockSize { get; }

	bool CanTransformMultipleBlocks { get; }

	bool CanReuseTransform { get; }

	int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset);

	byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount);
}

using System;

namespace Internal.Cryptography;

internal abstract class BasicSymmetricCipher : IDisposable
{
	public int BlockSizeInBytes { get; private set; }

	public int PaddingSizeInBytes { get; private set; }

	protected byte[] IV { get; private set; }

	protected BasicSymmetricCipher(byte[] iv, int blockSizeInBytes, int paddingSizeInBytes)
	{
		IV = iv;
		BlockSizeInBytes = blockSizeInBytes;
		PaddingSizeInBytes = ((paddingSizeInBytes > 0) ? paddingSizeInBytes : blockSizeInBytes);
	}

	public abstract int Transform(ReadOnlySpan<byte> input, Span<byte> output);

	public abstract int TransformFinal(ReadOnlySpan<byte> input, Span<byte> output);

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing && IV != null)
		{
			Array.Clear(IV);
			IV = null;
		}
	}
}

namespace System.Security.Cryptography;

public sealed class KeySizes
{
	public int MinSize { get; private set; }

	public int MaxSize { get; private set; }

	public int SkipSize { get; private set; }

	public KeySizes(int minSize, int maxSize, int skipSize)
	{
		MinSize = minSize;
		MaxSize = maxSize;
		SkipSize = skipSize;
	}
}

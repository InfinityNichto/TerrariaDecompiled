namespace System.Reflection.Internal;

internal readonly struct StreamConstraints
{
	public readonly object? GuardOpt;

	public readonly long ImageStart;

	public readonly int ImageSize;

	public StreamConstraints(object? guardOpt, long startPosition, int imageSize)
	{
		GuardOpt = guardOpt;
		ImageStart = startPosition;
		ImageSize = imageSize;
	}
}

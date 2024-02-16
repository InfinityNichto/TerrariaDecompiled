namespace System.Net;

internal static class RangeValidationHelpers
{
	public static bool ValidateRange(int actual, int fromAllowed, int toAllowed)
	{
		if (actual >= fromAllowed)
		{
			return actual <= toAllowed;
		}
		return false;
	}

	public static void ValidateSegment(ArraySegment<byte> segment)
	{
		if (segment.Array == null)
		{
			throw new ArgumentNullException("segment");
		}
		if (segment.Offset < 0 || segment.Count < 0 || segment.Count > segment.Array.Length - segment.Offset)
		{
			throw new ArgumentOutOfRangeException("segment");
		}
	}
}

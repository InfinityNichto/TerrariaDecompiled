namespace System.Resources;

internal readonly struct ResourceLocator
{
	internal int DataPosition { get; }

	internal object Value { get; }

	internal ResourceLocator(int dataPos, object value)
	{
		DataPosition = dataPos;
		Value = value;
	}

	internal static bool CanCache(ResourceTypeCode value)
	{
		return value <= ResourceTypeCode.TimeSpan;
	}
}

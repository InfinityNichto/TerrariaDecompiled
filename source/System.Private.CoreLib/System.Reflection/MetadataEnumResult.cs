namespace System.Reflection;

internal struct MetadataEnumResult
{
	private int[] largeResult;

	private int length;

	private unsafe fixed int smallResult[16];

	public int Length => length;

	public unsafe int this[int index]
	{
		get
		{
			if (largeResult != null)
			{
				return largeResult[index];
			}
			fixed (int* ptr = smallResult)
			{
				return ptr[index];
			}
		}
	}
}

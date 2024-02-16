namespace System.Collections;

public static class StructuralComparisons
{
	private static volatile IComparer s_StructuralComparer;

	private static volatile IEqualityComparer s_StructuralEqualityComparer;

	public static IComparer StructuralComparer
	{
		get
		{
			IComparer comparer = s_StructuralComparer;
			if (comparer == null)
			{
				comparer = (s_StructuralComparer = new StructuralComparer());
			}
			return comparer;
		}
	}

	public static IEqualityComparer StructuralEqualityComparer
	{
		get
		{
			IEqualityComparer equalityComparer = s_StructuralEqualityComparer;
			if (equalityComparer == null)
			{
				equalityComparer = (s_StructuralEqualityComparer = new StructuralEqualityComparer());
			}
			return equalityComparer;
		}
	}
}

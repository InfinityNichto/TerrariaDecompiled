using System.Collections.Generic;

namespace System.Formats.Asn1;

internal sealed class SetOfValueComparer : IComparer<ReadOnlyMemory<byte>>
{
	internal static SetOfValueComparer Instance { get; } = new SetOfValueComparer();


	public int Compare(ReadOnlyMemory<byte> x, ReadOnlyMemory<byte> y)
	{
		return Compare(x.Span, y.Span);
	}

	internal static int Compare(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y)
	{
		int num = Math.Min(x.Length, y.Length);
		for (int i = 0; i < num; i++)
		{
			int num2 = x[i];
			byte b = y[i];
			int num3 = num2 - b;
			if (num3 != 0)
			{
				return num3;
			}
		}
		return x.Length - y.Length;
	}
}

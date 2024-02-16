using System.Diagnostics.CodeAnalysis;

namespace System.Data;

internal readonly struct IndexField
{
	public readonly DataColumn Column;

	public readonly bool IsDescending;

	internal IndexField(DataColumn column, bool isDescending)
	{
		Column = column;
		IsDescending = isDescending;
	}

	public static bool operator ==(IndexField if1, IndexField if2)
	{
		if (if1.Column == if2.Column)
		{
			return if1.IsDescending == if2.IsDescending;
		}
		return false;
	}

	public override bool Equals([NotNullWhen(true)] object obj)
	{
		if (!(obj is IndexField))
		{
			return false;
		}
		return this == (IndexField)obj;
	}

	public override int GetHashCode()
	{
		return Column.GetHashCode() ^ IsDescending.GetHashCode();
	}
}

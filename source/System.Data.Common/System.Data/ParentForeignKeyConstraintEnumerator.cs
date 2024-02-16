namespace System.Data;

internal sealed class ParentForeignKeyConstraintEnumerator : ForeignKeyConstraintEnumerator
{
	private readonly DataTable _table;

	public ParentForeignKeyConstraintEnumerator(DataSet dataSet, DataTable inTable)
		: base(dataSet)
	{
		_table = inTable;
	}

	protected override bool IsValidCandidate(Constraint constraint)
	{
		if (constraint is ForeignKeyConstraint)
		{
			return ((ForeignKeyConstraint)constraint).RelatedTable == _table;
		}
		return false;
	}
}

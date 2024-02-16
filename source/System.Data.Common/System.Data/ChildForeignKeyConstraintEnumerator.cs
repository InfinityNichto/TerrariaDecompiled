namespace System.Data;

internal sealed class ChildForeignKeyConstraintEnumerator : ForeignKeyConstraintEnumerator
{
	private readonly DataTable _table;

	public ChildForeignKeyConstraintEnumerator(DataSet dataSet, DataTable inTable)
		: base(dataSet)
	{
		_table = inTable;
	}

	protected override bool IsValidCandidate(Constraint constraint)
	{
		if (constraint is ForeignKeyConstraint)
		{
			return ((ForeignKeyConstraint)constraint).Table == _table;
		}
		return false;
	}
}

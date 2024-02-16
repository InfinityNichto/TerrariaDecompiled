namespace System.Data;

internal class ForeignKeyConstraintEnumerator : ConstraintEnumerator
{
	public ForeignKeyConstraintEnumerator(DataSet dataSet)
		: base(dataSet)
	{
	}

	protected override bool IsValidCandidate(Constraint constraint)
	{
		return constraint is ForeignKeyConstraint;
	}

	public ForeignKeyConstraint GetForeignKeyConstraint()
	{
		return (ForeignKeyConstraint)base.CurrentObject;
	}
}

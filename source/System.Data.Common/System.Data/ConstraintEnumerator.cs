using System.Collections;

namespace System.Data;

internal class ConstraintEnumerator
{
	private IEnumerator _tables;

	private IEnumerator _constraints;

	private Constraint _currentObject;

	protected Constraint CurrentObject => _currentObject;

	public ConstraintEnumerator(DataSet dataSet)
	{
		_tables = dataSet?.Tables.GetEnumerator();
		_currentObject = null;
	}

	public bool GetNext()
	{
		_currentObject = null;
		while (_tables != null)
		{
			if (_constraints == null)
			{
				if (!_tables.MoveNext())
				{
					_tables = null;
					return false;
				}
				_constraints = ((DataTable)_tables.Current).Constraints.GetEnumerator();
			}
			if (!_constraints.MoveNext())
			{
				_constraints = null;
				continue;
			}
			Constraint constraint = (Constraint)_constraints.Current;
			if (!IsValidCandidate(constraint))
			{
				continue;
			}
			_currentObject = constraint;
			return true;
		}
		return false;
	}

	public Constraint GetConstraint()
	{
		return _currentObject;
	}

	protected virtual bool IsValidCandidate(Constraint constraint)
	{
		return true;
	}
}

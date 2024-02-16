namespace System.Data;

public class FillErrorEventArgs : EventArgs
{
	private bool _continueFlag;

	private readonly DataTable _dataTable;

	private Exception _errors;

	private readonly object[] _values;

	public bool Continue
	{
		get
		{
			return _continueFlag;
		}
		set
		{
			_continueFlag = value;
		}
	}

	public DataTable? DataTable => _dataTable;

	public Exception? Errors
	{
		get
		{
			return _errors;
		}
		set
		{
			_errors = value;
		}
	}

	public object?[] Values
	{
		get
		{
			object[] array = new object[_values.Length];
			for (int i = 0; i < _values.Length; i++)
			{
				array[i] = _values[i];
			}
			return array;
		}
	}

	public FillErrorEventArgs(DataTable? dataTable, object?[]? values)
	{
		_dataTable = dataTable;
		_values = values ?? Array.Empty<object>();
	}
}

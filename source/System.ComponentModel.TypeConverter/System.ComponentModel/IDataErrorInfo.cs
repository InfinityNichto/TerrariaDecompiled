namespace System.ComponentModel;

public interface IDataErrorInfo
{
	string this[string columnName] { get; }

	string Error { get; }
}

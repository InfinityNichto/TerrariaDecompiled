using System.Collections;

namespace System.Data;

public interface ITableMappingCollection : IList, ICollection, IEnumerable
{
	object this[string index] { get; set; }

	ITableMapping Add(string sourceTableName, string dataSetTableName);

	bool Contains(string? sourceTableName);

	ITableMapping GetByDataSetTable(string dataSetTableName);

	int IndexOf(string? sourceTableName);

	void RemoveAt(string sourceTableName);
}

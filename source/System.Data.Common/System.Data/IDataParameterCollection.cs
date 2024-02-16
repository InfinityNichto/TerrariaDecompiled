using System.Collections;

namespace System.Data;

public interface IDataParameterCollection : IList, ICollection, IEnumerable
{
	object this[string parameterName] { get; set; }

	bool Contains(string parameterName);

	int IndexOf(string parameterName);

	void RemoveAt(string parameterName);
}

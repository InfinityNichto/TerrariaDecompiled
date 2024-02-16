using System.Collections;
using System.Collections.Specialized;

namespace System.Runtime.Serialization;

internal class CodeObject
{
	private IDictionary _userData;

	public IDictionary UserData => _userData ?? (_userData = new ListDictionary());
}

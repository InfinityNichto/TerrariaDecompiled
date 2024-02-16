using System.Collections.Generic;

namespace System.Threading.Channels;

internal interface IDebugEnumerable<T>
{
	IEnumerator<T> GetEnumerator();
}

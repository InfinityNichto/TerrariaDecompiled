using System.Runtime.InteropServices;

namespace System.Collections;

[Guid("496B0ABE-CDEE-11d3-88E8-00902754C43A")]
[ComVisible(true)]
public interface IEnumerable
{
	IEnumerator GetEnumerator();
}

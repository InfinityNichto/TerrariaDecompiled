using System.Runtime.CompilerServices;

namespace System.Net.Http;

internal interface IHttpTrace
{
	void Trace(string message, [CallerMemberName] string memberName = null);
}

using System.Security.Principal;

namespace System.Net;

public class HttpListenerBasicIdentity : GenericIdentity
{
	public virtual string Password { get; }

	public HttpListenerBasicIdentity(string username, string password)
		: base(username, "Basic")
	{
		Password = password;
	}
}

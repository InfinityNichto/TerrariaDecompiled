using System.Security.Principal;

namespace System.Net.Http;

internal static class CurrentUserIdentityProvider
{
	public static string GetIdentity()
	{
		using WindowsIdentity windowsIdentity = WindowsIdentity.GetCurrent();
		return windowsIdentity.Name;
	}
}

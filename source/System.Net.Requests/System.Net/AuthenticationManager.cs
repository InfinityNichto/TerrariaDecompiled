using System.Collections;
using System.Collections.Specialized;

namespace System.Net;

public class AuthenticationManager
{
	public static ICredentialPolicy? CredentialPolicy { get; set; }

	public static StringDictionary CustomTargetNameDictionary { get; } = new StringDictionary();


	public static IEnumerator RegisteredModules => Array.Empty<IAuthenticationModule>().GetEnumerator();

	[Obsolete("The AuthenticationManager Authenticate and PreAuthenticate methods are not supported and throw PlatformNotSupportedException.", DiagnosticId = "SYSLIB0009", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public static Authorization? Authenticate(string challenge, WebRequest request, ICredentials credentials)
	{
		throw new PlatformNotSupportedException();
	}

	[Obsolete("The AuthenticationManager Authenticate and PreAuthenticate methods are not supported and throw PlatformNotSupportedException.", DiagnosticId = "SYSLIB0009", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public static Authorization? PreAuthenticate(WebRequest request, ICredentials credentials)
	{
		throw new PlatformNotSupportedException();
	}

	public static void Register(IAuthenticationModule authenticationModule)
	{
		if (authenticationModule == null)
		{
			throw new ArgumentNullException("authenticationModule");
		}
	}

	public static void Unregister(IAuthenticationModule authenticationModule)
	{
		if (authenticationModule == null)
		{
			throw new ArgumentNullException("authenticationModule");
		}
	}

	public static void Unregister(string authenticationScheme)
	{
		if (authenticationScheme == null)
		{
			throw new ArgumentNullException("authenticationScheme");
		}
	}
}

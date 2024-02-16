using System.Security.Authentication.ExtendedProtection;

namespace System.Net.Mail;

internal interface ISmtpAuthenticationModule
{
	string AuthenticationType { get; }

	Authorization Authenticate(string challenge, NetworkCredential credentials, object sessionCookie, string spn, ChannelBinding channelBindingToken);

	void CloseContext(object sessionCookie);
}

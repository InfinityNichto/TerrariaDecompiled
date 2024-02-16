using System.Collections.Generic;
using System.Security.Authentication.ExtendedProtection;
using System.Text;

namespace System.Net.Mail;

internal sealed class SmtpLoginAuthenticationModule : ISmtpAuthenticationModule
{
	private readonly Dictionary<object, NetworkCredential> _sessions = new Dictionary<object, NetworkCredential>();

	public string AuthenticationType => "login";

	internal SmtpLoginAuthenticationModule()
	{
	}

	public Authorization Authenticate(string challenge, NetworkCredential credential, object sessionCookie, string spn, ChannelBinding channelBindingToken)
	{
		lock (_sessions)
		{
			if (!_sessions.TryGetValue(sessionCookie, out var value))
			{
				if (credential == null || credential == CredentialCache.DefaultNetworkCredentials)
				{
					return null;
				}
				_sessions[sessionCookie] = credential;
				string text = credential.UserName;
				string domain = credential.Domain;
				if (domain != null && domain.Length > 0)
				{
					text = domain + "\\" + text;
				}
				return new Authorization(Convert.ToBase64String(Encoding.UTF8.GetBytes(text)), finished: false);
			}
			_sessions.Remove(sessionCookie);
			return new Authorization(Convert.ToBase64String(Encoding.UTF8.GetBytes(value.Password)), finished: true);
		}
	}

	public void CloseContext(object sessionCookie)
	{
	}
}

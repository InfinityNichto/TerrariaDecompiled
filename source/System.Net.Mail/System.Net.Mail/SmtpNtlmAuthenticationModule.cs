using System.Collections.Generic;
using System.Security.Authentication.ExtendedProtection;

namespace System.Net.Mail;

internal sealed class SmtpNtlmAuthenticationModule : ISmtpAuthenticationModule
{
	private readonly Dictionary<object, System.Net.NTAuthentication> _sessions = new Dictionary<object, System.Net.NTAuthentication>();

	public string AuthenticationType => "ntlm";

	internal SmtpNtlmAuthenticationModule()
	{
	}

	public Authorization Authenticate(string challenge, NetworkCredential credential, object sessionCookie, string spn, ChannelBinding channelBindingToken)
	{
		try
		{
			lock (_sessions)
			{
				if (!_sessions.TryGetValue(sessionCookie, out var value))
				{
					if (credential == null)
					{
						return null;
					}
					value = (_sessions[sessionCookie] = new System.Net.NTAuthentication(isServer: false, "Ntlm", credential, spn, System.Net.ContextFlagsPal.Connection, channelBindingToken));
				}
				string outgoingBlob = value.GetOutgoingBlob(challenge);
				if (!value.IsCompleted)
				{
					return new Authorization(outgoingBlob, finished: false);
				}
				_sessions.Remove(sessionCookie);
				return new Authorization(outgoingBlob, finished: true);
			}
		}
		catch (NullReferenceException)
		{
			return null;
		}
	}

	public void CloseContext(object sessionCookie)
	{
	}
}

using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Authentication.ExtendedProtection;

namespace System.Net.Mail;

internal sealed class SmtpNegotiateAuthenticationModule : ISmtpAuthenticationModule
{
	private readonly Dictionary<object, System.Net.NTAuthentication> _sessions = new Dictionary<object, System.Net.NTAuthentication>();

	public string AuthenticationType => "gssapi";

	internal SmtpNegotiateAuthenticationModule()
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
					value = (_sessions[sessionCookie] = new System.Net.NTAuthentication(isServer: false, "Negotiate", credential, spn, System.Net.ContextFlagsPal.Connection | System.Net.ContextFlagsPal.AcceptStream, channelBindingToken));
				}
				string token = null;
				if (!value.IsCompleted)
				{
					byte[] incomingBlob = null;
					if (challenge != null)
					{
						incomingBlob = Convert.FromBase64String(challenge);
					}
					byte[] outgoingBlob = value.GetOutgoingBlob(incomingBlob, thrownOnError: false);
					if (value.IsCompleted && outgoingBlob == null)
					{
						token = "\r\n";
					}
					if (outgoingBlob != null)
					{
						token = Convert.ToBase64String(outgoingBlob);
					}
				}
				else
				{
					token = GetSecurityLayerOutgoingBlob(challenge, value);
				}
				return new Authorization(token, value.IsCompleted);
			}
		}
		catch (NullReferenceException)
		{
			return null;
		}
	}

	public void CloseContext(object sessionCookie)
	{
		System.Net.NTAuthentication value = null;
		lock (_sessions)
		{
			if (_sessions.TryGetValue(sessionCookie, out value))
			{
				_sessions.Remove(sessionCookie);
			}
		}
		value?.CloseContext();
	}

	private string GetSecurityLayerOutgoingBlob(string challenge, System.Net.NTAuthentication clientContext)
	{
		if (challenge == null)
		{
			return null;
		}
		byte[] array = Convert.FromBase64String(challenge);
		int num;
		try
		{
			num = clientContext.VerifySignature(array, 0, array.Length);
		}
		catch (Win32Exception)
		{
			return null;
		}
		if (num < 4 || array[0] != 1 || array[1] != 0 || array[2] != 0 || array[3] != 0)
		{
			return null;
		}
		byte[] output = null;
		try
		{
			num = clientContext.MakeSignature(array, 0, 4, ref output);
		}
		catch (Win32Exception)
		{
			return null;
		}
		return Convert.ToBase64String(output, 0, num);
	}
}

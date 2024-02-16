using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Net;

internal readonly struct CredentialHostKey : IEquatable<CredentialHostKey>
{
	public readonly string Host;

	public readonly string AuthenticationType;

	public readonly int Port;

	internal CredentialHostKey(string host, int port, string authenticationType)
	{
		Host = host;
		Port = port;
		AuthenticationType = authenticationType;
	}

	public override int GetHashCode()
	{
		return StringComparer.OrdinalIgnoreCase.GetHashCode(AuthenticationType) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(Host) ^ Port.GetHashCode();
	}

	public bool Equals(CredentialHostKey other)
	{
		bool flag = string.Equals(AuthenticationType, other.AuthenticationType, StringComparison.OrdinalIgnoreCase) && string.Equals(Host, other.Host, StringComparison.OrdinalIgnoreCase) && Port == other.Port;
		if (NetEventSource.Log.IsEnabled())
		{
			NetEventSource.Info(this, $"Equals({this},{other}) returns {flag}", "Equals");
		}
		return flag;
	}

	public override bool Equals([NotNullWhen(true)] object obj)
	{
		if (obj is CredentialHostKey)
		{
			return Equals((CredentialHostKey)obj);
		}
		return false;
	}

	public override string ToString()
	{
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(2, 3, invariantCulture);
		handler.AppendFormatted(Host);
		handler.AppendLiteral(":");
		handler.AppendFormatted(Port);
		handler.AppendLiteral(":");
		handler.AppendFormatted(AuthenticationType);
		return string.Create(invariantCulture, ref handler);
	}
}

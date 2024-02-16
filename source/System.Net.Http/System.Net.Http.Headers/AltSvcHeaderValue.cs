using System.Globalization;
using System.Text;

namespace System.Net.Http.Headers;

internal sealed class AltSvcHeaderValue
{
	public static AltSvcHeaderValue Clear { get; } = new AltSvcHeaderValue("clear", null, 0, TimeSpan.Zero, persist: false);


	public string AlpnProtocolName { get; }

	public string Host { get; }

	public int Port { get; }

	public TimeSpan MaxAge { get; }

	public bool Persist { get; }

	public AltSvcHeaderValue(string alpnProtocolName, string host, int port, TimeSpan maxAge, bool persist)
	{
		AlpnProtocolName = alpnProtocolName;
		Host = host;
		Port = port;
		MaxAge = maxAge;
		Persist = persist;
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = System.Text.StringBuilderCache.Acquire(AlpnProtocolName.Length + (Host?.Length ?? 0) + 64);
		stringBuilder.Append(AlpnProtocolName);
		stringBuilder.Append("=\"");
		if (Host != null)
		{
			stringBuilder.Append(Host);
		}
		stringBuilder.Append(':');
		stringBuilder.Append((uint)Port);
		stringBuilder.Append('"');
		if (MaxAge != TimeSpan.FromTicks(864000000000L))
		{
			StringBuilder stringBuilder2 = stringBuilder;
			IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(5, 1, stringBuilder2, invariantCulture);
			handler.AppendLiteral("; ma=");
			handler.AppendFormatted(MaxAge.Ticks / 10000000);
			stringBuilder2.Append(invariantCulture, ref handler);
		}
		if (Persist)
		{
			stringBuilder.Append("; persist=1");
		}
		return System.Text.StringBuilderCache.GetStringAndRelease(stringBuilder);
	}
}

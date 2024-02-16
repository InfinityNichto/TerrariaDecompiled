using System;
using System.Security.Cryptography;

namespace Internal.Cryptography;

internal abstract class AsnFormatter
{
	private static readonly AsnFormatter s_instance = new CngAsnFormatter();

	internal static AsnFormatter Instance => s_instance;

	public string Format(Oid oid, byte[] rawData, bool multiLine)
	{
		return FormatNative(oid, rawData, multiLine) ?? Convert.ToHexString(rawData);
	}

	protected abstract string FormatNative(Oid oid, byte[] rawData, bool multiLine);
}

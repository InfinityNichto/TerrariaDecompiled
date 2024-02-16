using System.Text;

namespace System.Reflection;

internal static class AssemblyNameFormatter
{
	public static string ComputeDisplayName(string name, Version version, string cultureName, byte[] pkt, AssemblyNameFlags flags = AssemblyNameFlags.None, AssemblyContentType contentType = AssemblyContentType.Default)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendQuoted(name);
		if (version != null)
		{
			Version version2 = version.CanonicalizeVersion();
			if (version2.Major != 65535)
			{
				stringBuilder.Append(", Version=");
				stringBuilder.Append(version2.Major);
				if (version2.Minor != 65535)
				{
					stringBuilder.Append('.');
					stringBuilder.Append(version2.Minor);
					if (version2.Build != 65535)
					{
						stringBuilder.Append('.');
						stringBuilder.Append(version2.Build);
						if (version2.Revision != 65535)
						{
							stringBuilder.Append('.');
							stringBuilder.Append(version2.Revision);
						}
					}
				}
			}
		}
		if (cultureName != null)
		{
			if (cultureName.Length == 0)
			{
				cultureName = "neutral";
			}
			stringBuilder.Append(", Culture=");
			stringBuilder.AppendQuoted(cultureName);
		}
		if (pkt != null)
		{
			if (pkt.Length > 8)
			{
				throw new ArgumentException();
			}
			stringBuilder.Append(", PublicKeyToken=");
			if (pkt.Length == 0)
			{
				stringBuilder.Append("null");
			}
			else
			{
				stringBuilder.Append(HexConverter.ToString(pkt, HexConverter.Casing.Lower));
			}
		}
		if ((flags & AssemblyNameFlags.Retargetable) != 0)
		{
			stringBuilder.Append(", Retargetable=Yes");
		}
		if (contentType == AssemblyContentType.WindowsRuntime)
		{
			stringBuilder.Append(", ContentType=WindowsRuntime");
		}
		return stringBuilder.ToString();
	}

	private static void AppendQuoted(this StringBuilder sb, string s)
	{
		bool flag = false;
		if (s != s.Trim() || s.Contains('"') || s.Contains('\''))
		{
			flag = true;
		}
		if (flag)
		{
			sb.Append('"');
		}
		for (int i = 0; i < s.Length; i++)
		{
			switch (s[i])
			{
			case '"':
			case '\'':
			case ',':
			case '=':
			case '\\':
				sb.Append('\\');
				break;
			case '\t':
				sb.Append("\\t");
				continue;
			case '\r':
				sb.Append("\\r");
				continue;
			case '\n':
				sb.Append("\\n");
				continue;
			}
			sb.Append(s[i]);
		}
		if (flag)
		{
			sb.Append('"');
		}
	}

	private static Version CanonicalizeVersion(this Version version)
	{
		ushort num = (ushort)version.Major;
		ushort num2 = (ushort)version.Minor;
		ushort num3 = (ushort)version.Build;
		ushort num4 = (ushort)version.Revision;
		if (num == version.Major && num2 == version.Minor && num3 == version.Build && num4 == version.Revision)
		{
			return version;
		}
		return new Version(num, num2, num3, num4);
	}
}

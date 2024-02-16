using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace System;

public sealed class ApplicationId
{
	private readonly byte[] _publicKeyToken;

	public string? Culture { get; }

	public string Name { get; }

	public string? ProcessorArchitecture { get; }

	public Version Version { get; }

	public byte[] PublicKeyToken => (byte[])_publicKeyToken.Clone();

	public ApplicationId(byte[] publicKeyToken, string name, Version version, string? processorArchitecture, string? culture)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (name.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyApplicationName);
		}
		if (version == null)
		{
			throw new ArgumentNullException("version");
		}
		if (publicKeyToken == null)
		{
			throw new ArgumentNullException("publicKeyToken");
		}
		_publicKeyToken = (byte[])publicKeyToken.Clone();
		Name = name;
		Version = version;
		ProcessorArchitecture = processorArchitecture;
		Culture = culture;
	}

	public ApplicationId Copy()
	{
		return new ApplicationId(_publicKeyToken, Name, Version, ProcessorArchitecture, Culture);
	}

	public override string ToString()
	{
		Span<char> initialBuffer = stackalloc char[128];
		ValueStringBuilder valueStringBuilder = new ValueStringBuilder(initialBuffer);
		valueStringBuilder.Append(Name);
		if (Culture != null)
		{
			valueStringBuilder.Append(", culture=\"");
			valueStringBuilder.Append(Culture);
			valueStringBuilder.Append('"');
		}
		valueStringBuilder.Append(", version=\"");
		valueStringBuilder.Append(Version.ToString());
		valueStringBuilder.Append('"');
		if (_publicKeyToken != null)
		{
			valueStringBuilder.Append(", publicKeyToken=\"");
			HexConverter.EncodeToUtf16(_publicKeyToken, valueStringBuilder.AppendSpan(2 * _publicKeyToken.Length));
			valueStringBuilder.Append('"');
		}
		if (ProcessorArchitecture != null)
		{
			valueStringBuilder.Append(", processorArchitecture =\"");
			valueStringBuilder.Append(ProcessorArchitecture);
			valueStringBuilder.Append('"');
		}
		return valueStringBuilder.ToString();
	}

	public override bool Equals([NotNullWhen(true)] object? o)
	{
		if (!(o is ApplicationId applicationId))
		{
			return false;
		}
		if (!object.Equals(Name, applicationId.Name) || !object.Equals(Version, applicationId.Version) || !object.Equals(ProcessorArchitecture, applicationId.ProcessorArchitecture) || !object.Equals(Culture, applicationId.Culture))
		{
			return false;
		}
		if (_publicKeyToken.Length != applicationId._publicKeyToken.Length)
		{
			return false;
		}
		for (int i = 0; i < _publicKeyToken.Length; i++)
		{
			if (_publicKeyToken[i] != applicationId._publicKeyToken[i])
			{
				return false;
			}
		}
		return true;
	}

	public override int GetHashCode()
	{
		return Name.GetHashCode() ^ Version.GetHashCode();
	}
}

using System.Diagnostics.CodeAnalysis;

namespace System.Text;

public sealed class EncodingInfo
{
	public int CodePage { get; }

	public string Name { get; }

	public string DisplayName { get; }

	internal EncodingProvider? Provider { get; }

	public EncodingInfo(EncodingProvider provider, int codePage, string name, string displayName)
		: this(codePage, name, displayName)
	{
		if (name == null || displayName == null || provider == null)
		{
			throw new ArgumentNullException((name == null) ? "name" : ((displayName == null) ? "displayName" : "provider"));
		}
		Provider = provider;
	}

	internal EncodingInfo(int codePage, string name, string displayName)
	{
		CodePage = codePage;
		Name = name;
		DisplayName = displayName;
	}

	public Encoding GetEncoding()
	{
		return Provider?.GetEncoding(CodePage) ?? Encoding.GetEncoding(CodePage);
	}

	public override bool Equals([NotNullWhen(true)] object? value)
	{
		if (value is EncodingInfo encodingInfo)
		{
			return CodePage == encodingInfo.CodePage;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return CodePage;
	}
}

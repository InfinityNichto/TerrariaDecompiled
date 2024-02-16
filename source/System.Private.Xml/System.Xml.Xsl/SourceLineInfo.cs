using System.Diagnostics;

namespace System.Xml.Xsl;

[DebuggerDisplay("{Uri} [{StartLine},{StartPos} -- {EndLine},{EndPos}]")]
internal sealed class SourceLineInfo : ISourceLineInfo
{
	private string uriString;

	private Location start;

	private Location end;

	private const int NoSourceMagicNumber = 16707566;

	public static SourceLineInfo NoSource = new SourceLineInfo(string.Empty, 16707566, 0, 16707566, 0);

	public string Uri => uriString;

	public int StartLine => start.Line;

	public Location End => end;

	public Location Start => start;

	public bool IsNoSource => StartLine == 16707566;

	public SourceLineInfo(string uriString, int startLine, int startPos, int endLine, int endPos)
		: this(uriString, new Location(startLine, startPos), new Location(endLine, endPos))
	{
	}

	public SourceLineInfo(string uriString, Location start, Location end)
	{
		this.uriString = uriString;
		this.start = start;
		this.end = end;
	}

	[Conditional("DEBUG")]
	public static void Validate(ISourceLineInfo lineInfo)
	{
		if (lineInfo.Start.Line != 0)
		{
			_ = lineInfo.Start.Line;
			_ = 16707566;
		}
	}

	public static string GetFileName(string uriString)
	{
		if (uriString.Length != 0 && System.Uri.TryCreate(uriString, UriKind.Absolute, out Uri result) && result.IsFile)
		{
			return result.LocalPath;
		}
		return uriString;
	}
}

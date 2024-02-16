namespace System.Xml.Xsl;

internal interface ISourceLineInfo
{
	string Uri { get; }

	bool IsNoSource { get; }

	Location Start { get; }

	Location End { get; }
}

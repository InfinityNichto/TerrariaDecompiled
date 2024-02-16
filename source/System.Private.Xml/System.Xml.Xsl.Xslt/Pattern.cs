namespace System.Xml.Xsl.Xslt;

internal readonly struct Pattern
{
	public readonly TemplateMatch Match;

	public readonly int Priority;

	public Pattern(TemplateMatch match, int priority)
	{
		Match = match;
		Priority = priority;
	}
}

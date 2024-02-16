namespace System.Xml.Linq;

internal sealed class LineInfoEndElementAnnotation : LineInfoAnnotation
{
	public LineInfoEndElementAnnotation(int lineNumber, int linePosition)
		: base(lineNumber, linePosition)
	{
	}
}

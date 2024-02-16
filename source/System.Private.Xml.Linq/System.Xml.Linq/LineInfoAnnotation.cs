namespace System.Xml.Linq;

internal class LineInfoAnnotation
{
	internal int lineNumber;

	internal int linePosition;

	public LineInfoAnnotation(int lineNumber, int linePosition)
	{
		this.lineNumber = lineNumber;
		this.linePosition = linePosition;
	}
}

namespace System.Xml.Xsl.Xslt;

internal sealed class CompilerError
{
	public int Line { get; set; }

	public int Column { get; set; }

	public string ErrorNumber { get; set; }

	public string ErrorText { get; set; }

	public bool IsWarning { get; set; }

	public string FileName { get; set; }

	public CompilerError(string fileName, int line, int column, string errorNumber, string errorText)
	{
		Line = line;
		Column = column;
		ErrorNumber = errorNumber;
		ErrorText = errorText;
		FileName = fileName;
	}
}

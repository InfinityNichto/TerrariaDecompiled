namespace System.Xml.Xsl.Xslt;

internal sealed class Output
{
	public XmlWriterSettings Settings;

	public string Version;

	public string Encoding;

	public XmlQualifiedName Method;

	public int MethodPrec = int.MinValue;

	public int VersionPrec = int.MinValue;

	public int EncodingPrec = int.MinValue;

	public int OmitXmlDeclarationPrec = int.MinValue;

	public int StandalonePrec = int.MinValue;

	public int DocTypePublicPrec = int.MinValue;

	public int DocTypeSystemPrec = int.MinValue;

	public int IndentPrec = int.MinValue;

	public int MediaTypePrec = int.MinValue;

	public Output()
	{
		Settings = new XmlWriterSettings();
		Settings.OutputMethod = XmlOutputMethod.AutoDetect;
		Settings.AutoXmlDeclaration = true;
		Settings.ConformanceLevel = ConformanceLevel.Auto;
		Settings.MergeCDataSections = true;
	}
}

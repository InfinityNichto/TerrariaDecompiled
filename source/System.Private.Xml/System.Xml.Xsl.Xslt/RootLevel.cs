namespace System.Xml.Xsl.Xslt;

internal sealed class RootLevel : StylesheetLevel
{
	public RootLevel(Stylesheet principal)
	{
		Imports = new Stylesheet[1] { principal };
	}
}

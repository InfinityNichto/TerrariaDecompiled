using System.Collections.Generic;
using System.Xml.Xsl.Qil;

namespace System.Xml.Xsl.Xslt;

internal class StylesheetLevel
{
	public Stylesheet[] Imports;

	public Dictionary<QilName, XslFlags> ModeFlags = new Dictionary<QilName, XslFlags>();

	public Dictionary<QilName, List<QilFunction>> ApplyFunctions = new Dictionary<QilName, List<QilFunction>>();
}

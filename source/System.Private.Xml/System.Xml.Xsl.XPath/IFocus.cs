using System.Xml.Xsl.Qil;

namespace System.Xml.Xsl.XPath;

internal interface IFocus
{
	QilNode GetCurrent();

	QilNode GetPosition();

	QilNode GetLast();
}

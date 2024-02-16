using System.Xml.Xsl.Qil;

namespace System.Xml.Xsl.Xslt;

internal sealed class ReferenceReplacer : QilReplaceVisitor
{
	private QilReference _lookFor;

	private QilReference _replaceBy;

	public ReferenceReplacer(QilFactory f)
		: base(f)
	{
	}

	public QilNode Replace(QilNode expr, QilReference lookFor, QilReference replaceBy)
	{
		QilDepthChecker.Check(expr);
		_lookFor = lookFor;
		_replaceBy = replaceBy;
		return VisitAssumeReference(expr);
	}

	protected override QilNode VisitReference(QilNode n)
	{
		if (n != _lookFor)
		{
			return n;
		}
		return _replaceBy;
	}
}

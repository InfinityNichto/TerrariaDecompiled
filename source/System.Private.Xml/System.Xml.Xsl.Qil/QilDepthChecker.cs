using System.Collections.Generic;

namespace System.Xml.Xsl.Qil;

internal sealed class QilDepthChecker
{
	private readonly Dictionary<QilNode, bool> _visitedRef = new Dictionary<QilNode, bool>();

	public static void Check(QilNode input)
	{
		if (System.LocalAppContextSwitches.LimitXPathComplexity)
		{
			new QilDepthChecker().Check(input, 0);
		}
	}

	private void Check(QilNode input, int depth)
	{
		if (depth > 800)
		{
			throw XsltException.Create(System.SR.Xslt_InputTooComplex);
		}
		if (input is QilReference)
		{
			if (_visitedRef.ContainsKey(input))
			{
				return;
			}
			_visitedRef[input] = true;
		}
		int depth2 = depth + 1;
		for (int i = 0; i < input.Count; i++)
		{
			QilNode qilNode = input[i];
			if (qilNode != null)
			{
				Check(qilNode, depth2);
			}
		}
	}
}

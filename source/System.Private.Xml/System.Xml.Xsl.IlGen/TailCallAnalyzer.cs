using System.Xml.Xsl.Qil;

namespace System.Xml.Xsl.IlGen;

internal static class TailCallAnalyzer
{
	public static void Analyze(QilExpression qil)
	{
		foreach (QilFunction function in qil.FunctionList)
		{
			if (XmlILConstructInfo.Read(function).ConstructMethod == XmlILConstructMethod.Writer)
			{
				AnalyzeDefinition(function.Definition);
			}
		}
	}

	private static void AnalyzeDefinition(QilNode nd)
	{
		switch (nd.NodeType)
		{
		case QilNodeType.Invoke:
			if (XmlILConstructInfo.Read(nd).ConstructMethod == XmlILConstructMethod.Writer)
			{
				OptimizerPatterns.Write(nd).AddPattern(OptimizerPatternName.TailCall);
			}
			break;
		case QilNodeType.Loop:
		{
			QilLoop qilLoop = (QilLoop)nd;
			if (qilLoop.Variable.NodeType == QilNodeType.Let || !qilLoop.Variable.Binding.XmlType.MaybeMany)
			{
				AnalyzeDefinition(qilLoop.Body);
			}
			break;
		}
		case QilNodeType.Sequence:
		{
			QilList qilList = (QilList)nd;
			if (qilList.Count > 0)
			{
				AnalyzeDefinition(qilList[qilList.Count - 1]);
			}
			break;
		}
		case QilNodeType.Choice:
		{
			QilChoice qilChoice = (QilChoice)nd;
			for (int i = 0; i < qilChoice.Branches.Count; i++)
			{
				AnalyzeDefinition(qilChoice.Branches[i]);
			}
			break;
		}
		case QilNodeType.Conditional:
		{
			QilTernary qilTernary = (QilTernary)nd;
			AnalyzeDefinition(qilTernary.Center);
			AnalyzeDefinition(qilTernary.Right);
			break;
		}
		case QilNodeType.Nop:
			AnalyzeDefinition(((QilUnary)nd).Child);
			break;
		}
	}
}

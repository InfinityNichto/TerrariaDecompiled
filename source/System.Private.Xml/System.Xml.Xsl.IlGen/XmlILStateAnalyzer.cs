using System.Xml.Xsl.Qil;

namespace System.Xml.Xsl.IlGen;

internal class XmlILStateAnalyzer
{
	protected XmlILConstructInfo parentInfo;

	protected QilFactory fac;

	protected PossibleXmlStates xstates;

	protected bool withinElem;

	public XmlILStateAnalyzer(QilFactory fac)
	{
		this.fac = fac;
	}

	public virtual QilNode Analyze(QilNode ndConstr, QilNode ndContent)
	{
		if (ndConstr == null)
		{
			parentInfo = null;
			xstates = PossibleXmlStates.WithinSequence;
			withinElem = false;
			ndContent = AnalyzeContent(ndContent);
		}
		else
		{
			parentInfo = XmlILConstructInfo.Write(ndConstr);
			if (ndConstr.NodeType == QilNodeType.Function)
			{
				parentInfo.ConstructMethod = XmlILConstructMethod.Writer;
				PossibleXmlStates possibleXmlStates = PossibleXmlStates.None;
				foreach (XmlILConstructInfo item in parentInfo.CallersInfo)
				{
					if (possibleXmlStates == PossibleXmlStates.None)
					{
						possibleXmlStates = item.InitialStates;
					}
					else if (possibleXmlStates != item.InitialStates)
					{
						possibleXmlStates = PossibleXmlStates.Any;
					}
					item.PushToWriterFirst = true;
				}
				parentInfo.InitialStates = possibleXmlStates;
			}
			else
			{
				if (ndConstr.NodeType != QilNodeType.Choice)
				{
					XmlILConstructInfo xmlILConstructInfo2 = parentInfo;
					PossibleXmlStates initialStates = (parentInfo.FinalStates = PossibleXmlStates.WithinSequence);
					xmlILConstructInfo2.InitialStates = initialStates;
				}
				if (ndConstr.NodeType != QilNodeType.RtfCtor)
				{
					parentInfo.ConstructMethod = XmlILConstructMethod.WriterThenIterator;
				}
			}
			withinElem = ndConstr.NodeType == QilNodeType.ElementCtor;
			switch (ndConstr.NodeType)
			{
			case QilNodeType.DocumentCtor:
				xstates = PossibleXmlStates.WithinContent;
				break;
			case QilNodeType.ElementCtor:
				xstates = PossibleXmlStates.EnumAttrs;
				break;
			case QilNodeType.AttributeCtor:
				xstates = PossibleXmlStates.WithinAttr;
				break;
			case QilNodeType.CommentCtor:
				xstates = PossibleXmlStates.WithinComment;
				break;
			case QilNodeType.PICtor:
				xstates = PossibleXmlStates.WithinPI;
				break;
			case QilNodeType.XsltCopy:
				xstates = PossibleXmlStates.Any;
				break;
			case QilNodeType.Function:
				xstates = parentInfo.InitialStates;
				break;
			case QilNodeType.RtfCtor:
				xstates = PossibleXmlStates.WithinContent;
				break;
			case QilNodeType.Choice:
				xstates = PossibleXmlStates.Any;
				break;
			}
			if (ndContent != null)
			{
				ndContent = AnalyzeContent(ndContent);
			}
			if (ndConstr.NodeType == QilNodeType.Choice)
			{
				AnalyzeChoice(ndConstr as QilChoice, parentInfo);
			}
			if (ndConstr.NodeType == QilNodeType.Function)
			{
				parentInfo.FinalStates = xstates;
			}
		}
		return ndContent;
	}

	protected virtual QilNode AnalyzeContent(QilNode nd)
	{
		QilNodeType nodeType = nd.NodeType;
		if ((uint)(nodeType - 14) <= 2u)
		{
			nd = fac.Nop(nd);
		}
		XmlILConstructInfo xmlILConstructInfo = XmlILConstructInfo.Write(nd);
		xmlILConstructInfo.ParentInfo = parentInfo;
		xmlILConstructInfo.PushToWriterLast = true;
		xmlILConstructInfo.InitialStates = xstates;
		switch (nd.NodeType)
		{
		case QilNodeType.Loop:
			AnalyzeLoop(nd as QilLoop, xmlILConstructInfo);
			break;
		case QilNodeType.Sequence:
			AnalyzeSequence(nd as QilList, xmlILConstructInfo);
			break;
		case QilNodeType.Conditional:
			AnalyzeConditional(nd as QilTernary, xmlILConstructInfo);
			break;
		case QilNodeType.Choice:
			AnalyzeChoice(nd as QilChoice, xmlILConstructInfo);
			break;
		case QilNodeType.Error:
		case QilNodeType.Warning:
			xmlILConstructInfo.ConstructMethod = XmlILConstructMethod.Writer;
			break;
		case QilNodeType.Nop:
		{
			QilNode child = (nd as QilUnary).Child;
			QilNodeType nodeType2 = child.NodeType;
			if ((uint)(nodeType2 - 14) <= 2u)
			{
				AnalyzeCopy(nd, xmlILConstructInfo);
				break;
			}
			xmlILConstructInfo.ConstructMethod = XmlILConstructMethod.Writer;
			AnalyzeContent(child);
			break;
		}
		default:
			AnalyzeCopy(nd, xmlILConstructInfo);
			break;
		}
		xmlILConstructInfo.FinalStates = xstates;
		return nd;
	}

	protected virtual void AnalyzeLoop(QilLoop ndLoop, XmlILConstructInfo info)
	{
		XmlQueryType xmlType = ndLoop.XmlType;
		info.ConstructMethod = XmlILConstructMethod.Writer;
		if (!xmlType.IsSingleton)
		{
			StartLoop(xmlType, info);
		}
		ndLoop.Body = AnalyzeContent(ndLoop.Body);
		if (!xmlType.IsSingleton)
		{
			EndLoop(xmlType, info);
		}
	}

	protected virtual void AnalyzeSequence(QilList ndSeq, XmlILConstructInfo info)
	{
		info.ConstructMethod = XmlILConstructMethod.Writer;
		for (int i = 0; i < ndSeq.Count; i++)
		{
			ndSeq[i] = AnalyzeContent(ndSeq[i]);
		}
	}

	protected virtual void AnalyzeConditional(QilTernary ndCond, XmlILConstructInfo info)
	{
		info.ConstructMethod = XmlILConstructMethod.Writer;
		ndCond.Center = AnalyzeContent(ndCond.Center);
		PossibleXmlStates possibleXmlStates = xstates;
		xstates = info.InitialStates;
		ndCond.Right = AnalyzeContent(ndCond.Right);
		if (possibleXmlStates != xstates)
		{
			xstates = PossibleXmlStates.Any;
		}
	}

	protected virtual void AnalyzeChoice(QilChoice ndChoice, XmlILConstructInfo info)
	{
		int num = ndChoice.Branches.Count - 1;
		ndChoice.Branches[num] = AnalyzeContent(ndChoice.Branches[num]);
		PossibleXmlStates possibleXmlStates = xstates;
		while (--num >= 0)
		{
			xstates = info.InitialStates;
			ndChoice.Branches[num] = AnalyzeContent(ndChoice.Branches[num]);
			if (possibleXmlStates != xstates)
			{
				possibleXmlStates = PossibleXmlStates.Any;
			}
		}
		xstates = possibleXmlStates;
	}

	protected virtual void AnalyzeCopy(QilNode ndCopy, XmlILConstructInfo info)
	{
		XmlQueryType xmlType = ndCopy.XmlType;
		if (!xmlType.IsSingleton)
		{
			StartLoop(xmlType, info);
		}
		if (MaybeContent(xmlType))
		{
			if (MaybeAttrNmsp(xmlType))
			{
				if (xstates == PossibleXmlStates.EnumAttrs)
				{
					xstates = PossibleXmlStates.Any;
				}
			}
			else if (xstates == PossibleXmlStates.EnumAttrs || withinElem)
			{
				xstates = PossibleXmlStates.WithinContent;
			}
		}
		if (!xmlType.IsSingleton)
		{
			EndLoop(xmlType, info);
		}
	}

	private void StartLoop(XmlQueryType typ, XmlILConstructInfo info)
	{
		info.BeginLoopStates = xstates;
		if (typ.MaybeMany && xstates == PossibleXmlStates.EnumAttrs && MaybeContent(typ))
		{
			info.BeginLoopStates = (xstates = PossibleXmlStates.Any);
		}
	}

	private void EndLoop(XmlQueryType typ, XmlILConstructInfo info)
	{
		info.EndLoopStates = xstates;
		if (typ.MaybeEmpty && info.InitialStates != xstates)
		{
			xstates = PossibleXmlStates.Any;
		}
	}

	private bool MaybeAttrNmsp(XmlQueryType typ)
	{
		return (typ.NodeKinds & (XmlNodeKindFlags.Attribute | XmlNodeKindFlags.Namespace)) != 0;
	}

	private bool MaybeContent(XmlQueryType typ)
	{
		if (typ.IsNode)
		{
			return (typ.NodeKinds & ~(XmlNodeKindFlags.Attribute | XmlNodeKindFlags.Namespace)) != 0;
		}
		return true;
	}
}

namespace System.Xml.Xsl.Qil;

internal abstract class QilVisitor
{
	protected virtual QilNode VisitAssumeReference(QilNode expr)
	{
		if (expr is QilReference)
		{
			return VisitReference(expr);
		}
		return Visit(expr);
	}

	protected virtual QilNode VisitChildren(QilNode parent)
	{
		for (int i = 0; i < parent.Count; i++)
		{
			if (IsReference(parent, i))
			{
				VisitReference(parent[i]);
			}
			else
			{
				Visit(parent[i]);
			}
		}
		return parent;
	}

	protected virtual bool IsReference(QilNode parent, int childNum)
	{
		QilNode qilNode = parent[childNum];
		if (qilNode != null)
		{
			switch (qilNode.NodeType)
			{
			case QilNodeType.For:
			case QilNodeType.Let:
			case QilNodeType.Parameter:
				switch (parent.NodeType)
				{
				case QilNodeType.Loop:
				case QilNodeType.Filter:
				case QilNodeType.Sort:
					return childNum == 1;
				case QilNodeType.GlobalVariableList:
				case QilNodeType.GlobalParameterList:
				case QilNodeType.FormalParameterList:
					return false;
				default:
					return true;
				}
			case QilNodeType.Function:
				return parent.NodeType == QilNodeType.Invoke;
			}
		}
		return false;
	}

	protected virtual QilNode Visit(QilNode n)
	{
		if (n == null)
		{
			return VisitNull();
		}
		return n.NodeType switch
		{
			QilNodeType.QilExpression => VisitQilExpression((QilExpression)n), 
			QilNodeType.FunctionList => VisitFunctionList((QilList)n), 
			QilNodeType.GlobalVariableList => VisitGlobalVariableList((QilList)n), 
			QilNodeType.GlobalParameterList => VisitGlobalParameterList((QilList)n), 
			QilNodeType.ActualParameterList => VisitActualParameterList((QilList)n), 
			QilNodeType.FormalParameterList => VisitFormalParameterList((QilList)n), 
			QilNodeType.SortKeyList => VisitSortKeyList((QilList)n), 
			QilNodeType.BranchList => VisitBranchList((QilList)n), 
			QilNodeType.OptimizeBarrier => VisitOptimizeBarrier((QilUnary)n), 
			QilNodeType.Unknown => VisitUnknown(n), 
			QilNodeType.DataSource => VisitDataSource((QilDataSource)n), 
			QilNodeType.Nop => VisitNop((QilUnary)n), 
			QilNodeType.Error => VisitError((QilUnary)n), 
			QilNodeType.Warning => VisitWarning((QilUnary)n), 
			QilNodeType.For => VisitFor((QilIterator)n), 
			QilNodeType.Let => VisitLet((QilIterator)n), 
			QilNodeType.Parameter => VisitParameter((QilParameter)n), 
			QilNodeType.PositionOf => VisitPositionOf((QilUnary)n), 
			QilNodeType.True => VisitTrue(n), 
			QilNodeType.False => VisitFalse(n), 
			QilNodeType.LiteralString => VisitLiteralString((QilLiteral)n), 
			QilNodeType.LiteralInt32 => VisitLiteralInt32((QilLiteral)n), 
			QilNodeType.LiteralInt64 => VisitLiteralInt64((QilLiteral)n), 
			QilNodeType.LiteralDouble => VisitLiteralDouble((QilLiteral)n), 
			QilNodeType.LiteralDecimal => VisitLiteralDecimal((QilLiteral)n), 
			QilNodeType.LiteralQName => VisitLiteralQName((QilName)n), 
			QilNodeType.LiteralType => VisitLiteralType((QilLiteral)n), 
			QilNodeType.LiteralObject => VisitLiteralObject((QilLiteral)n), 
			QilNodeType.And => VisitAnd((QilBinary)n), 
			QilNodeType.Or => VisitOr((QilBinary)n), 
			QilNodeType.Not => VisitNot((QilUnary)n), 
			QilNodeType.Conditional => VisitConditional((QilTernary)n), 
			QilNodeType.Choice => VisitChoice((QilChoice)n), 
			QilNodeType.Length => VisitLength((QilUnary)n), 
			QilNodeType.Sequence => VisitSequence((QilList)n), 
			QilNodeType.Union => VisitUnion((QilBinary)n), 
			QilNodeType.Intersection => VisitIntersection((QilBinary)n), 
			QilNodeType.Difference => VisitDifference((QilBinary)n), 
			QilNodeType.Average => VisitAverage((QilUnary)n), 
			QilNodeType.Sum => VisitSum((QilUnary)n), 
			QilNodeType.Minimum => VisitMinimum((QilUnary)n), 
			QilNodeType.Maximum => VisitMaximum((QilUnary)n), 
			QilNodeType.Negate => VisitNegate((QilUnary)n), 
			QilNodeType.Add => VisitAdd((QilBinary)n), 
			QilNodeType.Subtract => VisitSubtract((QilBinary)n), 
			QilNodeType.Multiply => VisitMultiply((QilBinary)n), 
			QilNodeType.Divide => VisitDivide((QilBinary)n), 
			QilNodeType.Modulo => VisitModulo((QilBinary)n), 
			QilNodeType.StrLength => VisitStrLength((QilUnary)n), 
			QilNodeType.StrConcat => VisitStrConcat((QilStrConcat)n), 
			QilNodeType.StrParseQName => VisitStrParseQName((QilBinary)n), 
			QilNodeType.Ne => VisitNe((QilBinary)n), 
			QilNodeType.Eq => VisitEq((QilBinary)n), 
			QilNodeType.Gt => VisitGt((QilBinary)n), 
			QilNodeType.Ge => VisitGe((QilBinary)n), 
			QilNodeType.Lt => VisitLt((QilBinary)n), 
			QilNodeType.Le => VisitLe((QilBinary)n), 
			QilNodeType.Is => VisitIs((QilBinary)n), 
			QilNodeType.After => VisitAfter((QilBinary)n), 
			QilNodeType.Before => VisitBefore((QilBinary)n), 
			QilNodeType.Loop => VisitLoop((QilLoop)n), 
			QilNodeType.Filter => VisitFilter((QilLoop)n), 
			QilNodeType.Sort => VisitSort((QilLoop)n), 
			QilNodeType.SortKey => VisitSortKey((QilSortKey)n), 
			QilNodeType.DocOrderDistinct => VisitDocOrderDistinct((QilUnary)n), 
			QilNodeType.Function => VisitFunction((QilFunction)n), 
			QilNodeType.Invoke => VisitInvoke((QilInvoke)n), 
			QilNodeType.Content => VisitContent((QilUnary)n), 
			QilNodeType.Attribute => VisitAttribute((QilBinary)n), 
			QilNodeType.Parent => VisitParent((QilUnary)n), 
			QilNodeType.Root => VisitRoot((QilUnary)n), 
			QilNodeType.XmlContext => VisitXmlContext(n), 
			QilNodeType.Descendant => VisitDescendant((QilUnary)n), 
			QilNodeType.DescendantOrSelf => VisitDescendantOrSelf((QilUnary)n), 
			QilNodeType.Ancestor => VisitAncestor((QilUnary)n), 
			QilNodeType.AncestorOrSelf => VisitAncestorOrSelf((QilUnary)n), 
			QilNodeType.Preceding => VisitPreceding((QilUnary)n), 
			QilNodeType.FollowingSibling => VisitFollowingSibling((QilUnary)n), 
			QilNodeType.PrecedingSibling => VisitPrecedingSibling((QilUnary)n), 
			QilNodeType.NodeRange => VisitNodeRange((QilBinary)n), 
			QilNodeType.Deref => VisitDeref((QilBinary)n), 
			QilNodeType.ElementCtor => VisitElementCtor((QilBinary)n), 
			QilNodeType.AttributeCtor => VisitAttributeCtor((QilBinary)n), 
			QilNodeType.CommentCtor => VisitCommentCtor((QilUnary)n), 
			QilNodeType.PICtor => VisitPICtor((QilBinary)n), 
			QilNodeType.TextCtor => VisitTextCtor((QilUnary)n), 
			QilNodeType.RawTextCtor => VisitRawTextCtor((QilUnary)n), 
			QilNodeType.DocumentCtor => VisitDocumentCtor((QilUnary)n), 
			QilNodeType.NamespaceDecl => VisitNamespaceDecl((QilBinary)n), 
			QilNodeType.RtfCtor => VisitRtfCtor((QilBinary)n), 
			QilNodeType.NameOf => VisitNameOf((QilUnary)n), 
			QilNodeType.LocalNameOf => VisitLocalNameOf((QilUnary)n), 
			QilNodeType.NamespaceUriOf => VisitNamespaceUriOf((QilUnary)n), 
			QilNodeType.PrefixOf => VisitPrefixOf((QilUnary)n), 
			QilNodeType.TypeAssert => VisitTypeAssert((QilTargetType)n), 
			QilNodeType.IsType => VisitIsType((QilTargetType)n), 
			QilNodeType.IsEmpty => VisitIsEmpty((QilUnary)n), 
			QilNodeType.XPathNodeValue => VisitXPathNodeValue((QilUnary)n), 
			QilNodeType.XPathFollowing => VisitXPathFollowing((QilUnary)n), 
			QilNodeType.XPathPreceding => VisitXPathPreceding((QilUnary)n), 
			QilNodeType.XPathNamespace => VisitXPathNamespace((QilUnary)n), 
			QilNodeType.XsltGenerateId => VisitXsltGenerateId((QilUnary)n), 
			QilNodeType.XsltInvokeLateBound => VisitXsltInvokeLateBound((QilInvokeLateBound)n), 
			QilNodeType.XsltInvokeEarlyBound => VisitXsltInvokeEarlyBound((QilInvokeEarlyBound)n), 
			QilNodeType.XsltCopy => VisitXsltCopy((QilBinary)n), 
			QilNodeType.XsltCopyOf => VisitXsltCopyOf((QilUnary)n), 
			QilNodeType.XsltConvert => VisitXsltConvert((QilTargetType)n), 
			_ => VisitUnknown(n), 
		};
	}

	protected virtual QilNode VisitReference(QilNode n)
	{
		if (n == null)
		{
			return VisitNull();
		}
		return n.NodeType switch
		{
			QilNodeType.For => VisitForReference((QilIterator)n), 
			QilNodeType.Let => VisitLetReference((QilIterator)n), 
			QilNodeType.Parameter => VisitParameterReference((QilParameter)n), 
			QilNodeType.Function => VisitFunctionReference((QilFunction)n), 
			_ => VisitUnknown(n), 
		};
	}

	protected virtual QilNode VisitNull()
	{
		return null;
	}

	protected virtual QilNode VisitQilExpression(QilExpression n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitFunctionList(QilList n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitGlobalVariableList(QilList n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitGlobalParameterList(QilList n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitActualParameterList(QilList n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitFormalParameterList(QilList n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitSortKeyList(QilList n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitBranchList(QilList n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitOptimizeBarrier(QilUnary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitUnknown(QilNode n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitDataSource(QilDataSource n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitNop(QilUnary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitError(QilUnary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitWarning(QilUnary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitFor(QilIterator n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitForReference(QilIterator n)
	{
		return n;
	}

	protected virtual QilNode VisitLet(QilIterator n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitLetReference(QilIterator n)
	{
		return n;
	}

	protected virtual QilNode VisitParameter(QilParameter n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitParameterReference(QilParameter n)
	{
		return n;
	}

	protected virtual QilNode VisitPositionOf(QilUnary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitTrue(QilNode n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitFalse(QilNode n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitLiteralString(QilLiteral n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitLiteralInt32(QilLiteral n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitLiteralInt64(QilLiteral n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitLiteralDouble(QilLiteral n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitLiteralDecimal(QilLiteral n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitLiteralQName(QilName n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitLiteralType(QilLiteral n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitLiteralObject(QilLiteral n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitAnd(QilBinary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitOr(QilBinary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitNot(QilUnary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitConditional(QilTernary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitChoice(QilChoice n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitLength(QilUnary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitSequence(QilList n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitUnion(QilBinary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitIntersection(QilBinary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitDifference(QilBinary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitAverage(QilUnary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitSum(QilUnary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitMinimum(QilUnary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitMaximum(QilUnary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitNegate(QilUnary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitAdd(QilBinary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitSubtract(QilBinary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitMultiply(QilBinary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitDivide(QilBinary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitModulo(QilBinary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitStrLength(QilUnary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitStrConcat(QilStrConcat n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitStrParseQName(QilBinary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitNe(QilBinary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitEq(QilBinary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitGt(QilBinary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitGe(QilBinary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitLt(QilBinary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitLe(QilBinary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitIs(QilBinary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitAfter(QilBinary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitBefore(QilBinary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitLoop(QilLoop n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitFilter(QilLoop n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitSort(QilLoop n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitSortKey(QilSortKey n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitDocOrderDistinct(QilUnary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitFunction(QilFunction n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitFunctionReference(QilFunction n)
	{
		return n;
	}

	protected virtual QilNode VisitInvoke(QilInvoke n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitContent(QilUnary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitAttribute(QilBinary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitParent(QilUnary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitRoot(QilUnary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitXmlContext(QilNode n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitDescendant(QilUnary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitDescendantOrSelf(QilUnary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitAncestor(QilUnary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitAncestorOrSelf(QilUnary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitPreceding(QilUnary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitFollowingSibling(QilUnary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitPrecedingSibling(QilUnary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitNodeRange(QilBinary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitDeref(QilBinary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitElementCtor(QilBinary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitAttributeCtor(QilBinary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitCommentCtor(QilUnary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitPICtor(QilBinary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitTextCtor(QilUnary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitRawTextCtor(QilUnary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitDocumentCtor(QilUnary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitNamespaceDecl(QilBinary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitRtfCtor(QilBinary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitNameOf(QilUnary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitLocalNameOf(QilUnary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitNamespaceUriOf(QilUnary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitPrefixOf(QilUnary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitTypeAssert(QilTargetType n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitIsType(QilTargetType n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitIsEmpty(QilUnary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitXPathNodeValue(QilUnary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitXPathFollowing(QilUnary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitXPathPreceding(QilUnary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitXPathNamespace(QilUnary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitXsltGenerateId(QilUnary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitXsltInvokeLateBound(QilInvokeLateBound n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitXsltInvokeEarlyBound(QilInvokeEarlyBound n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitXsltCopy(QilBinary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitXsltCopyOf(QilUnary n)
	{
		return VisitChildren(n);
	}

	protected virtual QilNode VisitXsltConvert(QilTargetType n)
	{
		return VisitChildren(n);
	}
}

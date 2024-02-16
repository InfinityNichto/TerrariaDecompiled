using System.Xml.Schema;

namespace System.Xml.Xsl.Qil;

internal sealed class QilTypeChecker
{
	public XmlQueryType Check(QilNode n)
	{
		return n.NodeType switch
		{
			QilNodeType.QilExpression => CheckQilExpression((QilExpression)n), 
			QilNodeType.FunctionList => CheckFunctionList((QilList)n), 
			QilNodeType.GlobalVariableList => CheckGlobalVariableList((QilList)n), 
			QilNodeType.GlobalParameterList => CheckGlobalParameterList((QilList)n), 
			QilNodeType.ActualParameterList => CheckActualParameterList((QilList)n), 
			QilNodeType.FormalParameterList => CheckFormalParameterList((QilList)n), 
			QilNodeType.SortKeyList => CheckSortKeyList((QilList)n), 
			QilNodeType.BranchList => CheckBranchList((QilList)n), 
			QilNodeType.OptimizeBarrier => CheckOptimizeBarrier((QilUnary)n), 
			QilNodeType.Unknown => CheckUnknown(n), 
			QilNodeType.DataSource => CheckDataSource((QilDataSource)n), 
			QilNodeType.Nop => CheckNop((QilUnary)n), 
			QilNodeType.Error => CheckError((QilUnary)n), 
			QilNodeType.Warning => CheckWarning((QilUnary)n), 
			QilNodeType.For => CheckFor((QilIterator)n), 
			QilNodeType.Let => CheckLet((QilIterator)n), 
			QilNodeType.Parameter => CheckParameter((QilParameter)n), 
			QilNodeType.PositionOf => CheckPositionOf((QilUnary)n), 
			QilNodeType.True => CheckTrue(n), 
			QilNodeType.False => CheckFalse(n), 
			QilNodeType.LiteralString => CheckLiteralString((QilLiteral)n), 
			QilNodeType.LiteralInt32 => CheckLiteralInt32((QilLiteral)n), 
			QilNodeType.LiteralInt64 => CheckLiteralInt64((QilLiteral)n), 
			QilNodeType.LiteralDouble => CheckLiteralDouble((QilLiteral)n), 
			QilNodeType.LiteralDecimal => CheckLiteralDecimal((QilLiteral)n), 
			QilNodeType.LiteralQName => CheckLiteralQName((QilName)n), 
			QilNodeType.LiteralType => CheckLiteralType((QilLiteral)n), 
			QilNodeType.LiteralObject => CheckLiteralObject((QilLiteral)n), 
			QilNodeType.And => CheckAnd((QilBinary)n), 
			QilNodeType.Or => CheckOr((QilBinary)n), 
			QilNodeType.Not => CheckNot((QilUnary)n), 
			QilNodeType.Conditional => CheckConditional((QilTernary)n), 
			QilNodeType.Choice => CheckChoice((QilChoice)n), 
			QilNodeType.Length => CheckLength((QilUnary)n), 
			QilNodeType.Sequence => CheckSequence((QilList)n), 
			QilNodeType.Union => CheckUnion((QilBinary)n), 
			QilNodeType.Intersection => CheckIntersection((QilBinary)n), 
			QilNodeType.Difference => CheckDifference((QilBinary)n), 
			QilNodeType.Average => CheckAverage((QilUnary)n), 
			QilNodeType.Sum => CheckSum((QilUnary)n), 
			QilNodeType.Minimum => CheckMinimum((QilUnary)n), 
			QilNodeType.Maximum => CheckMaximum((QilUnary)n), 
			QilNodeType.Negate => CheckNegate((QilUnary)n), 
			QilNodeType.Add => CheckAdd((QilBinary)n), 
			QilNodeType.Subtract => CheckSubtract((QilBinary)n), 
			QilNodeType.Multiply => CheckMultiply((QilBinary)n), 
			QilNodeType.Divide => CheckDivide((QilBinary)n), 
			QilNodeType.Modulo => CheckModulo((QilBinary)n), 
			QilNodeType.StrLength => CheckStrLength((QilUnary)n), 
			QilNodeType.StrConcat => CheckStrConcat((QilStrConcat)n), 
			QilNodeType.StrParseQName => CheckStrParseQName((QilBinary)n), 
			QilNodeType.Ne => CheckNe((QilBinary)n), 
			QilNodeType.Eq => CheckEq((QilBinary)n), 
			QilNodeType.Gt => CheckGt((QilBinary)n), 
			QilNodeType.Ge => CheckGe((QilBinary)n), 
			QilNodeType.Lt => CheckLt((QilBinary)n), 
			QilNodeType.Le => CheckLe((QilBinary)n), 
			QilNodeType.Is => CheckIs((QilBinary)n), 
			QilNodeType.After => CheckAfter((QilBinary)n), 
			QilNodeType.Before => CheckBefore((QilBinary)n), 
			QilNodeType.Loop => CheckLoop((QilLoop)n), 
			QilNodeType.Filter => CheckFilter((QilLoop)n), 
			QilNodeType.Sort => CheckSort((QilLoop)n), 
			QilNodeType.SortKey => CheckSortKey((QilSortKey)n), 
			QilNodeType.DocOrderDistinct => CheckDocOrderDistinct((QilUnary)n), 
			QilNodeType.Function => CheckFunction((QilFunction)n), 
			QilNodeType.Invoke => CheckInvoke((QilInvoke)n), 
			QilNodeType.Content => CheckContent((QilUnary)n), 
			QilNodeType.Attribute => CheckAttribute((QilBinary)n), 
			QilNodeType.Parent => CheckParent((QilUnary)n), 
			QilNodeType.Root => CheckRoot((QilUnary)n), 
			QilNodeType.XmlContext => CheckXmlContext(n), 
			QilNodeType.Descendant => CheckDescendant((QilUnary)n), 
			QilNodeType.DescendantOrSelf => CheckDescendantOrSelf((QilUnary)n), 
			QilNodeType.Ancestor => CheckAncestor((QilUnary)n), 
			QilNodeType.AncestorOrSelf => CheckAncestorOrSelf((QilUnary)n), 
			QilNodeType.Preceding => CheckPreceding((QilUnary)n), 
			QilNodeType.FollowingSibling => CheckFollowingSibling((QilUnary)n), 
			QilNodeType.PrecedingSibling => CheckPrecedingSibling((QilUnary)n), 
			QilNodeType.NodeRange => CheckNodeRange((QilBinary)n), 
			QilNodeType.Deref => CheckDeref((QilBinary)n), 
			QilNodeType.ElementCtor => CheckElementCtor((QilBinary)n), 
			QilNodeType.AttributeCtor => CheckAttributeCtor((QilBinary)n), 
			QilNodeType.CommentCtor => CheckCommentCtor((QilUnary)n), 
			QilNodeType.PICtor => CheckPICtor((QilBinary)n), 
			QilNodeType.TextCtor => CheckTextCtor((QilUnary)n), 
			QilNodeType.RawTextCtor => CheckRawTextCtor((QilUnary)n), 
			QilNodeType.DocumentCtor => CheckDocumentCtor((QilUnary)n), 
			QilNodeType.NamespaceDecl => CheckNamespaceDecl((QilBinary)n), 
			QilNodeType.RtfCtor => CheckRtfCtor((QilBinary)n), 
			QilNodeType.NameOf => CheckNameOf((QilUnary)n), 
			QilNodeType.LocalNameOf => CheckLocalNameOf((QilUnary)n), 
			QilNodeType.NamespaceUriOf => CheckNamespaceUriOf((QilUnary)n), 
			QilNodeType.PrefixOf => CheckPrefixOf((QilUnary)n), 
			QilNodeType.TypeAssert => CheckTypeAssert((QilTargetType)n), 
			QilNodeType.IsType => CheckIsType((QilTargetType)n), 
			QilNodeType.IsEmpty => CheckIsEmpty((QilUnary)n), 
			QilNodeType.XPathNodeValue => CheckXPathNodeValue((QilUnary)n), 
			QilNodeType.XPathFollowing => CheckXPathFollowing((QilUnary)n), 
			QilNodeType.XPathPreceding => CheckXPathPreceding((QilUnary)n), 
			QilNodeType.XPathNamespace => CheckXPathNamespace((QilUnary)n), 
			QilNodeType.XsltGenerateId => CheckXsltGenerateId((QilUnary)n), 
			QilNodeType.XsltInvokeLateBound => CheckXsltInvokeLateBound((QilInvokeLateBound)n), 
			QilNodeType.XsltInvokeEarlyBound => CheckXsltInvokeEarlyBound((QilInvokeEarlyBound)n), 
			QilNodeType.XsltCopy => CheckXsltCopy((QilBinary)n), 
			QilNodeType.XsltCopyOf => CheckXsltCopyOf((QilUnary)n), 
			QilNodeType.XsltConvert => CheckXsltConvert((QilTargetType)n), 
			_ => CheckUnknown(n), 
		};
	}

	public XmlQueryType CheckQilExpression(QilExpression node)
	{
		return XmlQueryTypeFactory.ItemS;
	}

	public XmlQueryType CheckFunctionList(QilList node)
	{
		foreach (QilNode item in node)
		{
		}
		return node.XmlType;
	}

	public XmlQueryType CheckGlobalVariableList(QilList node)
	{
		foreach (QilNode item in node)
		{
		}
		return node.XmlType;
	}

	public XmlQueryType CheckGlobalParameterList(QilList node)
	{
		foreach (QilNode item in node)
		{
		}
		return node.XmlType;
	}

	public XmlQueryType CheckActualParameterList(QilList node)
	{
		return node.XmlType;
	}

	public XmlQueryType CheckFormalParameterList(QilList node)
	{
		foreach (QilNode item in node)
		{
		}
		return node.XmlType;
	}

	public XmlQueryType CheckSortKeyList(QilList node)
	{
		foreach (QilNode item in node)
		{
		}
		return node.XmlType;
	}

	public XmlQueryType CheckBranchList(QilList node)
	{
		return node.XmlType;
	}

	public XmlQueryType CheckOptimizeBarrier(QilUnary node)
	{
		return node.Child.XmlType;
	}

	public XmlQueryType CheckUnknown(QilNode node)
	{
		return node.XmlType;
	}

	public XmlQueryType CheckDataSource(QilDataSource node)
	{
		return XmlQueryTypeFactory.NodeNotRtfQ;
	}

	public XmlQueryType CheckNop(QilUnary node)
	{
		return node.Child.XmlType;
	}

	public XmlQueryType CheckError(QilUnary node)
	{
		return XmlQueryTypeFactory.None;
	}

	public XmlQueryType CheckWarning(QilUnary node)
	{
		return XmlQueryTypeFactory.Empty;
	}

	public XmlQueryType CheckFor(QilIterator node)
	{
		return node.Binding.XmlType.Prime;
	}

	public XmlQueryType CheckLet(QilIterator node)
	{
		return node.Binding.XmlType;
	}

	public XmlQueryType CheckParameter(QilParameter node)
	{
		return node.XmlType;
	}

	public XmlQueryType CheckPositionOf(QilUnary node)
	{
		return XmlQueryTypeFactory.IntX;
	}

	public XmlQueryType CheckTrue(QilNode node)
	{
		return XmlQueryTypeFactory.BooleanX;
	}

	public XmlQueryType CheckFalse(QilNode node)
	{
		return XmlQueryTypeFactory.BooleanX;
	}

	public XmlQueryType CheckLiteralString(QilLiteral node)
	{
		return XmlQueryTypeFactory.StringX;
	}

	public XmlQueryType CheckLiteralInt32(QilLiteral node)
	{
		return XmlQueryTypeFactory.IntX;
	}

	public XmlQueryType CheckLiteralInt64(QilLiteral node)
	{
		return XmlQueryTypeFactory.IntegerX;
	}

	public XmlQueryType CheckLiteralDouble(QilLiteral node)
	{
		return XmlQueryTypeFactory.DoubleX;
	}

	public XmlQueryType CheckLiteralDecimal(QilLiteral node)
	{
		return XmlQueryTypeFactory.DecimalX;
	}

	public XmlQueryType CheckLiteralQName(QilName node)
	{
		return XmlQueryTypeFactory.QNameX;
	}

	public XmlQueryType CheckLiteralType(QilLiteral node)
	{
		return node;
	}

	public XmlQueryType CheckLiteralObject(QilLiteral node)
	{
		return XmlQueryTypeFactory.ItemS;
	}

	public XmlQueryType CheckAnd(QilBinary node)
	{
		return XmlQueryTypeFactory.BooleanX;
	}

	public XmlQueryType CheckOr(QilBinary node)
	{
		return CheckAnd(node);
	}

	public XmlQueryType CheckNot(QilUnary node)
	{
		return XmlQueryTypeFactory.BooleanX;
	}

	public XmlQueryType CheckConditional(QilTernary node)
	{
		return XmlQueryTypeFactory.Choice(node.Center.XmlType, node.Right.XmlType);
	}

	public XmlQueryType CheckChoice(QilChoice node)
	{
		return node.Branches.XmlType;
	}

	public XmlQueryType CheckLength(QilUnary node)
	{
		return XmlQueryTypeFactory.IntX;
	}

	public XmlQueryType CheckSequence(QilList node)
	{
		return node.XmlType;
	}

	public XmlQueryType CheckUnion(QilBinary node)
	{
		return DistinctType(XmlQueryTypeFactory.Sequence(node.Left.XmlType, node.Right.XmlType));
	}

	public XmlQueryType CheckIntersection(QilBinary node)
	{
		return CheckUnion(node);
	}

	public XmlQueryType CheckDifference(QilBinary node)
	{
		return XmlQueryTypeFactory.AtMost(node.Left.XmlType, node.Left.XmlType.Cardinality);
	}

	public XmlQueryType CheckAverage(QilUnary node)
	{
		XmlQueryType xmlType = node.Child.XmlType;
		return XmlQueryTypeFactory.PrimeProduct(xmlType, xmlType.MaybeEmpty ? XmlQueryCardinality.ZeroOrOne : XmlQueryCardinality.One);
	}

	public XmlQueryType CheckSum(QilUnary node)
	{
		return CheckAverage(node);
	}

	public XmlQueryType CheckMinimum(QilUnary node)
	{
		return CheckAverage(node);
	}

	public XmlQueryType CheckMaximum(QilUnary node)
	{
		return CheckAverage(node);
	}

	public XmlQueryType CheckNegate(QilUnary node)
	{
		return node.Child.XmlType;
	}

	public XmlQueryType CheckAdd(QilBinary node)
	{
		if (node.Left.XmlType.TypeCode != 0)
		{
			return node.Left.XmlType;
		}
		return node.Right.XmlType;
	}

	public XmlQueryType CheckSubtract(QilBinary node)
	{
		return CheckAdd(node);
	}

	public XmlQueryType CheckMultiply(QilBinary node)
	{
		return CheckAdd(node);
	}

	public XmlQueryType CheckDivide(QilBinary node)
	{
		return CheckAdd(node);
	}

	public XmlQueryType CheckModulo(QilBinary node)
	{
		return CheckAdd(node);
	}

	public XmlQueryType CheckStrLength(QilUnary node)
	{
		return XmlQueryTypeFactory.IntX;
	}

	public XmlQueryType CheckStrConcat(QilStrConcat node)
	{
		return XmlQueryTypeFactory.StringX;
	}

	public XmlQueryType CheckStrParseQName(QilBinary node)
	{
		return XmlQueryTypeFactory.QNameX;
	}

	public XmlQueryType CheckNe(QilBinary node)
	{
		return XmlQueryTypeFactory.BooleanX;
	}

	public XmlQueryType CheckEq(QilBinary node)
	{
		return CheckNe(node);
	}

	public XmlQueryType CheckGt(QilBinary node)
	{
		return CheckNe(node);
	}

	public XmlQueryType CheckGe(QilBinary node)
	{
		return CheckNe(node);
	}

	public XmlQueryType CheckLt(QilBinary node)
	{
		return CheckNe(node);
	}

	public XmlQueryType CheckLe(QilBinary node)
	{
		return CheckNe(node);
	}

	public XmlQueryType CheckIs(QilBinary node)
	{
		return XmlQueryTypeFactory.BooleanX;
	}

	public XmlQueryType CheckAfter(QilBinary node)
	{
		return CheckIs(node);
	}

	public XmlQueryType CheckBefore(QilBinary node)
	{
		return CheckIs(node);
	}

	public XmlQueryType CheckLoop(QilLoop node)
	{
		XmlQueryType xmlType = node.Body.XmlType;
		XmlQueryCardinality xmlQueryCardinality = ((node.Variable.NodeType == QilNodeType.Let) ? XmlQueryCardinality.One : node.Variable.Binding.XmlType.Cardinality);
		return XmlQueryTypeFactory.PrimeProduct(xmlType, xmlQueryCardinality * xmlType.Cardinality);
	}

	public XmlQueryType CheckFilter(QilLoop node)
	{
		XmlQueryType xmlQueryType = FindFilterType(node.Variable, node.Body);
		if (xmlQueryType != null)
		{
			return xmlQueryType;
		}
		return XmlQueryTypeFactory.AtMost(node.Variable.Binding.XmlType, node.Variable.Binding.XmlType.Cardinality);
	}

	public XmlQueryType CheckSort(QilLoop node)
	{
		XmlQueryType xmlType = node.Variable.Binding.XmlType;
		return XmlQueryTypeFactory.PrimeProduct(xmlType, xmlType.Cardinality);
	}

	public XmlQueryType CheckSortKey(QilSortKey node)
	{
		return node.Key.XmlType;
	}

	public XmlQueryType CheckDocOrderDistinct(QilUnary node)
	{
		return DistinctType(node.Child.XmlType);
	}

	public XmlQueryType CheckFunction(QilFunction node)
	{
		return node.XmlType;
	}

	public XmlQueryType CheckInvoke(QilInvoke node)
	{
		return node.Function.XmlType;
	}

	public XmlQueryType CheckContent(QilUnary node)
	{
		return XmlQueryTypeFactory.AttributeOrContentS;
	}

	public XmlQueryType CheckAttribute(QilBinary node)
	{
		return XmlQueryTypeFactory.AttributeQ;
	}

	public XmlQueryType CheckParent(QilUnary node)
	{
		return XmlQueryTypeFactory.DocumentOrElementQ;
	}

	public XmlQueryType CheckRoot(QilUnary node)
	{
		return XmlQueryTypeFactory.NodeNotRtf;
	}

	public XmlQueryType CheckXmlContext(QilNode node)
	{
		return XmlQueryTypeFactory.NodeNotRtf;
	}

	public XmlQueryType CheckDescendant(QilUnary node)
	{
		return XmlQueryTypeFactory.ContentS;
	}

	public XmlQueryType CheckDescendantOrSelf(QilUnary node)
	{
		return XmlQueryTypeFactory.Choice(node.Child.XmlType, XmlQueryTypeFactory.ContentS);
	}

	public XmlQueryType CheckAncestor(QilUnary node)
	{
		return XmlQueryTypeFactory.DocumentOrElementS;
	}

	public XmlQueryType CheckAncestorOrSelf(QilUnary node)
	{
		return XmlQueryTypeFactory.Choice(node.Child.XmlType, XmlQueryTypeFactory.DocumentOrElementS);
	}

	public XmlQueryType CheckPreceding(QilUnary node)
	{
		return XmlQueryTypeFactory.DocumentOrContentS;
	}

	public XmlQueryType CheckFollowingSibling(QilUnary node)
	{
		return XmlQueryTypeFactory.ContentS;
	}

	public XmlQueryType CheckPrecedingSibling(QilUnary node)
	{
		return XmlQueryTypeFactory.ContentS;
	}

	public XmlQueryType CheckNodeRange(QilBinary node)
	{
		return XmlQueryTypeFactory.Choice(node.Left.XmlType, XmlQueryTypeFactory.ContentS, node.Right.XmlType);
	}

	public XmlQueryType CheckDeref(QilBinary node)
	{
		return XmlQueryTypeFactory.ElementS;
	}

	public XmlQueryType CheckElementCtor(QilBinary node)
	{
		return XmlQueryTypeFactory.UntypedElement;
	}

	public XmlQueryType CheckAttributeCtor(QilBinary node)
	{
		return XmlQueryTypeFactory.UntypedAttribute;
	}

	public XmlQueryType CheckCommentCtor(QilUnary node)
	{
		return XmlQueryTypeFactory.Comment;
	}

	public XmlQueryType CheckPICtor(QilBinary node)
	{
		return XmlQueryTypeFactory.PI;
	}

	public XmlQueryType CheckTextCtor(QilUnary node)
	{
		return XmlQueryTypeFactory.Text;
	}

	public XmlQueryType CheckRawTextCtor(QilUnary node)
	{
		return XmlQueryTypeFactory.Text;
	}

	public XmlQueryType CheckDocumentCtor(QilUnary node)
	{
		return XmlQueryTypeFactory.UntypedDocument;
	}

	public XmlQueryType CheckNamespaceDecl(QilBinary node)
	{
		return XmlQueryTypeFactory.Namespace;
	}

	public XmlQueryType CheckRtfCtor(QilBinary node)
	{
		return XmlQueryTypeFactory.Node;
	}

	public XmlQueryType CheckNameOf(QilUnary node)
	{
		return XmlQueryTypeFactory.QNameX;
	}

	public XmlQueryType CheckLocalNameOf(QilUnary node)
	{
		return XmlQueryTypeFactory.StringX;
	}

	public XmlQueryType CheckNamespaceUriOf(QilUnary node)
	{
		return XmlQueryTypeFactory.StringX;
	}

	public XmlQueryType CheckPrefixOf(QilUnary node)
	{
		return XmlQueryTypeFactory.StringX;
	}

	public XmlQueryType CheckTypeAssert(QilTargetType node)
	{
		return node.TargetType;
	}

	public XmlQueryType CheckIsType(QilTargetType node)
	{
		return XmlQueryTypeFactory.BooleanX;
	}

	public XmlQueryType CheckIsEmpty(QilUnary node)
	{
		return XmlQueryTypeFactory.BooleanX;
	}

	public XmlQueryType CheckXPathNodeValue(QilUnary node)
	{
		return XmlQueryTypeFactory.StringX;
	}

	public XmlQueryType CheckXPathFollowing(QilUnary node)
	{
		return XmlQueryTypeFactory.ContentS;
	}

	public XmlQueryType CheckXPathPreceding(QilUnary node)
	{
		return XmlQueryTypeFactory.ContentS;
	}

	public XmlQueryType CheckXPathNamespace(QilUnary node)
	{
		return XmlQueryTypeFactory.NamespaceS;
	}

	public XmlQueryType CheckXsltGenerateId(QilUnary node)
	{
		return XmlQueryTypeFactory.StringX;
	}

	public XmlQueryType CheckXsltInvokeLateBound(QilInvokeLateBound node)
	{
		return XmlQueryTypeFactory.ItemS;
	}

	public XmlQueryType CheckXsltInvokeEarlyBound(QilInvokeEarlyBound node)
	{
		return node.XmlType;
	}

	public XmlQueryType CheckXsltCopy(QilBinary node)
	{
		return XmlQueryTypeFactory.Choice(node.Left.XmlType, node.Right.XmlType);
	}

	public XmlQueryType CheckXsltCopyOf(QilUnary node)
	{
		if ((node.Child.XmlType.NodeKinds & XmlNodeKindFlags.Document) != 0)
		{
			return XmlQueryTypeFactory.NodeNotRtfS;
		}
		return node.Child.XmlType;
	}

	public XmlQueryType CheckXsltConvert(QilTargetType node)
	{
		return node.TargetType;
	}

	private XmlQueryType DistinctType(XmlQueryType type)
	{
		if (type.Cardinality == XmlQueryCardinality.More)
		{
			return XmlQueryTypeFactory.PrimeProduct(type, XmlQueryCardinality.OneOrMore);
		}
		if (type.Cardinality == XmlQueryCardinality.NotOne)
		{
			return XmlQueryTypeFactory.PrimeProduct(type, XmlQueryCardinality.ZeroOrMore);
		}
		return type;
	}

	private XmlQueryType FindFilterType(QilIterator variable, QilNode body)
	{
		if (body.XmlType.TypeCode == XmlTypeCode.None)
		{
			return XmlQueryTypeFactory.None;
		}
		switch (body.NodeType)
		{
		case QilNodeType.False:
			return XmlQueryTypeFactory.Empty;
		case QilNodeType.IsType:
			if (((QilTargetType)body).Source == variable)
			{
				return XmlQueryTypeFactory.AtMost(((QilTargetType)body).TargetType, variable.Binding.XmlType.Cardinality);
			}
			break;
		case QilNodeType.And:
		{
			XmlQueryType xmlQueryType = FindFilterType(variable, ((QilBinary)body).Left);
			if (xmlQueryType != null)
			{
				return xmlQueryType;
			}
			return FindFilterType(variable, ((QilBinary)body).Right);
		}
		case QilNodeType.Eq:
		{
			QilBinary qilBinary = (QilBinary)body;
			if (qilBinary.Left.NodeType == QilNodeType.PositionOf && ((QilUnary)qilBinary.Left).Child == variable)
			{
				return XmlQueryTypeFactory.AtMost(variable.Binding.XmlType, XmlQueryCardinality.ZeroOrOne);
			}
			break;
		}
		}
		return null;
	}
}

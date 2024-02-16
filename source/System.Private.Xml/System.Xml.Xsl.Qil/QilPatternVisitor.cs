using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Xml.Xsl.Qil;

internal abstract class QilPatternVisitor : QilReplaceVisitor
{
	internal sealed class QilPatterns
	{
		private readonly BitArray _bits;

		public QilPatterns(int szBits, bool allSet)
		{
			_bits = new BitArray(szBits, allSet);
		}

		public void Add(int i)
		{
			_bits.Set(i, value: true);
		}

		public bool IsSet(int i)
		{
			return _bits[i];
		}
	}

	[CompilerGenerated]
	private int _003CLastReplacement_003Ek__BackingField;

	public QilPatterns Patterns { get; set; }

	public int Threshold { get; } = int.MaxValue;


	public int ReplacementCount { get; private set; }

	private int LastReplacement
	{
		[CompilerGenerated]
		set
		{
			_003CLastReplacement_003Ek__BackingField = value;
		}
	}

	public bool Matching => ReplacementCount < Threshold;

	public QilPatternVisitor(QilPatterns patterns, QilFactory f)
		: base(f)
	{
		Patterns = patterns;
	}

	protected virtual bool AllowReplace(int pattern, QilNode original)
	{
		if (Matching)
		{
			ReplacementCount++;
			LastReplacement = pattern;
			return true;
		}
		return false;
	}

	protected virtual QilNode Replace(int pattern, QilNode original, QilNode replacement)
	{
		replacement.SourceLine = original.SourceLine;
		return replacement;
	}

	[return: NotNullIfNotNull("node")]
	protected virtual QilNode NoReplace(QilNode node)
	{
		return node;
	}

	protected override QilNode Visit(QilNode node)
	{
		if (node == null)
		{
			return VisitNull();
		}
		node = VisitChildren(node);
		return base.Visit(node);
	}

	protected override QilNode VisitQilExpression(QilExpression n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitFunctionList(QilList n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitGlobalVariableList(QilList n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitGlobalParameterList(QilList n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitActualParameterList(QilList n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitFormalParameterList(QilList n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitSortKeyList(QilList n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitBranchList(QilList n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitOptimizeBarrier(QilUnary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitUnknown(QilNode n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitDataSource(QilDataSource n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitNop(QilUnary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitError(QilUnary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitWarning(QilUnary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitFor(QilIterator n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitForReference(QilIterator n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitLet(QilIterator n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitLetReference(QilIterator n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitParameter(QilParameter n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitParameterReference(QilParameter n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitPositionOf(QilUnary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitTrue(QilNode n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitFalse(QilNode n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitLiteralString(QilLiteral n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitLiteralInt32(QilLiteral n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitLiteralInt64(QilLiteral n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitLiteralDouble(QilLiteral n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitLiteralDecimal(QilLiteral n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitLiteralQName(QilName n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitLiteralType(QilLiteral n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitLiteralObject(QilLiteral n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitAnd(QilBinary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitOr(QilBinary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitNot(QilUnary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitConditional(QilTernary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitChoice(QilChoice n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitLength(QilUnary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitSequence(QilList n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitUnion(QilBinary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitIntersection(QilBinary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitDifference(QilBinary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitAverage(QilUnary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitSum(QilUnary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitMinimum(QilUnary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitMaximum(QilUnary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitNegate(QilUnary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitAdd(QilBinary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitSubtract(QilBinary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitMultiply(QilBinary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitDivide(QilBinary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitModulo(QilBinary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitStrLength(QilUnary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitStrConcat(QilStrConcat n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitStrParseQName(QilBinary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitNe(QilBinary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitEq(QilBinary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitGt(QilBinary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitGe(QilBinary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitLt(QilBinary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitLe(QilBinary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitIs(QilBinary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitAfter(QilBinary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitBefore(QilBinary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitLoop(QilLoop n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitFilter(QilLoop n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitSort(QilLoop n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitSortKey(QilSortKey n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitDocOrderDistinct(QilUnary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitFunction(QilFunction n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitFunctionReference(QilFunction n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitInvoke(QilInvoke n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitContent(QilUnary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitAttribute(QilBinary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitParent(QilUnary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitRoot(QilUnary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitXmlContext(QilNode n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitDescendant(QilUnary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitDescendantOrSelf(QilUnary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitAncestor(QilUnary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitAncestorOrSelf(QilUnary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitPreceding(QilUnary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitFollowingSibling(QilUnary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitPrecedingSibling(QilUnary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitNodeRange(QilBinary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitDeref(QilBinary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitElementCtor(QilBinary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitAttributeCtor(QilBinary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitCommentCtor(QilUnary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitPICtor(QilBinary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitTextCtor(QilUnary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitRawTextCtor(QilUnary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitDocumentCtor(QilUnary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitNamespaceDecl(QilBinary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitRtfCtor(QilBinary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitNameOf(QilUnary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitLocalNameOf(QilUnary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitNamespaceUriOf(QilUnary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitPrefixOf(QilUnary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitTypeAssert(QilTargetType n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitIsType(QilTargetType n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitIsEmpty(QilUnary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitXPathNodeValue(QilUnary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitXPathFollowing(QilUnary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitXPathPreceding(QilUnary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitXPathNamespace(QilUnary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitXsltGenerateId(QilUnary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitXsltInvokeLateBound(QilInvokeLateBound n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitXsltInvokeEarlyBound(QilInvokeEarlyBound n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitXsltCopy(QilBinary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitXsltCopyOf(QilUnary n)
	{
		return NoReplace(n);
	}

	protected override QilNode VisitXsltConvert(QilTargetType n)
	{
		return NoReplace(n);
	}
}

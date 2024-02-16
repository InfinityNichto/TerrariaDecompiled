using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Schema;
using System.Xml.Xsl.Qil;
using System.Xml.Xsl.Runtime;

namespace System.Xml.Xsl.IlGen;

internal sealed class XmlILOptimizerVisitor : QilPatternVisitor
{
	private sealed class NodeCounter : QilVisitor
	{
		private QilNode target;

		private int cnt;

		public int Count(QilNode expr, QilNode target)
		{
			cnt = 0;
			this.target = target;
			Visit(expr);
			return cnt;
		}

		protected override QilNode Visit(QilNode n)
		{
			if (n == null)
			{
				return null;
			}
			if (n == target)
			{
				cnt++;
			}
			return VisitChildren(n);
		}

		protected override QilNode VisitReference(QilNode n)
		{
			if (n == target)
			{
				cnt++;
			}
			return n;
		}
	}

	private class NodeFinder : QilVisitor
	{
		protected bool result;

		protected QilNode target;

		protected QilNode parent;

		public bool Find(QilNode expr, QilNode target)
		{
			result = false;
			this.target = target;
			parent = null;
			VisitAssumeReference(expr);
			return result;
		}

		protected override QilNode Visit(QilNode expr)
		{
			if (!result)
			{
				if (expr == target)
				{
					result = OnFound(expr);
				}
				if (!result)
				{
					QilNode qilNode = parent;
					parent = expr;
					VisitChildren(expr);
					parent = qilNode;
				}
			}
			return expr;
		}

		protected override QilNode VisitReference(QilNode expr)
		{
			if (expr == target)
			{
				result = OnFound(expr);
			}
			return expr;
		}

		protected virtual bool OnFound(QilNode expr)
		{
			return true;
		}
	}

	private sealed class PositionOfFinder : NodeFinder
	{
		protected override bool OnFound(QilNode expr)
		{
			if (parent != null)
			{
				return parent.NodeType == QilNodeType.PositionOf;
			}
			return false;
		}
	}

	private sealed class EqualityIndexVisitor : QilVisitor
	{
		private bool result;

		private QilNode ctxt;

		private QilNode key;

		public bool Scan(QilNode expr, QilNode ctxt, QilNode key)
		{
			result = true;
			this.ctxt = ctxt;
			this.key = key;
			Visit(expr);
			return result;
		}

		protected override QilNode VisitReference(QilNode expr)
		{
			if (result && (expr == key || expr == ctxt))
			{
				result = false;
				return expr;
			}
			return expr;
		}

		protected override QilNode VisitRoot(QilUnary root)
		{
			if (root.Child == ctxt)
			{
				return root;
			}
			return VisitChildren(root);
		}
	}

	private static readonly QilPatterns s_patternsNoOpt = CreatePatternsNoOpt();

	private static readonly QilPatterns s_patternsOpt = new QilPatterns(141, allSet: true);

	private readonly QilExpression _qil;

	private readonly XmlILElementAnalyzer _elemAnalyzer;

	private readonly XmlILStateAnalyzer _contentAnalyzer;

	private readonly XmlILNamespaceAnalyzer _nmspAnalyzer;

	private readonly NodeCounter _nodeCounter = new NodeCounter();

	private readonly SubstitutionList _subs = new SubstitutionList();

	private bool this[XmlILOptimization ann] => base.Patterns.IsSet((int)ann);

	private static QilPatterns CreatePatternsNoOpt()
	{
		QilPatterns qilPatterns = new QilPatterns(141, allSet: false);
		qilPatterns.Add(104);
		qilPatterns.Add(88);
		qilPatterns.Add(97);
		qilPatterns.Add(71);
		qilPatterns.Add(70);
		qilPatterns.Add(58);
		qilPatterns.Add(96);
		qilPatterns.Add(79);
		qilPatterns.Add(78);
		qilPatterns.Add(91);
		qilPatterns.Add(93);
		qilPatterns.Add(134);
		qilPatterns.Add(118);
		qilPatterns.Add(112);
		qilPatterns.Add(41);
		qilPatterns.Add(48);
		qilPatterns.Add(15);
		qilPatterns.Add(8);
		qilPatterns.Add(23);
		qilPatterns.Add(24);
		qilPatterns.Add(7);
		qilPatterns.Add(18);
		return qilPatterns;
	}

	public XmlILOptimizerVisitor(QilExpression qil, bool optimize)
		: base(optimize ? s_patternsOpt : s_patternsNoOpt, qil.Factory)
	{
		_qil = qil;
		_elemAnalyzer = new XmlILElementAnalyzer(qil.Factory);
		_contentAnalyzer = new XmlILStateAnalyzer(qil.Factory);
		_nmspAnalyzer = new XmlILNamespaceAnalyzer();
	}

	public QilExpression Optimize()
	{
		QilExpression qilExpression = (QilExpression)Visit(_qil);
		if (this[XmlILOptimization.TailCall])
		{
			TailCallAnalyzer.Analyze(qilExpression);
		}
		return qilExpression;
	}

	protected override QilNode Visit(QilNode nd)
	{
		if (nd != null && this[XmlILOptimization.EliminateNamespaceDecl])
		{
			switch (nd.NodeType)
			{
			case QilNodeType.QilExpression:
				_nmspAnalyzer.Analyze(((QilExpression)nd).Root, defaultNmspInScope: true);
				break;
			case QilNodeType.ElementCtor:
				if (!XmlILConstructInfo.Read(nd).IsNamespaceInScope)
				{
					_nmspAnalyzer.Analyze(nd, defaultNmspInScope: false);
				}
				break;
			case QilNodeType.DocumentCtor:
				_nmspAnalyzer.Analyze(nd, defaultNmspInScope: true);
				break;
			}
		}
		return base.Visit(nd);
	}

	protected override QilNode VisitReference(QilNode oldNode)
	{
		QilNode qilNode = _subs.FindReplacement(oldNode);
		if (qilNode == null)
		{
			qilNode = oldNode;
		}
		if (this[XmlILOptimization.EliminateLiteralVariables] && qilNode != null && (qilNode.NodeType == QilNodeType.Let || qilNode.NodeType == QilNodeType.For))
		{
			QilNode binding = ((QilIterator)oldNode).Binding;
			if (IsLiteral(binding))
			{
				return Replace(XmlILOptimization.EliminateLiteralVariables, qilNode, binding.ShallowClone(f));
			}
		}
		if (this[XmlILOptimization.EliminateUnusedGlobals] && IsGlobalValue(qilNode))
		{
			OptimizerPatterns.Write(qilNode).AddPattern(OptimizerPatternName.IsReferenced);
		}
		return base.VisitReference(qilNode);
	}

	private bool AllowReplace(XmlILOptimization pattern, QilNode original)
	{
		return base.AllowReplace((int)pattern, original);
	}

	private QilNode Replace(XmlILOptimization pattern, QilNode original, QilNode replacement)
	{
		return base.Replace((int)pattern, original, replacement);
	}

	[return: NotNullIfNotNull("node")]
	protected override QilNode NoReplace(QilNode node)
	{
		if (node != null)
		{
			QilNodeType nodeType = node.NodeType;
			if (nodeType <= QilNodeType.Invoke)
			{
				if ((uint)(nodeType - 12) <= 1u || (nodeType == QilNodeType.Invoke && ((QilInvoke)node).Function.MaybeSideEffects))
				{
					goto IL_002c;
				}
			}
			else if (nodeType == QilNodeType.XsltInvokeLateBound || (nodeType == QilNodeType.XsltInvokeEarlyBound && ((QilInvokeEarlyBound)node).Name.NamespaceUri.Length != 0))
			{
				goto IL_002c;
			}
			int num = 0;
			while (num < node.Count)
			{
				if (node[num] == null || !OptimizerPatterns.Read(node[num]).MatchesPattern(OptimizerPatternName.MaybeSideEffects))
				{
					num++;
					continue;
				}
				goto IL_002c;
			}
		}
		goto IL_0095;
		IL_002c:
		OptimizerPatterns.Write(node).AddPattern(OptimizerPatternName.MaybeSideEffects);
		goto IL_0095;
		IL_0095:
		return node;
	}

	protected override void RecalculateType(QilNode node, XmlQueryType oldType)
	{
		if (node.NodeType != QilNodeType.Let || !_qil.GlobalVariableList.Contains(node))
		{
			base.RecalculateType(node, oldType);
		}
	}

	protected override QilNode VisitQilExpression(QilExpression local0)
	{
		QilNode qilNode = local0[0];
		if (this[XmlILOptimization.EliminateUnusedGlobals] && AllowReplace(XmlILOptimization.EliminateUnusedGlobals, local0))
		{
			EliminateUnusedGlobals(local0.GlobalVariableList);
			EliminateUnusedGlobals(local0.GlobalParameterList);
			EliminateUnusedGlobals(local0.FunctionList);
		}
		if (this[XmlILOptimization.AnnotateConstruction] && AllowReplace(XmlILOptimization.AnnotateConstruction, local0))
		{
			foreach (QilFunction function in local0.FunctionList)
			{
				if (IsConstructedExpression(function.Definition))
				{
					function.Definition = _contentAnalyzer.Analyze(function, function.Definition);
				}
			}
			local0.Root = _contentAnalyzer.Analyze(null, local0.Root);
			XmlILConstructInfo.Write(local0.Root).PushToWriterLast = true;
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitOptimizeBarrier(QilUnary local0)
	{
		QilNode ndSrc = local0[0];
		if (this[XmlILOptimization.AnnotateBarrier] && AllowReplace(XmlILOptimization.AnnotateBarrier, local0))
		{
			OptimizerPatterns.Inherit(ndSrc, local0, OptimizerPatternName.IsDocOrderDistinct);
			OptimizerPatterns.Inherit(ndSrc, local0, OptimizerPatternName.SameDepth);
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitDataSource(QilDataSource local0)
	{
		QilNode qilNode = local0[0];
		QilNode qilNode2 = local0[1];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.FoldNone] && (object)qilNode2.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode2)));
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitNop(QilUnary local0)
	{
		QilNode replacement = local0[0];
		if (this[XmlILOptimization.EliminateNop] && AllowReplace(XmlILOptimization.EliminateNop, local0))
		{
			return Replace(XmlILOptimization.EliminateNop, local0, replacement);
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitError(QilUnary local0)
	{
		QilNode qilNode = local0[0];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitWarning(QilUnary local0)
	{
		QilNode qilNode = local0[0];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitLet(QilIterator local0)
	{
		QilNode ndSrc = local0[0];
		if (local0.XmlType.IsSingleton && !IsGlobalVariable(local0) && this[XmlILOptimization.NormalizeSingletonLet] && AllowReplace(XmlILOptimization.NormalizeSingletonLet, local0))
		{
			local0.NodeType = QilNodeType.For;
			VisitFor(local0);
		}
		if (this[XmlILOptimization.AnnotateLet] && AllowReplace(XmlILOptimization.AnnotateLet, local0))
		{
			OptimizerPatterns.Inherit(ndSrc, local0, OptimizerPatternName.Step);
			OptimizerPatterns.Inherit(ndSrc, local0, OptimizerPatternName.IsDocOrderDistinct);
			OptimizerPatterns.Inherit(ndSrc, local0, OptimizerPatternName.SameDepth);
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitPositionOf(QilUnary local0)
	{
		QilNode qilNode = local0[0];
		if (this[XmlILOptimization.EliminatePositionOf] && qilNode.NodeType != QilNodeType.For && AllowReplace(XmlILOptimization.EliminatePositionOf, local0))
		{
			return Replace(XmlILOptimization.EliminatePositionOf, local0, VisitLiteralInt32(f.LiteralInt32(1)));
		}
		if (this[XmlILOptimization.EliminatePositionOf] && qilNode.NodeType == QilNodeType.For)
		{
			QilNode qilNode2 = qilNode[0];
			if (qilNode2.XmlType.IsSingleton && AllowReplace(XmlILOptimization.EliminatePositionOf, local0))
			{
				return Replace(XmlILOptimization.EliminatePositionOf, local0, VisitLiteralInt32(f.LiteralInt32(1)));
			}
		}
		if (this[XmlILOptimization.AnnotatePositionalIterator] && AllowReplace(XmlILOptimization.AnnotatePositionalIterator, local0))
		{
			OptimizerPatterns.Write(qilNode).AddPattern(OptimizerPatternName.IsPositional);
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitAnd(QilBinary local0)
	{
		QilNode qilNode = local0[0];
		QilNode qilNode2 = local0[1];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.FoldNone] && (object)qilNode2.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode2)));
		}
		if (this[XmlILOptimization.EliminateAnd] && qilNode.NodeType == QilNodeType.True && AllowReplace(XmlILOptimization.EliminateAnd, local0))
		{
			return Replace(XmlILOptimization.EliminateAnd, local0, qilNode2);
		}
		if (this[XmlILOptimization.EliminateAnd] && qilNode.NodeType == QilNodeType.False && AllowReplace(XmlILOptimization.EliminateAnd, local0))
		{
			return Replace(XmlILOptimization.EliminateAnd, local0, qilNode);
		}
		if (this[XmlILOptimization.EliminateAnd] && qilNode2.NodeType == QilNodeType.True && AllowReplace(XmlILOptimization.EliminateAnd, local0))
		{
			return Replace(XmlILOptimization.EliminateAnd, local0, qilNode);
		}
		if (this[XmlILOptimization.EliminateAnd] && qilNode2.NodeType == QilNodeType.False && AllowReplace(XmlILOptimization.EliminateAnd, local0))
		{
			return Replace(XmlILOptimization.EliminateAnd, local0, qilNode2);
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitOr(QilBinary local0)
	{
		QilNode qilNode = local0[0];
		QilNode qilNode2 = local0[1];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.FoldNone] && (object)qilNode2.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode2)));
		}
		if (this[XmlILOptimization.EliminateOr] && qilNode.NodeType == QilNodeType.True && AllowReplace(XmlILOptimization.EliminateOr, local0))
		{
			return Replace(XmlILOptimization.EliminateOr, local0, qilNode);
		}
		if (this[XmlILOptimization.EliminateOr] && qilNode.NodeType == QilNodeType.False && AllowReplace(XmlILOptimization.EliminateOr, local0))
		{
			return Replace(XmlILOptimization.EliminateOr, local0, qilNode2);
		}
		if (this[XmlILOptimization.EliminateOr] && qilNode2.NodeType == QilNodeType.True && AllowReplace(XmlILOptimization.EliminateOr, local0))
		{
			return Replace(XmlILOptimization.EliminateOr, local0, qilNode2);
		}
		if (this[XmlILOptimization.EliminateOr] && qilNode2.NodeType == QilNodeType.False && AllowReplace(XmlILOptimization.EliminateOr, local0))
		{
			return Replace(XmlILOptimization.EliminateOr, local0, qilNode);
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitNot(QilUnary local0)
	{
		QilNode qilNode = local0[0];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.EliminateNot] && qilNode.NodeType == QilNodeType.True && AllowReplace(XmlILOptimization.EliminateNot, local0))
		{
			return Replace(XmlILOptimization.EliminateNot, local0, VisitFalse(f.False()));
		}
		if (this[XmlILOptimization.EliminateNot] && qilNode.NodeType == QilNodeType.False && AllowReplace(XmlILOptimization.EliminateNot, local0))
		{
			return Replace(XmlILOptimization.EliminateNot, local0, VisitTrue(f.True()));
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitConditional(QilTernary local0)
	{
		QilNode qilNode = local0[0];
		QilNode qilNode2 = local0[1];
		QilNode qilNode3 = local0[2];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.EliminateConditional] && qilNode.NodeType == QilNodeType.True && AllowReplace(XmlILOptimization.EliminateConditional, local0))
		{
			return Replace(XmlILOptimization.EliminateConditional, local0, qilNode2);
		}
		if (this[XmlILOptimization.EliminateConditional] && qilNode.NodeType == QilNodeType.False && AllowReplace(XmlILOptimization.EliminateConditional, local0))
		{
			return Replace(XmlILOptimization.EliminateConditional, local0, qilNode3);
		}
		if (this[XmlILOptimization.EliminateConditional] && qilNode2.NodeType == QilNodeType.True && qilNode3.NodeType == QilNodeType.False && AllowReplace(XmlILOptimization.EliminateConditional, local0))
		{
			return Replace(XmlILOptimization.EliminateConditional, local0, qilNode);
		}
		if (this[XmlILOptimization.EliminateConditional] && qilNode2.NodeType == QilNodeType.False && qilNode3.NodeType == QilNodeType.True && AllowReplace(XmlILOptimization.EliminateConditional, local0))
		{
			return Replace(XmlILOptimization.EliminateConditional, local0, VisitNot(f.Not(qilNode)));
		}
		if (this[XmlILOptimization.FoldConditionalNot] && qilNode.NodeType == QilNodeType.Not)
		{
			QilNode left = qilNode[0];
			if (AllowReplace(XmlILOptimization.FoldConditionalNot, local0))
			{
				return Replace(XmlILOptimization.FoldConditionalNot, local0, VisitConditional(f.Conditional(left, qilNode3, qilNode2)));
			}
		}
		if (this[XmlILOptimization.NormalizeConditionalText] && qilNode2.NodeType == QilNodeType.TextCtor)
		{
			QilNode center = qilNode2[0];
			if (qilNode3.NodeType == QilNodeType.TextCtor)
			{
				QilNode right = qilNode3[0];
				if (AllowReplace(XmlILOptimization.NormalizeConditionalText, local0))
				{
					return Replace(XmlILOptimization.NormalizeConditionalText, local0, VisitTextCtor(f.TextCtor(VisitConditional(f.Conditional(qilNode, center, right)))));
				}
			}
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitChoice(QilChoice local0)
	{
		QilNode qilNode = local0[0];
		QilNode qilNode2 = local0[1];
		if (this[XmlILOptimization.AnnotateConstruction] && AllowReplace(XmlILOptimization.AnnotateConstruction, local0))
		{
			_contentAnalyzer.Analyze(local0, null);
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitLength(QilUnary local0)
	{
		QilNode qilNode = local0[0];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.EliminateLength] && qilNode.NodeType == QilNodeType.Sequence && qilNode.Count == 0 && AllowReplace(XmlILOptimization.EliminateLength, local0))
		{
			return Replace(XmlILOptimization.EliminateLength, local0, VisitLiteralInt32(f.LiteralInt32(0)));
		}
		if (this[XmlILOptimization.EliminateLength] && qilNode.XmlType.IsSingleton && !OptimizerPatterns.Read(qilNode).MatchesPattern(OptimizerPatternName.MaybeSideEffects) && AllowReplace(XmlILOptimization.EliminateLength, local0))
		{
			return Replace(XmlILOptimization.EliminateLength, local0, VisitLiteralInt32(f.LiteralInt32(1)));
		}
		if (this[XmlILOptimization.IntroducePrecedingDod] && !IsDocOrderDistinct(qilNode) && (IsStepPattern(qilNode, QilNodeType.XPathPreceding) || IsStepPattern(qilNode, QilNodeType.PrecedingSibling)) && AllowReplace(XmlILOptimization.IntroducePrecedingDod, local0))
		{
			return Replace(XmlILOptimization.IntroducePrecedingDod, local0, VisitLength(f.Length(VisitDocOrderDistinct(f.DocOrderDistinct(qilNode)))));
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitSequence(QilList local0)
	{
		if (local0.Count == 1 && this[XmlILOptimization.EliminateSequence] && AllowReplace(XmlILOptimization.EliminateSequence, local0))
		{
			return Replace(XmlILOptimization.EliminateSequence, local0, local0[0]);
		}
		if (HasNestedSequence(local0) && this[XmlILOptimization.NormalizeNestedSequences] && AllowReplace(XmlILOptimization.NormalizeNestedSequences, local0))
		{
			QilNode qilNode = VisitSequence(f.Sequence());
			foreach (QilNode item in local0)
			{
				if (item.NodeType == QilNodeType.Sequence)
				{
					qilNode.Add((IList<QilNode>)item);
				}
				else
				{
					qilNode.Add(item);
				}
			}
			qilNode = VisitSequence((QilList)qilNode);
			return Replace(XmlILOptimization.NormalizeNestedSequences, local0, qilNode);
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitUnion(QilBinary local0)
	{
		QilNode qilNode = local0[0];
		QilNode qilNode2 = local0[1];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.FoldNone] && (object)qilNode2.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode2)));
		}
		if (this[XmlILOptimization.EliminateUnion] && qilNode2 == qilNode && AllowReplace(XmlILOptimization.EliminateUnion, local0))
		{
			return Replace(XmlILOptimization.EliminateUnion, local0, VisitDocOrderDistinct(f.DocOrderDistinct(qilNode)));
		}
		if (this[XmlILOptimization.EliminateUnion] && qilNode.NodeType == QilNodeType.Sequence && qilNode.Count == 0 && AllowReplace(XmlILOptimization.EliminateUnion, local0))
		{
			return Replace(XmlILOptimization.EliminateUnion, local0, VisitDocOrderDistinct(f.DocOrderDistinct(qilNode2)));
		}
		if (this[XmlILOptimization.EliminateUnion] && qilNode2.NodeType == QilNodeType.Sequence && qilNode2.Count == 0 && AllowReplace(XmlILOptimization.EliminateUnion, local0))
		{
			return Replace(XmlILOptimization.EliminateUnion, local0, VisitDocOrderDistinct(f.DocOrderDistinct(qilNode)));
		}
		if (this[XmlILOptimization.EliminateUnion] && qilNode.NodeType == QilNodeType.XmlContext && qilNode2.NodeType == QilNodeType.XmlContext && AllowReplace(XmlILOptimization.EliminateUnion, local0))
		{
			return Replace(XmlILOptimization.EliminateUnion, local0, qilNode);
		}
		if (this[XmlILOptimization.NormalizeUnion] && (!IsDocOrderDistinct(qilNode) || !IsDocOrderDistinct(qilNode2)) && AllowReplace(XmlILOptimization.NormalizeUnion, local0))
		{
			return Replace(XmlILOptimization.NormalizeUnion, local0, VisitUnion(f.Union(VisitDocOrderDistinct(f.DocOrderDistinct(qilNode)), VisitDocOrderDistinct(f.DocOrderDistinct(qilNode2)))));
		}
		if (this[XmlILOptimization.AnnotateUnion] && AllowReplace(XmlILOptimization.AnnotateUnion, local0))
		{
			OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.IsDocOrderDistinct);
		}
		if (this[XmlILOptimization.AnnotateUnionContent] && (IsStepPattern(qilNode, QilNodeType.Content) || IsStepPattern(qilNode, QilNodeType.Union)) && (IsStepPattern(qilNode2, QilNodeType.Content) || IsStepPattern(qilNode2, QilNodeType.Union)) && OptimizerPatterns.Read(qilNode).GetArgument(OptimizerPatternArgument.StepInput) == OptimizerPatterns.Read(qilNode2).GetArgument(OptimizerPatternArgument.StepInput) && AllowReplace(XmlILOptimization.AnnotateUnionContent, local0))
		{
			AddStepPattern(local0, (QilNode)OptimizerPatterns.Read(qilNode).GetArgument(OptimizerPatternArgument.StepInput));
			OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.SameDepth);
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitIntersection(QilBinary local0)
	{
		QilNode qilNode = local0[0];
		QilNode qilNode2 = local0[1];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.FoldNone] && (object)qilNode2.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode2)));
		}
		if (this[XmlILOptimization.EliminateIntersection] && qilNode2 == qilNode && AllowReplace(XmlILOptimization.EliminateIntersection, local0))
		{
			return Replace(XmlILOptimization.EliminateIntersection, local0, VisitDocOrderDistinct(f.DocOrderDistinct(qilNode)));
		}
		if (this[XmlILOptimization.EliminateIntersection] && qilNode.NodeType == QilNodeType.Sequence && qilNode.Count == 0 && AllowReplace(XmlILOptimization.EliminateIntersection, local0))
		{
			return Replace(XmlILOptimization.EliminateIntersection, local0, qilNode);
		}
		if (this[XmlILOptimization.EliminateIntersection] && qilNode2.NodeType == QilNodeType.Sequence && qilNode2.Count == 0 && AllowReplace(XmlILOptimization.EliminateIntersection, local0))
		{
			return Replace(XmlILOptimization.EliminateIntersection, local0, qilNode2);
		}
		if (this[XmlILOptimization.EliminateIntersection] && qilNode.NodeType == QilNodeType.XmlContext && qilNode2.NodeType == QilNodeType.XmlContext && AllowReplace(XmlILOptimization.EliminateIntersection, local0))
		{
			return Replace(XmlILOptimization.EliminateIntersection, local0, qilNode);
		}
		if (this[XmlILOptimization.NormalizeIntersect] && (!IsDocOrderDistinct(qilNode) || !IsDocOrderDistinct(qilNode2)) && AllowReplace(XmlILOptimization.NormalizeIntersect, local0))
		{
			return Replace(XmlILOptimization.NormalizeIntersect, local0, VisitIntersection(f.Intersection(VisitDocOrderDistinct(f.DocOrderDistinct(qilNode)), VisitDocOrderDistinct(f.DocOrderDistinct(qilNode2)))));
		}
		if (this[XmlILOptimization.AnnotateIntersect] && AllowReplace(XmlILOptimization.AnnotateIntersect, local0))
		{
			OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.IsDocOrderDistinct);
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitDifference(QilBinary local0)
	{
		QilNode qilNode = local0[0];
		QilNode qilNode2 = local0[1];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.FoldNone] && (object)qilNode2.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode2)));
		}
		if (this[XmlILOptimization.EliminateDifference] && qilNode.NodeType == QilNodeType.Sequence && qilNode.Count == 0 && AllowReplace(XmlILOptimization.EliminateDifference, local0))
		{
			return Replace(XmlILOptimization.EliminateDifference, local0, qilNode);
		}
		if (this[XmlILOptimization.EliminateDifference] && qilNode2.NodeType == QilNodeType.Sequence && qilNode2.Count == 0 && AllowReplace(XmlILOptimization.EliminateDifference, local0))
		{
			return Replace(XmlILOptimization.EliminateDifference, local0, VisitDocOrderDistinct(f.DocOrderDistinct(qilNode)));
		}
		if (this[XmlILOptimization.EliminateDifference] && qilNode2 == qilNode && AllowReplace(XmlILOptimization.EliminateDifference, local0))
		{
			return Replace(XmlILOptimization.EliminateDifference, local0, VisitSequence(f.Sequence()));
		}
		if (this[XmlILOptimization.EliminateDifference] && qilNode.NodeType == QilNodeType.XmlContext && qilNode2.NodeType == QilNodeType.XmlContext && AllowReplace(XmlILOptimization.EliminateDifference, local0))
		{
			return Replace(XmlILOptimization.EliminateDifference, local0, VisitSequence(f.Sequence()));
		}
		if (this[XmlILOptimization.NormalizeDifference] && (!IsDocOrderDistinct(qilNode) || !IsDocOrderDistinct(qilNode2)) && AllowReplace(XmlILOptimization.NormalizeDifference, local0))
		{
			return Replace(XmlILOptimization.NormalizeDifference, local0, VisitDifference(f.Difference(VisitDocOrderDistinct(f.DocOrderDistinct(qilNode)), VisitDocOrderDistinct(f.DocOrderDistinct(qilNode2)))));
		}
		if (this[XmlILOptimization.AnnotateDifference] && AllowReplace(XmlILOptimization.AnnotateDifference, local0))
		{
			OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.IsDocOrderDistinct);
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitAverage(QilUnary local0)
	{
		QilNode qilNode = local0[0];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.EliminateAverage] && qilNode.XmlType.Cardinality == XmlQueryCardinality.Zero && AllowReplace(XmlILOptimization.EliminateAverage, local0))
		{
			return Replace(XmlILOptimization.EliminateAverage, local0, VisitNop(f.Nop(qilNode)));
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitSum(QilUnary local0)
	{
		QilNode qilNode = local0[0];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.EliminateSum] && qilNode.XmlType.Cardinality == XmlQueryCardinality.Zero && AllowReplace(XmlILOptimization.EliminateSum, local0))
		{
			return Replace(XmlILOptimization.EliminateSum, local0, VisitNop(f.Nop(qilNode)));
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitMinimum(QilUnary local0)
	{
		QilNode qilNode = local0[0];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.EliminateMinimum] && qilNode.XmlType.Cardinality == XmlQueryCardinality.Zero && AllowReplace(XmlILOptimization.EliminateMinimum, local0))
		{
			return Replace(XmlILOptimization.EliminateMinimum, local0, VisitNop(f.Nop(qilNode)));
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitMaximum(QilUnary local0)
	{
		QilNode qilNode = local0[0];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.EliminateMaximum] && qilNode.XmlType.Cardinality == XmlQueryCardinality.Zero && AllowReplace(XmlILOptimization.EliminateMaximum, local0))
		{
			return Replace(XmlILOptimization.EliminateMaximum, local0, VisitNop(f.Nop(qilNode)));
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitNegate(QilUnary local0)
	{
		QilNode qilNode = local0[0];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.EliminateNegate] && qilNode.NodeType == QilNodeType.LiteralDecimal)
		{
			decimal num = (decimal)((QilLiteral)qilNode).Value;
			if (AllowReplace(XmlILOptimization.EliminateNegate, local0))
			{
				return Replace(XmlILOptimization.EliminateNegate, local0, VisitLiteralDecimal(f.LiteralDecimal(-num)));
			}
		}
		if (this[XmlILOptimization.EliminateNegate] && qilNode.NodeType == QilNodeType.LiteralDouble)
		{
			double num2 = (double)((QilLiteral)qilNode).Value;
			if (AllowReplace(XmlILOptimization.EliminateNegate, local0))
			{
				return Replace(XmlILOptimization.EliminateNegate, local0, VisitLiteralDouble(f.LiteralDouble(0.0 - num2)));
			}
		}
		if (this[XmlILOptimization.EliminateNegate] && qilNode.NodeType == QilNodeType.LiteralInt32)
		{
			int num3 = (int)((QilLiteral)qilNode).Value;
			if (AllowReplace(XmlILOptimization.EliminateNegate, local0))
			{
				return Replace(XmlILOptimization.EliminateNegate, local0, VisitLiteralInt32(f.LiteralInt32(-num3)));
			}
		}
		if (this[XmlILOptimization.EliminateNegate] && qilNode.NodeType == QilNodeType.LiteralInt64)
		{
			long num4 = (long)((QilLiteral)qilNode).Value;
			if (AllowReplace(XmlILOptimization.EliminateNegate, local0))
			{
				return Replace(XmlILOptimization.EliminateNegate, local0, VisitLiteralInt64(f.LiteralInt64(-num4)));
			}
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitAdd(QilBinary local0)
	{
		QilNode qilNode = local0[0];
		QilNode qilNode2 = local0[1];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.FoldNone] && (object)qilNode2.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode2)));
		}
		if (this[XmlILOptimization.EliminateAdd] && IsLiteral(qilNode) && IsLiteral(qilNode2) && CanFoldArithmetic(QilNodeType.Add, (QilLiteral)qilNode, (QilLiteral)qilNode2) && AllowReplace(XmlILOptimization.EliminateAdd, local0))
		{
			return Replace(XmlILOptimization.EliminateAdd, local0, FoldArithmetic(QilNodeType.Add, (QilLiteral)qilNode, (QilLiteral)qilNode2));
		}
		if (this[XmlILOptimization.NormalizeAddLiteral] && IsLiteral(qilNode) && !IsLiteral(qilNode2) && AllowReplace(XmlILOptimization.NormalizeAddLiteral, local0))
		{
			return Replace(XmlILOptimization.NormalizeAddLiteral, local0, VisitAdd(f.Add(qilNode2, qilNode)));
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitSubtract(QilBinary local0)
	{
		QilNode qilNode = local0[0];
		QilNode qilNode2 = local0[1];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.FoldNone] && (object)qilNode2.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode2)));
		}
		if (this[XmlILOptimization.EliminateSubtract] && IsLiteral(qilNode) && IsLiteral(qilNode2) && CanFoldArithmetic(QilNodeType.Subtract, (QilLiteral)qilNode, (QilLiteral)qilNode2) && AllowReplace(XmlILOptimization.EliminateSubtract, local0))
		{
			return Replace(XmlILOptimization.EliminateSubtract, local0, FoldArithmetic(QilNodeType.Subtract, (QilLiteral)qilNode, (QilLiteral)qilNode2));
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitMultiply(QilBinary local0)
	{
		QilNode qilNode = local0[0];
		QilNode qilNode2 = local0[1];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.FoldNone] && (object)qilNode2.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode2)));
		}
		if (this[XmlILOptimization.EliminateMultiply] && IsLiteral(qilNode) && IsLiteral(qilNode2) && CanFoldArithmetic(QilNodeType.Multiply, (QilLiteral)qilNode, (QilLiteral)qilNode2) && AllowReplace(XmlILOptimization.EliminateMultiply, local0))
		{
			return Replace(XmlILOptimization.EliminateMultiply, local0, FoldArithmetic(QilNodeType.Multiply, (QilLiteral)qilNode, (QilLiteral)qilNode2));
		}
		if (this[XmlILOptimization.NormalizeMultiplyLiteral] && IsLiteral(qilNode) && !IsLiteral(qilNode2) && AllowReplace(XmlILOptimization.NormalizeMultiplyLiteral, local0))
		{
			return Replace(XmlILOptimization.NormalizeMultiplyLiteral, local0, VisitMultiply(f.Multiply(qilNode2, qilNode)));
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitDivide(QilBinary local0)
	{
		QilNode qilNode = local0[0];
		QilNode qilNode2 = local0[1];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.FoldNone] && (object)qilNode2.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode2)));
		}
		if (this[XmlILOptimization.EliminateDivide] && IsLiteral(qilNode) && IsLiteral(qilNode2) && CanFoldArithmetic(QilNodeType.Divide, (QilLiteral)qilNode, (QilLiteral)qilNode2) && AllowReplace(XmlILOptimization.EliminateDivide, local0))
		{
			return Replace(XmlILOptimization.EliminateDivide, local0, FoldArithmetic(QilNodeType.Divide, (QilLiteral)qilNode, (QilLiteral)qilNode2));
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitModulo(QilBinary local0)
	{
		QilNode qilNode = local0[0];
		QilNode qilNode2 = local0[1];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.FoldNone] && (object)qilNode2.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode2)));
		}
		if (this[XmlILOptimization.EliminateModulo] && IsLiteral(qilNode) && IsLiteral(qilNode2) && CanFoldArithmetic(QilNodeType.Modulo, (QilLiteral)qilNode, (QilLiteral)qilNode2) && AllowReplace(XmlILOptimization.EliminateModulo, local0))
		{
			return Replace(XmlILOptimization.EliminateModulo, local0, FoldArithmetic(QilNodeType.Modulo, (QilLiteral)qilNode, (QilLiteral)qilNode2));
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitStrLength(QilUnary local0)
	{
		QilNode qilNode = local0[0];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.EliminateStrLength] && qilNode.NodeType == QilNodeType.LiteralString)
		{
			string text = (string)((QilLiteral)qilNode).Value;
			if (AllowReplace(XmlILOptimization.EliminateStrLength, local0))
			{
				return Replace(XmlILOptimization.EliminateStrLength, local0, VisitLiteralInt32(f.LiteralInt32(text.Length)));
			}
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitStrConcat(QilStrConcat local0)
	{
		QilNode qilNode = local0[0];
		QilNode qilNode2 = local0[1];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.FoldNone] && (object)qilNode2.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode2)));
		}
		if (qilNode2.XmlType.IsSingleton && this[XmlILOptimization.EliminateStrConcatSingle] && AllowReplace(XmlILOptimization.EliminateStrConcatSingle, local0))
		{
			return Replace(XmlILOptimization.EliminateStrConcatSingle, local0, VisitNop(f.Nop(qilNode2)));
		}
		if (this[XmlILOptimization.EliminateStrConcat] && qilNode.NodeType == QilNodeType.LiteralString)
		{
			string delimiter = (string)((QilLiteral)qilNode).Value;
			if (qilNode2.NodeType == QilNodeType.Sequence && AreLiteralArgs(qilNode2) && AllowReplace(XmlILOptimization.EliminateStrConcat, local0))
			{
				StringConcat stringConcat = default(StringConcat);
				stringConcat.Delimiter = delimiter;
				foreach (QilLiteral item in qilNode2)
				{
					stringConcat.Concat(item);
				}
				return Replace(XmlILOptimization.EliminateStrConcat, local0, VisitLiteralString(f.LiteralString(stringConcat.GetResult())));
			}
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitStrParseQName(QilBinary local0)
	{
		QilNode qilNode = local0[0];
		QilNode qilNode2 = local0[1];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.FoldNone] && (object)qilNode2.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode2)));
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitNe(QilBinary local0)
	{
		QilNode qilNode = local0[0];
		QilNode qilNode2 = local0[1];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.FoldNone] && (object)qilNode2.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode2)));
		}
		if (this[XmlILOptimization.EliminateNe] && IsLiteral(qilNode) && IsLiteral(qilNode2) && AllowReplace(XmlILOptimization.EliminateNe, local0))
		{
			return Replace(XmlILOptimization.EliminateNe, local0, FoldComparison(QilNodeType.Ne, qilNode, qilNode2));
		}
		if (this[XmlILOptimization.NormalizeNeLiteral] && IsLiteral(qilNode) && !IsLiteral(qilNode2) && AllowReplace(XmlILOptimization.NormalizeNeLiteral, local0))
		{
			return Replace(XmlILOptimization.NormalizeNeLiteral, local0, VisitNe(f.Ne(qilNode2, qilNode)));
		}
		if (this[XmlILOptimization.NormalizeXsltConvertNe] && qilNode.NodeType == QilNodeType.XsltConvert)
		{
			QilNode qilNode3 = qilNode[0];
			QilNode qilNode4 = qilNode[1];
			if (qilNode4.NodeType == QilNodeType.LiteralType)
			{
				XmlQueryType typ = (XmlQueryType)((QilLiteral)qilNode4).Value;
				if (IsPrimitiveNumeric(qilNode3.XmlType) && IsPrimitiveNumeric(typ) && IsLiteral(qilNode2) && CanFoldXsltConvertNonLossy(qilNode2, qilNode3.XmlType) && AllowReplace(XmlILOptimization.NormalizeXsltConvertNe, local0))
				{
					return Replace(XmlILOptimization.NormalizeXsltConvertNe, local0, VisitNe(f.Ne(qilNode3, FoldXsltConvert(qilNode2, qilNode3.XmlType))));
				}
			}
		}
		if (this[XmlILOptimization.NormalizeIdNe] && qilNode.NodeType == QilNodeType.XsltGenerateId)
		{
			QilNode qilNode5 = qilNode[0];
			if (qilNode5.XmlType.IsSingleton && qilNode2.NodeType == QilNodeType.XsltGenerateId)
			{
				QilNode qilNode6 = qilNode2[0];
				if (qilNode6.XmlType.IsSingleton && AllowReplace(XmlILOptimization.NormalizeIdNe, local0))
				{
					return Replace(XmlILOptimization.NormalizeIdNe, local0, VisitNot(f.Not(VisitIs(f.Is(qilNode5, qilNode6)))));
				}
			}
		}
		if (this[XmlILOptimization.NormalizeLengthNe] && qilNode.NodeType == QilNodeType.Length)
		{
			QilNode child = qilNode[0];
			if (qilNode2.NodeType == QilNodeType.LiteralInt32 && (int)((QilLiteral)qilNode2).Value == 0 && AllowReplace(XmlILOptimization.NormalizeLengthNe, local0))
			{
				return Replace(XmlILOptimization.NormalizeLengthNe, local0, VisitNot(f.Not(VisitIsEmpty(f.IsEmpty(child)))));
			}
		}
		if (this[XmlILOptimization.AnnotateMaxLengthNe] && qilNode.NodeType == QilNodeType.Length && qilNode2.NodeType == QilNodeType.LiteralInt32)
		{
			int num = (int)((QilLiteral)qilNode2).Value;
			if (AllowReplace(XmlILOptimization.AnnotateMaxLengthNe, local0))
			{
				OptimizerPatterns.Write(qilNode).AddPattern(OptimizerPatternName.MaxPosition);
				OptimizerPatterns.Write(qilNode).AddArgument(OptimizerPatternArgument.ElementQName, num);
			}
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitEq(QilBinary local0)
	{
		QilNode qilNode = local0[0];
		QilNode qilNode2 = local0[1];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.FoldNone] && (object)qilNode2.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode2)));
		}
		if (this[XmlILOptimization.EliminateEq] && IsLiteral(qilNode) && IsLiteral(qilNode2) && AllowReplace(XmlILOptimization.EliminateEq, local0))
		{
			return Replace(XmlILOptimization.EliminateEq, local0, FoldComparison(QilNodeType.Eq, qilNode, qilNode2));
		}
		if (this[XmlILOptimization.NormalizeEqLiteral] && IsLiteral(qilNode) && !IsLiteral(qilNode2) && AllowReplace(XmlILOptimization.NormalizeEqLiteral, local0))
		{
			return Replace(XmlILOptimization.NormalizeEqLiteral, local0, VisitEq(f.Eq(qilNode2, qilNode)));
		}
		if (this[XmlILOptimization.NormalizeXsltConvertEq] && qilNode.NodeType == QilNodeType.XsltConvert)
		{
			QilNode qilNode3 = qilNode[0];
			QilNode qilNode4 = qilNode[1];
			if (qilNode4.NodeType == QilNodeType.LiteralType)
			{
				XmlQueryType typ = (XmlQueryType)((QilLiteral)qilNode4).Value;
				if (IsPrimitiveNumeric(qilNode3.XmlType) && IsPrimitiveNumeric(typ) && IsLiteral(qilNode2) && CanFoldXsltConvertNonLossy(qilNode2, qilNode3.XmlType) && AllowReplace(XmlILOptimization.NormalizeXsltConvertEq, local0))
				{
					return Replace(XmlILOptimization.NormalizeXsltConvertEq, local0, VisitEq(f.Eq(qilNode3, FoldXsltConvert(qilNode2, qilNode3.XmlType))));
				}
			}
		}
		if (this[XmlILOptimization.NormalizeAddEq] && qilNode.NodeType == QilNodeType.Add)
		{
			QilNode left = qilNode[0];
			QilNode qilNode5 = qilNode[1];
			if (IsLiteral(qilNode5) && IsLiteral(qilNode2) && CanFoldArithmetic(QilNodeType.Subtract, (QilLiteral)qilNode2, (QilLiteral)qilNode5) && AllowReplace(XmlILOptimization.NormalizeAddEq, local0))
			{
				return Replace(XmlILOptimization.NormalizeAddEq, local0, VisitEq(f.Eq(left, FoldArithmetic(QilNodeType.Subtract, (QilLiteral)qilNode2, (QilLiteral)qilNode5))));
			}
		}
		if (this[XmlILOptimization.NormalizeIdEq] && qilNode.NodeType == QilNodeType.XsltGenerateId)
		{
			QilNode qilNode6 = qilNode[0];
			if (qilNode6.XmlType.IsSingleton && qilNode2.NodeType == QilNodeType.XsltGenerateId)
			{
				QilNode qilNode7 = qilNode2[0];
				if (qilNode7.XmlType.IsSingleton && AllowReplace(XmlILOptimization.NormalizeIdEq, local0))
				{
					return Replace(XmlILOptimization.NormalizeIdEq, local0, VisitIs(f.Is(qilNode6, qilNode7)));
				}
			}
		}
		if (this[XmlILOptimization.NormalizeIdEq] && qilNode.NodeType == QilNodeType.XsltGenerateId)
		{
			QilNode qilNode8 = qilNode[0];
			if (qilNode8.XmlType.IsSingleton && qilNode2.NodeType == QilNodeType.StrConcat)
			{
				QilNode qilNode9 = qilNode2[1];
				if (qilNode9.NodeType == QilNodeType.Loop)
				{
					QilNode qilNode10 = qilNode9[0];
					QilNode qilNode11 = qilNode9[1];
					if (qilNode10.NodeType == QilNodeType.For)
					{
						QilNode qilNode12 = qilNode10[0];
						if (!qilNode12.XmlType.MaybeMany && qilNode11.NodeType == QilNodeType.XsltGenerateId)
						{
							QilNode qilNode13 = qilNode11[0];
							if (qilNode13 == qilNode10 && AllowReplace(XmlILOptimization.NormalizeIdEq, local0))
							{
								QilNode qilNode14 = VisitFor(f.For(qilNode12));
								return Replace(XmlILOptimization.NormalizeIdEq, local0, VisitNot(f.Not(VisitIsEmpty(f.IsEmpty(VisitFilter(f.Filter(qilNode14, VisitIs(f.Is(qilNode8, qilNode14)))))))));
							}
						}
					}
				}
			}
		}
		if (this[XmlILOptimization.NormalizeIdEq] && qilNode.NodeType == QilNodeType.StrConcat)
		{
			QilNode qilNode15 = qilNode[1];
			if (qilNode15.NodeType == QilNodeType.Loop)
			{
				QilNode qilNode16 = qilNode15[0];
				QilNode qilNode17 = qilNode15[1];
				if (qilNode16.NodeType == QilNodeType.For)
				{
					QilNode qilNode18 = qilNode16[0];
					if (!qilNode18.XmlType.MaybeMany && qilNode17.NodeType == QilNodeType.XsltGenerateId)
					{
						QilNode qilNode19 = qilNode17[0];
						if (qilNode19 == qilNode16 && qilNode2.NodeType == QilNodeType.XsltGenerateId)
						{
							QilNode qilNode20 = qilNode2[0];
							if (qilNode20.XmlType.IsSingleton && AllowReplace(XmlILOptimization.NormalizeIdEq, local0))
							{
								QilNode qilNode21 = VisitFor(f.For(qilNode18));
								return Replace(XmlILOptimization.NormalizeIdEq, local0, VisitNot(f.Not(VisitIsEmpty(f.IsEmpty(VisitFilter(f.Filter(qilNode21, VisitIs(f.Is(qilNode20, qilNode21)))))))));
							}
						}
					}
				}
			}
		}
		if (this[XmlILOptimization.NormalizeMuenchian] && qilNode.NodeType == QilNodeType.Length)
		{
			QilNode qilNode22 = qilNode[0];
			if (qilNode22.NodeType == QilNodeType.Union)
			{
				QilNode qilNode23 = qilNode22[0];
				QilNode qilNode24 = qilNode22[1];
				if (qilNode23.XmlType.IsSingleton && !qilNode24.XmlType.MaybeMany && qilNode2.NodeType == QilNodeType.LiteralInt32)
				{
					int num = (int)((QilLiteral)qilNode2).Value;
					if (num == 1 && AllowReplace(XmlILOptimization.NormalizeMuenchian, local0))
					{
						QilNode qilNode25 = VisitFor(f.For(qilNode24));
						return Replace(XmlILOptimization.NormalizeMuenchian, local0, VisitIsEmpty(f.IsEmpty(VisitFilter(f.Filter(qilNode25, VisitNot(f.Not(VisitIs(f.Is(qilNode23, qilNode25)))))))));
					}
				}
			}
		}
		if (this[XmlILOptimization.NormalizeMuenchian] && qilNode.NodeType == QilNodeType.Length)
		{
			QilNode qilNode26 = qilNode[0];
			if (qilNode26.NodeType == QilNodeType.Union)
			{
				QilNode qilNode27 = qilNode26[0];
				QilNode qilNode28 = qilNode26[1];
				if (!qilNode27.XmlType.MaybeMany && qilNode28.XmlType.IsSingleton && qilNode2.NodeType == QilNodeType.LiteralInt32)
				{
					int num2 = (int)((QilLiteral)qilNode2).Value;
					if (num2 == 1 && AllowReplace(XmlILOptimization.NormalizeMuenchian, local0))
					{
						QilNode qilNode29 = VisitFor(f.For(qilNode27));
						return Replace(XmlILOptimization.NormalizeMuenchian, local0, VisitIsEmpty(f.IsEmpty(VisitFilter(f.Filter(qilNode29, VisitNot(f.Not(VisitIs(f.Is(qilNode29, qilNode28)))))))));
					}
				}
			}
		}
		if (this[XmlILOptimization.AnnotateMaxLengthEq] && qilNode.NodeType == QilNodeType.Length && qilNode2.NodeType == QilNodeType.LiteralInt32)
		{
			int num3 = (int)((QilLiteral)qilNode2).Value;
			if (AllowReplace(XmlILOptimization.AnnotateMaxLengthEq, local0))
			{
				OptimizerPatterns.Write(qilNode).AddPattern(OptimizerPatternName.MaxPosition);
				OptimizerPatterns.Write(qilNode).AddArgument(OptimizerPatternArgument.ElementQName, num3);
			}
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitGt(QilBinary local0)
	{
		QilNode qilNode = local0[0];
		QilNode qilNode2 = local0[1];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.FoldNone] && (object)qilNode2.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode2)));
		}
		if (this[XmlILOptimization.EliminateGt] && IsLiteral(qilNode) && IsLiteral(qilNode2) && AllowReplace(XmlILOptimization.EliminateGt, local0))
		{
			return Replace(XmlILOptimization.EliminateGt, local0, FoldComparison(QilNodeType.Gt, qilNode, qilNode2));
		}
		if (this[XmlILOptimization.NormalizeGtLiteral] && IsLiteral(qilNode) && !IsLiteral(qilNode2) && AllowReplace(XmlILOptimization.NormalizeGtLiteral, local0))
		{
			return Replace(XmlILOptimization.NormalizeGtLiteral, local0, VisitLt(f.Lt(qilNode2, qilNode)));
		}
		if (this[XmlILOptimization.NormalizeXsltConvertGt] && qilNode.NodeType == QilNodeType.XsltConvert)
		{
			QilNode qilNode3 = qilNode[0];
			QilNode qilNode4 = qilNode[1];
			if (qilNode4.NodeType == QilNodeType.LiteralType)
			{
				XmlQueryType typ = (XmlQueryType)((QilLiteral)qilNode4).Value;
				if (IsPrimitiveNumeric(qilNode3.XmlType) && IsPrimitiveNumeric(typ) && IsLiteral(qilNode2) && CanFoldXsltConvertNonLossy(qilNode2, qilNode3.XmlType) && AllowReplace(XmlILOptimization.NormalizeXsltConvertGt, local0))
				{
					return Replace(XmlILOptimization.NormalizeXsltConvertGt, local0, VisitGt(f.Gt(qilNode3, FoldXsltConvert(qilNode2, qilNode3.XmlType))));
				}
			}
		}
		if (this[XmlILOptimization.NormalizeLengthGt] && qilNode.NodeType == QilNodeType.Length)
		{
			QilNode child = qilNode[0];
			if (qilNode2.NodeType == QilNodeType.LiteralInt32 && (int)((QilLiteral)qilNode2).Value == 0 && AllowReplace(XmlILOptimization.NormalizeLengthGt, local0))
			{
				return Replace(XmlILOptimization.NormalizeLengthGt, local0, VisitNot(f.Not(VisitIsEmpty(f.IsEmpty(child)))));
			}
		}
		if (this[XmlILOptimization.AnnotateMaxLengthGt] && qilNode.NodeType == QilNodeType.Length && qilNode2.NodeType == QilNodeType.LiteralInt32)
		{
			int num = (int)((QilLiteral)qilNode2).Value;
			if (AllowReplace(XmlILOptimization.AnnotateMaxLengthGt, local0))
			{
				OptimizerPatterns.Write(qilNode).AddPattern(OptimizerPatternName.MaxPosition);
				OptimizerPatterns.Write(qilNode).AddArgument(OptimizerPatternArgument.ElementQName, num);
			}
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitGe(QilBinary local0)
	{
		QilNode qilNode = local0[0];
		QilNode qilNode2 = local0[1];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.FoldNone] && (object)qilNode2.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode2)));
		}
		if (this[XmlILOptimization.EliminateGe] && IsLiteral(qilNode) && IsLiteral(qilNode2) && AllowReplace(XmlILOptimization.EliminateGe, local0))
		{
			return Replace(XmlILOptimization.EliminateGe, local0, FoldComparison(QilNodeType.Ge, qilNode, qilNode2));
		}
		if (this[XmlILOptimization.NormalizeGeLiteral] && IsLiteral(qilNode) && !IsLiteral(qilNode2) && AllowReplace(XmlILOptimization.NormalizeGeLiteral, local0))
		{
			return Replace(XmlILOptimization.NormalizeGeLiteral, local0, VisitLe(f.Le(qilNode2, qilNode)));
		}
		if (this[XmlILOptimization.NormalizeXsltConvertGe] && qilNode.NodeType == QilNodeType.XsltConvert)
		{
			QilNode qilNode3 = qilNode[0];
			QilNode qilNode4 = qilNode[1];
			if (qilNode4.NodeType == QilNodeType.LiteralType)
			{
				XmlQueryType typ = (XmlQueryType)((QilLiteral)qilNode4).Value;
				if (IsPrimitiveNumeric(qilNode3.XmlType) && IsPrimitiveNumeric(typ) && IsLiteral(qilNode2) && CanFoldXsltConvertNonLossy(qilNode2, qilNode3.XmlType) && AllowReplace(XmlILOptimization.NormalizeXsltConvertGe, local0))
				{
					return Replace(XmlILOptimization.NormalizeXsltConvertGe, local0, VisitGe(f.Ge(qilNode3, FoldXsltConvert(qilNode2, qilNode3.XmlType))));
				}
			}
		}
		if (this[XmlILOptimization.AnnotateMaxLengthGe] && qilNode.NodeType == QilNodeType.Length && qilNode2.NodeType == QilNodeType.LiteralInt32)
		{
			int num = (int)((QilLiteral)qilNode2).Value;
			if (AllowReplace(XmlILOptimization.AnnotateMaxLengthGe, local0))
			{
				OptimizerPatterns.Write(qilNode).AddPattern(OptimizerPatternName.MaxPosition);
				OptimizerPatterns.Write(qilNode).AddArgument(OptimizerPatternArgument.ElementQName, num);
			}
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitLt(QilBinary local0)
	{
		QilNode qilNode = local0[0];
		QilNode qilNode2 = local0[1];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.FoldNone] && (object)qilNode2.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode2)));
		}
		if (this[XmlILOptimization.EliminateLt] && IsLiteral(qilNode) && IsLiteral(qilNode2) && AllowReplace(XmlILOptimization.EliminateLt, local0))
		{
			return Replace(XmlILOptimization.EliminateLt, local0, FoldComparison(QilNodeType.Lt, qilNode, qilNode2));
		}
		if (this[XmlILOptimization.NormalizeLtLiteral] && IsLiteral(qilNode) && !IsLiteral(qilNode2) && AllowReplace(XmlILOptimization.NormalizeLtLiteral, local0))
		{
			return Replace(XmlILOptimization.NormalizeLtLiteral, local0, VisitGt(f.Gt(qilNode2, qilNode)));
		}
		if (this[XmlILOptimization.NormalizeXsltConvertLt] && qilNode.NodeType == QilNodeType.XsltConvert)
		{
			QilNode qilNode3 = qilNode[0];
			QilNode qilNode4 = qilNode[1];
			if (qilNode4.NodeType == QilNodeType.LiteralType)
			{
				XmlQueryType typ = (XmlQueryType)((QilLiteral)qilNode4).Value;
				if (IsPrimitiveNumeric(qilNode3.XmlType) && IsPrimitiveNumeric(typ) && IsLiteral(qilNode2) && CanFoldXsltConvertNonLossy(qilNode2, qilNode3.XmlType) && AllowReplace(XmlILOptimization.NormalizeXsltConvertLt, local0))
				{
					return Replace(XmlILOptimization.NormalizeXsltConvertLt, local0, VisitLt(f.Lt(qilNode3, FoldXsltConvert(qilNode2, qilNode3.XmlType))));
				}
			}
		}
		if (this[XmlILOptimization.AnnotateMaxLengthLt] && qilNode.NodeType == QilNodeType.Length && qilNode2.NodeType == QilNodeType.LiteralInt32)
		{
			int num = (int)((QilLiteral)qilNode2).Value;
			if (AllowReplace(XmlILOptimization.AnnotateMaxLengthLt, local0))
			{
				OptimizerPatterns.Write(qilNode).AddPattern(OptimizerPatternName.MaxPosition);
				OptimizerPatterns.Write(qilNode).AddArgument(OptimizerPatternArgument.ElementQName, num);
			}
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitLe(QilBinary local0)
	{
		QilNode qilNode = local0[0];
		QilNode qilNode2 = local0[1];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.FoldNone] && (object)qilNode2.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode2)));
		}
		if (this[XmlILOptimization.EliminateLe] && IsLiteral(qilNode) && IsLiteral(qilNode2) && AllowReplace(XmlILOptimization.EliminateLe, local0))
		{
			return Replace(XmlILOptimization.EliminateLe, local0, FoldComparison(QilNodeType.Le, qilNode, qilNode2));
		}
		if (this[XmlILOptimization.NormalizeLeLiteral] && IsLiteral(qilNode) && !IsLiteral(qilNode2) && AllowReplace(XmlILOptimization.NormalizeLeLiteral, local0))
		{
			return Replace(XmlILOptimization.NormalizeLeLiteral, local0, VisitGe(f.Ge(qilNode2, qilNode)));
		}
		if (this[XmlILOptimization.NormalizeXsltConvertLe] && qilNode.NodeType == QilNodeType.XsltConvert)
		{
			QilNode qilNode3 = qilNode[0];
			QilNode qilNode4 = qilNode[1];
			if (qilNode4.NodeType == QilNodeType.LiteralType)
			{
				XmlQueryType typ = (XmlQueryType)((QilLiteral)qilNode4).Value;
				if (IsPrimitiveNumeric(qilNode3.XmlType) && IsPrimitiveNumeric(typ) && IsLiteral(qilNode2) && CanFoldXsltConvertNonLossy(qilNode2, qilNode3.XmlType) && AllowReplace(XmlILOptimization.NormalizeXsltConvertLe, local0))
				{
					return Replace(XmlILOptimization.NormalizeXsltConvertLe, local0, VisitLe(f.Le(qilNode3, FoldXsltConvert(qilNode2, qilNode3.XmlType))));
				}
			}
		}
		if (this[XmlILOptimization.AnnotateMaxLengthLe] && qilNode.NodeType == QilNodeType.Length && qilNode2.NodeType == QilNodeType.LiteralInt32)
		{
			int num = (int)((QilLiteral)qilNode2).Value;
			if (AllowReplace(XmlILOptimization.AnnotateMaxLengthLe, local0))
			{
				OptimizerPatterns.Write(qilNode).AddPattern(OptimizerPatternName.MaxPosition);
				OptimizerPatterns.Write(qilNode).AddArgument(OptimizerPatternArgument.ElementQName, num);
			}
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitIs(QilBinary local0)
	{
		QilNode qilNode = local0[0];
		QilNode qilNode2 = local0[1];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.FoldNone] && (object)qilNode2.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode2)));
		}
		if (this[XmlILOptimization.EliminateIs] && qilNode2 == qilNode && AllowReplace(XmlILOptimization.EliminateIs, local0))
		{
			return Replace(XmlILOptimization.EliminateIs, local0, VisitTrue(f.True()));
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitAfter(QilBinary local0)
	{
		QilNode qilNode = local0[0];
		QilNode qilNode2 = local0[1];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.FoldNone] && (object)qilNode2.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode2)));
		}
		if (this[XmlILOptimization.EliminateAfter] && qilNode2 == qilNode && AllowReplace(XmlILOptimization.EliminateAfter, local0))
		{
			return Replace(XmlILOptimization.EliminateAfter, local0, VisitFalse(f.False()));
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitBefore(QilBinary local0)
	{
		QilNode qilNode = local0[0];
		QilNode qilNode2 = local0[1];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.FoldNone] && (object)qilNode2.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode2)));
		}
		if (this[XmlILOptimization.EliminateBefore] && qilNode2 == qilNode && AllowReplace(XmlILOptimization.EliminateBefore, local0))
		{
			return Replace(XmlILOptimization.EliminateBefore, local0, VisitFalse(f.False()));
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitLoop(QilLoop local0)
	{
		QilNode qilNode = local0[0];
		QilNode qilNode2 = local0[1];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode[0])));
		}
		if (this[XmlILOptimization.EliminateIterator] && qilNode.NodeType == QilNodeType.For)
		{
			QilNode qilNode3 = qilNode[0];
			if (qilNode3.NodeType == QilNodeType.For && !OptimizerPatterns.Read(qilNode).MatchesPattern(OptimizerPatternName.IsPositional) && AllowReplace(XmlILOptimization.EliminateIterator, local0))
			{
				return Replace(XmlILOptimization.EliminateIterator, local0, Subs(qilNode2, qilNode, qilNode3));
			}
		}
		if (this[XmlILOptimization.EliminateLoop] && qilNode.NodeType == QilNodeType.For)
		{
			QilNode qilNode4 = qilNode[0];
			if (qilNode4.NodeType == QilNodeType.Sequence && qilNode4.Count == 0 && AllowReplace(XmlILOptimization.EliminateLoop, local0))
			{
				return Replace(XmlILOptimization.EliminateLoop, local0, VisitSequence(f.Sequence()));
			}
		}
		if (this[XmlILOptimization.EliminateLoop] && !OptimizerPatterns.Read(qilNode).MatchesPattern(OptimizerPatternName.MaybeSideEffects) && qilNode2.NodeType == QilNodeType.Sequence && qilNode2.Count == 0 && AllowReplace(XmlILOptimization.EliminateLoop, local0))
		{
			return Replace(XmlILOptimization.EliminateLoop, local0, VisitSequence(f.Sequence()));
		}
		if (this[XmlILOptimization.EliminateLoop] && qilNode2 == qilNode && AllowReplace(XmlILOptimization.EliminateLoop, local0))
		{
			return Replace(XmlILOptimization.EliminateLoop, local0, qilNode[0]);
		}
		if (this[XmlILOptimization.NormalizeLoopText] && qilNode.NodeType == QilNodeType.For)
		{
			QilNode qilNode5 = qilNode[0];
			if (qilNode5.XmlType.IsSingleton && qilNode2.NodeType == QilNodeType.TextCtor)
			{
				QilNode body = qilNode2[0];
				if (AllowReplace(XmlILOptimization.NormalizeLoopText, local0))
				{
					return Replace(XmlILOptimization.NormalizeLoopText, local0, VisitTextCtor(f.TextCtor(VisitLoop(f.Loop(qilNode, body)))));
				}
			}
		}
		if (this[XmlILOptimization.EliminateIteratorUsedAtMostOnce] && (qilNode.NodeType == QilNodeType.Let || qilNode[0].XmlType.IsSingleton) && !OptimizerPatterns.Read(qilNode).MatchesPattern(OptimizerPatternName.MaybeSideEffects) && _nodeCounter.Count(qilNode2, qilNode) <= 1 && AllowReplace(XmlILOptimization.EliminateIteratorUsedAtMostOnce, local0))
		{
			return Replace(XmlILOptimization.EliminateIteratorUsedAtMostOnce, local0, Subs(qilNode2, qilNode, qilNode[0]));
		}
		if (this[XmlILOptimization.NormalizeLoopConditional] && qilNode2.NodeType == QilNodeType.Conditional)
		{
			QilNode child = qilNode2[0];
			QilNode qilNode6 = qilNode2[1];
			QilNode qilNode7 = qilNode2[2];
			if (qilNode6.NodeType == QilNodeType.Sequence && qilNode6.Count == 0 && qilNode7 == qilNode && AllowReplace(XmlILOptimization.NormalizeLoopConditional, local0))
			{
				return Replace(XmlILOptimization.NormalizeLoopConditional, local0, VisitFilter(f.Filter(qilNode, VisitNot(f.Not(child)))));
			}
		}
		if (this[XmlILOptimization.NormalizeLoopConditional] && qilNode2.NodeType == QilNodeType.Conditional)
		{
			QilNode body2 = qilNode2[0];
			QilNode qilNode8 = qilNode2[1];
			QilNode qilNode9 = qilNode2[2];
			if (qilNode8 == qilNode && qilNode9.NodeType == QilNodeType.Sequence && qilNode9.Count == 0 && AllowReplace(XmlILOptimization.NormalizeLoopConditional, local0))
			{
				return Replace(XmlILOptimization.NormalizeLoopConditional, local0, VisitFilter(f.Filter(qilNode, body2)));
			}
		}
		if (this[XmlILOptimization.NormalizeLoopConditional] && qilNode.NodeType == QilNodeType.For && qilNode2.NodeType == QilNodeType.Conditional)
		{
			QilNode child2 = qilNode2[0];
			QilNode qilNode10 = qilNode2[1];
			QilNode expr = qilNode2[2];
			if (qilNode10.NodeType == QilNodeType.Sequence && qilNode10.Count == 0 && NonPositional(expr, qilNode) && AllowReplace(XmlILOptimization.NormalizeLoopConditional, local0))
			{
				QilNode qilNode11 = VisitFor(f.For(VisitFilter(f.Filter(qilNode, VisitNot(f.Not(child2))))));
				return Replace(XmlILOptimization.NormalizeLoopConditional, local0, VisitLoop(f.Loop(qilNode11, Subs(expr, qilNode, qilNode11))));
			}
		}
		if (this[XmlILOptimization.NormalizeLoopConditional] && qilNode.NodeType == QilNodeType.For && qilNode2.NodeType == QilNodeType.Conditional)
		{
			QilNode body3 = qilNode2[0];
			QilNode expr2 = qilNode2[1];
			QilNode qilNode12 = qilNode2[2];
			if (NonPositional(expr2, qilNode) && qilNode12.NodeType == QilNodeType.Sequence && qilNode12.Count == 0 && AllowReplace(XmlILOptimization.NormalizeLoopConditional, local0))
			{
				QilNode qilNode13 = VisitFor(f.For(VisitFilter(f.Filter(qilNode, body3))));
				return Replace(XmlILOptimization.NormalizeLoopConditional, local0, VisitLoop(f.Loop(qilNode13, Subs(expr2, qilNode, qilNode13))));
			}
		}
		if (this[XmlILOptimization.NormalizeLoopLoop] && qilNode2.NodeType == QilNodeType.Loop)
		{
			QilNode qilNode14 = qilNode2[0];
			QilNode expr3 = qilNode2[1];
			if (qilNode14.NodeType == QilNodeType.For)
			{
				QilNode body4 = qilNode14[0];
				if (!DependsOn(expr3, qilNode) && NonPositional(expr3, qilNode14) && AllowReplace(XmlILOptimization.NormalizeLoopLoop, local0))
				{
					QilNode qilNode15 = VisitFor(f.For(VisitLoop(f.Loop(qilNode, body4))));
					return Replace(XmlILOptimization.NormalizeLoopLoop, local0, VisitLoop(f.Loop(qilNode15, Subs(expr3, qilNode14, qilNode15))));
				}
			}
		}
		if (this[XmlILOptimization.AnnotateSingletonLoop] && qilNode.NodeType == QilNodeType.For)
		{
			QilNode qilNode16 = qilNode[0];
			if (!qilNode16.XmlType.MaybeMany && AllowReplace(XmlILOptimization.AnnotateSingletonLoop, local0))
			{
				OptimizerPatterns.Inherit(qilNode2, local0, OptimizerPatternName.IsDocOrderDistinct);
				OptimizerPatterns.Inherit(qilNode2, local0, OptimizerPatternName.SameDepth);
			}
		}
		if (this[XmlILOptimization.AnnotateRootLoop] && IsStepPattern(qilNode2, QilNodeType.Root) && AllowReplace(XmlILOptimization.AnnotateRootLoop, local0))
		{
			OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.SameDepth);
		}
		if (this[XmlILOptimization.AnnotateContentLoop] && qilNode.NodeType == QilNodeType.For)
		{
			QilNode qilNode17 = qilNode[0];
			if (OptimizerPatterns.Read(qilNode17).MatchesPattern(OptimizerPatternName.SameDepth) && (IsStepPattern(qilNode2, QilNodeType.Content) || IsStepPattern(qilNode2, QilNodeType.Union)) && qilNode == OptimizerPatterns.Read(qilNode2).GetArgument(OptimizerPatternArgument.StepInput) && AllowReplace(XmlILOptimization.AnnotateContentLoop, local0))
			{
				OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.SameDepth);
				OptimizerPatterns.Inherit(qilNode17, local0, OptimizerPatternName.IsDocOrderDistinct);
			}
		}
		if (this[XmlILOptimization.AnnotateAttrNmspLoop] && qilNode.NodeType == QilNodeType.For)
		{
			QilNode ndSrc = qilNode[0];
			if ((IsStepPattern(qilNode2, QilNodeType.Attribute) || IsStepPattern(qilNode2, QilNodeType.XPathNamespace) || OptimizerPatterns.Read(qilNode2).MatchesPattern(OptimizerPatternName.FilterAttributeKind)) && qilNode == OptimizerPatterns.Read(qilNode2).GetArgument(OptimizerPatternArgument.StepInput) && AllowReplace(XmlILOptimization.AnnotateAttrNmspLoop, local0))
			{
				OptimizerPatterns.Inherit(ndSrc, local0, OptimizerPatternName.SameDepth);
				OptimizerPatterns.Inherit(ndSrc, local0, OptimizerPatternName.IsDocOrderDistinct);
			}
		}
		if (this[XmlILOptimization.AnnotateDescendantLoop] && qilNode.NodeType == QilNodeType.For)
		{
			QilNode qilNode18 = qilNode[0];
			if (OptimizerPatterns.Read(qilNode18).MatchesPattern(OptimizerPatternName.SameDepth) && (IsStepPattern(qilNode2, QilNodeType.Descendant) || IsStepPattern(qilNode2, QilNodeType.DescendantOrSelf)) && qilNode == OptimizerPatterns.Read(qilNode2).GetArgument(OptimizerPatternArgument.StepInput) && AllowReplace(XmlILOptimization.AnnotateDescendantLoop, local0))
			{
				OptimizerPatterns.Inherit(qilNode18, local0, OptimizerPatternName.IsDocOrderDistinct);
			}
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitFilter(QilLoop local0)
	{
		QilNode qilNode = local0[0];
		QilNode qilNode2 = local0[1];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode[0])));
		}
		if (this[XmlILOptimization.FoldNone] && (object)qilNode2.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitLoop(f.Loop(qilNode, qilNode2)));
		}
		if (this[XmlILOptimization.EliminateFilter] && !OptimizerPatterns.Read(qilNode).MatchesPattern(OptimizerPatternName.MaybeSideEffects) && qilNode2.NodeType == QilNodeType.False && AllowReplace(XmlILOptimization.EliminateFilter, local0))
		{
			return Replace(XmlILOptimization.EliminateFilter, local0, VisitSequence(f.Sequence()));
		}
		if (this[XmlILOptimization.EliminateFilter] && qilNode2.NodeType == QilNodeType.True && AllowReplace(XmlILOptimization.EliminateFilter, local0))
		{
			return Replace(XmlILOptimization.EliminateFilter, local0, qilNode[0]);
		}
		if (this[XmlILOptimization.NormalizeAttribute] && qilNode.NodeType == QilNodeType.For)
		{
			QilNode qilNode3 = qilNode[0];
			if (qilNode3.NodeType == QilNodeType.Content)
			{
				QilNode left = qilNode3[0];
				if (qilNode2.NodeType == QilNodeType.And)
				{
					QilNode qilNode4 = qilNode2[0];
					QilNode qilNode5 = qilNode2[1];
					if (qilNode4.NodeType == QilNodeType.IsType)
					{
						QilNode qilNode6 = qilNode4[0];
						QilNode qilNode7 = qilNode4[1];
						if (qilNode6 == qilNode && qilNode7.NodeType == QilNodeType.LiteralType)
						{
							XmlQueryType xmlQueryType = (XmlQueryType)((QilLiteral)qilNode7).Value;
							if (xmlQueryType == XmlQueryTypeFactory.Attribute && qilNode5.NodeType == QilNodeType.Eq)
							{
								QilNode qilNode8 = qilNode5[0];
								QilNode qilNode9 = qilNode5[1];
								if (qilNode8.NodeType == QilNodeType.NameOf)
								{
									QilNode qilNode10 = qilNode8[0];
									if (qilNode10 == qilNode && qilNode9.NodeType == QilNodeType.LiteralQName && AllowReplace(XmlILOptimization.NormalizeAttribute, local0))
									{
										return Replace(XmlILOptimization.NormalizeAttribute, local0, VisitAttribute(f.Attribute(left, qilNode9)));
									}
								}
							}
						}
					}
				}
			}
		}
		if (this[XmlILOptimization.CommuteFilterLoop] && qilNode.NodeType == QilNodeType.For)
		{
			QilNode qilNode11 = qilNode[0];
			if (qilNode11.NodeType == QilNodeType.Loop)
			{
				QilNode variable = qilNode11[0];
				QilNode binding = qilNode11[1];
				if (NonPositional(qilNode2, qilNode) && !IsDocOrderDistinct(qilNode11) && AllowReplace(XmlILOptimization.CommuteFilterLoop, local0))
				{
					QilNode qilNode12 = VisitFor(f.For(binding));
					return Replace(XmlILOptimization.CommuteFilterLoop, local0, VisitLoop(f.Loop(variable, VisitFilter(f.Filter(qilNode12, Subs(qilNode2, qilNode, qilNode12))))));
				}
			}
		}
		if (this[XmlILOptimization.NormalizeLoopInvariant] && !OptimizerPatterns.Read(qilNode).MatchesPattern(OptimizerPatternName.MaybeSideEffects) && qilNode[0].NodeType != QilNodeType.OptimizeBarrier && !DependsOn(qilNode2, qilNode) && !OptimizerPatterns.Read(qilNode2).MatchesPattern(OptimizerPatternName.MaybeSideEffects) && AllowReplace(XmlILOptimization.NormalizeLoopInvariant, local0))
		{
			return Replace(XmlILOptimization.NormalizeLoopInvariant, local0, VisitConditional(f.Conditional(qilNode2, qilNode[0], VisitSequence(f.Sequence()))));
		}
		if (this[XmlILOptimization.AnnotateMaxPositionEq] && qilNode2.NodeType == QilNodeType.Eq)
		{
			QilNode qilNode13 = qilNode2[0];
			QilNode qilNode14 = qilNode2[1];
			if (qilNode13.NodeType == QilNodeType.PositionOf)
			{
				QilNode qilNode15 = qilNode13[0];
				if (qilNode15 == qilNode && qilNode14.NodeType == QilNodeType.LiteralInt32)
				{
					int num = (int)((QilLiteral)qilNode14).Value;
					if (AllowReplace(XmlILOptimization.AnnotateMaxPositionEq, local0))
					{
						OptimizerPatterns.Write(qilNode).AddPattern(OptimizerPatternName.MaxPosition);
						OptimizerPatterns.Write(qilNode).AddArgument(OptimizerPatternArgument.ElementQName, num);
					}
				}
			}
		}
		if (this[XmlILOptimization.AnnotateMaxPositionLe] && qilNode2.NodeType == QilNodeType.Le)
		{
			QilNode qilNode16 = qilNode2[0];
			QilNode qilNode17 = qilNode2[1];
			if (qilNode16.NodeType == QilNodeType.PositionOf)
			{
				QilNode qilNode18 = qilNode16[0];
				if (qilNode18 == qilNode && qilNode17.NodeType == QilNodeType.LiteralInt32)
				{
					int num2 = (int)((QilLiteral)qilNode17).Value;
					if (AllowReplace(XmlILOptimization.AnnotateMaxPositionLe, local0))
					{
						OptimizerPatterns.Write(qilNode).AddPattern(OptimizerPatternName.MaxPosition);
						OptimizerPatterns.Write(qilNode).AddArgument(OptimizerPatternArgument.ElementQName, num2);
					}
				}
			}
		}
		if (this[XmlILOptimization.AnnotateMaxPositionLt] && qilNode2.NodeType == QilNodeType.Lt)
		{
			QilNode qilNode19 = qilNode2[0];
			QilNode qilNode20 = qilNode2[1];
			if (qilNode19.NodeType == QilNodeType.PositionOf)
			{
				QilNode qilNode21 = qilNode19[0];
				if (qilNode21 == qilNode && qilNode20.NodeType == QilNodeType.LiteralInt32)
				{
					int num3 = (int)((QilLiteral)qilNode20).Value;
					if (AllowReplace(XmlILOptimization.AnnotateMaxPositionLt, local0))
					{
						OptimizerPatterns.Write(qilNode).AddPattern(OptimizerPatternName.MaxPosition);
						OptimizerPatterns.Write(qilNode).AddArgument(OptimizerPatternArgument.ElementQName, num3 - 1);
					}
				}
			}
		}
		if (this[XmlILOptimization.AnnotateFilter] && qilNode.NodeType == QilNodeType.For)
		{
			QilNode ndSrc = qilNode[0];
			if (AllowReplace(XmlILOptimization.AnnotateFilter, local0))
			{
				OptimizerPatterns.Inherit(ndSrc, local0, OptimizerPatternName.Step);
				OptimizerPatterns.Inherit(ndSrc, local0, OptimizerPatternName.IsDocOrderDistinct);
				OptimizerPatterns.Inherit(ndSrc, local0, OptimizerPatternName.SameDepth);
			}
		}
		if (this[XmlILOptimization.AnnotateFilterElements] && qilNode.NodeType == QilNodeType.For)
		{
			QilNode nd = qilNode[0];
			if (OptimizerPatterns.Read(nd).MatchesPattern(OptimizerPatternName.Axis) && qilNode2.NodeType == QilNodeType.And)
			{
				QilNode qilNode22 = qilNode2[0];
				QilNode qilNode23 = qilNode2[1];
				if (qilNode22.NodeType == QilNodeType.IsType)
				{
					QilNode qilNode24 = qilNode22[0];
					QilNode qilNode25 = qilNode22[1];
					if (qilNode24 == qilNode && qilNode25.NodeType == QilNodeType.LiteralType)
					{
						XmlQueryType xmlQueryType2 = (XmlQueryType)((QilLiteral)qilNode25).Value;
						if (xmlQueryType2 == XmlQueryTypeFactory.Element && qilNode23.NodeType == QilNodeType.Eq)
						{
							QilNode qilNode26 = qilNode23[0];
							QilNode qilNode27 = qilNode23[1];
							if (qilNode26.NodeType == QilNodeType.NameOf)
							{
								QilNode qilNode28 = qilNode26[0];
								if (qilNode28 == qilNode && qilNode27.NodeType == QilNodeType.LiteralQName && AllowReplace(XmlILOptimization.AnnotateFilterElements, local0))
								{
									OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.FilterElements);
									OptimizerPatterns.Write(local0).AddArgument(OptimizerPatternArgument.ElementQName, qilNode27);
								}
							}
						}
					}
				}
			}
		}
		if (this[XmlILOptimization.AnnotateFilterContentKind] && qilNode.NodeType == QilNodeType.For)
		{
			QilNode nd2 = qilNode[0];
			if (OptimizerPatterns.Read(nd2).MatchesPattern(OptimizerPatternName.Axis) && qilNode2.NodeType == QilNodeType.IsType)
			{
				QilNode qilNode29 = qilNode2[0];
				QilNode qilNode30 = qilNode2[1];
				if (qilNode29 == qilNode && qilNode30.NodeType == QilNodeType.LiteralType)
				{
					XmlQueryType xmlQueryType3 = (XmlQueryType)((QilLiteral)qilNode30).Value;
					if (MatchesContentTest(xmlQueryType3) && AllowReplace(XmlILOptimization.AnnotateFilterContentKind, local0))
					{
						OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.FilterContentKind);
						OptimizerPatterns.Write(local0).AddArgument(OptimizerPatternArgument.ElementQName, xmlQueryType3);
					}
				}
			}
		}
		if (this[XmlILOptimization.AnnotateFilterAttributeKind] && qilNode.NodeType == QilNodeType.For)
		{
			QilNode qilNode31 = qilNode[0];
			if (qilNode31.NodeType == QilNodeType.Content && qilNode2.NodeType == QilNodeType.IsType)
			{
				QilNode qilNode32 = qilNode2[0];
				QilNode qilNode33 = qilNode2[1];
				if (qilNode32 == qilNode && qilNode33.NodeType == QilNodeType.LiteralType)
				{
					XmlQueryType xmlQueryType4 = (XmlQueryType)((QilLiteral)qilNode33).Value;
					if (xmlQueryType4 == XmlQueryTypeFactory.Attribute && AllowReplace(XmlILOptimization.AnnotateFilterAttributeKind, local0))
					{
						OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.FilterAttributeKind);
					}
				}
			}
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitSort(QilLoop local0)
	{
		QilNode qilNode = local0[0];
		QilNode qilNode2 = local0[1];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode[0])));
		}
		if (this[XmlILOptimization.EliminateSort] && qilNode.NodeType == QilNodeType.For)
		{
			QilNode qilNode3 = qilNode[0];
			if (qilNode3.XmlType.IsSingleton && AllowReplace(XmlILOptimization.EliminateSort, local0))
			{
				return Replace(XmlILOptimization.EliminateSort, local0, VisitNop(f.Nop(qilNode3)));
			}
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitSortKey(QilSortKey local0)
	{
		QilNode qilNode = local0[0];
		QilNode collation = local0[1];
		if (this[XmlILOptimization.NormalizeSortXsltConvert] && qilNode.NodeType == QilNodeType.XsltConvert)
		{
			QilNode qilNode2 = qilNode[0];
			QilNode qilNode3 = qilNode[1];
			if (qilNode3.NodeType == QilNodeType.LiteralType)
			{
				XmlQueryType xmlQueryType = (XmlQueryType)((QilLiteral)qilNode3).Value;
				if (qilNode2.XmlType == XmlQueryTypeFactory.IntX && xmlQueryType == XmlQueryTypeFactory.DoubleX && AllowReplace(XmlILOptimization.NormalizeSortXsltConvert, local0))
				{
					return Replace(XmlILOptimization.NormalizeSortXsltConvert, local0, VisitSortKey(f.SortKey(qilNode2, collation)));
				}
			}
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitDocOrderDistinct(QilUnary local0)
	{
		QilNode qilNode = local0[0];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.EliminateDod] && IsDocOrderDistinct(qilNode) && AllowReplace(XmlILOptimization.EliminateDod, local0))
		{
			return Replace(XmlILOptimization.EliminateDod, local0, qilNode);
		}
		if (this[XmlILOptimization.FoldNamedDescendants] && qilNode.NodeType == QilNodeType.Loop)
		{
			QilNode qilNode2 = qilNode[0];
			QilNode qilNode3 = qilNode[1];
			if (qilNode2.NodeType == QilNodeType.For)
			{
				QilNode qilNode4 = qilNode2[0];
				if (qilNode4.NodeType == QilNodeType.Loop)
				{
					QilNode variable = qilNode4[0];
					QilNode qilNode5 = qilNode4[1];
					if (qilNode5.NodeType == QilNodeType.DescendantOrSelf)
					{
						QilNode child = qilNode5[0];
						if (qilNode3.NodeType == QilNodeType.Filter)
						{
							QilNode refOld = qilNode3[0];
							QilNode expr = qilNode3[1];
							if ((OptimizerPatterns.Read(qilNode3).MatchesPattern(OptimizerPatternName.FilterElements) || OptimizerPatterns.Read(qilNode3).MatchesPattern(OptimizerPatternName.FilterContentKind)) && IsStepPattern(qilNode3, QilNodeType.Content) && AllowReplace(XmlILOptimization.FoldNamedDescendants, local0))
							{
								QilNode qilNode6 = VisitFor(f.For(VisitDescendant(f.Descendant(child))));
								return Replace(XmlILOptimization.FoldNamedDescendants, local0, VisitDocOrderDistinct(f.DocOrderDistinct(VisitLoop(f.Loop(variable, VisitFilter(f.Filter(qilNode6, Subs(expr, refOld, qilNode6))))))));
							}
						}
					}
				}
			}
		}
		if (this[XmlILOptimization.FoldNamedDescendants] && qilNode.NodeType == QilNodeType.Loop)
		{
			QilNode qilNode7 = qilNode[0];
			QilNode qilNode8 = qilNode[1];
			if (qilNode7.NodeType == QilNodeType.For)
			{
				QilNode qilNode9 = qilNode7[0];
				if (qilNode9.NodeType == QilNodeType.DescendantOrSelf)
				{
					QilNode child2 = qilNode9[0];
					if (qilNode8.NodeType == QilNodeType.Filter)
					{
						QilNode refOld2 = qilNode8[0];
						QilNode expr2 = qilNode8[1];
						if ((OptimizerPatterns.Read(qilNode8).MatchesPattern(OptimizerPatternName.FilterElements) || OptimizerPatterns.Read(qilNode8).MatchesPattern(OptimizerPatternName.FilterContentKind)) && IsStepPattern(qilNode8, QilNodeType.Content) && AllowReplace(XmlILOptimization.FoldNamedDescendants, local0))
						{
							QilNode qilNode10 = VisitFor(f.For(VisitDescendant(f.Descendant(child2))));
							return Replace(XmlILOptimization.FoldNamedDescendants, local0, VisitFilter(f.Filter(qilNode10, Subs(expr2, refOld2, qilNode10))));
						}
					}
				}
			}
		}
		if (this[XmlILOptimization.CommuteDodFilter] && qilNode.NodeType == QilNodeType.Filter)
		{
			QilNode qilNode11 = qilNode[0];
			QilNode expr3 = qilNode[1];
			if (qilNode11.NodeType == QilNodeType.For)
			{
				QilNode child3 = qilNode11[0];
				if (!OptimizerPatterns.Read(qilNode11).MatchesPattern(OptimizerPatternName.IsPositional) && !OptimizerPatterns.Read(qilNode).MatchesPattern(OptimizerPatternName.FilterElements) && !OptimizerPatterns.Read(qilNode).MatchesPattern(OptimizerPatternName.FilterContentKind) && !OptimizerPatterns.Read(qilNode).MatchesPattern(OptimizerPatternName.FilterAttributeKind) && AllowReplace(XmlILOptimization.CommuteDodFilter, local0))
				{
					QilNode qilNode12 = VisitFor(f.For(VisitDocOrderDistinct(f.DocOrderDistinct(child3))));
					return Replace(XmlILOptimization.CommuteDodFilter, local0, VisitFilter(f.Filter(qilNode12, Subs(expr3, qilNode11, qilNode12))));
				}
			}
		}
		if (this[XmlILOptimization.CommuteDodFilter] && qilNode.NodeType == QilNodeType.Loop)
		{
			QilNode qilNode13 = qilNode[0];
			QilNode qilNode14 = qilNode[1];
			if (qilNode14.NodeType == QilNodeType.Filter)
			{
				QilNode qilNode15 = qilNode14[0];
				QilNode expr4 = qilNode14[1];
				if (qilNode15.NodeType == QilNodeType.For)
				{
					QilNode body = qilNode15[0];
					if (!OptimizerPatterns.Read(qilNode15).MatchesPattern(OptimizerPatternName.IsPositional) && !DependsOn(expr4, qilNode13) && !OptimizerPatterns.Read(qilNode14).MatchesPattern(OptimizerPatternName.FilterElements) && !OptimizerPatterns.Read(qilNode14).MatchesPattern(OptimizerPatternName.FilterContentKind) && !OptimizerPatterns.Read(qilNode14).MatchesPattern(OptimizerPatternName.FilterAttributeKind) && AllowReplace(XmlILOptimization.CommuteDodFilter, local0))
					{
						QilNode qilNode16 = VisitFor(f.For(VisitDocOrderDistinct(f.DocOrderDistinct(VisitLoop(f.Loop(qilNode13, body))))));
						return Replace(XmlILOptimization.CommuteDodFilter, local0, VisitFilter(f.Filter(qilNode16, Subs(expr4, qilNode15, qilNode16))));
					}
				}
			}
		}
		if (this[XmlILOptimization.IntroduceDod] && qilNode.NodeType == QilNodeType.Loop)
		{
			QilNode qilNode17 = qilNode[0];
			QilNode expr5 = qilNode[1];
			if (qilNode17.NodeType == QilNodeType.For)
			{
				QilNode qilNode18 = qilNode17[0];
				if (!IsDocOrderDistinct(qilNode18) && !OptimizerPatterns.Read(qilNode17).MatchesPattern(OptimizerPatternName.IsPositional) && qilNode18.XmlType.IsSubtypeOf(XmlQueryTypeFactory.NodeNotRtfS) && !OptimizerPatterns.Read(qilNode).MatchesPattern(OptimizerPatternName.FilterElements) && !OptimizerPatterns.Read(qilNode).MatchesPattern(OptimizerPatternName.FilterContentKind) && !OptimizerPatterns.Read(qilNode).MatchesPattern(OptimizerPatternName.FilterAttributeKind) && AllowReplace(XmlILOptimization.IntroduceDod, local0))
				{
					QilNode qilNode19 = VisitFor(f.For(VisitDocOrderDistinct(f.DocOrderDistinct(qilNode18))));
					return Replace(XmlILOptimization.IntroduceDod, local0, VisitDocOrderDistinct(f.DocOrderDistinct(VisitLoop(f.Loop(qilNode19, Subs(expr5, qilNode17, qilNode19))))));
				}
			}
		}
		if (this[XmlILOptimization.IntroducePrecedingDod] && qilNode.NodeType == QilNodeType.Loop)
		{
			QilNode variable2 = qilNode[0];
			QilNode qilNode20 = qilNode[1];
			if (!IsDocOrderDistinct(qilNode20) && IsStepPattern(qilNode20, QilNodeType.PrecedingSibling) && AllowReplace(XmlILOptimization.IntroducePrecedingDod, local0))
			{
				return Replace(XmlILOptimization.IntroducePrecedingDod, local0, VisitDocOrderDistinct(f.DocOrderDistinct(VisitLoop(f.Loop(variable2, VisitDocOrderDistinct(f.DocOrderDistinct(qilNode20)))))));
			}
		}
		if (this[XmlILOptimization.EliminateReturnDod] && qilNode.NodeType == QilNodeType.Loop)
		{
			QilNode variable3 = qilNode[0];
			QilNode qilNode21 = qilNode[1];
			if (qilNode21.NodeType == QilNodeType.DocOrderDistinct)
			{
				QilNode qilNode22 = qilNode21[0];
				if (!IsStepPattern(qilNode22, QilNodeType.PrecedingSibling) && AllowReplace(XmlILOptimization.EliminateReturnDod, local0))
				{
					return Replace(XmlILOptimization.EliminateReturnDod, local0, VisitDocOrderDistinct(f.DocOrderDistinct(VisitLoop(f.Loop(variable3, qilNode22)))));
				}
			}
		}
		if (this[XmlILOptimization.AnnotateDod] && AllowReplace(XmlILOptimization.AnnotateDod, local0))
		{
			OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.IsDocOrderDistinct);
			OptimizerPatterns.Inherit(qilNode, local0, OptimizerPatternName.SameDepth);
		}
		if (this[XmlILOptimization.AnnotateDodReverse] && AllowDodReverse(qilNode) && AllowReplace(XmlILOptimization.AnnotateDodReverse, local0))
		{
			OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.DodReverse);
			OptimizerPatterns.Write(local0).AddArgument(OptimizerPatternArgument.ElementQName, qilNode);
		}
		if (this[XmlILOptimization.AnnotateJoinAndDod] && qilNode.NodeType == QilNodeType.Loop)
		{
			QilNode qilNode23 = qilNode[0];
			QilNode qilNode24 = qilNode[1];
			if (qilNode23.NodeType == QilNodeType.For)
			{
				QilNode nd = qilNode23[0];
				if (IsDocOrderDistinct(nd) && AllowJoinAndDod(qilNode24) && qilNode23 == OptimizerPatterns.Read(qilNode24).GetArgument(OptimizerPatternArgument.StepInput) && AllowReplace(XmlILOptimization.AnnotateJoinAndDod, local0))
				{
					OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.JoinAndDod);
					OptimizerPatterns.Write(local0).AddArgument(OptimizerPatternArgument.ElementQName, qilNode24);
				}
			}
		}
		if (this[XmlILOptimization.AnnotateDodMerge] && qilNode.NodeType == QilNodeType.Loop)
		{
			QilNode qilNode25 = qilNode[1];
			if (qilNode25.NodeType == QilNodeType.Invoke && IsDocOrderDistinct(qilNode25) && AllowReplace(XmlILOptimization.AnnotateDodMerge, local0))
			{
				OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.DodMerge);
			}
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitFunction(QilFunction local0)
	{
		QilNode qilNode = local0[0];
		QilNode qilNode2 = local0[1];
		QilNode qilNode3 = local0[2];
		XmlQueryType xmlType = local0.XmlType;
		if (local0.XmlType.IsSubtypeOf(XmlQueryTypeFactory.NodeS) && this[XmlILOptimization.AnnotateIndex1] && qilNode.Count == 2 && qilNode[0].XmlType.IsSubtypeOf(XmlQueryTypeFactory.Node) && qilNode[1].XmlType == XmlQueryTypeFactory.StringX && qilNode2.NodeType == QilNodeType.Filter)
		{
			QilNode qilNode4 = qilNode2[0];
			QilNode qilNode5 = qilNode2[1];
			if (qilNode4.NodeType == QilNodeType.For)
			{
				QilNode expr = qilNode4[0];
				if (qilNode5.NodeType == QilNodeType.Not)
				{
					QilNode qilNode6 = qilNode5[0];
					if (qilNode6.NodeType == QilNodeType.IsEmpty)
					{
						QilNode qilNode7 = qilNode6[0];
						if (qilNode7.NodeType == QilNodeType.Filter)
						{
							QilNode qilNode8 = qilNode7[0];
							QilNode qilNode9 = qilNode7[1];
							if (qilNode8.NodeType == QilNodeType.For)
							{
								QilNode qilNode10 = qilNode8[0];
								if (qilNode9.NodeType == QilNodeType.Eq)
								{
									QilNode qilNode11 = qilNode9[0];
									QilNode qilNode12 = qilNode9[1];
									if (qilNode11 == qilNode8 && qilNode12.NodeType == QilNodeType.Parameter && qilNode12 == qilNode[1] && IsDocOrderDistinct(qilNode2) && AllowReplace(XmlILOptimization.AnnotateIndex1, local0))
									{
										EqualityIndexVisitor equalityIndexVisitor = new EqualityIndexVisitor();
										if (equalityIndexVisitor.Scan(expr, qilNode[0], qilNode12) && equalityIndexVisitor.Scan(qilNode10, qilNode[0], qilNode12))
										{
											OptimizerPatterns optimizerPatterns = OptimizerPatterns.Write(qilNode2);
											optimizerPatterns.AddPattern(OptimizerPatternName.EqualityIndex);
											optimizerPatterns.AddArgument(OptimizerPatternArgument.StepNode, qilNode4);
											optimizerPatterns.AddArgument(OptimizerPatternArgument.StepInput, qilNode10);
										}
									}
								}
							}
						}
					}
				}
			}
		}
		if (local0.XmlType.IsSubtypeOf(XmlQueryTypeFactory.NodeS) && this[XmlILOptimization.AnnotateIndex2] && qilNode.Count == 2 && qilNode[0].XmlType == XmlQueryTypeFactory.Node && qilNode[1].XmlType == XmlQueryTypeFactory.StringX && qilNode2.NodeType == QilNodeType.Filter)
		{
			QilNode qilNode13 = qilNode2[0];
			QilNode qilNode14 = qilNode2[1];
			if (qilNode13.NodeType == QilNodeType.For)
			{
				QilNode expr2 = qilNode13[0];
				if (qilNode14.NodeType == QilNodeType.Eq)
				{
					QilNode qilNode15 = qilNode14[0];
					QilNode qilNode16 = qilNode14[1];
					if (qilNode16.NodeType == QilNodeType.Parameter && qilNode16 == qilNode[1] && IsDocOrderDistinct(qilNode2) && AllowReplace(XmlILOptimization.AnnotateIndex2, local0))
					{
						EqualityIndexVisitor equalityIndexVisitor2 = new EqualityIndexVisitor();
						if (equalityIndexVisitor2.Scan(expr2, qilNode[0], qilNode16) && equalityIndexVisitor2.Scan(qilNode15, qilNode[0], qilNode16))
						{
							OptimizerPatterns optimizerPatterns2 = OptimizerPatterns.Write(qilNode2);
							optimizerPatterns2.AddPattern(OptimizerPatternName.EqualityIndex);
							optimizerPatterns2.AddArgument(OptimizerPatternArgument.StepNode, qilNode13);
							optimizerPatterns2.AddArgument(OptimizerPatternArgument.StepInput, qilNode15);
						}
					}
				}
			}
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitInvoke(QilInvoke local0)
	{
		QilNode qilNode = local0[0];
		QilNode qilNode2 = local0[1];
		if (this[XmlILOptimization.NormalizeInvokeEmpty] && qilNode.NodeType == QilNodeType.Function)
		{
			QilNode qilNode3 = qilNode[1];
			if (qilNode3.NodeType == QilNodeType.Sequence && qilNode3.Count == 0 && AllowReplace(XmlILOptimization.NormalizeInvokeEmpty, local0))
			{
				return Replace(XmlILOptimization.NormalizeInvokeEmpty, local0, VisitSequence(f.Sequence()));
			}
		}
		if (this[XmlILOptimization.AnnotateTrackCallers] && AllowReplace(XmlILOptimization.AnnotateTrackCallers, local0))
		{
			XmlILConstructInfo.Write(qilNode).CallersInfo.Add(XmlILConstructInfo.Write(local0));
		}
		if (this[XmlILOptimization.AnnotateInvoke] && qilNode.NodeType == QilNodeType.Function)
		{
			QilNode ndSrc = qilNode[1];
			if (AllowReplace(XmlILOptimization.AnnotateInvoke, local0))
			{
				OptimizerPatterns.Inherit(ndSrc, local0, OptimizerPatternName.IsDocOrderDistinct);
				OptimizerPatterns.Inherit(ndSrc, local0, OptimizerPatternName.SameDepth);
			}
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitContent(QilUnary local0)
	{
		QilNode qilNode = local0[0];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.AnnotateContent] && AllowReplace(XmlILOptimization.AnnotateContent, local0))
		{
			AddStepPattern(local0, qilNode);
			OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.Axis);
			OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.IsDocOrderDistinct);
			OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.SameDepth);
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitAttribute(QilBinary local0)
	{
		QilNode qilNode = local0[0];
		QilNode qilNode2 = local0[1];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.FoldNone] && (object)qilNode2.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode2)));
		}
		if (this[XmlILOptimization.AnnotateAttribute] && AllowReplace(XmlILOptimization.AnnotateAttribute, local0))
		{
			OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.Axis);
			AddStepPattern(local0, qilNode);
			OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.IsDocOrderDistinct);
			OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.SameDepth);
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitParent(QilUnary local0)
	{
		QilNode qilNode = local0[0];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.AnnotateParent] && AllowReplace(XmlILOptimization.AnnotateParent, local0))
		{
			OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.Axis);
			AddStepPattern(local0, qilNode);
			OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.IsDocOrderDistinct);
			OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.SameDepth);
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitRoot(QilUnary local0)
	{
		QilNode qilNode = local0[0];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.AnnotateRoot] && AllowReplace(XmlILOptimization.AnnotateRoot, local0))
		{
			OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.Axis);
			AddStepPattern(local0, qilNode);
			OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.IsDocOrderDistinct);
			OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.SameDepth);
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitDescendant(QilUnary local0)
	{
		QilNode qilNode = local0[0];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.AnnotateDescendant] && AllowReplace(XmlILOptimization.AnnotateDescendant, local0))
		{
			OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.Axis);
			AddStepPattern(local0, qilNode);
			OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.IsDocOrderDistinct);
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitDescendantOrSelf(QilUnary local0)
	{
		QilNode qilNode = local0[0];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.AnnotateDescendantSelf] && AllowReplace(XmlILOptimization.AnnotateDescendantSelf, local0))
		{
			OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.Axis);
			AddStepPattern(local0, qilNode);
			OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.IsDocOrderDistinct);
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitAncestor(QilUnary local0)
	{
		QilNode qilNode = local0[0];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.AnnotateAncestor] && AllowReplace(XmlILOptimization.AnnotateAncestor, local0))
		{
			OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.Axis);
			AddStepPattern(local0, qilNode);
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitAncestorOrSelf(QilUnary local0)
	{
		QilNode qilNode = local0[0];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.AnnotateAncestorSelf] && AllowReplace(XmlILOptimization.AnnotateAncestorSelf, local0))
		{
			OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.Axis);
			AddStepPattern(local0, qilNode);
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitPreceding(QilUnary local0)
	{
		QilNode qilNode = local0[0];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.AnnotatePreceding] && AllowReplace(XmlILOptimization.AnnotatePreceding, local0))
		{
			OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.Axis);
			AddStepPattern(local0, qilNode);
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitFollowingSibling(QilUnary local0)
	{
		QilNode qilNode = local0[0];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.AnnotateFollowingSibling] && AllowReplace(XmlILOptimization.AnnotateFollowingSibling, local0))
		{
			OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.Axis);
			AddStepPattern(local0, qilNode);
			OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.IsDocOrderDistinct);
			OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.SameDepth);
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitPrecedingSibling(QilUnary local0)
	{
		QilNode qilNode = local0[0];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.AnnotatePrecedingSibling] && AllowReplace(XmlILOptimization.AnnotatePrecedingSibling, local0))
		{
			OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.Axis);
			AddStepPattern(local0, qilNode);
			OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.SameDepth);
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitNodeRange(QilBinary local0)
	{
		QilNode qilNode = local0[0];
		QilNode qilNode2 = local0[1];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.FoldNone] && (object)qilNode2.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode2)));
		}
		if (this[XmlILOptimization.AnnotateNodeRange] && AllowReplace(XmlILOptimization.AnnotateNodeRange, local0))
		{
			OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.Axis);
			AddStepPattern(local0, qilNode);
			OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.IsDocOrderDistinct);
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitDeref(QilBinary local0)
	{
		QilNode qilNode = local0[0];
		QilNode qilNode2 = local0[1];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.FoldNone] && (object)qilNode2.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode2)));
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitElementCtor(QilBinary local0)
	{
		QilNode qilNode = local0[0];
		QilNode qilNode2 = local0[1];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.FoldNone] && (object)qilNode2.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode2)));
		}
		if (this[XmlILOptimization.AnnotateConstruction] && AllowReplace(XmlILOptimization.AnnotateConstruction, local0))
		{
			local0.Right = _elemAnalyzer.Analyze(local0, qilNode2);
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitAttributeCtor(QilBinary local0)
	{
		QilNode qilNode = local0[0];
		QilNode qilNode2 = local0[1];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.FoldNone] && (object)qilNode2.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode2)));
		}
		if (this[XmlILOptimization.AnnotateConstruction] && AllowReplace(XmlILOptimization.AnnotateConstruction, local0))
		{
			local0.Right = _contentAnalyzer.Analyze(local0, qilNode2);
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitCommentCtor(QilUnary local0)
	{
		QilNode qilNode = local0[0];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.AnnotateConstruction] && AllowReplace(XmlILOptimization.AnnotateConstruction, local0))
		{
			local0.Child = _contentAnalyzer.Analyze(local0, qilNode);
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitPICtor(QilBinary local0)
	{
		QilNode qilNode = local0[0];
		QilNode qilNode2 = local0[1];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.FoldNone] && (object)qilNode2.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode2)));
		}
		if (this[XmlILOptimization.AnnotateConstruction] && AllowReplace(XmlILOptimization.AnnotateConstruction, local0))
		{
			local0.Right = _contentAnalyzer.Analyze(local0, qilNode2);
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitTextCtor(QilUnary local0)
	{
		QilNode qilNode = local0[0];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.AnnotateConstruction] && AllowReplace(XmlILOptimization.AnnotateConstruction, local0))
		{
			_contentAnalyzer.Analyze(local0, null);
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitRawTextCtor(QilUnary local0)
	{
		QilNode qilNode = local0[0];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.AnnotateConstruction] && AllowReplace(XmlILOptimization.AnnotateConstruction, local0))
		{
			_contentAnalyzer.Analyze(local0, null);
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitDocumentCtor(QilUnary local0)
	{
		QilNode qilNode = local0[0];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.AnnotateConstruction] && AllowReplace(XmlILOptimization.AnnotateConstruction, local0))
		{
			local0.Child = _contentAnalyzer.Analyze(local0, qilNode);
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitNamespaceDecl(QilBinary local0)
	{
		QilNode qilNode = local0[0];
		QilNode qilNode2 = local0[1];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.FoldNone] && (object)qilNode2.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode2)));
		}
		if (XmlILConstructInfo.Read(local0).IsNamespaceInScope && this[XmlILOptimization.EliminateNamespaceDecl] && AllowReplace(XmlILOptimization.EliminateNamespaceDecl, local0))
		{
			return Replace(XmlILOptimization.EliminateNamespaceDecl, local0, VisitSequence(f.Sequence()));
		}
		if (this[XmlILOptimization.AnnotateConstruction] && AllowReplace(XmlILOptimization.AnnotateConstruction, local0))
		{
			_contentAnalyzer.Analyze(local0, null);
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitRtfCtor(QilBinary local0)
	{
		QilNode qilNode = local0[0];
		QilNode qilNode2 = local0[1];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.AnnotateConstruction] && AllowReplace(XmlILOptimization.AnnotateConstruction, local0))
		{
			local0.Left = _contentAnalyzer.Analyze(local0, qilNode);
		}
		if (this[XmlILOptimization.AnnotateSingleTextRtf] && qilNode.NodeType == QilNodeType.TextCtor)
		{
			QilNode arg = qilNode[0];
			if (AllowReplace(XmlILOptimization.AnnotateSingleTextRtf, local0))
			{
				OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.SingleTextRtf);
				OptimizerPatterns.Write(local0).AddArgument(OptimizerPatternArgument.ElementQName, arg);
				XmlILConstructInfo.Write(local0).PullFromIteratorFirst = true;
			}
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitNameOf(QilUnary local0)
	{
		QilNode qilNode = local0[0];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitLocalNameOf(QilUnary local0)
	{
		QilNode qilNode = local0[0];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitNamespaceUriOf(QilUnary local0)
	{
		QilNode qilNode = local0[0];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitPrefixOf(QilUnary local0)
	{
		QilNode qilNode = local0[0];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitTypeAssert(QilTargetType local0)
	{
		QilNode qilNode = local0[0];
		QilNode qilNode2 = local0[1];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.EliminateTypeAssert] && qilNode2.NodeType == QilNodeType.LiteralType)
		{
			XmlQueryType baseType = (XmlQueryType)((QilLiteral)qilNode2).Value;
			if (qilNode.XmlType.NeverSubtypeOf(baseType) && AllowReplace(XmlILOptimization.EliminateTypeAssert, local0))
			{
				return Replace(XmlILOptimization.EliminateTypeAssert, local0, VisitError(f.Error(VisitLiteralString(f.LiteralString(string.Empty)))));
			}
		}
		if (this[XmlILOptimization.EliminateTypeAssert] && qilNode2.NodeType == QilNodeType.LiteralType)
		{
			XmlQueryType xmlQueryType = (XmlQueryType)((QilLiteral)qilNode2).Value;
			if (qilNode.XmlType.Prime.NeverSubtypeOf(xmlQueryType.Prime) && AllowReplace(XmlILOptimization.EliminateTypeAssert, local0))
			{
				return Replace(XmlILOptimization.EliminateTypeAssert, local0, VisitConditional(f.Conditional(VisitIsEmpty(f.IsEmpty(qilNode)), VisitSequence(f.Sequence()), VisitError(f.Error(VisitLiteralString(f.LiteralString(string.Empty)))))));
			}
		}
		if (this[XmlILOptimization.EliminateTypeAssertOptional] && qilNode2.NodeType == QilNodeType.LiteralType)
		{
			XmlQueryType baseType2 = (XmlQueryType)((QilLiteral)qilNode2).Value;
			if (qilNode.XmlType.IsSubtypeOf(baseType2) && AllowReplace(XmlILOptimization.EliminateTypeAssertOptional, local0))
			{
				return Replace(XmlILOptimization.EliminateTypeAssertOptional, local0, qilNode);
			}
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitIsType(QilTargetType local0)
	{
		QilNode qilNode = local0[0];
		QilNode qilNode2 = local0[1];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.EliminateIsType] && !OptimizerPatterns.Read(qilNode).MatchesPattern(OptimizerPatternName.MaybeSideEffects) && qilNode2.NodeType == QilNodeType.LiteralType)
		{
			XmlQueryType baseType = (XmlQueryType)((QilLiteral)qilNode2).Value;
			if (qilNode.XmlType.IsSubtypeOf(baseType) && AllowReplace(XmlILOptimization.EliminateIsType, local0))
			{
				return Replace(XmlILOptimization.EliminateIsType, local0, VisitTrue(f.True()));
			}
		}
		if (this[XmlILOptimization.EliminateIsType] && !OptimizerPatterns.Read(qilNode).MatchesPattern(OptimizerPatternName.MaybeSideEffects) && qilNode2.NodeType == QilNodeType.LiteralType)
		{
			XmlQueryType baseType2 = (XmlQueryType)((QilLiteral)qilNode2).Value;
			if (qilNode.XmlType.NeverSubtypeOf(baseType2) && AllowReplace(XmlILOptimization.EliminateIsType, local0))
			{
				return Replace(XmlILOptimization.EliminateIsType, local0, VisitFalse(f.False()));
			}
		}
		if (this[XmlILOptimization.EliminateIsType] && qilNode2.NodeType == QilNodeType.LiteralType)
		{
			XmlQueryType xmlQueryType = (XmlQueryType)((QilLiteral)qilNode2).Value;
			if (qilNode.XmlType.Prime.NeverSubtypeOf(xmlQueryType.Prime) && AllowReplace(XmlILOptimization.EliminateIsType, local0))
			{
				return Replace(XmlILOptimization.EliminateIsType, local0, VisitIsEmpty(f.IsEmpty(qilNode)));
			}
		}
		if (this[XmlILOptimization.EliminateIsType] && OptimizerPatterns.Read(qilNode).MatchesPattern(OptimizerPatternName.MaybeSideEffects) && qilNode2.NodeType == QilNodeType.LiteralType)
		{
			XmlQueryType baseType3 = (XmlQueryType)((QilLiteral)qilNode2).Value;
			if (qilNode.XmlType.IsSubtypeOf(baseType3) && AllowReplace(XmlILOptimization.EliminateIsType, local0))
			{
				return Replace(XmlILOptimization.EliminateIsType, local0, VisitLoop(f.Loop(VisitLet(f.Let(qilNode)), VisitTrue(f.True()))));
			}
		}
		if (this[XmlILOptimization.EliminateIsType] && OptimizerPatterns.Read(qilNode).MatchesPattern(OptimizerPatternName.MaybeSideEffects) && qilNode2.NodeType == QilNodeType.LiteralType)
		{
			XmlQueryType baseType4 = (XmlQueryType)((QilLiteral)qilNode2).Value;
			if (qilNode.XmlType.NeverSubtypeOf(baseType4) && AllowReplace(XmlILOptimization.EliminateIsType, local0))
			{
				return Replace(XmlILOptimization.EliminateIsType, local0, VisitLoop(f.Loop(VisitLet(f.Let(qilNode)), VisitFalse(f.False()))));
			}
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitIsEmpty(QilUnary local0)
	{
		QilNode qilNode = local0[0];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.EliminateIsEmpty] && qilNode.NodeType == QilNodeType.Sequence && qilNode.Count == 0 && AllowReplace(XmlILOptimization.EliminateIsEmpty, local0))
		{
			return Replace(XmlILOptimization.EliminateIsEmpty, local0, VisitTrue(f.True()));
		}
		if (this[XmlILOptimization.EliminateIsEmpty] && !qilNode.XmlType.MaybeEmpty && !OptimizerPatterns.Read(qilNode).MatchesPattern(OptimizerPatternName.MaybeSideEffects) && AllowReplace(XmlILOptimization.EliminateIsEmpty, local0))
		{
			return Replace(XmlILOptimization.EliminateIsEmpty, local0, VisitFalse(f.False()));
		}
		if (this[XmlILOptimization.EliminateIsEmpty] && !qilNode.XmlType.MaybeEmpty && AllowReplace(XmlILOptimization.EliminateIsEmpty, local0))
		{
			return Replace(XmlILOptimization.EliminateIsEmpty, local0, VisitLoop(f.Loop(VisitLet(f.Let(qilNode)), VisitFalse(f.False()))));
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitXPathNodeValue(QilUnary local0)
	{
		QilNode qilNode = local0[0];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitXPathFollowing(QilUnary local0)
	{
		QilNode qilNode = local0[0];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.AnnotateXPathFollowing] && AllowReplace(XmlILOptimization.AnnotateXPathFollowing, local0))
		{
			OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.Axis);
			AddStepPattern(local0, qilNode);
			OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.IsDocOrderDistinct);
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitXPathPreceding(QilUnary local0)
	{
		QilNode qilNode = local0[0];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.AnnotateXPathPreceding] && AllowReplace(XmlILOptimization.AnnotateXPathPreceding, local0))
		{
			OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.Axis);
			AddStepPattern(local0, qilNode);
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitXPathNamespace(QilUnary local0)
	{
		QilNode qilNode = local0[0];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.AnnotateNamespace] && AllowReplace(XmlILOptimization.AnnotateNamespace, local0))
		{
			OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.Axis);
			AddStepPattern(local0, qilNode);
			OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.IsDocOrderDistinct);
			OptimizerPatterns.Write(local0).AddPattern(OptimizerPatternName.SameDepth);
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitXsltGenerateId(QilUnary local0)
	{
		QilNode qilNode = local0[0];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitXsltCopy(QilBinary local0)
	{
		QilNode qilNode = local0[0];
		QilNode qilNode2 = local0[1];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.FoldNone] && (object)qilNode2.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode2)));
		}
		if (this[XmlILOptimization.AnnotateConstruction] && AllowReplace(XmlILOptimization.AnnotateConstruction, local0))
		{
			local0.Right = _contentAnalyzer.Analyze(local0, qilNode2);
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitXsltCopyOf(QilUnary local0)
	{
		QilNode qilNode = local0[0];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.AnnotateConstruction] && AllowReplace(XmlILOptimization.AnnotateConstruction, local0))
		{
			_contentAnalyzer.Analyze(local0, null);
		}
		return NoReplace(local0);
	}

	protected override QilNode VisitXsltConvert(QilTargetType local0)
	{
		QilNode qilNode = local0[0];
		QilNode qilNode2 = local0[1];
		if (this[XmlILOptimization.FoldNone] && (object)qilNode.XmlType == XmlQueryTypeFactory.None && AllowReplace(XmlILOptimization.FoldNone, local0))
		{
			return Replace(XmlILOptimization.FoldNone, local0, VisitNop(f.Nop(qilNode)));
		}
		if (this[XmlILOptimization.FoldXsltConvertLiteral] && IsLiteral(qilNode) && qilNode2.NodeType == QilNodeType.LiteralType)
		{
			XmlQueryType typTarget = (XmlQueryType)((QilLiteral)qilNode2).Value;
			if (CanFoldXsltConvert(qilNode, typTarget) && AllowReplace(XmlILOptimization.FoldXsltConvertLiteral, local0))
			{
				return Replace(XmlILOptimization.FoldXsltConvertLiteral, local0, FoldXsltConvert(qilNode, typTarget));
			}
		}
		if (this[XmlILOptimization.EliminateXsltConvert] && qilNode2.NodeType == QilNodeType.LiteralType)
		{
			XmlQueryType xmlQueryType = (XmlQueryType)((QilLiteral)qilNode2).Value;
			if (qilNode.XmlType == xmlQueryType && AllowReplace(XmlILOptimization.EliminateXsltConvert, local0))
			{
				return Replace(XmlILOptimization.EliminateXsltConvert, local0, qilNode);
			}
		}
		return NoReplace(local0);
	}

	private bool DependsOn(QilNode expr, QilNode target)
	{
		return new NodeFinder().Find(expr, target);
	}

	private bool NonPositional(QilNode expr, QilNode iter)
	{
		return !new PositionOfFinder().Find(expr, iter);
	}

	private QilNode Subs(QilNode expr, QilNode refOld, QilNode refNew)
	{
		_subs.AddSubstitutionPair(refOld, refNew);
		QilNode result = ((!(expr is QilReference)) ? Visit(expr) : VisitReference(expr));
		_subs.RemoveLastSubstitutionPair();
		return result;
	}

	private bool IsGlobalVariable(QilIterator iter)
	{
		return _qil.GlobalVariableList.Contains(iter);
	}

	private bool IsGlobalValue(QilNode nd)
	{
		if (nd.NodeType == QilNodeType.Let)
		{
			return _qil.GlobalVariableList.Contains(nd);
		}
		if (nd.NodeType == QilNodeType.Parameter)
		{
			return _qil.GlobalParameterList.Contains(nd);
		}
		return false;
	}

	private bool IsPrimitiveNumeric(XmlQueryType typ)
	{
		if (typ == XmlQueryTypeFactory.IntX)
		{
			return true;
		}
		if (typ == XmlQueryTypeFactory.IntegerX)
		{
			return true;
		}
		if (typ == XmlQueryTypeFactory.DecimalX)
		{
			return true;
		}
		if (typ == XmlQueryTypeFactory.FloatX)
		{
			return true;
		}
		if (typ == XmlQueryTypeFactory.DoubleX)
		{
			return true;
		}
		return false;
	}

	private bool MatchesContentTest(XmlQueryType typ)
	{
		if (typ == XmlQueryTypeFactory.Element)
		{
			return true;
		}
		if (typ == XmlQueryTypeFactory.Text)
		{
			return true;
		}
		if (typ == XmlQueryTypeFactory.Comment)
		{
			return true;
		}
		if (typ == XmlQueryTypeFactory.PI)
		{
			return true;
		}
		if (typ == XmlQueryTypeFactory.Content)
		{
			return true;
		}
		return false;
	}

	private bool IsConstructedExpression(QilNode nd)
	{
		if (_qil.IsDebug)
		{
			return true;
		}
		if (nd.XmlType.IsNode)
		{
			switch (nd.NodeType)
			{
			case QilNodeType.Choice:
			case QilNodeType.ElementCtor:
			case QilNodeType.AttributeCtor:
			case QilNodeType.CommentCtor:
			case QilNodeType.PICtor:
			case QilNodeType.TextCtor:
			case QilNodeType.RawTextCtor:
			case QilNodeType.DocumentCtor:
			case QilNodeType.NamespaceDecl:
			case QilNodeType.XsltCopy:
			case QilNodeType.XsltCopyOf:
				return true;
			case QilNodeType.Loop:
				return IsConstructedExpression(((QilLoop)nd).Body);
			case QilNodeType.Sequence:
				if (nd.Count == 0)
				{
					return true;
				}
				foreach (QilNode item in nd)
				{
					if (IsConstructedExpression(item))
					{
						return true;
					}
				}
				break;
			case QilNodeType.Conditional:
			{
				QilTernary qilTernary = (QilTernary)nd;
				if (!IsConstructedExpression(qilTernary.Center))
				{
					return IsConstructedExpression(qilTernary.Right);
				}
				return true;
			}
			case QilNodeType.Invoke:
				return !((QilInvoke)nd).Function.XmlType.IsAtomicValue;
			}
		}
		return false;
	}

	private bool IsLiteral(QilNode nd)
	{
		QilNodeType nodeType = nd.NodeType;
		if ((uint)(nodeType - 18) <= 7u)
		{
			return true;
		}
		return false;
	}

	private bool AreLiteralArgs(QilNode nd)
	{
		foreach (QilNode item in nd)
		{
			if (!IsLiteral(item))
			{
				return false;
			}
		}
		return true;
	}

	private object ExtractLiteralValue(QilNode nd)
	{
		if (nd.NodeType == QilNodeType.True)
		{
			return true;
		}
		if (nd.NodeType == QilNodeType.False)
		{
			return false;
		}
		if (nd.NodeType == QilNodeType.LiteralQName)
		{
			return nd;
		}
		return ((QilLiteral)nd).Value;
	}

	private bool HasNestedSequence(QilNode nd)
	{
		foreach (QilNode item in nd)
		{
			if (item.NodeType == QilNodeType.Sequence)
			{
				return true;
			}
		}
		return false;
	}

	private bool AllowJoinAndDod(QilNode nd)
	{
		OptimizerPatterns optimizerPatterns = OptimizerPatterns.Read(nd);
		if ((optimizerPatterns.MatchesPattern(OptimizerPatternName.FilterElements) || optimizerPatterns.MatchesPattern(OptimizerPatternName.FilterContentKind)) && (IsStepPattern(optimizerPatterns, QilNodeType.DescendantOrSelf) || IsStepPattern(optimizerPatterns, QilNodeType.Descendant) || IsStepPattern(optimizerPatterns, QilNodeType.Content) || IsStepPattern(optimizerPatterns, QilNodeType.XPathPreceding) || IsStepPattern(optimizerPatterns, QilNodeType.XPathFollowing) || IsStepPattern(optimizerPatterns, QilNodeType.FollowingSibling)))
		{
			return true;
		}
		return false;
	}

	private bool AllowDodReverse(QilNode nd)
	{
		OptimizerPatterns optimizerPatterns = OptimizerPatterns.Read(nd);
		if ((optimizerPatterns.MatchesPattern(OptimizerPatternName.Axis) || optimizerPatterns.MatchesPattern(OptimizerPatternName.FilterElements) || optimizerPatterns.MatchesPattern(OptimizerPatternName.FilterContentKind)) && (IsStepPattern(optimizerPatterns, QilNodeType.Ancestor) || IsStepPattern(optimizerPatterns, QilNodeType.AncestorOrSelf) || IsStepPattern(optimizerPatterns, QilNodeType.XPathPreceding) || IsStepPattern(optimizerPatterns, QilNodeType.PrecedingSibling)))
		{
			return true;
		}
		return false;
	}

	private bool CanFoldXsltConvert(QilNode ndLiteral, XmlQueryType typTarget)
	{
		return FoldXsltConvert(ndLiteral, typTarget).NodeType != QilNodeType.XsltConvert;
	}

	private bool CanFoldXsltConvertNonLossy(QilNode ndLiteral, XmlQueryType typTarget)
	{
		QilNode qilNode = FoldXsltConvert(ndLiteral, typTarget);
		if (qilNode.NodeType == QilNodeType.XsltConvert)
		{
			return false;
		}
		qilNode = FoldXsltConvert(qilNode, ndLiteral.XmlType);
		if (qilNode.NodeType == QilNodeType.XsltConvert)
		{
			return false;
		}
		return ExtractLiteralValue(ndLiteral).Equals(ExtractLiteralValue(qilNode));
	}

	private QilNode FoldXsltConvert(QilNode ndLiteral, XmlQueryType typTarget)
	{
		try
		{
			if (typTarget.IsAtomicValue)
			{
				XmlAtomicValue value = new XmlAtomicValue(ndLiteral.XmlType.SchemaType, ExtractLiteralValue(ndLiteral));
				value = XsltConvert.ConvertToType(value, typTarget);
				if (typTarget == XmlQueryTypeFactory.StringX)
				{
					return f.LiteralString(value.Value);
				}
				if (typTarget == XmlQueryTypeFactory.IntX)
				{
					return f.LiteralInt32(value.ValueAsInt);
				}
				if (typTarget == XmlQueryTypeFactory.IntegerX)
				{
					return f.LiteralInt64(value.ValueAsLong);
				}
				if (typTarget == XmlQueryTypeFactory.DecimalX)
				{
					return f.LiteralDecimal((decimal)value.ValueAs(XsltConvert.DecimalType));
				}
				if (typTarget == XmlQueryTypeFactory.DoubleX)
				{
					return f.LiteralDouble(value.ValueAsDouble);
				}
				if (typTarget == XmlQueryTypeFactory.BooleanX)
				{
					return value.ValueAsBoolean ? f.True() : f.False();
				}
			}
		}
		catch (OverflowException)
		{
		}
		catch (FormatException)
		{
		}
		return f.XsltConvert(ndLiteral, typTarget);
	}

	private QilNode FoldComparison(QilNodeType opType, QilNode left, QilNode right)
	{
		object obj = ExtractLiteralValue(left);
		object obj2 = ExtractLiteralValue(right);
		if (left.NodeType == QilNodeType.LiteralDouble && (double.IsNaN((double)obj) || double.IsNaN((double)obj2)))
		{
			if (opType != QilNodeType.Ne)
			{
				return f.False();
			}
			return f.True();
		}
		switch (opType)
		{
		case QilNodeType.Eq:
			if (!obj.Equals(obj2))
			{
				return f.False();
			}
			return f.True();
		case QilNodeType.Ne:
			if (!obj.Equals(obj2))
			{
				return f.True();
			}
			return f.False();
		default:
		{
			int num = ((left.NodeType != QilNodeType.LiteralString) ? ((IComparable)obj).CompareTo(obj2) : string.CompareOrdinal((string)obj, (string)obj2));
			switch (opType)
			{
			case QilNodeType.Gt:
				if (num <= 0)
				{
					return f.False();
				}
				return f.True();
			case QilNodeType.Ge:
				if (num < 0)
				{
					return f.False();
				}
				return f.True();
			case QilNodeType.Lt:
				if (num >= 0)
				{
					return f.False();
				}
				return f.True();
			case QilNodeType.Le:
				if (num > 0)
				{
					return f.False();
				}
				return f.True();
			default:
				return null;
			}
		}
		}
	}

	private bool CanFoldArithmetic(QilNodeType opType, QilLiteral left, QilLiteral right)
	{
		return FoldArithmetic(opType, left, right) is QilLiteral;
	}

	private QilNode FoldArithmetic(QilNodeType opType, QilLiteral left, QilLiteral right)
	{
		checked
		{
			try
			{
				switch (left.NodeType)
				{
				case QilNodeType.LiteralInt32:
				{
					int num7 = left;
					int num8 = right;
					switch (opType)
					{
					case QilNodeType.Add:
						return f.LiteralInt32(num7 + num8);
					case QilNodeType.Subtract:
						return f.LiteralInt32(num7 - num8);
					case QilNodeType.Multiply:
						return f.LiteralInt32(num7 * num8);
					case QilNodeType.Divide:
						return f.LiteralInt32(unchecked(num7 / num8));
					case QilNodeType.Modulo:
						return f.LiteralInt32(unchecked(num7 % num8));
					}
					break;
				}
				case QilNodeType.LiteralInt64:
				{
					long num5 = left;
					long num6 = right;
					switch (opType)
					{
					case QilNodeType.Add:
						return f.LiteralInt64(num5 + num6);
					case QilNodeType.Subtract:
						return f.LiteralInt64(num5 - num6);
					case QilNodeType.Multiply:
						return f.LiteralInt64(num5 * num6);
					case QilNodeType.Divide:
						return f.LiteralInt64(unchecked(num5 / num6));
					case QilNodeType.Modulo:
						return f.LiteralInt64(unchecked(num5 % num6));
					}
					break;
				}
				case QilNodeType.LiteralDecimal:
				{
					decimal num3 = left;
					decimal num4 = right;
					switch (opType)
					{
					case QilNodeType.Add:
						return f.LiteralDecimal(num3 + num4);
					case QilNodeType.Subtract:
						return f.LiteralDecimal(num3 - num4);
					case QilNodeType.Multiply:
						return f.LiteralDecimal(num3 * num4);
					case QilNodeType.Divide:
						return f.LiteralDecimal(num3 / num4);
					case QilNodeType.Modulo:
						return f.LiteralDecimal(num3 % num4);
					}
					break;
				}
				case QilNodeType.LiteralDouble:
				{
					double num = left;
					double num2 = right;
					switch (opType)
					{
					case QilNodeType.Add:
						return f.LiteralDouble(num + num2);
					case QilNodeType.Subtract:
						return f.LiteralDouble(num - num2);
					case QilNodeType.Multiply:
						return f.LiteralDouble(num * num2);
					case QilNodeType.Divide:
						return f.LiteralDouble(num / num2);
					case QilNodeType.Modulo:
						return f.LiteralDouble(num % num2);
					}
					break;
				}
				}
			}
			catch (OverflowException)
			{
			}
			catch (DivideByZeroException)
			{
			}
			return opType switch
			{
				QilNodeType.Add => f.Add(left, right), 
				QilNodeType.Subtract => f.Subtract(left, right), 
				QilNodeType.Multiply => f.Multiply(left, right), 
				QilNodeType.Divide => f.Divide(left, right), 
				QilNodeType.Modulo => f.Modulo(left, right), 
				_ => null, 
			};
		}
	}

	private void AddStepPattern(QilNode nd, QilNode input)
	{
		OptimizerPatterns optimizerPatterns = OptimizerPatterns.Write(nd);
		optimizerPatterns.AddPattern(OptimizerPatternName.Step);
		optimizerPatterns.AddArgument(OptimizerPatternArgument.StepNode, nd);
		optimizerPatterns.AddArgument(OptimizerPatternArgument.StepInput, input);
	}

	private bool IsDocOrderDistinct(QilNode nd)
	{
		return OptimizerPatterns.Read(nd).MatchesPattern(OptimizerPatternName.IsDocOrderDistinct);
	}

	private bool IsStepPattern(QilNode nd, QilNodeType stepType)
	{
		return IsStepPattern(OptimizerPatterns.Read(nd), stepType);
	}

	private bool IsStepPattern(OptimizerPatterns patt, QilNodeType stepType)
	{
		if (patt.MatchesPattern(OptimizerPatternName.Step))
		{
			return ((QilNode)patt.GetArgument(OptimizerPatternArgument.StepNode)).NodeType == stepType;
		}
		return false;
	}

	private static void EliminateUnusedGlobals(IList<QilNode> globals)
	{
		int num = 0;
		for (int i = 0; i < globals.Count; i++)
		{
			QilNode qilNode = globals[i];
			bool flag;
			if (qilNode.NodeType == QilNodeType.Function)
			{
				flag = XmlILConstructInfo.Read(qilNode).CallersInfo.Count != 0;
			}
			else
			{
				OptimizerPatterns optimizerPatterns = OptimizerPatterns.Read(qilNode);
				flag = optimizerPatterns.MatchesPattern(OptimizerPatternName.IsReferenced) || optimizerPatterns.MatchesPattern(OptimizerPatternName.MaybeSideEffects);
			}
			if (flag)
			{
				if (num < i)
				{
					globals[num] = globals[i];
				}
				num++;
			}
		}
		for (int num2 = globals.Count - 1; num2 >= num; num2--)
		{
			globals.RemoveAt(num2);
		}
	}
}

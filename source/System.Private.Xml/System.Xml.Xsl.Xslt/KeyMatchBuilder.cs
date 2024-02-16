using System.Diagnostics.CodeAnalysis;
using System.Xml.Xsl.Qil;
using System.Xml.Xsl.XPath;

namespace System.Xml.Xsl.Xslt;

internal sealed class KeyMatchBuilder : XPathBuilder, XPathPatternParser.IPatternBuilder, IXPathBuilder<QilNode>
{
	internal sealed class PathConvertor : QilReplaceVisitor
	{
		private new readonly XPathQilFactory f;

		private QilNode _fixup;

		public PathConvertor(XPathQilFactory f)
			: base(f.BaseFactory)
		{
			this.f = f;
		}

		public QilNode ConvertReletive2Absolute(QilNode node, QilNode fixup)
		{
			QilDepthChecker.Check(node);
			_fixup = fixup;
			return Visit(node);
		}

		protected override QilNode Visit(QilNode n)
		{
			if (n.NodeType == QilNodeType.Union || n.NodeType == QilNodeType.DocOrderDistinct || n.NodeType == QilNodeType.Filter || n.NodeType == QilNodeType.Loop)
			{
				return base.Visit(n);
			}
			return n;
		}

		protected override QilNode VisitLoop(QilLoop n)
		{
			if (n.Variable.Binding.NodeType == QilNodeType.Root || n.Variable.Binding.NodeType == QilNodeType.Deref)
			{
				return n;
			}
			if (n.Variable.Binding.NodeType == QilNodeType.Content)
			{
				QilUnary qilUnary = (QilUnary)n.Variable.Binding;
				QilIterator variable = (QilIterator)(qilUnary.Child = f.For(f.DescendantOrSelf(f.Root(_fixup))));
				n.Variable.Binding = f.Loop(variable, qilUnary);
				return n;
			}
			n.Variable.Binding = Visit(n.Variable.Binding);
			return n;
		}

		protected override QilNode VisitFilter(QilLoop n)
		{
			return VisitLoop(n);
		}
	}

	private int _depth;

	private readonly PathConvertor _convertor;

	public KeyMatchBuilder(IXPathEnvironment env)
		: base(env)
	{
		_convertor = new PathConvertor(env.Factory);
	}

	public override void StartBuild()
	{
		if (_depth == 0)
		{
			base.StartBuild();
		}
		_depth++;
	}

	[return: NotNullIfNotNull("result")]
	public override QilNode EndBuild(QilNode result)
	{
		_depth--;
		if (result == null)
		{
			return base.EndBuild(result);
		}
		if (_depth == 0)
		{
			result = _convertor.ConvertReletive2Absolute(result, fixupCurrent);
			result = base.EndBuild(result);
		}
		return result;
	}

	public IXPathBuilder<QilNode> GetPredicateBuilder(QilNode ctx)
	{
		return this;
	}
}

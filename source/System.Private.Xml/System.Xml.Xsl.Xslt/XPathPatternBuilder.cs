using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml.XPath;
using System.Xml.Xsl.Qil;
using System.Xml.Xsl.XPath;

namespace System.Xml.Xsl.Xslt;

internal sealed class XPathPatternBuilder : XPathPatternParser.IPatternBuilder, IXPathBuilder<QilNode>
{
	private sealed class Annotation
	{
		public double Priority;

		public QilLoop Parent;
	}

	private sealed class XPathPredicateEnvironment : IXPathEnvironment, IFocus
	{
		private readonly IXPathEnvironment _baseEnvironment;

		private readonly XPathQilFactory _f;

		public readonly XPathBuilder.FixupVisitor fixupVisitor;

		private readonly QilNode _fixupCurrent;

		private readonly QilNode _fixupPosition;

		private readonly QilNode _fixupLast;

		public int numFixupCurrent;

		public int numFixupPosition;

		public int numFixupLast;

		public XPathQilFactory Factory => _f;

		public XPathPredicateEnvironment(IXPathEnvironment baseEnvironment)
		{
			_baseEnvironment = baseEnvironment;
			_f = baseEnvironment.Factory;
			_fixupCurrent = _f.Unknown(XmlQueryTypeFactory.NodeNotRtf);
			_fixupPosition = _f.Unknown(XmlQueryTypeFactory.DoubleX);
			_fixupLast = _f.Unknown(XmlQueryTypeFactory.DoubleX);
			fixupVisitor = new XPathBuilder.FixupVisitor(_f, _fixupCurrent, _fixupPosition, _fixupLast);
		}

		public QilNode ResolveVariable(string prefix, string name)
		{
			return _baseEnvironment.ResolveVariable(prefix, name);
		}

		public QilNode ResolveFunction(string prefix, string name, IList<QilNode> args, IFocus env)
		{
			return _baseEnvironment.ResolveFunction(prefix, name, args, env);
		}

		public string ResolvePrefix(string prefix)
		{
			return _baseEnvironment.ResolvePrefix(prefix);
		}

		public QilNode GetCurrent()
		{
			numFixupCurrent++;
			return _fixupCurrent;
		}

		public QilNode GetPosition()
		{
			numFixupPosition++;
			return _fixupPosition;
		}

		public QilNode GetLast()
		{
			numFixupLast++;
			return _fixupLast;
		}
	}

	private sealed class XsltFunctionFocus : IFocus
	{
		private readonly QilIterator _current;

		public XsltFunctionFocus(QilIterator current)
		{
			_current = current;
		}

		public QilNode GetCurrent()
		{
			return _current;
		}

		public QilNode GetPosition()
		{
			return null;
		}

		public QilNode GetLast()
		{
			return null;
		}
	}

	private readonly XPathPredicateEnvironment _predicateEnvironment;

	private readonly XPathBuilder _predicateBuilder;

	private bool _inTheBuild;

	private readonly XPathQilFactory _f;

	private readonly QilNode _fixupNode;

	private readonly IXPathEnvironment _environment;

	public QilNode FixupNode => _fixupNode;

	public XPathPatternBuilder(IXPathEnvironment environment)
	{
		_environment = environment;
		_f = environment.Factory;
		_predicateEnvironment = new XPathPredicateEnvironment(environment);
		_predicateBuilder = new XPathBuilder(_predicateEnvironment);
		_fixupNode = _f.Unknown(XmlQueryTypeFactory.NodeNotRtfS);
	}

	public void StartBuild()
	{
		_inTheBuild = true;
	}

	private void FixupFilterBinding(QilLoop filter, QilNode newBinding)
	{
		filter.Variable.Binding = newBinding;
	}

	[return: NotNullIfNotNull("result")]
	public QilNode EndBuild(QilNode result)
	{
		_inTheBuild = false;
		return result;
	}

	public QilNode Operator(XPathOperator op, QilNode left, QilNode right)
	{
		if (left.NodeType == QilNodeType.Sequence)
		{
			((QilList)left).Add(right);
			return left;
		}
		return _f.Sequence(left, right);
	}

	private static QilLoop BuildAxisFilter(QilPatternFactory f, QilIterator itr, XPathAxis xpathAxis, XPathNodeType nodeType, string name, string nsUri)
	{
		QilNode right = ((name != null && nsUri != null) ? f.Eq(f.NameOf(itr), f.QName(name, nsUri)) : ((nsUri != null) ? f.Eq(f.NamespaceUriOf(itr), f.String(nsUri)) : ((name != null) ? f.Eq(f.LocalNameOf(itr), f.String(name)) : f.True())));
		XmlNodeKindFlags xmlNodeKindFlags = XPathBuilder.AxisTypeMask(itr.XmlType.NodeKinds, nodeType, xpathAxis);
		QilNode left = ((xmlNodeKindFlags == XmlNodeKindFlags.None) ? f.False() : ((xmlNodeKindFlags == itr.XmlType.NodeKinds) ? f.True() : f.IsType(itr, XmlQueryTypeFactory.NodeChoice(xmlNodeKindFlags))));
		QilLoop qilLoop = f.BaseFactory.Filter(itr, f.And(left, right));
		qilLoop.XmlType = XmlQueryTypeFactory.PrimeProduct(XmlQueryTypeFactory.NodeChoice(xmlNodeKindFlags), qilLoop.XmlType.Cardinality);
		return qilLoop;
	}

	public QilNode Axis(XPathAxis xpathAxis, XPathNodeType nodeType, string prefix, string name)
	{
		QilLoop qilLoop;
		double priority;
		switch (xpathAxis)
		{
		case XPathAxis.DescendantOrSelf:
			return _f.Nop(_fixupNode);
		case XPathAxis.Root:
		{
			QilIterator expr;
			qilLoop = _f.BaseFactory.Filter(expr = _f.For(_fixupNode), _f.IsType(expr, XmlQueryTypeFactory.Document));
			priority = 0.5;
			break;
		}
		default:
		{
			string nsUri = ((prefix == null) ? null : _environment.ResolvePrefix(prefix));
			qilLoop = BuildAxisFilter(_f, _f.For(_fixupNode), xpathAxis, nodeType, name, nsUri);
			switch (nodeType)
			{
			case XPathNodeType.Element:
			case XPathNodeType.Attribute:
				priority = ((name == null) ? ((prefix == null) ? (-0.5) : (-0.25)) : 0.0);
				break;
			case XPathNodeType.ProcessingInstruction:
				priority = ((name != null) ? 0.0 : (-0.5));
				break;
			default:
				priority = -0.5;
				break;
			}
			break;
		}
		}
		SetPriority(qilLoop, priority);
		SetLastParent(qilLoop, qilLoop);
		return qilLoop;
	}

	public QilNode JoinStep(QilNode left, QilNode right)
	{
		if (left.NodeType == QilNodeType.Nop)
		{
			QilUnary qilUnary = (QilUnary)left;
			qilUnary.Child = right;
			return qilUnary;
		}
		CleanAnnotation(left);
		QilLoop qilLoop = (QilLoop)left;
		bool flag = false;
		if (right.NodeType == QilNodeType.Nop)
		{
			flag = true;
			QilUnary qilUnary2 = (QilUnary)right;
			right = qilUnary2.Child;
		}
		QilLoop lastParent = GetLastParent(right);
		FixupFilterBinding(qilLoop, flag ? _f.Ancestor(lastParent.Variable) : _f.Parent(lastParent.Variable));
		lastParent.Body = _f.And(lastParent.Body, _f.Not(_f.IsEmpty(qilLoop)));
		SetPriority(right, 0.5);
		SetLastParent(right, qilLoop);
		return right;
	}

	QilNode IXPathBuilder<QilNode>.Predicate(QilNode node, QilNode condition, bool isReverseStep)
	{
		return null;
	}

	public QilNode BuildPredicates(QilNode nodeset, List<QilNode> predicates)
	{
		List<QilNode> list = new List<QilNode>(predicates.Count);
		foreach (QilNode predicate in predicates)
		{
			list.Add(XPathBuilder.PredicateToBoolean(predicate, _f, _predicateEnvironment));
		}
		QilLoop qilLoop = (QilLoop)nodeset;
		QilIterator variable = qilLoop.Variable;
		if (_predicateEnvironment.numFixupLast == 0 && _predicateEnvironment.numFixupPosition == 0)
		{
			foreach (QilNode item in list)
			{
				qilLoop.Body = _f.And(qilLoop.Body, item);
			}
			qilLoop.Body = _predicateEnvironment.fixupVisitor.Fixup(qilLoop.Body, variable, null);
		}
		else
		{
			QilIterator qilIterator = _f.For(_f.Parent(variable));
			QilNode binding = _f.Content(qilIterator);
			QilLoop qilLoop2 = (QilLoop)nodeset.DeepClone(_f.BaseFactory);
			qilLoop2.Variable.Binding = binding;
			qilLoop2 = (QilLoop)_f.Loop(qilIterator, qilLoop2);
			QilNode qilNode = qilLoop2;
			foreach (QilNode item2 in list)
			{
				qilNode = XPathBuilder.BuildOnePredicate(qilNode, item2, isReverseStep: false, _f, _predicateEnvironment.fixupVisitor, ref _predicateEnvironment.numFixupCurrent, ref _predicateEnvironment.numFixupPosition, ref _predicateEnvironment.numFixupLast);
			}
			QilIterator qilIterator2 = _f.For(qilNode);
			QilNode set = _f.Filter(qilIterator2, _f.Is(qilIterator2, variable));
			qilLoop.Body = _f.Not(_f.IsEmpty(set));
			qilLoop.Body = _f.And(_f.IsType(variable, qilLoop.XmlType), qilLoop.Body);
		}
		SetPriority(nodeset, 0.5);
		return nodeset;
	}

	public QilNode Function(string prefix, string name, IList<QilNode> args)
	{
		QilIterator qilIterator = _f.For(_fixupNode);
		QilNode binding = ((!(name == "id")) ? _environment.ResolveFunction(prefix, name, args, new XsltFunctionFocus(qilIterator)) : _f.Id(qilIterator, args[0]));
		QilIterator left;
		QilLoop qilLoop = _f.BaseFactory.Filter(qilIterator, _f.Not(_f.IsEmpty(_f.Filter(left = _f.For(binding), _f.Is(left, qilIterator)))));
		SetPriority(qilLoop, 0.5);
		SetLastParent(qilLoop, qilLoop);
		return qilLoop;
	}

	public QilNode String(string value)
	{
		return _f.String(value);
	}

	public QilNode Number(double value)
	{
		throw new XmlException(System.SR.Xml_InternalError);
	}

	public QilNode Variable(string prefix, string name)
	{
		throw new XmlException(System.SR.Xml_InternalError);
	}

	public static void SetPriority(QilNode node, double priority)
	{
		Annotation annotation = ((Annotation)node.Annotation) ?? new Annotation();
		annotation.Priority = priority;
		node.Annotation = annotation;
	}

	public static double GetPriority(QilNode node)
	{
		return ((Annotation)node.Annotation).Priority;
	}

	private static void SetLastParent(QilNode node, QilLoop parent)
	{
		Annotation annotation = ((Annotation)node.Annotation) ?? new Annotation();
		annotation.Parent = parent;
		node.Annotation = annotation;
	}

	private static QilLoop GetLastParent(QilNode node)
	{
		return ((Annotation)node.Annotation).Parent;
	}

	public static void CleanAnnotation(QilNode node)
	{
		node.Annotation = null;
	}

	public IXPathBuilder<QilNode> GetPredicateBuilder(QilNode ctx)
	{
		QilLoop qilLoop = (QilLoop)ctx;
		return _predicateBuilder;
	}
}

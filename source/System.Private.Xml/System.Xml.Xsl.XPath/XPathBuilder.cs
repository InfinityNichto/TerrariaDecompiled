using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Xml.Xsl.Qil;

namespace System.Xml.Xsl.XPath;

internal class XPathBuilder : IXPathBuilder<QilNode>, IXPathEnvironment, IFocus
{
	private enum XPathOperatorGroup
	{
		Unknown,
		Logical,
		Equality,
		Relational,
		Arithmetic,
		Negate,
		Union
	}

	internal enum FuncId
	{
		Last,
		Position,
		Count,
		LocalName,
		NamespaceUri,
		Name,
		String,
		Number,
		Boolean,
		True,
		False,
		Not,
		Id,
		Concat,
		StartsWith,
		Contains,
		SubstringBefore,
		SubstringAfter,
		Substring,
		StringLength,
		Normalize,
		Translate,
		Lang,
		Sum,
		Floor,
		Ceiling,
		Round
	}

	internal sealed class FixupVisitor : QilReplaceVisitor
	{
		private new readonly QilPatternFactory f;

		private readonly QilNode _fixupCurrent;

		private readonly QilNode _fixupPosition;

		private readonly QilNode _fixupLast;

		private QilIterator _current;

		private QilNode _last;

		private bool _justCount;

		private IXPathEnvironment _environment;

		public int numCurrent;

		public int numPosition;

		public int numLast;

		public FixupVisitor(QilPatternFactory f, QilNode fixupCurrent, QilNode fixupPosition, QilNode fixupLast)
			: base(f.BaseFactory)
		{
			this.f = f;
			_fixupCurrent = fixupCurrent;
			_fixupPosition = fixupPosition;
			_fixupLast = fixupLast;
		}

		public QilNode Fixup(QilNode inExpr, QilIterator current, QilNode last)
		{
			QilDepthChecker.Check(inExpr);
			_current = current;
			_last = last;
			_justCount = false;
			_environment = null;
			numCurrent = (numPosition = (numLast = 0));
			inExpr = VisitAssumeReference(inExpr);
			return inExpr;
		}

		public QilNode Fixup(QilNode inExpr, IXPathEnvironment environment)
		{
			QilDepthChecker.Check(inExpr);
			_justCount = false;
			_current = null;
			_environment = environment;
			numCurrent = (numPosition = (numLast = 0));
			inExpr = VisitAssumeReference(inExpr);
			return inExpr;
		}

		public int CountUnfixedLast(QilNode inExpr)
		{
			_justCount = true;
			numCurrent = (numPosition = (numLast = 0));
			VisitAssumeReference(inExpr);
			return numLast;
		}

		protected override QilNode VisitUnknown(QilNode unknown)
		{
			if (unknown == _fixupCurrent)
			{
				numCurrent++;
				if (!_justCount)
				{
					if (_environment != null)
					{
						unknown = _environment.GetCurrent();
					}
					else if (_current != null)
					{
						unknown = _current;
					}
				}
			}
			else if (unknown == _fixupPosition)
			{
				numPosition++;
				if (!_justCount)
				{
					if (_environment != null)
					{
						unknown = _environment.GetPosition();
					}
					else if (_current != null)
					{
						unknown = f.XsltConvert(f.PositionOf(_current), XmlQueryTypeFactory.DoubleX);
					}
				}
			}
			else if (unknown == _fixupLast)
			{
				numLast++;
				if (!_justCount)
				{
					if (_environment != null)
					{
						unknown = _environment.GetLast();
					}
					else if (_current != null)
					{
						unknown = _last;
					}
				}
			}
			return unknown;
		}
	}

	internal sealed class FunctionInfo<T>
	{
		public T id;

		public int minArgs;

		public int maxArgs;

		public XmlTypeCode[] argTypes;

		public FunctionInfo(T id, int minArgs, int maxArgs, XmlTypeCode[] argTypes)
		{
			this.id = id;
			this.minArgs = minArgs;
			this.maxArgs = maxArgs;
			this.argTypes = argTypes;
		}

		public static void CheckArity(int minArgs, int maxArgs, string name, int numArgs)
		{
			if (minArgs <= numArgs && numArgs <= maxArgs)
			{
				return;
			}
			string resId = ((minArgs == maxArgs) ? System.SR.XPath_NArgsExpected : ((maxArgs == minArgs + 1) ? System.SR.XPath_NOrMArgsExpected : ((numArgs >= minArgs) ? System.SR.XPath_AtMostMArgsExpected : System.SR.XPath_AtLeastNArgsExpected)));
			throw new XPathCompileException(resId, name, minArgs.ToString(CultureInfo.InvariantCulture), maxArgs.ToString(CultureInfo.InvariantCulture));
		}

		public void CastArguments(IList<QilNode> args, string name, XPathQilFactory f)
		{
			CheckArity(minArgs, maxArgs, name, args.Count);
			if (maxArgs == int.MaxValue)
			{
				for (int i = 0; i < args.Count; i++)
				{
					args[i] = f.ConvertToType(XmlTypeCode.String, args[i]);
				}
				return;
			}
			for (int j = 0; j < args.Count; j++)
			{
				if (argTypes[j] == XmlTypeCode.Node && f.CannotBeNodeSet(args[j]))
				{
					throw new XPathCompileException(System.SR.XPath_NodeSetArgumentExpected, name, (j + 1).ToString(CultureInfo.InvariantCulture));
				}
				args[j] = f.ConvertToType(argTypes[j], args[j]);
			}
		}
	}

	private readonly XPathQilFactory _f;

	private readonly IXPathEnvironment _environment;

	private bool _inTheBuild;

	protected QilNode fixupCurrent;

	protected QilNode fixupPosition;

	protected QilNode fixupLast;

	protected int numFixupCurrent;

	protected int numFixupPosition;

	protected int numFixupLast;

	private readonly FixupVisitor _fixupVisitor;

	private static readonly XmlNodeKindFlags[] s_XPathNodeType2QilXmlNodeKind = new XmlNodeKindFlags[10]
	{
		XmlNodeKindFlags.Document,
		XmlNodeKindFlags.Element,
		XmlNodeKindFlags.Attribute,
		XmlNodeKindFlags.Namespace,
		XmlNodeKindFlags.Text,
		XmlNodeKindFlags.Text,
		XmlNodeKindFlags.Text,
		XmlNodeKindFlags.PI,
		XmlNodeKindFlags.Comment,
		XmlNodeKindFlags.Any
	};

	private static readonly XPathOperatorGroup[] s_operatorGroup = new XPathOperatorGroup[16]
	{
		XPathOperatorGroup.Unknown,
		XPathOperatorGroup.Logical,
		XPathOperatorGroup.Logical,
		XPathOperatorGroup.Equality,
		XPathOperatorGroup.Equality,
		XPathOperatorGroup.Relational,
		XPathOperatorGroup.Relational,
		XPathOperatorGroup.Relational,
		XPathOperatorGroup.Relational,
		XPathOperatorGroup.Arithmetic,
		XPathOperatorGroup.Arithmetic,
		XPathOperatorGroup.Arithmetic,
		XPathOperatorGroup.Arithmetic,
		XPathOperatorGroup.Arithmetic,
		XPathOperatorGroup.Negate,
		XPathOperatorGroup.Union
	};

	private static readonly QilNodeType[] s_qilOperator = new QilNodeType[16]
	{
		QilNodeType.Unknown,
		QilNodeType.Or,
		QilNodeType.And,
		QilNodeType.Eq,
		QilNodeType.Ne,
		QilNodeType.Lt,
		QilNodeType.Le,
		QilNodeType.Gt,
		QilNodeType.Ge,
		QilNodeType.Add,
		QilNodeType.Subtract,
		QilNodeType.Multiply,
		QilNodeType.Divide,
		QilNodeType.Modulo,
		QilNodeType.Negate,
		QilNodeType.Sequence
	};

	private static readonly XmlNodeKindFlags[] s_XPathAxisMask = new XmlNodeKindFlags[15]
	{
		XmlNodeKindFlags.None,
		XmlNodeKindFlags.Document | XmlNodeKindFlags.Element,
		XmlNodeKindFlags.Any,
		XmlNodeKindFlags.Attribute,
		XmlNodeKindFlags.Content,
		XmlNodeKindFlags.Content,
		XmlNodeKindFlags.Any,
		XmlNodeKindFlags.Content,
		XmlNodeKindFlags.Content,
		XmlNodeKindFlags.Namespace,
		XmlNodeKindFlags.Document | XmlNodeKindFlags.Element,
		XmlNodeKindFlags.Content,
		XmlNodeKindFlags.Content,
		XmlNodeKindFlags.Any,
		XmlNodeKindFlags.Document
	};

	public static readonly XmlTypeCode[] argAny = new XmlTypeCode[1] { XmlTypeCode.Item };

	public static readonly XmlTypeCode[] argNodeSet = new XmlTypeCode[1] { XmlTypeCode.Node };

	public static readonly XmlTypeCode[] argBoolean = new XmlTypeCode[1] { XmlTypeCode.Boolean };

	public static readonly XmlTypeCode[] argDouble = new XmlTypeCode[1] { XmlTypeCode.Double };

	public static readonly XmlTypeCode[] argString = new XmlTypeCode[1] { XmlTypeCode.String };

	public static readonly XmlTypeCode[] argString2 = new XmlTypeCode[2]
	{
		XmlTypeCode.String,
		XmlTypeCode.String
	};

	public static readonly XmlTypeCode[] argString3 = new XmlTypeCode[3]
	{
		XmlTypeCode.String,
		XmlTypeCode.String,
		XmlTypeCode.String
	};

	public static readonly XmlTypeCode[] argFnSubstr = new XmlTypeCode[3]
	{
		XmlTypeCode.String,
		XmlTypeCode.Double,
		XmlTypeCode.Double
	};

	public static Dictionary<string, FunctionInfo<FuncId>> FunctionTable = CreateFunctionTable();

	XPathQilFactory IXPathEnvironment.Factory => _f;

	QilNode IFocus.GetCurrent()
	{
		return GetCurrentNode();
	}

	QilNode IFocus.GetPosition()
	{
		return GetCurrentPosition();
	}

	QilNode IFocus.GetLast()
	{
		return GetLastPosition();
	}

	QilNode IXPathEnvironment.ResolveVariable(string prefix, string name)
	{
		return Variable(prefix, name);
	}

	QilNode IXPathEnvironment.ResolveFunction(string prefix, string name, IList<QilNode> args, IFocus env)
	{
		return null;
	}

	string IXPathEnvironment.ResolvePrefix(string prefix)
	{
		return _environment.ResolvePrefix(prefix);
	}

	public XPathBuilder(IXPathEnvironment environment)
	{
		_environment = environment;
		_f = _environment.Factory;
		fixupCurrent = _f.Unknown(XmlQueryTypeFactory.NodeNotRtf);
		fixupPosition = _f.Unknown(XmlQueryTypeFactory.DoubleX);
		fixupLast = _f.Unknown(XmlQueryTypeFactory.DoubleX);
		_fixupVisitor = new FixupVisitor(_f, fixupCurrent, fixupPosition, fixupLast);
	}

	public virtual void StartBuild()
	{
		_inTheBuild = true;
		numFixupCurrent = (numFixupPosition = (numFixupLast = 0));
	}

	[return: NotNullIfNotNull("result")]
	public virtual QilNode EndBuild(QilNode result)
	{
		if (result == null)
		{
			_inTheBuild = false;
			return result;
		}
		if (result.XmlType.MaybeMany && result.XmlType.IsNode && result.XmlType.IsNotRtf)
		{
			result = _f.DocOrderDistinct(result);
		}
		result = _fixupVisitor.Fixup(result, _environment);
		numFixupCurrent -= _fixupVisitor.numCurrent;
		numFixupPosition -= _fixupVisitor.numPosition;
		numFixupLast -= _fixupVisitor.numLast;
		_inTheBuild = false;
		return result;
	}

	private QilNode GetCurrentNode()
	{
		numFixupCurrent++;
		return fixupCurrent;
	}

	private QilNode GetCurrentPosition()
	{
		numFixupPosition++;
		return fixupPosition;
	}

	private QilNode GetLastPosition()
	{
		numFixupLast++;
		return fixupLast;
	}

	public virtual QilNode String(string value)
	{
		return _f.String(value);
	}

	public virtual QilNode Number(double value)
	{
		return _f.Double(value);
	}

	public virtual QilNode Operator(XPathOperator op, QilNode left, QilNode right)
	{
		return s_operatorGroup[(int)op] switch
		{
			XPathOperatorGroup.Logical => LogicalOperator(op, left, right), 
			XPathOperatorGroup.Equality => EqualityOperator(op, left, right), 
			XPathOperatorGroup.Relational => RelationalOperator(op, left, right), 
			XPathOperatorGroup.Arithmetic => ArithmeticOperator(op, left, right), 
			XPathOperatorGroup.Negate => NegateOperator(op, left), 
			XPathOperatorGroup.Union => UnionOperator(op, left, right), 
			_ => null, 
		};
	}

	private QilNode LogicalOperator(XPathOperator op, QilNode left, QilNode right)
	{
		left = _f.ConvertToBoolean(left);
		right = _f.ConvertToBoolean(right);
		if (op != XPathOperator.Or)
		{
			return _f.And(left, right);
		}
		return _f.Or(left, right);
	}

	private QilNode CompareValues(XPathOperator op, QilNode left, QilNode right, XmlTypeCode compType)
	{
		left = _f.ConvertToType(compType, left);
		right = _f.ConvertToType(compType, right);
		return op switch
		{
			XPathOperator.Eq => _f.Eq(left, right), 
			XPathOperator.Ne => _f.Ne(left, right), 
			XPathOperator.Lt => _f.Lt(left, right), 
			XPathOperator.Le => _f.Le(left, right), 
			XPathOperator.Gt => _f.Gt(left, right), 
			XPathOperator.Ge => _f.Ge(left, right), 
			_ => null, 
		};
	}

	private QilNode CompareNodeSetAndValue(XPathOperator op, QilNode nodeset, QilNode val, XmlTypeCode compType)
	{
		if (compType == XmlTypeCode.Boolean || nodeset.XmlType.IsSingleton)
		{
			return CompareValues(op, nodeset, val, compType);
		}
		QilIterator qilIterator = _f.For(nodeset);
		return _f.Not(_f.IsEmpty(_f.Filter(qilIterator, CompareValues(op, _f.XPathNodeValue(qilIterator), val, compType))));
	}

	private static XPathOperator InvertOp(XPathOperator op)
	{
		return op switch
		{
			XPathOperator.Ge => XPathOperator.Le, 
			XPathOperator.Gt => XPathOperator.Lt, 
			XPathOperator.Le => XPathOperator.Ge, 
			XPathOperator.Lt => XPathOperator.Gt, 
			_ => op, 
		};
	}

	private QilNode CompareNodeSetAndNodeSet(XPathOperator op, QilNode left, QilNode right, XmlTypeCode compType)
	{
		if (right.XmlType.IsSingleton)
		{
			return CompareNodeSetAndValue(op, left, right, compType);
		}
		if (left.XmlType.IsSingleton)
		{
			op = InvertOp(op);
			return CompareNodeSetAndValue(op, right, left, compType);
		}
		QilIterator qilIterator = _f.For(left);
		QilIterator qilIterator2 = _f.For(right);
		return _f.Not(_f.IsEmpty(_f.Loop(qilIterator, _f.Filter(qilIterator2, CompareValues(op, _f.XPathNodeValue(qilIterator), _f.XPathNodeValue(qilIterator2), compType)))));
	}

	private QilNode EqualityOperator(XPathOperator op, QilNode left, QilNode right)
	{
		XmlQueryType xmlType = left.XmlType;
		XmlQueryType xmlType2 = right.XmlType;
		if (_f.IsAnyType(left) || _f.IsAnyType(right))
		{
			return _f.InvokeEqualityOperator(s_qilOperator[(int)op], left, right);
		}
		if (xmlType.IsNode && xmlType2.IsNode)
		{
			return CompareNodeSetAndNodeSet(op, left, right, XmlTypeCode.String);
		}
		if (xmlType.IsNode)
		{
			return CompareNodeSetAndValue(op, left, right, xmlType2.TypeCode);
		}
		if (xmlType2.IsNode)
		{
			return CompareNodeSetAndValue(op, right, left, xmlType.TypeCode);
		}
		XmlTypeCode compType = ((xmlType.TypeCode == XmlTypeCode.Boolean || xmlType2.TypeCode == XmlTypeCode.Boolean) ? XmlTypeCode.Boolean : ((xmlType.TypeCode == XmlTypeCode.Double || xmlType2.TypeCode == XmlTypeCode.Double) ? XmlTypeCode.Double : XmlTypeCode.String));
		return CompareValues(op, left, right, compType);
	}

	private QilNode RelationalOperator(XPathOperator op, QilNode left, QilNode right)
	{
		XmlQueryType xmlType = left.XmlType;
		XmlQueryType xmlType2 = right.XmlType;
		if (_f.IsAnyType(left) || _f.IsAnyType(right))
		{
			return _f.InvokeRelationalOperator(s_qilOperator[(int)op], left, right);
		}
		if (xmlType.IsNode && xmlType2.IsNode)
		{
			return CompareNodeSetAndNodeSet(op, left, right, XmlTypeCode.Double);
		}
		if (xmlType.IsNode)
		{
			XmlTypeCode compType = ((xmlType2.TypeCode == XmlTypeCode.Boolean) ? XmlTypeCode.Boolean : XmlTypeCode.Double);
			return CompareNodeSetAndValue(op, left, right, compType);
		}
		if (xmlType2.IsNode)
		{
			XmlTypeCode compType2 = ((xmlType.TypeCode == XmlTypeCode.Boolean) ? XmlTypeCode.Boolean : XmlTypeCode.Double);
			op = InvertOp(op);
			return CompareNodeSetAndValue(op, right, left, compType2);
		}
		return CompareValues(op, left, right, XmlTypeCode.Double);
	}

	private QilNode NegateOperator(XPathOperator op, QilNode left)
	{
		return _f.Negate(_f.ConvertToNumber(left));
	}

	private QilNode ArithmeticOperator(XPathOperator op, QilNode left, QilNode right)
	{
		left = _f.ConvertToNumber(left);
		right = _f.ConvertToNumber(right);
		return op switch
		{
			XPathOperator.Plus => _f.Add(left, right), 
			XPathOperator.Minus => _f.Subtract(left, right), 
			XPathOperator.Multiply => _f.Multiply(left, right), 
			XPathOperator.Divide => _f.Divide(left, right), 
			XPathOperator.Modulo => _f.Modulo(left, right), 
			_ => null, 
		};
	}

	private QilNode UnionOperator(XPathOperator op, QilNode left, QilNode right)
	{
		if (left == null)
		{
			return _f.EnsureNodeSet(right);
		}
		left = _f.EnsureNodeSet(left);
		right = _f.EnsureNodeSet(right);
		if (left.NodeType == QilNodeType.Sequence)
		{
			((QilList)left).Add(right);
			return left;
		}
		return _f.Union(left, right);
	}

	public static XmlNodeKindFlags AxisTypeMask(XmlNodeKindFlags inputTypeMask, XPathNodeType nodeType, XPathAxis xpathAxis)
	{
		return inputTypeMask & s_XPathNodeType2QilXmlNodeKind[(int)nodeType] & s_XPathAxisMask[(int)xpathAxis];
	}

	private QilNode BuildAxisFilter(QilNode qilAxis, XPathAxis xpathAxis, XPathNodeType nodeType, string name, string nsUri)
	{
		XmlNodeKindFlags nodeKinds = qilAxis.XmlType.NodeKinds;
		XmlNodeKindFlags xmlNodeKindFlags = AxisTypeMask(nodeKinds, nodeType, xpathAxis);
		if (xmlNodeKindFlags == XmlNodeKindFlags.None)
		{
			return _f.Sequence();
		}
		QilIterator expr;
		if (xmlNodeKindFlags != nodeKinds)
		{
			qilAxis = _f.Filter(expr = _f.For(qilAxis), _f.IsType(expr, XmlQueryTypeFactory.NodeChoice(xmlNodeKindFlags)));
			qilAxis.XmlType = XmlQueryTypeFactory.PrimeProduct(XmlQueryTypeFactory.NodeChoice(xmlNodeKindFlags), qilAxis.XmlType.Cardinality);
			if (qilAxis.NodeType == QilNodeType.Filter)
			{
				QilLoop qilLoop = (QilLoop)qilAxis;
				qilLoop.Body = _f.And(qilLoop.Body, (name != null && nsUri != null) ? _f.Eq(_f.NameOf(expr), _f.QName(name, nsUri)) : ((nsUri != null) ? _f.Eq(_f.NamespaceUriOf(expr), _f.String(nsUri)) : ((name != null) ? _f.Eq(_f.LocalNameOf(expr), _f.String(name)) : _f.True())));
				return qilLoop;
			}
		}
		return _f.Filter(expr = _f.For(qilAxis), (name != null && nsUri != null) ? _f.Eq(_f.NameOf(expr), _f.QName(name, nsUri)) : ((nsUri != null) ? _f.Eq(_f.NamespaceUriOf(expr), _f.String(nsUri)) : ((name != null) ? _f.Eq(_f.LocalNameOf(expr), _f.String(name)) : _f.True())));
	}

	private QilNode BuildAxis(XPathAxis xpathAxis, XPathNodeType nodeType, string nsUri, string name)
	{
		QilNode currentNode = GetCurrentNode();
		QilNode qilAxis;
		switch (xpathAxis)
		{
		case XPathAxis.Ancestor:
			qilAxis = _f.Ancestor(currentNode);
			break;
		case XPathAxis.AncestorOrSelf:
			qilAxis = _f.AncestorOrSelf(currentNode);
			break;
		case XPathAxis.Attribute:
			qilAxis = _f.Content(currentNode);
			break;
		case XPathAxis.Child:
			qilAxis = _f.Content(currentNode);
			break;
		case XPathAxis.Descendant:
			qilAxis = _f.Descendant(currentNode);
			break;
		case XPathAxis.DescendantOrSelf:
			qilAxis = _f.DescendantOrSelf(currentNode);
			break;
		case XPathAxis.Following:
			qilAxis = _f.XPathFollowing(currentNode);
			break;
		case XPathAxis.FollowingSibling:
			qilAxis = _f.FollowingSibling(currentNode);
			break;
		case XPathAxis.Namespace:
			qilAxis = _f.XPathNamespace(currentNode);
			break;
		case XPathAxis.Parent:
			qilAxis = _f.Parent(currentNode);
			break;
		case XPathAxis.Preceding:
			qilAxis = _f.XPathPreceding(currentNode);
			break;
		case XPathAxis.PrecedingSibling:
			qilAxis = _f.PrecedingSibling(currentNode);
			break;
		case XPathAxis.Self:
			qilAxis = currentNode;
			break;
		case XPathAxis.Root:
			return _f.Root(currentNode);
		default:
			qilAxis = null;
			break;
		}
		QilNode qilNode = BuildAxisFilter(qilAxis, xpathAxis, nodeType, name, nsUri);
		if (xpathAxis == XPathAxis.Ancestor || xpathAxis == XPathAxis.Preceding || xpathAxis == XPathAxis.AncestorOrSelf || xpathAxis == XPathAxis.PrecedingSibling)
		{
			qilNode = _f.BaseFactory.DocOrderDistinct(qilNode);
		}
		return qilNode;
	}

	public virtual QilNode Axis(XPathAxis xpathAxis, XPathNodeType nodeType, string prefix, string name)
	{
		string nsUri = ((prefix == null) ? null : _environment.ResolvePrefix(prefix));
		return BuildAxis(xpathAxis, nodeType, nsUri, name);
	}

	public virtual QilNode JoinStep(QilNode left, QilNode right)
	{
		QilIterator qilIterator = _f.For(_f.EnsureNodeSet(left));
		right = _fixupVisitor.Fixup(right, qilIterator, null);
		numFixupCurrent -= _fixupVisitor.numCurrent;
		numFixupPosition -= _fixupVisitor.numPosition;
		numFixupLast -= _fixupVisitor.numLast;
		return _f.DocOrderDistinct(_f.Loop(qilIterator, right));
	}

	public virtual QilNode Predicate(QilNode nodeset, QilNode predicate, bool isReverseStep)
	{
		if (isReverseStep)
		{
			nodeset = ((QilUnary)nodeset).Child;
		}
		predicate = PredicateToBoolean(predicate, _f, this);
		return BuildOnePredicate(nodeset, predicate, isReverseStep, _f, _fixupVisitor, ref numFixupCurrent, ref numFixupPosition, ref numFixupLast);
	}

	public static QilNode PredicateToBoolean(QilNode predicate, XPathQilFactory f, IXPathEnvironment env)
	{
		QilIterator qilIterator;
		predicate = (f.IsAnyType(predicate) ? f.Loop(qilIterator = f.Let(predicate), f.Conditional(f.IsType(qilIterator, XmlQueryTypeFactory.Double), f.Eq(env.GetPosition(), f.TypeAssert(qilIterator, XmlQueryTypeFactory.DoubleX)), f.ConvertToBoolean(qilIterator))) : ((predicate.XmlType.TypeCode != XmlTypeCode.Double) ? f.ConvertToBoolean(predicate) : f.Eq(env.GetPosition(), predicate)));
		return predicate;
	}

	public static QilNode BuildOnePredicate(QilNode nodeset, QilNode predicate, bool isReverseStep, XPathQilFactory f, FixupVisitor fixupVisitor, ref int numFixupCurrent, ref int numFixupPosition, ref int numFixupLast)
	{
		nodeset = f.EnsureNodeSet(nodeset);
		QilNode qilNode;
		if (numFixupLast != 0 && fixupVisitor.CountUnfixedLast(predicate) != 0)
		{
			QilIterator qilIterator = f.Let(nodeset);
			QilIterator qilIterator2 = f.Let(f.XsltConvert(f.Length(qilIterator), XmlQueryTypeFactory.DoubleX));
			QilIterator qilIterator3 = f.For(qilIterator);
			predicate = fixupVisitor.Fixup(predicate, qilIterator3, qilIterator2);
			numFixupCurrent -= fixupVisitor.numCurrent;
			numFixupPosition -= fixupVisitor.numPosition;
			numFixupLast -= fixupVisitor.numLast;
			qilNode = f.Loop(qilIterator, f.Loop(qilIterator2, f.Filter(qilIterator3, predicate)));
		}
		else
		{
			QilIterator qilIterator4 = f.For(nodeset);
			predicate = fixupVisitor.Fixup(predicate, qilIterator4, null);
			numFixupCurrent -= fixupVisitor.numCurrent;
			numFixupPosition -= fixupVisitor.numPosition;
			numFixupLast -= fixupVisitor.numLast;
			qilNode = f.Filter(qilIterator4, predicate);
		}
		if (isReverseStep)
		{
			qilNode = f.DocOrderDistinct(qilNode);
		}
		return qilNode;
	}

	public virtual QilNode Variable(string prefix, string name)
	{
		return _environment.ResolveVariable(prefix, name);
	}

	public virtual QilNode Function(string prefix, string name, IList<QilNode> args)
	{
		if (prefix.Length == 0 && FunctionTable.TryGetValue(name, out var value))
		{
			value.CastArguments(args, name, _f);
			switch (value.id)
			{
			case FuncId.Not:
				return _f.Not(args[0]);
			case FuncId.Last:
				return GetLastPosition();
			case FuncId.Position:
				return GetCurrentPosition();
			case FuncId.Count:
				return _f.XsltConvert(_f.Length(_f.DocOrderDistinct(args[0])), XmlQueryTypeFactory.DoubleX);
			case FuncId.LocalName:
				if (args.Count != 0)
				{
					return LocalNameOfFirstNode(args[0]);
				}
				return _f.LocalNameOf(GetCurrentNode());
			case FuncId.NamespaceUri:
				if (args.Count != 0)
				{
					return NamespaceOfFirstNode(args[0]);
				}
				return _f.NamespaceUriOf(GetCurrentNode());
			case FuncId.Name:
				if (args.Count != 0)
				{
					return NameOfFirstNode(args[0]);
				}
				return NameOf(GetCurrentNode());
			case FuncId.String:
				if (args.Count != 0)
				{
					return _f.ConvertToString(args[0]);
				}
				return _f.XPathNodeValue(GetCurrentNode());
			case FuncId.Number:
				if (args.Count != 0)
				{
					return _f.ConvertToNumber(args[0]);
				}
				return _f.XsltConvert(_f.XPathNodeValue(GetCurrentNode()), XmlQueryTypeFactory.DoubleX);
			case FuncId.Boolean:
				return _f.ConvertToBoolean(args[0]);
			case FuncId.True:
				return _f.True();
			case FuncId.False:
				return _f.False();
			case FuncId.Id:
				return _f.DocOrderDistinct(_f.Id(GetCurrentNode(), args[0]));
			case FuncId.Concat:
				return _f.StrConcat(args);
			case FuncId.StartsWith:
				return _f.InvokeStartsWith(args[0], args[1]);
			case FuncId.Contains:
				return _f.InvokeContains(args[0], args[1]);
			case FuncId.SubstringBefore:
				return _f.InvokeSubstringBefore(args[0], args[1]);
			case FuncId.SubstringAfter:
				return _f.InvokeSubstringAfter(args[0], args[1]);
			case FuncId.Substring:
				if (args.Count != 2)
				{
					return _f.InvokeSubstring(args[0], args[1], args[2]);
				}
				return _f.InvokeSubstring(args[0], args[1]);
			case FuncId.StringLength:
				return _f.XsltConvert(_f.StrLength((args.Count == 0) ? _f.XPathNodeValue(GetCurrentNode()) : args[0]), XmlQueryTypeFactory.DoubleX);
			case FuncId.Normalize:
				return _f.InvokeNormalizeSpace((args.Count == 0) ? _f.XPathNodeValue(GetCurrentNode()) : args[0]);
			case FuncId.Translate:
				return _f.InvokeTranslate(args[0], args[1], args[2]);
			case FuncId.Lang:
				return _f.InvokeLang(args[0], GetCurrentNode());
			case FuncId.Sum:
				return Sum(_f.DocOrderDistinct(args[0]));
			case FuncId.Floor:
				return _f.InvokeFloor(args[0]);
			case FuncId.Ceiling:
				return _f.InvokeCeiling(args[0]);
			case FuncId.Round:
				return _f.InvokeRound(args[0]);
			default:
				return null;
			}
		}
		return _environment.ResolveFunction(prefix, name, args, this);
	}

	private QilNode LocalNameOfFirstNode(QilNode arg)
	{
		if (arg.XmlType.IsSingleton)
		{
			return _f.LocalNameOf(arg);
		}
		QilIterator expr;
		return _f.StrConcat(_f.Loop(expr = _f.FirstNode(arg), _f.LocalNameOf(expr)));
	}

	private QilNode NamespaceOfFirstNode(QilNode arg)
	{
		if (arg.XmlType.IsSingleton)
		{
			return _f.NamespaceUriOf(arg);
		}
		QilIterator expr;
		return _f.StrConcat(_f.Loop(expr = _f.FirstNode(arg), _f.NamespaceUriOf(expr)));
	}

	private QilNode NameOf(QilNode arg)
	{
		QilIterator qilIterator;
		QilIterator qilIterator2;
		if (arg is QilIterator)
		{
			return _f.Loop(qilIterator = _f.Let(_f.PrefixOf(arg)), _f.Loop(qilIterator2 = _f.Let(_f.LocalNameOf(arg)), _f.Conditional(_f.Eq(_f.StrLength(qilIterator), _f.Int32(0)), qilIterator2, _f.StrConcat(qilIterator, _f.String(":"), qilIterator2))));
		}
		QilIterator qilIterator3 = _f.Let(arg);
		return _f.Loop(qilIterator3, NameOf(qilIterator3));
	}

	private QilNode NameOfFirstNode(QilNode arg)
	{
		if (arg.XmlType.IsSingleton)
		{
			return NameOf(arg);
		}
		QilIterator arg2;
		return _f.StrConcat(_f.Loop(arg2 = _f.FirstNode(arg), NameOf(arg2)));
	}

	private QilNode Sum(QilNode arg)
	{
		QilIterator n;
		return _f.Sum(_f.Sequence(_f.Double(0.0), _f.Loop(n = _f.For(arg), _f.ConvertToNumber(n))));
	}

	private static Dictionary<string, FunctionInfo<FuncId>> CreateFunctionTable()
	{
		Dictionary<string, FunctionInfo<FuncId>> dictionary = new Dictionary<string, FunctionInfo<FuncId>>(36);
		dictionary.Add("last", new FunctionInfo<FuncId>(FuncId.Last, 0, 0, null));
		dictionary.Add("position", new FunctionInfo<FuncId>(FuncId.Position, 0, 0, null));
		dictionary.Add("name", new FunctionInfo<FuncId>(FuncId.Name, 0, 1, argNodeSet));
		dictionary.Add("namespace-uri", new FunctionInfo<FuncId>(FuncId.NamespaceUri, 0, 1, argNodeSet));
		dictionary.Add("local-name", new FunctionInfo<FuncId>(FuncId.LocalName, 0, 1, argNodeSet));
		dictionary.Add("count", new FunctionInfo<FuncId>(FuncId.Count, 1, 1, argNodeSet));
		dictionary.Add("id", new FunctionInfo<FuncId>(FuncId.Id, 1, 1, argAny));
		dictionary.Add("string", new FunctionInfo<FuncId>(FuncId.String, 0, 1, argAny));
		dictionary.Add("concat", new FunctionInfo<FuncId>(FuncId.Concat, 2, int.MaxValue, null));
		dictionary.Add("starts-with", new FunctionInfo<FuncId>(FuncId.StartsWith, 2, 2, argString2));
		dictionary.Add("contains", new FunctionInfo<FuncId>(FuncId.Contains, 2, 2, argString2));
		dictionary.Add("substring-before", new FunctionInfo<FuncId>(FuncId.SubstringBefore, 2, 2, argString2));
		dictionary.Add("substring-after", new FunctionInfo<FuncId>(FuncId.SubstringAfter, 2, 2, argString2));
		dictionary.Add("substring", new FunctionInfo<FuncId>(FuncId.Substring, 2, 3, argFnSubstr));
		dictionary.Add("string-length", new FunctionInfo<FuncId>(FuncId.StringLength, 0, 1, argString));
		dictionary.Add("normalize-space", new FunctionInfo<FuncId>(FuncId.Normalize, 0, 1, argString));
		dictionary.Add("translate", new FunctionInfo<FuncId>(FuncId.Translate, 3, 3, argString3));
		dictionary.Add("boolean", new FunctionInfo<FuncId>(FuncId.Boolean, 1, 1, argAny));
		dictionary.Add("not", new FunctionInfo<FuncId>(FuncId.Not, 1, 1, argBoolean));
		dictionary.Add("true", new FunctionInfo<FuncId>(FuncId.True, 0, 0, null));
		dictionary.Add("false", new FunctionInfo<FuncId>(FuncId.False, 0, 0, null));
		dictionary.Add("lang", new FunctionInfo<FuncId>(FuncId.Lang, 1, 1, argString));
		dictionary.Add("number", new FunctionInfo<FuncId>(FuncId.Number, 0, 1, argAny));
		dictionary.Add("sum", new FunctionInfo<FuncId>(FuncId.Sum, 1, 1, argNodeSet));
		dictionary.Add("floor", new FunctionInfo<FuncId>(FuncId.Floor, 1, 1, argDouble));
		dictionary.Add("ceiling", new FunctionInfo<FuncId>(FuncId.Ceiling, 1, 1, argDouble));
		dictionary.Add("round", new FunctionInfo<FuncId>(FuncId.Round, 1, 1, argDouble));
		return dictionary;
	}

	public static bool IsFunctionAvailable(string localName, string nsUri)
	{
		if (nsUri.Length != 0)
		{
			return false;
		}
		return FunctionTable.ContainsKey(localName);
	}
}

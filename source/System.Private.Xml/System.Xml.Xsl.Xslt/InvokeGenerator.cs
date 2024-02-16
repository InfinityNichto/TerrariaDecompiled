using System.Collections.Generic;
using System.Xml.Xsl.Qil;

namespace System.Xml.Xsl.Xslt;

internal sealed class InvokeGenerator : QilCloneVisitor
{
	private readonly bool _debug;

	private readonly Stack<QilIterator> _iterStack;

	private QilList _formalArgs;

	private QilList _invokeArgs;

	private int _curArg;

	private readonly XsltQilFactory _fac;

	public InvokeGenerator(XsltQilFactory f, bool debug)
		: base(f.BaseFactory)
	{
		_debug = debug;
		_fac = f;
		_iterStack = new Stack<QilIterator>();
	}

	public QilNode GenerateInvoke(QilFunction func, IList<XslNode> actualArgs)
	{
		_iterStack.Clear();
		_formalArgs = func.Arguments;
		_invokeArgs = _fac.ActualParameterList();
		for (_curArg = 0; _curArg < _formalArgs.Count; _curArg++)
		{
			QilParameter qilParameter = (QilParameter)_formalArgs[_curArg];
			QilNode qilNode = FindActualArg(qilParameter, actualArgs);
			if (qilNode == null)
			{
				qilNode = ((!_debug) ? Clone(qilParameter.DefaultValue) : ((!(qilParameter.Name.NamespaceUri == "urn:schemas-microsoft-com:xslt-debug")) ? _fac.DefaultValueMarker() : Clone(qilParameter.DefaultValue)));
			}
			XmlQueryType xmlType = qilParameter.XmlType;
			XmlQueryType xmlType2 = qilNode.XmlType;
			if (!xmlType2.IsSubtypeOf(xmlType))
			{
				qilNode = _fac.TypeAssert(qilNode, xmlType);
			}
			_invokeArgs.Add(qilNode);
		}
		QilNode qilNode2 = _fac.Invoke(func, _invokeArgs);
		while (_iterStack.Count != 0)
		{
			qilNode2 = _fac.Loop(_iterStack.Pop(), qilNode2);
		}
		return qilNode2;
	}

	private QilNode FindActualArg(QilParameter formalArg, IList<XslNode> actualArgs)
	{
		QilName name = formalArg.Name;
		foreach (XslNode actualArg in actualArgs)
		{
			if (actualArg.Name.Equals(name))
			{
				return ((VarPar)actualArg).Value;
			}
		}
		return null;
	}

	protected override QilNode VisitReference(QilNode n)
	{
		QilNode qilNode = FindClonedReference(n);
		if (qilNode != null)
		{
			return qilNode;
		}
		for (int i = 0; i < _curArg; i++)
		{
			if (n == _formalArgs[i])
			{
				if (_invokeArgs[i] is QilLiteral)
				{
					return _invokeArgs[i].ShallowClone(_fac.BaseFactory);
				}
				if (!(_invokeArgs[i] is QilIterator))
				{
					QilIterator qilIterator = _fac.BaseFactory.Let(_invokeArgs[i]);
					_iterStack.Push(qilIterator);
					_invokeArgs[i] = qilIterator;
				}
				return _invokeArgs[i];
			}
		}
		return n;
	}

	protected override QilNode VisitFunction(QilFunction n)
	{
		return n;
	}
}

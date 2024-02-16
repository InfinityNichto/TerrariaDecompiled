using System.Collections;
using System.Xml.Xsl.Qil;

namespace System.Xml.Xsl.IlGen;

internal sealed class XmlILConstructInfo : IQilAnnotation
{
	private readonly QilNodeType _nodeType;

	private PossibleXmlStates _xstatesInitial;

	private PossibleXmlStates _xstatesFinal;

	private PossibleXmlStates _xstatesBeginLoop;

	private PossibleXmlStates _xstatesEndLoop;

	private bool _isNmspInScope;

	private bool _mightHaveNmsp;

	private bool _mightHaveAttrs;

	private bool _mightHaveDupAttrs;

	private bool _mightHaveNmspAfterAttrs;

	private XmlILConstructMethod _constrMeth;

	private XmlILConstructInfo _parentInfo;

	private ArrayList _callersInfo;

	private bool _isReadOnly;

	private static volatile XmlILConstructInfo s_default;

	public PossibleXmlStates InitialStates
	{
		get
		{
			return _xstatesInitial;
		}
		set
		{
			_xstatesInitial = value;
		}
	}

	public PossibleXmlStates FinalStates
	{
		get
		{
			return _xstatesFinal;
		}
		set
		{
			_xstatesFinal = value;
		}
	}

	public PossibleXmlStates BeginLoopStates
	{
		set
		{
			_xstatesBeginLoop = value;
		}
	}

	public PossibleXmlStates EndLoopStates
	{
		set
		{
			_xstatesEndLoop = value;
		}
	}

	public XmlILConstructMethod ConstructMethod
	{
		get
		{
			return _constrMeth;
		}
		set
		{
			_constrMeth = value;
		}
	}

	public bool PushToWriterFirst
	{
		set
		{
			switch (_constrMeth)
			{
			case XmlILConstructMethod.Iterator:
				_constrMeth = XmlILConstructMethod.WriterThenIterator;
				break;
			case XmlILConstructMethod.IteratorThenWriter:
				_constrMeth = XmlILConstructMethod.Writer;
				break;
			}
		}
	}

	public bool PushToWriterLast
	{
		get
		{
			if (_constrMeth != XmlILConstructMethod.Writer)
			{
				return _constrMeth == XmlILConstructMethod.IteratorThenWriter;
			}
			return true;
		}
		set
		{
			switch (_constrMeth)
			{
			case XmlILConstructMethod.Iterator:
				_constrMeth = XmlILConstructMethod.IteratorThenWriter;
				break;
			case XmlILConstructMethod.WriterThenIterator:
				_constrMeth = XmlILConstructMethod.Writer;
				break;
			}
		}
	}

	public bool PullFromIteratorFirst
	{
		set
		{
			switch (_constrMeth)
			{
			case XmlILConstructMethod.Writer:
				_constrMeth = XmlILConstructMethod.IteratorThenWriter;
				break;
			case XmlILConstructMethod.WriterThenIterator:
				_constrMeth = XmlILConstructMethod.Iterator;
				break;
			}
		}
	}

	public XmlILConstructInfo ParentInfo
	{
		set
		{
			_parentInfo = value;
		}
	}

	public XmlILConstructInfo ParentElementInfo
	{
		get
		{
			if (_parentInfo != null && _parentInfo._nodeType == QilNodeType.ElementCtor)
			{
				return _parentInfo;
			}
			return null;
		}
	}

	public bool IsNamespaceInScope
	{
		get
		{
			return _isNmspInScope;
		}
		set
		{
			_isNmspInScope = value;
		}
	}

	public bool MightHaveNamespaces
	{
		get
		{
			return _mightHaveNmsp;
		}
		set
		{
			_mightHaveNmsp = value;
		}
	}

	public bool MightHaveNamespacesAfterAttributes
	{
		get
		{
			return _mightHaveNmspAfterAttrs;
		}
		set
		{
			_mightHaveNmspAfterAttrs = value;
		}
	}

	public bool MightHaveAttributes
	{
		get
		{
			return _mightHaveAttrs;
		}
		set
		{
			_mightHaveAttrs = value;
		}
	}

	public bool MightHaveDuplicateAttributes
	{
		get
		{
			return _mightHaveDupAttrs;
		}
		set
		{
			_mightHaveDupAttrs = value;
		}
	}

	public ArrayList CallersInfo
	{
		get
		{
			if (_callersInfo == null)
			{
				_callersInfo = new ArrayList();
			}
			return _callersInfo;
		}
	}

	public static XmlILConstructInfo Read(QilNode nd)
	{
		XmlILConstructInfo xmlILConstructInfo = ((nd.Annotation is XmlILAnnotation xmlILAnnotation) ? xmlILAnnotation.ConstructInfo : null);
		if (xmlILConstructInfo == null)
		{
			if (s_default == null)
			{
				xmlILConstructInfo = new XmlILConstructInfo(QilNodeType.Unknown);
				xmlILConstructInfo._isReadOnly = true;
				s_default = xmlILConstructInfo;
			}
			else
			{
				xmlILConstructInfo = s_default;
			}
		}
		return xmlILConstructInfo;
	}

	public static XmlILConstructInfo Write(QilNode nd)
	{
		XmlILAnnotation xmlILAnnotation = XmlILAnnotation.Write(nd);
		XmlILConstructInfo xmlILConstructInfo = xmlILAnnotation.ConstructInfo;
		if (xmlILConstructInfo == null || xmlILConstructInfo._isReadOnly)
		{
			xmlILConstructInfo = (xmlILAnnotation.ConstructInfo = new XmlILConstructInfo(nd.NodeType));
		}
		return xmlILConstructInfo;
	}

	private XmlILConstructInfo(QilNodeType nodeType)
	{
		_nodeType = nodeType;
		_xstatesInitial = (_xstatesFinal = PossibleXmlStates.Any);
		_xstatesBeginLoop = (_xstatesEndLoop = PossibleXmlStates.None);
		_isNmspInScope = false;
		_mightHaveNmsp = true;
		_mightHaveAttrs = true;
		_mightHaveDupAttrs = true;
		_mightHaveNmspAfterAttrs = true;
		_constrMeth = XmlILConstructMethod.Iterator;
		_parentInfo = null;
	}

	public override string ToString()
	{
		string text = "";
		if (_constrMeth != 0)
		{
			text += _constrMeth;
			text = text + ", " + _xstatesInitial;
			if (_xstatesBeginLoop != 0)
			{
				text = text + " => " + _xstatesBeginLoop.ToString() + " => " + _xstatesEndLoop;
			}
			text = text + " => " + _xstatesFinal;
			if (!MightHaveAttributes)
			{
				text += ", NoAttrs";
			}
			if (!MightHaveDuplicateAttributes)
			{
				text += ", NoDupAttrs";
			}
			if (!MightHaveNamespaces)
			{
				text += ", NoNmsp";
			}
			if (!MightHaveNamespacesAfterAttributes)
			{
				text += ", NoNmspAfterAttrs";
			}
		}
		return text;
	}
}

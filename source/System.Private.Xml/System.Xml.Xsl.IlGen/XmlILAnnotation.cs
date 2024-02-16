using System.Reflection;
using System.Xml.Xsl.Qil;

namespace System.Xml.Xsl.IlGen;

internal sealed class XmlILAnnotation : ListBase<object>
{
	private readonly object _annPrev;

	private MethodInfo _funcMethod;

	private int _argPos;

	private IteratorDescriptor _iterInfo;

	private XmlILConstructInfo _constrInfo;

	private OptimizerPatterns _optPatt;

	public MethodInfo FunctionBinding
	{
		get
		{
			return _funcMethod;
		}
		set
		{
			_funcMethod = value;
		}
	}

	public int ArgumentPosition
	{
		get
		{
			return _argPos;
		}
		set
		{
			_argPos = value;
		}
	}

	public IteratorDescriptor CachedIteratorDescriptor
	{
		get
		{
			return _iterInfo;
		}
		set
		{
			_iterInfo = value;
		}
	}

	public XmlILConstructInfo ConstructInfo
	{
		get
		{
			return _constrInfo;
		}
		set
		{
			_constrInfo = value;
		}
	}

	public OptimizerPatterns Patterns
	{
		get
		{
			return _optPatt;
		}
		set
		{
			_optPatt = value;
		}
	}

	public override int Count
	{
		get
		{
			if (_annPrev == null)
			{
				return 2;
			}
			return 3;
		}
	}

	public override object this[int index]
	{
		get
		{
			if (_annPrev != null)
			{
				if (index == 0)
				{
					return _annPrev;
				}
				index--;
			}
			return index switch
			{
				0 => _constrInfo, 
				1 => _optPatt, 
				_ => throw new IndexOutOfRangeException(), 
			};
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	public static XmlILAnnotation Write(QilNode nd)
	{
		XmlILAnnotation xmlILAnnotation = nd.Annotation as XmlILAnnotation;
		if (xmlILAnnotation == null)
		{
			xmlILAnnotation = (XmlILAnnotation)(nd.Annotation = new XmlILAnnotation(nd.Annotation));
		}
		return xmlILAnnotation;
	}

	private XmlILAnnotation(object annPrev)
	{
		_annPrev = annPrev;
	}
}

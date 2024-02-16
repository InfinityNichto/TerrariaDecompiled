using System.Collections.Generic;
using System.Xml.Xsl.Runtime;

namespace System.Xml.Xsl.Qil;

internal sealed class QilExpression : QilNode
{
	private QilFactory _factory;

	private QilNode _isDebug;

	private QilNode _defWSet;

	private QilNode _wsRules;

	private QilNode _gloVars;

	private QilNode _gloParams;

	private QilNode _earlBnd;

	private QilNode _funList;

	private QilNode _rootNod;

	public override int Count => 8;

	public override QilNode this[int index]
	{
		get
		{
			return index switch
			{
				0 => _isDebug, 
				1 => _defWSet, 
				2 => _wsRules, 
				3 => _gloParams, 
				4 => _gloVars, 
				5 => _earlBnd, 
				6 => _funList, 
				7 => _rootNod, 
				_ => throw new IndexOutOfRangeException(), 
			};
		}
		set
		{
			switch (index)
			{
			case 0:
				_isDebug = value;
				break;
			case 1:
				_defWSet = value;
				break;
			case 2:
				_wsRules = value;
				break;
			case 3:
				_gloParams = value;
				break;
			case 4:
				_gloVars = value;
				break;
			case 5:
				_earlBnd = value;
				break;
			case 6:
				_funList = value;
				break;
			case 7:
				_rootNod = value;
				break;
			default:
				throw new IndexOutOfRangeException();
			}
		}
	}

	public QilFactory Factory => _factory;

	public bool IsDebug
	{
		get
		{
			return _isDebug.NodeType == QilNodeType.True;
		}
		set
		{
			_isDebug = (value ? _factory.True() : _factory.False());
		}
	}

	public XmlWriterSettings DefaultWriterSettings
	{
		get
		{
			return (XmlWriterSettings)((QilLiteral)_defWSet).Value;
		}
		set
		{
			value.ReadOnly = true;
			((QilLiteral)_defWSet).Value = value;
		}
	}

	public IList<WhitespaceRule> WhitespaceRules
	{
		get
		{
			return (IList<WhitespaceRule>)((QilLiteral)_wsRules).Value;
		}
		set
		{
			((QilLiteral)_wsRules).Value = value;
		}
	}

	public QilList GlobalParameterList
	{
		get
		{
			return (QilList)_gloParams;
		}
		set
		{
			_gloParams = value;
		}
	}

	public QilList GlobalVariableList
	{
		get
		{
			return (QilList)_gloVars;
		}
		set
		{
			_gloVars = value;
		}
	}

	public IList<EarlyBoundInfo> EarlyBoundTypes
	{
		get
		{
			return (IList<EarlyBoundInfo>)((QilLiteral)_earlBnd).Value;
		}
		set
		{
			((QilLiteral)_earlBnd).Value = value;
		}
	}

	public QilList FunctionList
	{
		get
		{
			return (QilList)_funList;
		}
		set
		{
			_funList = value;
		}
	}

	public QilNode Root
	{
		get
		{
			return _rootNod;
		}
		set
		{
			_rootNod = value;
		}
	}

	public QilExpression(QilNodeType nodeType, QilNode root, QilFactory factory)
		: base(nodeType)
	{
		_factory = factory;
		_isDebug = factory.False();
		_defWSet = factory.LiteralObject(new XmlWriterSettings
		{
			ConformanceLevel = ConformanceLevel.Auto
		});
		_wsRules = factory.LiteralObject(new List<WhitespaceRule>());
		_gloVars = factory.GlobalVariableList();
		_gloParams = factory.GlobalParameterList();
		_earlBnd = factory.LiteralObject(new List<EarlyBoundInfo>());
		_funList = factory.FunctionList();
		_rootNod = root;
	}
}

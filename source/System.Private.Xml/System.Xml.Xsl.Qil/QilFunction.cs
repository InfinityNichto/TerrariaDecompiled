namespace System.Xml.Xsl.Qil;

internal sealed class QilFunction : QilReference
{
	private QilNode _arguments;

	private QilNode _definition;

	private QilNode _sideEffects;

	public override int Count => 3;

	public override QilNode this[int index]
	{
		get
		{
			return index switch
			{
				0 => _arguments, 
				1 => _definition, 
				2 => _sideEffects, 
				_ => throw new IndexOutOfRangeException(), 
			};
		}
		set
		{
			switch (index)
			{
			case 0:
				_arguments = value;
				break;
			case 1:
				_definition = value;
				break;
			case 2:
				_sideEffects = value;
				break;
			default:
				throw new IndexOutOfRangeException();
			}
		}
	}

	public QilList Arguments => (QilList)_arguments;

	public QilNode Definition
	{
		get
		{
			return _definition;
		}
		set
		{
			_definition = value;
		}
	}

	public bool MaybeSideEffects => _sideEffects.NodeType == QilNodeType.True;

	public QilFunction(QilNodeType nodeType, QilNode arguments, QilNode definition, QilNode sideEffects, XmlQueryType resultType)
		: base(nodeType)
	{
		_arguments = arguments;
		_definition = definition;
		_sideEffects = sideEffects;
		xmlType = resultType;
	}
}

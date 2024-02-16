namespace System.Xml.Xsl.Qil;

internal sealed class QilUnary : QilNode
{
	private QilNode _child;

	public override int Count => 1;

	public override QilNode this[int index]
	{
		get
		{
			if (index != 0)
			{
				throw new IndexOutOfRangeException();
			}
			return _child;
		}
		set
		{
			if (index != 0)
			{
				throw new IndexOutOfRangeException();
			}
			_child = value;
		}
	}

	public QilNode Child
	{
		get
		{
			return _child;
		}
		set
		{
			_child = value;
		}
	}

	public QilUnary(QilNodeType nodeType, QilNode child)
		: base(nodeType)
	{
		_child = child;
	}
}

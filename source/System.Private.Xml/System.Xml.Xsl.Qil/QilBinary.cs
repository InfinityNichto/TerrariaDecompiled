namespace System.Xml.Xsl.Qil;

internal class QilBinary : QilNode
{
	private QilNode _left;

	private QilNode _right;

	public override int Count => 2;

	public override QilNode this[int index]
	{
		get
		{
			return index switch
			{
				0 => _left, 
				1 => _right, 
				_ => throw new IndexOutOfRangeException(), 
			};
		}
		set
		{
			switch (index)
			{
			case 0:
				_left = value;
				break;
			case 1:
				_right = value;
				break;
			default:
				throw new IndexOutOfRangeException();
			}
		}
	}

	public QilNode Left
	{
		get
		{
			return _left;
		}
		set
		{
			_left = value;
		}
	}

	public QilNode Right
	{
		get
		{
			return _right;
		}
		set
		{
			_right = value;
		}
	}

	public QilBinary(QilNodeType nodeType, QilNode left, QilNode right)
		: base(nodeType)
	{
		_left = left;
		_right = right;
	}
}

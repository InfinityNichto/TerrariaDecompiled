namespace System.Xml.Xsl.Qil;

internal class QilTernary : QilNode
{
	private QilNode _left;

	private QilNode _center;

	private QilNode _right;

	public override int Count => 3;

	public override QilNode this[int index]
	{
		get
		{
			return index switch
			{
				0 => _left, 
				1 => _center, 
				2 => _right, 
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
				_center = value;
				break;
			case 2:
				_right = value;
				break;
			default:
				throw new IndexOutOfRangeException();
			}
		}
	}

	public QilNode Left => _left;

	public QilNode Center
	{
		get
		{
			return _center;
		}
		set
		{
			_center = value;
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

	public QilTernary(QilNodeType nodeType, QilNode left, QilNode center, QilNode right)
		: base(nodeType)
	{
		_left = left;
		_center = center;
		_right = right;
	}
}

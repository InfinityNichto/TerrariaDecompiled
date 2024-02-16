namespace System.Xml.Xsl.Qil;

internal sealed class QilList : QilNode
{
	private int _count;

	private QilNode[] _members;

	public override XmlQueryType XmlType
	{
		get
		{
			if (xmlType == null)
			{
				XmlQueryType left = XmlQueryTypeFactory.Empty;
				if (_count > 0)
				{
					if (nodeType == QilNodeType.Sequence)
					{
						for (int i = 0; i < _count; i++)
						{
							left = XmlQueryTypeFactory.Sequence(left, _members[i].XmlType);
						}
					}
					else if (nodeType == QilNodeType.BranchList)
					{
						left = _members[0].XmlType;
						for (int j = 1; j < _count; j++)
						{
							left = XmlQueryTypeFactory.Choice(left, _members[j].XmlType);
						}
					}
				}
				xmlType = left;
			}
			return xmlType;
		}
	}

	public override int Count => _count;

	public override QilNode this[int index]
	{
		get
		{
			if (index >= 0 && index < _count)
			{
				return _members[index];
			}
			throw new IndexOutOfRangeException();
		}
		set
		{
			if (index >= 0 && index < _count)
			{
				_members[index] = value;
				xmlType = null;
				return;
			}
			throw new IndexOutOfRangeException();
		}
	}

	public QilList(QilNodeType nodeType)
		: base(nodeType)
	{
		_members = new QilNode[4];
		xmlType = null;
	}

	public override QilNode ShallowClone(QilFactory f)
	{
		QilList qilList = (QilList)MemberwiseClone();
		qilList._members = (QilNode[])_members.Clone();
		return qilList;
	}

	public override void Insert(int index, QilNode node)
	{
		if (index < 0 || index > _count)
		{
			throw new IndexOutOfRangeException();
		}
		if (_count == _members.Length)
		{
			QilNode[] array = new QilNode[_count * 2];
			Array.Copy(_members, array, _count);
			_members = array;
		}
		if (index < _count)
		{
			Array.Copy(_members, index, _members, index + 1, _count - index);
		}
		_count++;
		_members[index] = node;
		xmlType = null;
	}

	public override void RemoveAt(int index)
	{
		if (index < 0 || index >= _count)
		{
			throw new IndexOutOfRangeException();
		}
		_count--;
		if (index < _count)
		{
			Array.Copy(_members, index + 1, _members, index, _count - index);
		}
		_members[_count] = null;
		xmlType = null;
	}
}

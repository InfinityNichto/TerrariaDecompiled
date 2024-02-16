using System.Collections;
using System.Collections.Generic;

namespace System.Xml.Xsl.Qil;

internal class QilNode : IList<QilNode>, ICollection<QilNode>, IEnumerable<QilNode>, IEnumerable
{
	protected QilNodeType nodeType;

	protected XmlQueryType xmlType;

	protected ISourceLineInfo sourceLine;

	protected object annotation;

	public QilNodeType NodeType
	{
		get
		{
			return nodeType;
		}
		set
		{
			nodeType = value;
		}
	}

	public virtual XmlQueryType XmlType
	{
		get
		{
			return xmlType;
		}
		set
		{
			xmlType = value;
		}
	}

	public ISourceLineInfo SourceLine
	{
		get
		{
			return sourceLine;
		}
		set
		{
			sourceLine = value;
		}
	}

	public object Annotation
	{
		get
		{
			return annotation;
		}
		set
		{
			annotation = value;
		}
	}

	public virtual int Count => 0;

	public virtual QilNode this[int index]
	{
		get
		{
			throw new IndexOutOfRangeException();
		}
		set
		{
			throw new IndexOutOfRangeException();
		}
	}

	public virtual bool IsReadOnly => false;

	public QilNode(QilNodeType nodeType)
	{
		this.nodeType = nodeType;
	}

	public QilNode(QilNodeType nodeType, XmlQueryType xmlType)
	{
		this.nodeType = nodeType;
		this.xmlType = xmlType;
	}

	public virtual QilNode DeepClone(QilFactory f)
	{
		return new QilCloneVisitor(f).Clone(this);
	}

	public virtual QilNode ShallowClone(QilFactory f)
	{
		return (QilNode)MemberwiseClone();
	}

	public virtual void Insert(int index, QilNode node)
	{
		throw new NotSupportedException();
	}

	public virtual void RemoveAt(int index)
	{
		throw new NotSupportedException();
	}

	public IEnumerator<QilNode> GetEnumerator()
	{
		return new IListEnumerator<QilNode>(this);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new IListEnumerator<QilNode>(this);
	}

	public virtual void Add(QilNode node)
	{
		Insert(Count, node);
	}

	public virtual void Add(IList<QilNode> list)
	{
		for (int i = 0; i < list.Count; i++)
		{
			Insert(Count, list[i]);
		}
	}

	public virtual void Clear()
	{
		for (int num = Count - 1; num >= 0; num--)
		{
			RemoveAt(num);
		}
	}

	public virtual bool Contains(QilNode node)
	{
		return IndexOf(node) != -1;
	}

	public virtual void CopyTo(QilNode[] array, int index)
	{
		for (int i = 0; i < Count; i++)
		{
			array[index + i] = this[i];
		}
	}

	public virtual bool Remove(QilNode node)
	{
		int num = IndexOf(node);
		if (num >= 0)
		{
			RemoveAt(num);
			return true;
		}
		return false;
	}

	public virtual int IndexOf(QilNode node)
	{
		for (int i = 0; i < Count; i++)
		{
			if (node.Equals(this[i]))
			{
				return i;
			}
		}
		return -1;
	}
}

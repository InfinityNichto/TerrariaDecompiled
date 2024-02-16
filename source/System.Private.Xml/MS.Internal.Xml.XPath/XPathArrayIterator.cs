using System;
using System.Collections;
using System.Diagnostics;
using System.Xml.XPath;

namespace MS.Internal.Xml.XPath;

[DebuggerDisplay("Position={CurrentPosition}, Current={debuggerDisplayProxy, nq}")]
internal class XPathArrayIterator : ResetableIterator
{
	protected IList list;

	protected int index;

	public IList AsList => list;

	public override XPathNavigator Current
	{
		get
		{
			if (index < 1)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.Sch_EnumNotStarted, string.Empty));
			}
			return (XPathNavigator)list[index - 1];
		}
	}

	public override int CurrentPosition => index;

	public override int Count => list.Count;

	private object debuggerDisplayProxy
	{
		get
		{
			if (index >= 1)
			{
				return new XPathNavigator.DebuggerDisplayProxy(Current);
			}
			return null;
		}
	}

	public XPathArrayIterator(IList list)
	{
		this.list = list;
	}

	public XPathArrayIterator(XPathArrayIterator it)
	{
		list = it.list;
		index = it.index;
	}

	public XPathArrayIterator(XPathNodeIterator nodeIterator)
	{
		list = new ArrayList();
		while (nodeIterator.MoveNext())
		{
			list.Add(nodeIterator.Current.Clone());
		}
	}

	public override XPathNodeIterator Clone()
	{
		return new XPathArrayIterator(this);
	}

	public override bool MoveNext()
	{
		if (index == list.Count)
		{
			return false;
		}
		index++;
		return true;
	}

	public override void Reset()
	{
		index = 0;
	}

	public override IEnumerator GetEnumerator()
	{
		return list.GetEnumerator();
	}
}

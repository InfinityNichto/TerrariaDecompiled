using System.Collections;

namespace System.Xml;

internal sealed class XmlChildEnumerator : IEnumerator
{
	internal XmlNode container;

	internal XmlNode child;

	internal bool isFirst;

	object IEnumerator.Current => Current;

	internal XmlNode Current
	{
		get
		{
			if (isFirst || child == null)
			{
				throw new InvalidOperationException(System.SR.Xml_InvalidOperation);
			}
			return child;
		}
	}

	internal XmlChildEnumerator(XmlNode container)
	{
		this.container = container;
		child = container.FirstChild;
		isFirst = true;
	}

	bool IEnumerator.MoveNext()
	{
		return MoveNext();
	}

	internal bool MoveNext()
	{
		if (isFirst)
		{
			child = container.FirstChild;
			isFirst = false;
		}
		else if (child != null)
		{
			child = child.NextSibling;
		}
		return child != null;
	}

	void IEnumerator.Reset()
	{
		isFirst = true;
		child = container.FirstChild;
	}
}

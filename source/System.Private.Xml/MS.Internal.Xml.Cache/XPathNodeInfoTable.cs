using System.Text;
using System.Xml.XPath;

namespace MS.Internal.Xml.Cache;

internal sealed class XPathNodeInfoTable
{
	private XPathNodeInfoAtom[] _hashTable;

	private int _sizeTable;

	private XPathNodeInfoAtom _infoCached;

	public XPathNodeInfoTable()
	{
		_hashTable = new XPathNodeInfoAtom[32];
		_sizeTable = 0;
	}

	public XPathNodeInfoAtom Create(string localName, string namespaceUri, string prefix, string baseUri, XPathNode[] pageParent, XPathNode[] pageSibling, XPathNode[] pageSimilar, XPathDocument doc, int lineNumBase, int linePosBase)
	{
		XPathNodeInfoAtom xPathNodeInfoAtom;
		if (_infoCached == null)
		{
			xPathNodeInfoAtom = new XPathNodeInfoAtom(localName, namespaceUri, prefix, baseUri, pageParent, pageSibling, pageSimilar, doc, lineNumBase, linePosBase);
		}
		else
		{
			xPathNodeInfoAtom = _infoCached;
			_infoCached = xPathNodeInfoAtom.Next;
			xPathNodeInfoAtom.Init(localName, namespaceUri, prefix, baseUri, pageParent, pageSibling, pageSimilar, doc, lineNumBase, linePosBase);
		}
		return Atomize(xPathNodeInfoAtom);
	}

	private XPathNodeInfoAtom Atomize(XPathNodeInfoAtom info)
	{
		for (XPathNodeInfoAtom xPathNodeInfoAtom = _hashTable[info.GetHashCode() & (_hashTable.Length - 1)]; xPathNodeInfoAtom != null; xPathNodeInfoAtom = xPathNodeInfoAtom.Next)
		{
			if (info.Equals(xPathNodeInfoAtom))
			{
				info.Next = _infoCached;
				_infoCached = info;
				return xPathNodeInfoAtom;
			}
		}
		if (_sizeTable >= _hashTable.Length)
		{
			XPathNodeInfoAtom[] hashTable = _hashTable;
			_hashTable = new XPathNodeInfoAtom[hashTable.Length * 2];
			for (int i = 0; i < hashTable.Length; i++)
			{
				XPathNodeInfoAtom xPathNodeInfoAtom = hashTable[i];
				while (xPathNodeInfoAtom != null)
				{
					XPathNodeInfoAtom next = xPathNodeInfoAtom.Next;
					AddInfo(xPathNodeInfoAtom);
					xPathNodeInfoAtom = next;
				}
			}
		}
		AddInfo(info);
		return info;
	}

	private void AddInfo(XPathNodeInfoAtom info)
	{
		int num = info.GetHashCode() & (_hashTable.Length - 1);
		info.Next = _hashTable[num];
		_hashTable[num] = info;
		_sizeTable++;
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < _hashTable.Length; i++)
		{
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(2, 1, stringBuilder2);
			handler.AppendFormatted(i, 4);
			handler.AppendLiteral(": ");
			stringBuilder2.Append(ref handler);
			for (XPathNodeInfoAtom xPathNodeInfoAtom = _hashTable[i]; xPathNodeInfoAtom != null; xPathNodeInfoAtom = xPathNodeInfoAtom.Next)
			{
				if (xPathNodeInfoAtom != _hashTable[i])
				{
					stringBuilder.Append("\n      ");
				}
				stringBuilder.Append(xPathNodeInfoAtom);
			}
			stringBuilder.Append('\n');
		}
		return stringBuilder.ToString();
	}
}

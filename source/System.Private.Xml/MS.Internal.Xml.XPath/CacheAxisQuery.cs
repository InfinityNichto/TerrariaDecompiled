using System.Collections.Generic;
using System.Xml.XPath;

namespace MS.Internal.Xml.XPath;

internal abstract class CacheAxisQuery : BaseAxisQuery
{
	protected List<XPathNavigator> outputBuffer;

	public override XPathNavigator Current
	{
		get
		{
			if (count == 0)
			{
				return null;
			}
			return outputBuffer[count - 1];
		}
	}

	public override int CurrentPosition => count;

	public override int Count => outputBuffer.Count;

	public override QueryProps Properties => (QueryProps)23;

	public CacheAxisQuery(Query qyInput, string name, string prefix, XPathNodeType typeTest)
		: base(qyInput, name, prefix, typeTest)
	{
		outputBuffer = new List<XPathNavigator>();
		count = 0;
	}

	protected CacheAxisQuery(CacheAxisQuery other)
		: base(other)
	{
		outputBuffer = new List<XPathNavigator>(other.outputBuffer);
		count = other.count;
	}

	public override void Reset()
	{
		count = 0;
	}

	public override object Evaluate(XPathNodeIterator context)
	{
		base.Evaluate(context);
		outputBuffer.Clear();
		return this;
	}

	public override XPathNavigator Advance()
	{
		if (count < outputBuffer.Count)
		{
			return outputBuffer[count++];
		}
		return null;
	}
}

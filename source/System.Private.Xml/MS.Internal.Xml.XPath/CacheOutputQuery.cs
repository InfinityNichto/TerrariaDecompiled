using System.Collections.Generic;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace MS.Internal.Xml.XPath;

internal abstract class CacheOutputQuery : Query
{
	internal Query input;

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

	public override XPathResultType StaticType => XPathResultType.NodeSet;

	public override int CurrentPosition => count;

	public override int Count => outputBuffer.Count;

	public override QueryProps Properties => (QueryProps)23;

	public CacheOutputQuery(Query input)
	{
		this.input = input;
		outputBuffer = new List<XPathNavigator>();
		count = 0;
	}

	protected CacheOutputQuery(CacheOutputQuery other)
		: base(other)
	{
		input = Query.Clone(other.input);
		outputBuffer = new List<XPathNavigator>(other.outputBuffer);
		count = other.count;
	}

	public override void Reset()
	{
		count = 0;
	}

	public override void SetXsltContext(XsltContext context)
	{
		input.SetXsltContext(context);
	}

	public override object Evaluate(XPathNodeIterator context)
	{
		outputBuffer.Clear();
		count = 0;
		return input.Evaluate(context);
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

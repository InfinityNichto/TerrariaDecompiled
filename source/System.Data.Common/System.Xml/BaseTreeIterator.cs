using System.Diagnostics.CodeAnalysis;

namespace System.Xml;

internal abstract class BaseTreeIterator
{
	protected DataSetMapper mapper;

	internal abstract XmlNode CurrentNode { get; }

	internal BaseTreeIterator(DataSetMapper mapper)
	{
		this.mapper = mapper;
	}

	[MemberNotNullWhen(true, "CurrentNode")]
	internal abstract bool Next();

	[MemberNotNullWhen(true, "CurrentNode")]
	internal abstract bool NextRight();

	[MemberNotNullWhen(true, "CurrentNode")]
	internal bool NextRowElement()
	{
		while (Next())
		{
			if (OnRowElement())
			{
				return true;
			}
		}
		return false;
	}

	[MemberNotNullWhen(true, "CurrentNode")]
	internal bool NextRightRowElement()
	{
		if (NextRight())
		{
			if (OnRowElement())
			{
				return true;
			}
			return NextRowElement();
		}
		return false;
	}

	[MemberNotNullWhen(true, "CurrentNode")]
	internal bool OnRowElement()
	{
		if (CurrentNode is XmlBoundElement xmlBoundElement)
		{
			return xmlBoundElement.Row != null;
		}
		return false;
	}
}

using System.ComponentModel;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public enum SetIteratorResult
{
	NoMoreNodes,
	InitRightIterator,
	NeedLeftNode,
	NeedRightNode,
	HaveCurrentNode
}

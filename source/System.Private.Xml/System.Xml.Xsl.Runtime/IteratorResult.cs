using System.ComponentModel;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public enum IteratorResult
{
	NoMoreNodes,
	NeedInputNode,
	HaveCurrentNode
}

namespace System.Xml.Linq;

internal sealed class XObjectChangeAnnotation
{
	internal EventHandler<XObjectChangeEventArgs> changing;

	internal EventHandler<XObjectChangeEventArgs> changed;
}

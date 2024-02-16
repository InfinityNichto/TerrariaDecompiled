namespace System.Xml.Xsl.XsltOld;

internal abstract class Action
{
	internal abstract void Execute(Processor processor, ActionFrame frame);

	internal virtual void ReplaceNamespaceAlias(Compiler compiler)
	{
	}
}

using System.Xml.XPath;

namespace System.Xml.Xsl.XsltOld;

internal abstract class CompiledAction : Action
{
	internal abstract void Compile(Compiler compiler);

	internal virtual bool CompileAttribute(Compiler compiler)
	{
		return false;
	}

	public void CompileAttributes(Compiler compiler)
	{
		NavigatorInput input = compiler.Input;
		string localName = input.LocalName;
		if (!input.MoveToFirstAttribute())
		{
			return;
		}
		do
		{
			if (input.NamespaceURI.Length != 0)
			{
				continue;
			}
			try
			{
				if (!CompileAttribute(compiler))
				{
					throw XsltException.Create(System.SR.Xslt_InvalidAttribute, input.LocalName, localName);
				}
			}
			catch
			{
				if (!compiler.ForwardCompatibility)
				{
					throw;
				}
			}
		}
		while (input.MoveToNextAttribute());
		input.ToParent();
	}

	internal static string PrecalculateAvt(ref Avt avt)
	{
		string result = null;
		if (avt != null && avt.IsConstant)
		{
			result = avt.Evaluate(null, null);
			avt = null;
		}
		return result;
	}

	public void CheckEmpty(Compiler compiler)
	{
		string name = compiler.Input.Name;
		if (!compiler.Recurse())
		{
			return;
		}
		do
		{
			XPathNodeType nodeType = compiler.Input.NodeType;
			if (nodeType != XPathNodeType.Whitespace && nodeType != XPathNodeType.Comment && nodeType != XPathNodeType.ProcessingInstruction)
			{
				throw XsltException.Create(System.SR.Xslt_NotEmptyContents, name);
			}
		}
		while (compiler.Advance());
		compiler.ToParent();
	}

	public void CheckRequiredAttribute(Compiler compiler, object attrValue, string attrName)
	{
		CheckRequiredAttribute(compiler, attrValue != null, attrName);
	}

	public void CheckRequiredAttribute(Compiler compiler, bool attr, string attrName)
	{
		if (!attr)
		{
			throw XsltException.Create(System.SR.Xslt_MissingAttribute, attrName);
		}
	}
}

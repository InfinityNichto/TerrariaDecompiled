namespace System.Xml.Xsl.Xslt;

internal abstract class XslVisitor<T>
{
	protected virtual T Visit(XslNode node)
	{
		return node.NodeType switch
		{
			XslNodeType.ApplyImports => VisitApplyImports(node), 
			XslNodeType.ApplyTemplates => VisitApplyTemplates(node), 
			XslNodeType.Attribute => VisitAttribute((NodeCtor)node), 
			XslNodeType.AttributeSet => VisitAttributeSet((AttributeSet)node), 
			XslNodeType.CallTemplate => VisitCallTemplate(node), 
			XslNodeType.Choose => VisitChoose(node), 
			XslNodeType.Comment => VisitComment(node), 
			XslNodeType.Copy => VisitCopy(node), 
			XslNodeType.CopyOf => VisitCopyOf(node), 
			XslNodeType.Element => VisitElement((NodeCtor)node), 
			XslNodeType.Error => VisitError(node), 
			XslNodeType.ForEach => VisitForEach(node), 
			XslNodeType.If => VisitIf(node), 
			XslNodeType.Key => VisitKey((Key)node), 
			XslNodeType.List => VisitList(node), 
			XslNodeType.LiteralAttribute => VisitLiteralAttribute(node), 
			XslNodeType.LiteralElement => VisitLiteralElement(node), 
			XslNodeType.Message => VisitMessage(node), 
			XslNodeType.Nop => VisitNop(node), 
			XslNodeType.Number => VisitNumber((Number)node), 
			XslNodeType.Otherwise => VisitOtherwise(node), 
			XslNodeType.Param => VisitParam((VarPar)node), 
			XslNodeType.PI => VisitPI(node), 
			XslNodeType.Sort => VisitSort((Sort)node), 
			XslNodeType.Template => VisitTemplate((Template)node), 
			XslNodeType.Text => VisitText((Text)node), 
			XslNodeType.UseAttributeSet => VisitUseAttributeSet(node), 
			XslNodeType.ValueOf => VisitValueOf(node), 
			XslNodeType.ValueOfDoe => VisitValueOfDoe(node), 
			XslNodeType.Variable => VisitVariable((VarPar)node), 
			XslNodeType.WithParam => VisitWithParam((VarPar)node), 
			_ => VisitUnknown(node), 
		};
	}

	protected virtual T VisitApplyImports(XslNode node)
	{
		return VisitChildren(node);
	}

	protected virtual T VisitApplyTemplates(XslNode node)
	{
		return VisitChildren(node);
	}

	protected virtual T VisitAttribute(NodeCtor node)
	{
		return VisitChildren(node);
	}

	protected virtual T VisitAttributeSet(AttributeSet node)
	{
		return VisitChildren(node);
	}

	protected virtual T VisitCallTemplate(XslNode node)
	{
		return VisitChildren(node);
	}

	protected virtual T VisitChoose(XslNode node)
	{
		return VisitChildren(node);
	}

	protected virtual T VisitComment(XslNode node)
	{
		return VisitChildren(node);
	}

	protected virtual T VisitCopy(XslNode node)
	{
		return VisitChildren(node);
	}

	protected virtual T VisitCopyOf(XslNode node)
	{
		return VisitChildren(node);
	}

	protected virtual T VisitElement(NodeCtor node)
	{
		return VisitChildren(node);
	}

	protected virtual T VisitError(XslNode node)
	{
		return VisitChildren(node);
	}

	protected virtual T VisitForEach(XslNode node)
	{
		return VisitChildren(node);
	}

	protected virtual T VisitIf(XslNode node)
	{
		return VisitChildren(node);
	}

	protected virtual T VisitKey(Key node)
	{
		return VisitChildren(node);
	}

	protected virtual T VisitList(XslNode node)
	{
		return VisitChildren(node);
	}

	protected virtual T VisitLiteralAttribute(XslNode node)
	{
		return VisitChildren(node);
	}

	protected virtual T VisitLiteralElement(XslNode node)
	{
		return VisitChildren(node);
	}

	protected virtual T VisitMessage(XslNode node)
	{
		return VisitChildren(node);
	}

	protected virtual T VisitNop(XslNode node)
	{
		return VisitChildren(node);
	}

	protected virtual T VisitNumber(Number node)
	{
		return VisitChildren(node);
	}

	protected virtual T VisitOtherwise(XslNode node)
	{
		return VisitChildren(node);
	}

	protected virtual T VisitParam(VarPar node)
	{
		return VisitChildren(node);
	}

	protected virtual T VisitPI(XslNode node)
	{
		return VisitChildren(node);
	}

	protected virtual T VisitSort(Sort node)
	{
		return VisitChildren(node);
	}

	protected virtual T VisitTemplate(Template node)
	{
		return VisitChildren(node);
	}

	protected virtual T VisitText(Text node)
	{
		return VisitChildren(node);
	}

	protected virtual T VisitUseAttributeSet(XslNode node)
	{
		return VisitChildren(node);
	}

	protected virtual T VisitValueOf(XslNode node)
	{
		return VisitChildren(node);
	}

	protected virtual T VisitValueOfDoe(XslNode node)
	{
		return VisitChildren(node);
	}

	protected virtual T VisitVariable(VarPar node)
	{
		return VisitChildren(node);
	}

	protected virtual T VisitWithParam(VarPar node)
	{
		return VisitChildren(node);
	}

	protected virtual T VisitUnknown(XslNode node)
	{
		return VisitChildren(node);
	}

	protected virtual T VisitChildren(XslNode node)
	{
		foreach (XslNode item in node.Content)
		{
			Visit(item);
		}
		return default(T);
	}
}

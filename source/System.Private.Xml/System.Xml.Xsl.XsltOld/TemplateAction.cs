using System.Xml.XPath;
using MS.Internal.Xml.XPath;

namespace System.Xml.Xsl.XsltOld;

internal class TemplateAction : TemplateBaseAction
{
	private int _matchKey = -1;

	private XmlQualifiedName _name;

	private double _priority = double.NaN;

	private XmlQualifiedName _mode;

	private int _templateId;

	private bool _replaceNSAliasesDone;

	internal int MatchKey => _matchKey;

	internal XmlQualifiedName Name => _name;

	internal double Priority => _priority;

	internal XmlQualifiedName Mode => _mode;

	internal int TemplateId
	{
		get
		{
			return _templateId;
		}
		set
		{
			_templateId = value;
		}
	}

	internal override void Compile(Compiler compiler)
	{
		CompileAttributes(compiler);
		if (_matchKey == -1)
		{
			if (_name == null)
			{
				throw XsltException.Create(System.SR.Xslt_TemplateNoAttrib);
			}
			if (_mode != null)
			{
				throw XsltException.Create(System.SR.Xslt_InvalidModeAttribute);
			}
		}
		compiler.BeginTemplate(this);
		if (compiler.Recurse())
		{
			CompileParameters(compiler);
			CompileTemplate(compiler);
			compiler.ToParent();
		}
		compiler.EndTemplate();
		AnalyzePriority(compiler);
	}

	internal virtual void CompileSingle(Compiler compiler)
	{
		_matchKey = compiler.AddQuery("/", allowVar: false, allowKey: true, isPattern: true);
		_priority = 0.5;
		CompileOnceTemplate(compiler);
	}

	internal override bool CompileAttribute(Compiler compiler)
	{
		string localName = compiler.Input.LocalName;
		string value = compiler.Input.Value;
		if (Ref.Equal(localName, compiler.Atoms.Match))
		{
			_matchKey = compiler.AddQuery(value, allowVar: false, allowKey: true, isPattern: true);
		}
		else if (Ref.Equal(localName, compiler.Atoms.Name))
		{
			_name = compiler.CreateXPathQName(value);
		}
		else if (Ref.Equal(localName, compiler.Atoms.Priority))
		{
			_priority = XmlConvert.ToXPathDouble(value);
			if (double.IsNaN(_priority) && !compiler.ForwardCompatibility)
			{
				throw XsltException.Create(System.SR.Xslt_InvalidAttrValue, "priority", value);
			}
		}
		else
		{
			if (!Ref.Equal(localName, compiler.Atoms.Mode))
			{
				return false;
			}
			if (compiler.AllowBuiltInMode && value == "*")
			{
				_mode = Compiler.BuiltInMode;
			}
			else
			{
				_mode = compiler.CreateXPathQName(value);
			}
		}
		return true;
	}

	private void AnalyzePriority(Compiler compiler)
	{
		if (double.IsNaN(_priority) && _matchKey != -1)
		{
			TheQuery theQuery = compiler.QueryStore[MatchKey];
			CompiledXpathExpr compiledQuery = theQuery.CompiledQuery;
			Query query;
			for (query = compiledQuery.QueryTree; query is UnionExpr unionExpr; query = unionExpr.qy1)
			{
				TemplateAction templateAction = CloneWithoutName();
				compiler.QueryStore.Add(new TheQuery(new CompiledXpathExpr(unionExpr.qy2, compiledQuery.Expression, needContext: false), theQuery._ScopeManager));
				templateAction._matchKey = compiler.QueryStore.Count - 1;
				templateAction._priority = unionExpr.qy2.XsltDefaultPriority;
				compiler.AddTemplate(templateAction);
			}
			if (compiledQuery.QueryTree != query)
			{
				compiler.QueryStore[MatchKey] = new TheQuery(new CompiledXpathExpr(query, compiledQuery.Expression, needContext: false), theQuery._ScopeManager);
			}
			_priority = query.XsltDefaultPriority;
		}
	}

	protected void CompileParameters(Compiler compiler)
	{
		NavigatorInput input = compiler.Input;
		do
		{
			switch (input.NodeType)
			{
			case XPathNodeType.Element:
				if (Ref.Equal(input.NamespaceURI, input.Atoms.UriXsl) && Ref.Equal(input.LocalName, input.Atoms.Param))
				{
					compiler.PushNamespaceScope();
					AddAction(compiler.CreateVariableAction(VariableType.LocalParameter));
					compiler.PopScope();
					break;
				}
				return;
			case XPathNodeType.Text:
				return;
			case XPathNodeType.SignificantWhitespace:
				AddEvent(compiler.CreateTextEvent());
				break;
			}
		}
		while (input.Advance());
	}

	private TemplateAction CloneWithoutName()
	{
		TemplateAction templateAction = new TemplateAction();
		templateAction.containedActions = containedActions;
		templateAction._mode = _mode;
		templateAction.variableCount = variableCount;
		templateAction._replaceNSAliasesDone = true;
		return templateAction;
	}

	internal override void ReplaceNamespaceAlias(Compiler compiler)
	{
		if (!_replaceNSAliasesDone)
		{
			base.ReplaceNamespaceAlias(compiler);
			_replaceNSAliasesDone = true;
		}
	}

	internal override void Execute(Processor processor, ActionFrame frame)
	{
		switch (frame.State)
		{
		case 0:
			if (variableCount > 0)
			{
				frame.AllocateVariables(variableCount);
			}
			if (containedActions != null && containedActions.Count > 0)
			{
				processor.PushActionFrame(frame);
				frame.State = 1;
			}
			else
			{
				frame.Finished();
			}
			break;
		case 1:
			frame.Finished();
			break;
		}
	}
}

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml.XPath;
using System.Xml.Xsl.Runtime;
using System.Xml.Xsl.Xslt;
using MS.Internal.Xml.XPath;

namespace System.Xml.Xsl.XsltOld;

internal class Compiler
{
	internal sealed class ErrorXPathExpression : CompiledXpathExpr
	{
		private readonly string _baseUri;

		private readonly int _lineNumber;

		private readonly int _linePosition;

		public ErrorXPathExpression(string expression, string baseUri, int lineNumber, int linePosition)
			: base(null, expression, needContext: false)
		{
			_baseUri = baseUri;
			_lineNumber = lineNumber;
			_linePosition = linePosition;
		}

		public override XPathExpression Clone()
		{
			return this;
		}

		public override void CheckErrors()
		{
			throw new XsltException(System.SR.Xslt_InvalidXPath, new string[1] { Expression }, _baseUri, _linePosition, _lineNumber, null);
		}
	}

	internal StringBuilder AvtStringBuilder = new StringBuilder();

	private int _stylesheetid;

	private InputScope _rootScope;

	private XmlResolver _xmlResolver;

	private TemplateBaseAction _currentTemplate;

	private XmlQualifiedName _currentMode;

	private Hashtable _globalNamespaceAliasTable;

	private Stack<Stylesheet> _stylesheets;

	private readonly HybridDictionary _documentURIs = new HybridDictionary();

	private NavigatorInput _input;

	private KeywordsTable _atoms;

	private InputScopeManager _scopeManager;

	internal Stylesheet stylesheet;

	internal Stylesheet rootStylesheet;

	private RootAction _rootAction;

	private List<TheQuery> _queryStore;

	private readonly QueryBuilder _queryBuilder = new QueryBuilder();

	private int _rtfCount;

	public bool AllowBuiltInMode;

	public static XmlQualifiedName BuiltInMode = new XmlQualifiedName("*", string.Empty);

	private readonly Hashtable[] _typeDeclsByLang = new Hashtable[3]
	{
		new Hashtable(),
		new Hashtable(),
		new Hashtable()
	};

	internal KeywordsTable Atoms => _atoms;

	internal int Stylesheetid
	{
		get
		{
			return _stylesheetid;
		}
		set
		{
			_stylesheetid = value;
		}
	}

	internal NavigatorInput Document => _input;

	internal NavigatorInput Input => _input;

	internal Stylesheet CompiledStylesheet => stylesheet;

	internal RootAction RootAction
	{
		get
		{
			return _rootAction;
		}
		set
		{
			_rootAction = value;
			_currentTemplate = _rootAction;
		}
	}

	internal List<TheQuery> QueryStore => _queryStore;

	internal bool ForwardCompatibility
	{
		get
		{
			return _scopeManager.CurrentScope.ForwardCompatibility;
		}
		set
		{
			_scopeManager.CurrentScope.ForwardCompatibility = value;
		}
	}

	internal bool CanHaveApplyImports
	{
		get
		{
			return _scopeManager.CurrentScope.CanHaveApplyImports;
		}
		set
		{
			_scopeManager.CurrentScope.CanHaveApplyImports = value;
		}
	}

	internal string DefaultNamespace => _scopeManager.DefaultNamespace;

	internal XmlQualifiedName CurrentMode => _currentMode;

	internal bool Advance()
	{
		return Document.Advance();
	}

	internal bool Recurse()
	{
		return Document.Recurse();
	}

	internal bool ToParent()
	{
		return Document.ToParent();
	}

	internal string GetUnicRtfId()
	{
		_rtfCount++;
		return _rtfCount.ToString(CultureInfo.InvariantCulture);
	}

	internal void Compile(NavigatorInput input, XmlResolver xmlResolver)
	{
		_xmlResolver = xmlResolver;
		PushInputDocument(input);
		_rootScope = _scopeManager.PushScope();
		_queryStore = new List<TheQuery>();
		try
		{
			rootStylesheet = new Stylesheet();
			PushStylesheet(rootStylesheet);
			try
			{
				CreateRootAction();
			}
			catch (XsltCompileException)
			{
				throw;
			}
			catch (Exception inner)
			{
				throw new XsltCompileException(inner, Input.BaseURI, Input.LineNumber, Input.LinePosition);
			}
			stylesheet.ProcessTemplates();
			_rootAction.PorcessAttributeSets(rootStylesheet);
			stylesheet.SortWhiteSpace();
			if (_globalNamespaceAliasTable != null)
			{
				stylesheet.ReplaceNamespaceAlias(this);
				_rootAction.ReplaceNamespaceAlias(this);
			}
		}
		finally
		{
			PopInputDocument();
		}
	}

	internal void InsertExtensionNamespace(string value)
	{
		string[] array = ResolvePrefixes(value);
		if (array != null)
		{
			_scopeManager.InsertExtensionNamespaces(array);
		}
	}

	internal void InsertExcludedNamespace(string value)
	{
		string[] array = ResolvePrefixes(value);
		if (array != null)
		{
			_scopeManager.InsertExcludedNamespaces(array);
		}
	}

	internal void InsertExtensionNamespace()
	{
		InsertExtensionNamespace(Input.Navigator.GetAttribute(Input.Atoms.ExtensionElementPrefixes, Input.Atoms.UriXsl));
	}

	internal void InsertExcludedNamespace()
	{
		InsertExcludedNamespace(Input.Navigator.GetAttribute(Input.Atoms.ExcludeResultPrefixes, Input.Atoms.UriXsl));
	}

	internal bool IsExtensionNamespace(string nspace)
	{
		return _scopeManager.IsExtensionNamespace(nspace);
	}

	internal bool IsExcludedNamespace(string nspace)
	{
		return _scopeManager.IsExcludedNamespace(nspace);
	}

	internal void PushLiteralScope()
	{
		PushNamespaceScope();
		string attribute = Input.Navigator.GetAttribute(Atoms.Version, Atoms.UriXsl);
		if (attribute.Length != 0)
		{
			ForwardCompatibility = attribute != "1.0";
		}
	}

	internal void PushNamespaceScope()
	{
		_scopeManager.PushScope();
		NavigatorInput input = Input;
		if (input.MoveToFirstNamespace())
		{
			do
			{
				_scopeManager.PushNamespace(input.LocalName, input.Value);
			}
			while (input.MoveToNextNamespace());
			input.ToParent();
		}
	}

	internal virtual void PopScope()
	{
		_currentTemplate.ReleaseVariableSlots(_scopeManager.CurrentScope.GetVeriablesCount());
		_scopeManager.PopScope();
	}

	internal InputScopeManager CloneScopeManager()
	{
		return _scopeManager.Clone();
	}

	internal int InsertVariable(VariableAction variable)
	{
		InputScope inputScope = ((!variable.IsGlobal) ? _scopeManager.VariableScope : _rootScope);
		VariableAction variableAction = inputScope.ResolveVariable(variable.Name);
		if (variableAction != null)
		{
			if (!variableAction.IsGlobal)
			{
				throw XsltException.Create(System.SR.Xslt_DupVarName, variable.NameStr);
			}
			if (variable.IsGlobal)
			{
				if (variable.Stylesheetid == variableAction.Stylesheetid)
				{
					throw XsltException.Create(System.SR.Xslt_DupVarName, variable.NameStr);
				}
				if (variable.Stylesheetid < variableAction.Stylesheetid)
				{
					inputScope.InsertVariable(variable);
					return variableAction.VarKey;
				}
				return -1;
			}
		}
		inputScope.InsertVariable(variable);
		return _currentTemplate.AllocateVariableSlot();
	}

	internal void AddNamespaceAlias(string StylesheetURI, NamespaceInfo AliasInfo)
	{
		if (_globalNamespaceAliasTable == null)
		{
			_globalNamespaceAliasTable = new Hashtable();
		}
		if (!(_globalNamespaceAliasTable[StylesheetURI] is NamespaceInfo namespaceInfo) || AliasInfo.stylesheetId <= namespaceInfo.stylesheetId)
		{
			_globalNamespaceAliasTable[StylesheetURI] = AliasInfo;
		}
	}

	internal bool IsNamespaceAlias(string StylesheetURI)
	{
		if (_globalNamespaceAliasTable == null)
		{
			return false;
		}
		return _globalNamespaceAliasTable.Contains(StylesheetURI);
	}

	internal NamespaceInfo FindNamespaceAlias(string StylesheetURI)
	{
		if (_globalNamespaceAliasTable != null)
		{
			return (NamespaceInfo)_globalNamespaceAliasTable[StylesheetURI];
		}
		return null;
	}

	internal string ResolveXmlNamespace(string prefix)
	{
		return _scopeManager.ResolveXmlNamespace(prefix);
	}

	internal string ResolveXPathNamespace(string prefix)
	{
		return _scopeManager.ResolveXPathNamespace(prefix);
	}

	internal void InsertKey(XmlQualifiedName name, int MatchKey, int UseKey)
	{
		_rootAction.InsertKey(name, MatchKey, UseKey);
	}

	internal void AddDecimalFormat(XmlQualifiedName name, DecimalFormat formatinfo)
	{
		_rootAction.AddDecimalFormat(name, formatinfo);
	}

	private string[] ResolvePrefixes(string tokens)
	{
		if (tokens == null || tokens.Length == 0)
		{
			return null;
		}
		string[] array = XmlConvert.SplitString(tokens);
		try
		{
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i];
				array[i] = _scopeManager.ResolveXmlNamespace((text == "#default") ? string.Empty : text);
			}
			return array;
		}
		catch (XsltException)
		{
			if (!ForwardCompatibility)
			{
				throw;
			}
			return null;
		}
	}

	internal bool GetYesNo(string value)
	{
		if (value == "yes")
		{
			return true;
		}
		if (value == "no")
		{
			return false;
		}
		throw XsltException.Create(System.SR.Xslt_InvalidAttrValue, Input.LocalName, value);
	}

	internal string GetSingleAttribute(string attributeAtom)
	{
		NavigatorInput input = Input;
		string localName = input.LocalName;
		string text = null;
		if (input.MoveToFirstAttribute())
		{
			do
			{
				string namespaceURI = input.NamespaceURI;
				string localName2 = input.LocalName;
				if (namespaceURI.Length == 0)
				{
					if (Ref.Equal(localName2, attributeAtom))
					{
						text = input.Value;
					}
					else if (!ForwardCompatibility)
					{
						throw XsltException.Create(System.SR.Xslt_InvalidAttribute, localName2, localName);
					}
				}
			}
			while (input.MoveToNextAttribute());
			input.ToParent();
		}
		if (text == null)
		{
			throw XsltException.Create(System.SR.Xslt_MissingAttribute, attributeAtom);
		}
		return text;
	}

	internal XmlQualifiedName CreateXPathQName(string qname)
	{
		PrefixQName.ParseQualifiedName(qname, out var prefix, out var local);
		return new XmlQualifiedName(local, _scopeManager.ResolveXPathNamespace(prefix));
	}

	internal XmlQualifiedName CreateXmlQName(string qname)
	{
		PrefixQName.ParseQualifiedName(qname, out var prefix, out var local);
		return new XmlQualifiedName(local, _scopeManager.ResolveXmlNamespace(prefix));
	}

	internal static XPathDocument LoadDocument(XmlTextReaderImpl reader)
	{
		reader.EntityHandling = EntityHandling.ExpandEntities;
		reader.XmlValidatingReaderCompatibilityMode = true;
		try
		{
			return new XPathDocument(reader, XmlSpace.Preserve);
		}
		finally
		{
			reader.Close();
		}
	}

	private void AddDocumentURI(string href)
	{
		_documentURIs.Add(href, null);
	}

	private void RemoveDocumentURI(string href)
	{
		_documentURIs.Remove(href);
	}

	internal bool IsCircularReference(string href)
	{
		return _documentURIs.Contains(href);
	}

	internal Uri ResolveUri(string relativeUri)
	{
		string baseURI = Input.BaseURI;
		Uri uri = _xmlResolver.ResolveUri((baseURI.Length != 0) ? _xmlResolver.ResolveUri(null, baseURI) : null, relativeUri);
		if (uri == null)
		{
			throw XsltException.Create(System.SR.Xslt_CantResolve, relativeUri);
		}
		return uri;
	}

	internal NavigatorInput ResolveDocument(Uri absoluteUri)
	{
		object entity = _xmlResolver.GetEntity(absoluteUri, null, null);
		string text = absoluteUri.ToString();
		if (entity is Stream)
		{
			XmlTextReaderImpl xmlTextReaderImpl = new XmlTextReaderImpl(text, (Stream)entity);
			xmlTextReaderImpl.XmlResolver = _xmlResolver;
			return new NavigatorInput(LoadDocument(xmlTextReaderImpl).CreateNavigator(), text, _rootScope);
		}
		if (entity is XPathNavigator)
		{
			return new NavigatorInput((XPathNavigator)entity, text, _rootScope);
		}
		throw XsltException.Create(System.SR.Xslt_CantResolve, text);
	}

	internal void PushInputDocument(NavigatorInput newInput)
	{
		string href = newInput.Href;
		AddDocumentURI(href);
		newInput.Next = _input;
		_input = newInput;
		_atoms = _input.Atoms;
		_scopeManager = _input.InputScopeManager;
	}

	internal void PopInputDocument()
	{
		NavigatorInput input = _input;
		_input = input.Next;
		input.Next = null;
		if (_input != null)
		{
			_atoms = _input.Atoms;
			_scopeManager = _input.InputScopeManager;
		}
		else
		{
			_atoms = null;
			_scopeManager = null;
		}
		RemoveDocumentURI(input.Href);
		input.Close();
	}

	internal void PushStylesheet(Stylesheet stylesheet)
	{
		if (_stylesheets == null)
		{
			_stylesheets = new Stack<Stylesheet>();
		}
		_stylesheets.Push(stylesheet);
		this.stylesheet = stylesheet;
	}

	internal Stylesheet PopStylesheet()
	{
		Stylesheet result = _stylesheets.Pop();
		stylesheet = _stylesheets.Peek();
		return result;
	}

	internal void AddAttributeSet(AttributeSetAction attributeSet)
	{
		stylesheet.AddAttributeSet(attributeSet);
	}

	internal void AddTemplate(TemplateAction template)
	{
		stylesheet.AddTemplate(template);
	}

	internal void BeginTemplate(TemplateAction template)
	{
		_currentTemplate = template;
		_currentMode = template.Mode;
		CanHaveApplyImports = template.MatchKey != -1;
	}

	internal void EndTemplate()
	{
		_currentTemplate = _rootAction;
	}

	internal int AddQuery(string xpathQuery)
	{
		return AddQuery(xpathQuery, allowVar: true, allowKey: true, isPattern: false);
	}

	internal int AddQuery(string xpathQuery, bool allowVar, bool allowKey, bool isPattern)
	{
		CompiledXpathExpr compiledQuery;
		try
		{
			compiledQuery = new CompiledXpathExpr(isPattern ? _queryBuilder.BuildPatternQuery(xpathQuery, allowVar, allowKey) : _queryBuilder.Build(xpathQuery, allowVar, allowKey), xpathQuery, needContext: false);
		}
		catch (XPathException inner)
		{
			if (!ForwardCompatibility)
			{
				throw XsltException.Create(System.SR.Xslt_InvalidXPath, new string[1] { xpathQuery }, inner);
			}
			compiledQuery = new ErrorXPathExpression(xpathQuery, Input.BaseURI, Input.LineNumber, Input.LinePosition);
		}
		_queryStore.Add(new TheQuery(compiledQuery, _scopeManager));
		return _queryStore.Count - 1;
	}

	internal int AddStringQuery(string xpathQuery)
	{
		string xpathQuery2 = (XmlCharType.IsOnlyWhitespace(xpathQuery) ? xpathQuery : ("string(" + xpathQuery + ")"));
		return AddQuery(xpathQuery2);
	}

	internal int AddBooleanQuery(string xpathQuery)
	{
		string xpathQuery2 = (XmlCharType.IsOnlyWhitespace(xpathQuery) ? xpathQuery : ("boolean(" + xpathQuery + ")"));
		return AddQuery(xpathQuery2);
	}

	internal void AddScript(string source, ScriptingLanguage lang, string ns, string fileName, int lineNumber)
	{
		ValidateExtensionNamespace(ns);
		for (ScriptingLanguage scriptingLanguage = ScriptingLanguage.JScript; scriptingLanguage <= ScriptingLanguage.CSharp; scriptingLanguage++)
		{
			Hashtable hashtable = _typeDeclsByLang[(int)scriptingLanguage];
			if (lang == scriptingLanguage)
			{
				throw new PlatformNotSupportedException(System.SR.CompilingScriptsNotSupported);
			}
			if (hashtable.Contains(ns))
			{
				throw XsltException.Create(System.SR.Xslt_ScriptMixedLanguages, ns);
			}
		}
	}

	private static void ValidateExtensionNamespace(string nsUri)
	{
		if (nsUri.Length == 0 || nsUri == "http://www.w3.org/1999/XSL/Transform")
		{
			throw XsltException.Create(System.SR.Xslt_InvalidExtensionNamespace);
		}
		XmlConvert.ToUri(nsUri);
	}

	public string GetNsAlias(ref string prefix)
	{
		if (prefix == "#default")
		{
			prefix = string.Empty;
			return DefaultNamespace;
		}
		if (!PrefixQName.ValidatePrefix(prefix))
		{
			throw XsltException.Create(System.SR.Xslt_InvalidAttrValue, _input.LocalName, prefix);
		}
		return ResolveXPathNamespace(prefix);
	}

	private static void getTextLex(string avt, ref int start, StringBuilder lex)
	{
		int length = avt.Length;
		int i;
		char c;
		for (i = start; i < length; lex.Append(c), i++)
		{
			c = avt[i];
			switch (c)
			{
			case '{':
				if (i + 1 < length && avt[i + 1] == '{')
				{
					i++;
					continue;
				}
				break;
			case '}':
				if (i + 1 < length && avt[i + 1] == '}')
				{
					i++;
					continue;
				}
				throw XsltException.Create(System.SR.Xslt_SingleRightAvt, avt);
			default:
				continue;
			}
			break;
		}
		start = i;
	}

	private static void getXPathLex(string avt, ref int start, StringBuilder lex)
	{
		int length = avt.Length;
		int num = 0;
		for (int i = start + 1; i < length; i++)
		{
			char c = avt[i];
			switch (num)
			{
			case 0:
				switch (c)
				{
				case '{':
					throw XsltException.Create(System.SR.Xslt_NestedAvt, avt);
				case '}':
					i++;
					if (i == start + 2)
					{
						throw XsltException.Create(System.SR.Xslt_EmptyAvtExpr, avt);
					}
					lex.Append(avt, start + 1, i - start - 2);
					start = i;
					return;
				case '\'':
					num = 1;
					break;
				case '"':
					num = 2;
					break;
				}
				break;
			case 1:
				if (c == '\'')
				{
					num = 0;
				}
				break;
			case 2:
				if (c == '"')
				{
					num = 0;
				}
				break;
			}
		}
		throw XsltException.Create((num == 0) ? System.SR.Xslt_OpenBracesAvt : System.SR.Xslt_OpenLiteralAvt, avt);
	}

	private static bool GetNextAvtLex(string avt, ref int start, StringBuilder lex, out bool isAvt)
	{
		isAvt = false;
		if (start == avt.Length)
		{
			return false;
		}
		lex.Length = 0;
		getTextLex(avt, ref start, lex);
		if (lex.Length == 0)
		{
			isAvt = true;
			getXPathLex(avt, ref start, lex);
		}
		return true;
	}

	internal ArrayList CompileAvt(string avtText, out bool constant)
	{
		ArrayList arrayList = new ArrayList();
		constant = true;
		int start = 0;
		bool isAvt;
		while (GetNextAvtLex(avtText, ref start, AvtStringBuilder, out isAvt))
		{
			string text = AvtStringBuilder.ToString();
			if (isAvt)
			{
				arrayList.Add(new AvtEvent(AddStringQuery(text)));
				constant = false;
			}
			else
			{
				arrayList.Add(new TextEvent(text));
			}
		}
		return arrayList;
	}

	internal ArrayList CompileAvt(string avtText)
	{
		bool constant;
		return CompileAvt(avtText, out constant);
	}

	public virtual ApplyImportsAction CreateApplyImportsAction()
	{
		ApplyImportsAction applyImportsAction = new ApplyImportsAction();
		applyImportsAction.Compile(this);
		return applyImportsAction;
	}

	public virtual ApplyTemplatesAction CreateApplyTemplatesAction()
	{
		ApplyTemplatesAction applyTemplatesAction = new ApplyTemplatesAction();
		applyTemplatesAction.Compile(this);
		return applyTemplatesAction;
	}

	public virtual AttributeAction CreateAttributeAction()
	{
		AttributeAction attributeAction = new AttributeAction();
		attributeAction.Compile(this);
		return attributeAction;
	}

	public virtual AttributeSetAction CreateAttributeSetAction()
	{
		AttributeSetAction attributeSetAction = new AttributeSetAction();
		attributeSetAction.Compile(this);
		return attributeSetAction;
	}

	public virtual CallTemplateAction CreateCallTemplateAction()
	{
		CallTemplateAction callTemplateAction = new CallTemplateAction();
		callTemplateAction.Compile(this);
		return callTemplateAction;
	}

	public virtual ChooseAction CreateChooseAction()
	{
		ChooseAction chooseAction = new ChooseAction();
		chooseAction.Compile(this);
		return chooseAction;
	}

	public virtual CommentAction CreateCommentAction()
	{
		CommentAction commentAction = new CommentAction();
		commentAction.Compile(this);
		return commentAction;
	}

	public virtual CopyAction CreateCopyAction()
	{
		CopyAction copyAction = new CopyAction();
		copyAction.Compile(this);
		return copyAction;
	}

	public virtual CopyOfAction CreateCopyOfAction()
	{
		CopyOfAction copyOfAction = new CopyOfAction();
		copyOfAction.Compile(this);
		return copyOfAction;
	}

	public virtual ElementAction CreateElementAction()
	{
		ElementAction elementAction = new ElementAction();
		elementAction.Compile(this);
		return elementAction;
	}

	public virtual ForEachAction CreateForEachAction()
	{
		ForEachAction forEachAction = new ForEachAction();
		forEachAction.Compile(this);
		return forEachAction;
	}

	public virtual IfAction CreateIfAction(IfAction.ConditionType type)
	{
		IfAction ifAction = new IfAction(type);
		ifAction.Compile(this);
		return ifAction;
	}

	public virtual MessageAction CreateMessageAction()
	{
		MessageAction messageAction = new MessageAction();
		messageAction.Compile(this);
		return messageAction;
	}

	public virtual NewInstructionAction CreateNewInstructionAction()
	{
		NewInstructionAction newInstructionAction = new NewInstructionAction();
		newInstructionAction.Compile(this);
		return newInstructionAction;
	}

	public virtual NumberAction CreateNumberAction()
	{
		NumberAction numberAction = new NumberAction();
		numberAction.Compile(this);
		return numberAction;
	}

	public virtual ProcessingInstructionAction CreateProcessingInstructionAction()
	{
		ProcessingInstructionAction processingInstructionAction = new ProcessingInstructionAction();
		processingInstructionAction.Compile(this);
		return processingInstructionAction;
	}

	public virtual void CreateRootAction()
	{
		RootAction = new RootAction();
		RootAction.Compile(this);
	}

	public virtual SortAction CreateSortAction()
	{
		SortAction sortAction = new SortAction();
		sortAction.Compile(this);
		return sortAction;
	}

	public virtual TemplateAction CreateTemplateAction()
	{
		TemplateAction templateAction = new TemplateAction();
		templateAction.Compile(this);
		return templateAction;
	}

	public virtual TemplateAction CreateSingleTemplateAction()
	{
		TemplateAction templateAction = new TemplateAction();
		templateAction.CompileSingle(this);
		return templateAction;
	}

	public virtual TextAction CreateTextAction()
	{
		TextAction textAction = new TextAction();
		textAction.Compile(this);
		return textAction;
	}

	public virtual UseAttributeSetsAction CreateUseAttributeSetsAction()
	{
		UseAttributeSetsAction useAttributeSetsAction = new UseAttributeSetsAction();
		useAttributeSetsAction.Compile(this);
		return useAttributeSetsAction;
	}

	public virtual ValueOfAction CreateValueOfAction()
	{
		ValueOfAction valueOfAction = new ValueOfAction();
		valueOfAction.Compile(this);
		return valueOfAction;
	}

	public virtual VariableAction CreateVariableAction(VariableType type)
	{
		VariableAction variableAction = new VariableAction(type);
		variableAction.Compile(this);
		if (variableAction.VarKey != -1)
		{
			return variableAction;
		}
		return null;
	}

	public virtual WithParamAction CreateWithParamAction()
	{
		WithParamAction withParamAction = new WithParamAction();
		withParamAction.Compile(this);
		return withParamAction;
	}

	public virtual BeginEvent CreateBeginEvent()
	{
		return new BeginEvent(this);
	}

	public virtual TextEvent CreateTextEvent()
	{
		return new TextEvent(this);
	}

	public XsltException UnexpectedKeyword()
	{
		XPathNavigator xPathNavigator = Input.Navigator.Clone();
		string name = xPathNavigator.Name;
		xPathNavigator.MoveToParent();
		string name2 = xPathNavigator.Name;
		return XsltException.Create(System.SR.Xslt_UnexpectedKeyword, name, name2);
	}
}

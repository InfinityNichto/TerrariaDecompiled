using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml.XPath;
using System.Xml.Xsl.XsltOld.Debugger;
using MS.Internal.Xml.XPath;

namespace System.Xml.Xsl.XsltOld;

internal sealed class Processor : IXsltProcessor
{
	internal enum ExecResult
	{
		Continue,
		Interrupt,
		Done
	}

	internal enum OutputResult
	{
		Continue,
		Interrupt,
		Overflow,
		Error,
		Ignore
	}

	internal sealed class DebuggerFrame
	{
		internal XmlQualifiedName currentMode;
	}

	private ExecResult _execResult;

	private readonly Stylesheet _stylesheet;

	private readonly RootAction _rootAction;

	private readonly Key[] _keyList;

	private readonly List<TheQuery> _queryStore;

	private readonly XPathNavigator _document;

	private readonly HWStack _actionStack;

	private readonly HWStack _debuggerStack;

	private StringBuilder _sharedStringBuilder;

	private int _ignoreLevel;

	private readonly StateMachine _xsm;

	private RecordBuilder _builder;

	private XsltOutput _output;

	private readonly XmlNameTable _nameTable = new NameTable();

	private readonly XmlResolver _resolver;

	private readonly XsltArgumentList _args;

	private readonly Hashtable _scriptExtensions;

	private ArrayList _numberList;

	private readonly TemplateLookupAction _templateLookup = new TemplateLookupAction();

	private readonly IXsltDebugger _debugger;

	private readonly Query[] _queryList;

	private ArrayList _sortArray;

	private Hashtable _documentCache;

	private XsltCompileContext _valueOfContext;

	private XsltCompileContext _matchesContext;

	internal XPathNavigator Current => ((ActionFrame)_actionStack.Peek())?.Node;

	internal ExecResult ExecutionResult
	{
		set
		{
			_execResult = value;
		}
	}

	internal Stylesheet Stylesheet => _stylesheet;

	internal XmlResolver Resolver => _resolver;

	internal ArrayList SortArray => _sortArray;

	internal Key[] KeyList => _keyList;

	internal RootAction RootAction => _rootAction;

	internal XPathNavigator Document => _document;

	internal ArrayList NumberList
	{
		get
		{
			if (_numberList == null)
			{
				_numberList = new ArrayList();
			}
			return _numberList;
		}
	}

	internal IXsltDebugger Debugger => _debugger;

	internal XsltOutput Output => _output;

	internal XmlNameTable NameTable => _nameTable;

	internal bool CanContinue => _execResult == ExecResult.Continue;

	internal XPathNavigator GetNavigator(Uri ruri)
	{
		XPathNavigator xPathNavigator = null;
		if (_documentCache != null)
		{
			if (_documentCache[ruri] is XPathNavigator xPathNavigator2)
			{
				return xPathNavigator2.Clone();
			}
		}
		else
		{
			_documentCache = new Hashtable();
		}
		object entity = _resolver.GetEntity(ruri, null, null);
		if (entity is Stream)
		{
			XmlTextReaderImpl xmlTextReaderImpl = new XmlTextReaderImpl(ruri.ToString(), (Stream)entity);
			xmlTextReaderImpl.XmlResolver = _resolver;
			xPathNavigator = ((IXPathNavigable)Compiler.LoadDocument(xmlTextReaderImpl)).CreateNavigator();
		}
		else
		{
			if (!(entity is XPathNavigator))
			{
				throw XsltException.Create(System.SR.Xslt_CantResolve, ruri.ToString());
			}
			xPathNavigator = (XPathNavigator)entity;
		}
		_documentCache[ruri] = xPathNavigator.Clone();
		return xPathNavigator;
	}

	internal void AddSort(Sort sortinfo)
	{
		_sortArray.Add(sortinfo);
	}

	internal void InitSortArray()
	{
		if (_sortArray == null)
		{
			_sortArray = new ArrayList();
		}
		else
		{
			_sortArray.Clear();
		}
	}

	internal object GetGlobalParameter(XmlQualifiedName qname)
	{
		object obj = _args.GetParam(qname.Name, qname.Namespace);
		if (obj == null)
		{
			return null;
		}
		if (!(obj is XPathNodeIterator) && !(obj is XPathNavigator) && !(obj is bool) && !(obj is double) && !(obj is string))
		{
			obj = ((!(obj is short) && !(obj is ushort) && !(obj is int) && !(obj is uint) && !(obj is long) && !(obj is ulong) && !(obj is float) && !(obj is decimal)) ? obj.ToString() : ((object)XmlConvert.ToXPathDouble(obj)));
		}
		return obj;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "In order for this code path to be hit, a previous call to XsltArgumentList.AddExtensionObject is required. That method is already annotated as unsafe and throwing a warning, so we can suppress here.")]
	internal object GetExtensionObject(string nsUri)
	{
		return _args.GetExtensionObject(nsUri);
	}

	internal object GetScriptObject(string nsUri)
	{
		return _scriptExtensions[nsUri];
	}

	internal StringBuilder GetSharedStringBuilder()
	{
		if (_sharedStringBuilder == null)
		{
			_sharedStringBuilder = new StringBuilder();
		}
		else
		{
			_sharedStringBuilder.Length = 0;
		}
		return _sharedStringBuilder;
	}

	internal void ReleaseSharedStringBuilder()
	{
	}

	public Processor(XPathNavigator doc, XsltArgumentList args, XmlResolver resolver, Stylesheet stylesheet, List<TheQuery> queryStore, RootAction rootAction, IXsltDebugger debugger)
	{
		_stylesheet = stylesheet;
		_queryStore = queryStore;
		_rootAction = rootAction;
		_queryList = new Query[queryStore.Count];
		for (int i = 0; i < queryStore.Count; i++)
		{
			_queryList[i] = Query.Clone(queryStore[i].CompiledQuery.QueryTree);
		}
		_xsm = new StateMachine();
		_document = doc;
		_builder = null;
		_actionStack = new HWStack(10);
		_output = _rootAction.Output;
		_resolver = resolver ?? XmlNullResolver.Singleton;
		_args = args ?? new XsltArgumentList();
		_debugger = debugger;
		if (_debugger != null)
		{
			_debuggerStack = new HWStack(10, 1000);
			_templateLookup = new TemplateLookupActionDbg();
		}
		if (_rootAction.KeyList != null)
		{
			_keyList = new Key[_rootAction.KeyList.Count];
			for (int j = 0; j < _keyList.Length; j++)
			{
				_keyList[j] = _rootAction.KeyList[j].Clone();
			}
		}
		_scriptExtensions = new Hashtable(_stylesheet.ScriptObjectTypes.Count);
		if (_stylesheet.ScriptObjectTypes.Count > 0)
		{
			throw new PlatformNotSupportedException(System.SR.CompilingScriptsNotSupported);
		}
		PushActionFrame(_rootAction, null);
	}

	public ReaderOutput StartReader()
	{
		ReaderOutput readerOutput = new ReaderOutput(this);
		_builder = new RecordBuilder(readerOutput, _nameTable);
		return readerOutput;
	}

	public void Execute(Stream stream)
	{
		IRecordOutput output = null;
		switch (_output.Method)
		{
		case XsltOutput.OutputMethod.Text:
			output = new TextOnlyOutput(this, stream);
			break;
		case XsltOutput.OutputMethod.Xml:
		case XsltOutput.OutputMethod.Html:
		case XsltOutput.OutputMethod.Other:
		case XsltOutput.OutputMethod.Unknown:
			output = new TextOutput(this, stream);
			break;
		}
		_builder = new RecordBuilder(output, _nameTable);
		Execute();
	}

	public void Execute(TextWriter writer)
	{
		IRecordOutput output = null;
		switch (_output.Method)
		{
		case XsltOutput.OutputMethod.Text:
			output = new TextOnlyOutput(this, writer);
			break;
		case XsltOutput.OutputMethod.Xml:
		case XsltOutput.OutputMethod.Html:
		case XsltOutput.OutputMethod.Other:
		case XsltOutput.OutputMethod.Unknown:
			output = new TextOutput(this, writer);
			break;
		}
		_builder = new RecordBuilder(output, _nameTable);
		Execute();
	}

	public void Execute(XmlWriter writer)
	{
		_builder = new RecordBuilder(new WriterOutput(this, writer), _nameTable);
		Execute();
	}

	internal void Execute()
	{
		while (_execResult == ExecResult.Continue)
		{
			ActionFrame actionFrame = (ActionFrame)_actionStack.Peek();
			if (actionFrame == null)
			{
				_builder.TheEnd();
				ExecutionResult = ExecResult.Done;
				break;
			}
			if (actionFrame.Execute(this))
			{
				_actionStack.Pop();
			}
		}
		if (_execResult == ExecResult.Interrupt)
		{
			_execResult = ExecResult.Continue;
		}
	}

	internal ActionFrame PushNewFrame()
	{
		ActionFrame actionFrame = (ActionFrame)_actionStack.Peek();
		ActionFrame actionFrame2 = (ActionFrame)_actionStack.Push();
		if (actionFrame2 == null)
		{
			actionFrame2 = new ActionFrame();
			_actionStack.AddToTop(actionFrame2);
		}
		if (actionFrame != null)
		{
			actionFrame2.Inherit(actionFrame);
		}
		return actionFrame2;
	}

	internal void PushActionFrame(Action action, XPathNodeIterator nodeSet)
	{
		ActionFrame actionFrame = PushNewFrame();
		actionFrame.Init(action, nodeSet);
	}

	internal void PushActionFrame(ActionFrame container)
	{
		PushActionFrame(container, container.NodeSet);
	}

	internal void PushActionFrame(ActionFrame container, XPathNodeIterator nodeSet)
	{
		ActionFrame actionFrame = PushNewFrame();
		actionFrame.Init(container, nodeSet);
	}

	internal void PushTemplateLookup(XPathNodeIterator nodeSet, XmlQualifiedName mode, Stylesheet importsOf)
	{
		_templateLookup.Initialize(mode, importsOf);
		PushActionFrame(_templateLookup, nodeSet);
	}

	internal string GetQueryExpression(int key)
	{
		return _queryStore[key].CompiledQuery.Expression;
	}

	internal Query GetCompiledQuery(int key)
	{
		TheQuery theQuery = _queryStore[key];
		theQuery.CompiledQuery.CheckErrors();
		Query query = Query.Clone(_queryList[key]);
		query.SetXsltContext(new XsltCompileContext(theQuery._ScopeManager, this));
		return query;
	}

	internal Query GetValueQuery(int key)
	{
		return GetValueQuery(key, null);
	}

	internal Query GetValueQuery(int key, XsltCompileContext context)
	{
		TheQuery theQuery = _queryStore[key];
		theQuery.CompiledQuery.CheckErrors();
		Query query = _queryList[key];
		if (context == null)
		{
			context = new XsltCompileContext(theQuery._ScopeManager, this);
		}
		else
		{
			context.Reinitialize(theQuery._ScopeManager, this);
		}
		query.SetXsltContext(context);
		return query;
	}

	private XsltCompileContext GetValueOfContext()
	{
		if (_valueOfContext == null)
		{
			_valueOfContext = new XsltCompileContext();
		}
		return _valueOfContext;
	}

	private XsltCompileContext GetMatchesContext()
	{
		if (_matchesContext == null)
		{
			_matchesContext = new XsltCompileContext();
		}
		return _matchesContext;
	}

	internal string ValueOf(ActionFrame context, int key)
	{
		Query valueQuery = GetValueQuery(key, GetValueOfContext());
		object obj = valueQuery.Evaluate(context.NodeSet);
		if (obj is XPathNodeIterator)
		{
			XPathNavigator xPathNavigator = valueQuery.Advance();
			return (xPathNavigator != null) ? ValueOf(xPathNavigator) : string.Empty;
		}
		return XmlConvert.ToXPathString(obj);
	}

	internal string ValueOf(XPathNavigator n)
	{
		if (_stylesheet.Whitespace && n.NodeType == XPathNodeType.Element)
		{
			StringBuilder sharedStringBuilder = GetSharedStringBuilder();
			ElementValueWithoutWS(n, sharedStringBuilder);
			ReleaseSharedStringBuilder();
			return sharedStringBuilder.ToString();
		}
		return n.Value;
	}

	private void ElementValueWithoutWS(XPathNavigator nav, StringBuilder builder)
	{
		bool flag = Stylesheet.PreserveWhiteSpace(this, nav);
		if (!nav.MoveToFirstChild())
		{
			return;
		}
		do
		{
			switch (nav.NodeType)
			{
			case XPathNodeType.Text:
			case XPathNodeType.SignificantWhitespace:
				builder.Append(nav.Value);
				break;
			case XPathNodeType.Whitespace:
				if (flag)
				{
					builder.Append(nav.Value);
				}
				break;
			case XPathNodeType.Element:
				ElementValueWithoutWS(nav, builder);
				break;
			}
		}
		while (nav.MoveToNext());
		nav.MoveToParent();
	}

	internal XPathNodeIterator StartQuery(XPathNodeIterator context, int key)
	{
		Query compiledQuery = GetCompiledQuery(key);
		object obj = compiledQuery.Evaluate(context);
		if (obj is XPathNodeIterator)
		{
			return new XPathSelectionIterator(context.Current, compiledQuery);
		}
		throw XsltException.Create(System.SR.XPath_NodeSetExpected);
	}

	internal object Evaluate(ActionFrame context, int key)
	{
		return GetValueQuery(key).Evaluate(context.NodeSet);
	}

	internal object RunQuery(ActionFrame context, int key)
	{
		Query compiledQuery = GetCompiledQuery(key);
		object obj = compiledQuery.Evaluate(context.NodeSet);
		if (obj is XPathNodeIterator nodeIterator)
		{
			return new XPathArrayIterator(nodeIterator);
		}
		return obj;
	}

	internal string EvaluateString(ActionFrame context, int key)
	{
		object obj = Evaluate(context, key);
		string text = null;
		if (obj != null)
		{
			text = XmlConvert.ToXPathString(obj);
		}
		if (text == null)
		{
			text = string.Empty;
		}
		return text;
	}

	internal bool EvaluateBoolean(ActionFrame context, int key)
	{
		object obj = Evaluate(context, key);
		if (obj != null)
		{
			if (!(obj is XPathNavigator xPathNavigator))
			{
				return Convert.ToBoolean(obj, CultureInfo.InvariantCulture);
			}
			return Convert.ToBoolean(xPathNavigator.Value, CultureInfo.InvariantCulture);
		}
		return false;
	}

	internal bool Matches(XPathNavigator context, int key)
	{
		Query valueQuery = GetValueQuery(key, GetMatchesContext());
		try
		{
			return valueQuery.MatchNode(context) != null;
		}
		catch (XPathException)
		{
			throw XsltException.Create(System.SR.Xslt_InvalidPattern, GetQueryExpression(key));
		}
	}

	internal void ResetOutput()
	{
		_builder.Reset();
	}

	internal bool BeginEvent(XPathNodeType nodeType, string prefix, string name, string nspace, bool empty)
	{
		return BeginEvent(nodeType, prefix, name, nspace, empty, null, search: true);
	}

	internal bool BeginEvent(XPathNodeType nodeType, string prefix, string name, string nspace, bool empty, object htmlProps, bool search)
	{
		int num = _xsm.BeginOutlook(nodeType);
		if (_ignoreLevel > 0 || num == 16)
		{
			_ignoreLevel++;
			return true;
		}
		switch (_builder.BeginEvent(num, nodeType, prefix, name, nspace, empty, htmlProps, search))
		{
		case OutputResult.Continue:
			_xsm.Begin(nodeType);
			return true;
		case OutputResult.Interrupt:
			_xsm.Begin(nodeType);
			ExecutionResult = ExecResult.Interrupt;
			return true;
		case OutputResult.Overflow:
			ExecutionResult = ExecResult.Interrupt;
			return false;
		case OutputResult.Error:
			_ignoreLevel++;
			return true;
		case OutputResult.Ignore:
			return true;
		default:
			return true;
		}
	}

	internal bool TextEvent(string text)
	{
		return TextEvent(text, disableOutputEscaping: false);
	}

	internal bool TextEvent(string text, bool disableOutputEscaping)
	{
		if (_ignoreLevel > 0)
		{
			return true;
		}
		int state = _xsm.BeginOutlook(XPathNodeType.Text);
		switch (_builder.TextEvent(state, text, disableOutputEscaping))
		{
		case OutputResult.Continue:
			_xsm.Begin(XPathNodeType.Text);
			return true;
		case OutputResult.Interrupt:
			_xsm.Begin(XPathNodeType.Text);
			ExecutionResult = ExecResult.Interrupt;
			return true;
		case OutputResult.Overflow:
			ExecutionResult = ExecResult.Interrupt;
			return false;
		case OutputResult.Error:
		case OutputResult.Ignore:
			return true;
		default:
			return true;
		}
	}

	internal bool EndEvent(XPathNodeType nodeType)
	{
		if (_ignoreLevel > 0)
		{
			_ignoreLevel--;
			return true;
		}
		int state = _xsm.EndOutlook(nodeType);
		switch (_builder.EndEvent(state, nodeType))
		{
		case OutputResult.Continue:
			_xsm.End(nodeType);
			return true;
		case OutputResult.Interrupt:
			_xsm.End(nodeType);
			ExecutionResult = ExecResult.Interrupt;
			return true;
		case OutputResult.Overflow:
			ExecutionResult = ExecResult.Interrupt;
			return false;
		default:
			return true;
		}
	}

	internal bool CopyBeginEvent(XPathNavigator node, bool emptyflag)
	{
		switch (node.NodeType)
		{
		case XPathNodeType.Element:
		case XPathNodeType.Attribute:
		case XPathNodeType.ProcessingInstruction:
		case XPathNodeType.Comment:
			return BeginEvent(node.NodeType, node.Prefix, node.LocalName, node.NamespaceURI, emptyflag);
		case XPathNodeType.Namespace:
			return BeginEvent(XPathNodeType.Namespace, null, node.LocalName, node.Value, empty: false);
		default:
			return true;
		}
	}

	internal bool CopyTextEvent(XPathNavigator node)
	{
		switch (node.NodeType)
		{
		case XPathNodeType.Attribute:
		case XPathNodeType.Text:
		case XPathNodeType.SignificantWhitespace:
		case XPathNodeType.Whitespace:
		case XPathNodeType.ProcessingInstruction:
		case XPathNodeType.Comment:
		{
			string value = node.Value;
			return TextEvent(value);
		}
		default:
			return true;
		}
	}

	internal bool CopyEndEvent(XPathNavigator node)
	{
		switch (node.NodeType)
		{
		case XPathNodeType.Element:
		case XPathNodeType.Attribute:
		case XPathNodeType.Namespace:
		case XPathNodeType.ProcessingInstruction:
		case XPathNodeType.Comment:
			return EndEvent(node.NodeType);
		default:
			return true;
		}
	}

	internal static bool IsRoot(XPathNavigator navigator)
	{
		if (navigator.NodeType == XPathNodeType.Root)
		{
			return true;
		}
		if (navigator.NodeType == XPathNodeType.Element)
		{
			XPathNavigator xPathNavigator = navigator.Clone();
			xPathNavigator.MoveToRoot();
			return xPathNavigator.IsSamePosition(navigator);
		}
		return false;
	}

	internal void PushOutput(IRecordOutput output)
	{
		_builder.OutputState = _xsm.State;
		RecordBuilder builder = _builder;
		_builder = new RecordBuilder(output, _nameTable);
		_builder.Next = builder;
		_xsm.Reset();
	}

	internal IRecordOutput PopOutput()
	{
		RecordBuilder builder = _builder;
		_builder = builder.Next;
		_xsm.State = _builder.OutputState;
		builder.TheEnd();
		return builder.Output;
	}

	internal bool SetDefaultOutput(XsltOutput.OutputMethod method)
	{
		if (Output.Method != method)
		{
			_output = _output.CreateDerivedOutput(method);
			return true;
		}
		return false;
	}

	internal object GetVariableValue(VariableAction variable)
	{
		int varKey = variable.VarKey;
		if (variable.IsGlobal)
		{
			ActionFrame actionFrame = (ActionFrame)_actionStack[0];
			object variable2 = actionFrame.GetVariable(varKey);
			if (variable2 == VariableAction.BeingComputedMark)
			{
				throw XsltException.Create(System.SR.Xslt_CircularReference, variable.NameStr);
			}
			if (variable2 != null)
			{
				return variable2;
			}
			int length = _actionStack.Length;
			ActionFrame actionFrame2 = PushNewFrame();
			actionFrame2.Inherit(actionFrame);
			actionFrame2.Init(variable, actionFrame.NodeSet);
			do
			{
				if (((ActionFrame)_actionStack.Peek()).Execute(this))
				{
					_actionStack.Pop();
				}
			}
			while (length < _actionStack.Length);
			return actionFrame.GetVariable(varKey);
		}
		return ((ActionFrame)_actionStack.Peek()).GetVariable(varKey);
	}

	internal void SetParameter(XmlQualifiedName name, object value)
	{
		ActionFrame actionFrame = (ActionFrame)_actionStack[_actionStack.Length - 2];
		actionFrame.SetParameter(name, value);
	}

	internal void ResetParams()
	{
		ActionFrame actionFrame = (ActionFrame)_actionStack[_actionStack.Length - 1];
		actionFrame.ResetParams();
	}

	internal object GetParameter(XmlQualifiedName name)
	{
		ActionFrame actionFrame = (ActionFrame)_actionStack[_actionStack.Length - 3];
		return actionFrame.GetParameter(name);
	}

	internal void PopDebuggerStack()
	{
		_debuggerStack.Pop();
	}

	internal XmlQualifiedName GetPreviousMode()
	{
		return ((DebuggerFrame)_debuggerStack[_debuggerStack.Length - 2]).currentMode;
	}

	internal void SetCurrentMode(XmlQualifiedName mode)
	{
		((DebuggerFrame)_debuggerStack[_debuggerStack.Length - 1]).currentMode = mode;
	}
}

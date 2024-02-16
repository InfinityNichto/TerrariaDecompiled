using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Xml.Xsl.Qil;
using System.Xml.Xsl.Runtime;
using System.Xml.Xsl.XPath;

namespace System.Xml.Xsl.Xslt;

internal sealed class QilGenerator : IErrorHelper, IXPathEnvironment, IFocus
{
	private sealed class VariableHelper
	{
		private readonly Stack<QilIterator> _vars = new Stack<QilIterator>();

		private readonly XPathQilFactory _f;

		public VariableHelper(XPathQilFactory f)
		{
			_f = f;
		}

		public int StartVariables()
		{
			return _vars.Count;
		}

		public void AddVariable(QilIterator let)
		{
			_vars.Push(let);
		}

		public QilNode FinishVariables(QilNode node, int varScope)
		{
			int num = _vars.Count - varScope;
			while (num-- != 0)
			{
				node = _f.Loop(_vars.Pop(), node);
			}
			return node;
		}
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private readonly struct ThrowErrorHelper : IErrorHelper
	{
		public void ReportError(string res, params string[] args)
		{
			throw new XslLoadException(System.SR.Xml_UserException, res);
		}
	}

	public enum FuncId
	{
		Current,
		Document,
		Key,
		FormatNumber,
		UnparsedEntityUri,
		GenerateId,
		SystemProperty,
		ElementAvailable,
		FunctionAvailable
	}

	private readonly CompilerScopeManager<QilIterator> _scope;

	private readonly OutputScopeManager _outputScope;

	private readonly HybridDictionary _prefixesInUse;

	private readonly XsltQilFactory _f;

	private readonly XPathBuilder _xpathBuilder;

	private readonly XPathParser<QilNode> _xpathParser;

	private readonly XPathPatternBuilder _ptrnBuilder;

	private readonly XPathPatternParser _ptrnParser;

	private readonly ReferenceReplacer _refReplacer;

	private KeyMatchBuilder _keyMatchBuilder;

	private readonly InvokeGenerator _invkGen;

	private readonly MatcherBuilder _matcherBuilder;

	private readonly QilStrConcatenator _strConcat;

	private readonly VariableHelper _varHelper;

	private Compiler _compiler;

	private QilList _functions;

	private QilFunction _generalKey;

	private bool _formatNumberDynamicUsed;

	private QilList _extPars;

	private QilList _gloVars;

	private QilList _nsVars;

	private readonly XmlQueryType _elementOrDocumentType;

	private readonly XmlQueryType _textOrAttributeType;

	private XslNode _lastScope;

	private XslVersion _xslVersion;

	private readonly QilName _nameCurrent;

	private readonly QilName _namePosition;

	private readonly QilName _nameLast;

	private readonly QilName _nameNamespaces;

	private readonly QilName _nameInit;

	private SingletonFocus _singlFocus;

	private FunctionFocus _funcFocus;

	private LoopFocus _curLoop;

	private int _formatterCnt;

	private readonly StringBuilder _unescapedText = new StringBuilder();

	private static readonly char[] s_curlyBraces = new char[2] { '{', '}' };

	private bool _allowVariables = true;

	private bool _allowCurrent = true;

	private bool _allowKey = true;

	private static readonly XmlTypeCode[] s_argFnDocument = new XmlTypeCode[2]
	{
		XmlTypeCode.Item,
		XmlTypeCode.Node
	};

	private static readonly XmlTypeCode[] s_argFnKey = new XmlTypeCode[2]
	{
		XmlTypeCode.String,
		XmlTypeCode.Item
	};

	private static readonly XmlTypeCode[] s_argFnFormatNumber = new XmlTypeCode[3]
	{
		XmlTypeCode.Double,
		XmlTypeCode.String,
		XmlTypeCode.String
	};

	public static Dictionary<string, XPathBuilder.FunctionInfo<FuncId>> FunctionTable = CreateFunctionTable();

	private bool IsDebug => _compiler.IsDebug;

	private bool EvaluateFuncCalls => !IsDebug;

	private bool InferXPathTypes => !IsDebug;

	XPathQilFactory IXPathEnvironment.Factory => _f;

	public static QilExpression CompileStylesheet(Compiler compiler)
	{
		return new QilGenerator(compiler.IsDebug).Compile(compiler);
	}

	private QilGenerator(bool debug)
	{
		_scope = new CompilerScopeManager<QilIterator>();
		_outputScope = new OutputScopeManager();
		_prefixesInUse = new HybridDictionary();
		_f = new XsltQilFactory(new QilFactory(), debug);
		_xpathBuilder = new XPathBuilder(this);
		_xpathParser = new XPathParser<QilNode>();
		_ptrnBuilder = new XPathPatternBuilder(this);
		_ptrnParser = new XPathPatternParser();
		_refReplacer = new ReferenceReplacer(_f.BaseFactory);
		_invkGen = new InvokeGenerator(_f, debug);
		_matcherBuilder = new MatcherBuilder(_f, _refReplacer, _invkGen);
		_singlFocus = new SingletonFocus(_f);
		_funcFocus = default(FunctionFocus);
		_curLoop = new LoopFocus(_f);
		_strConcat = new QilStrConcatenator(_f);
		_varHelper = new VariableHelper(_f);
		_elementOrDocumentType = XmlQueryTypeFactory.DocumentOrElement;
		_textOrAttributeType = XmlQueryTypeFactory.NodeChoice(XmlNodeKindFlags.Attribute | XmlNodeKindFlags.Text);
		_nameCurrent = _f.QName("current", "urn:schemas-microsoft-com:xslt-debug");
		_namePosition = _f.QName("position", "urn:schemas-microsoft-com:xslt-debug");
		_nameLast = _f.QName("last", "urn:schemas-microsoft-com:xslt-debug");
		_nameNamespaces = _f.QName("namespaces", "urn:schemas-microsoft-com:xslt-debug");
		_nameInit = _f.QName("init", "urn:schemas-microsoft-com:xslt-debug");
		_formatterCnt = 0;
	}

	private QilExpression Compile(Compiler compiler)
	{
		_compiler = compiler;
		_functions = _f.FunctionList();
		_extPars = _f.GlobalParameterList();
		_gloVars = _f.GlobalVariableList();
		_nsVars = _f.GlobalVariableList();
		new XslAstRewriter().Rewrite(compiler);
		if (!IsDebug)
		{
			new XslAstAnalyzer().Analyze(compiler);
		}
		CreateGlobalVarPars();
		try
		{
			CompileKeys();
			CompileAndSortMatches(compiler.Root.Imports[0]);
			PrecompileProtoTemplatesHeaders();
			CompileGlobalVariables();
			foreach (ProtoTemplate allTemplate in compiler.AllTemplates)
			{
				CompileProtoTemplate(allTemplate);
			}
		}
		catch (XslLoadException ex)
		{
			ex.SetSourceLineInfo(_lastScope.SourceLine);
			throw;
		}
		catch (Exception ex2)
		{
			if (!XmlException.IsCatchableException(ex2))
			{
				throw;
			}
			throw new XslLoadException(ex2, _lastScope.SourceLine);
		}
		CompileInitializationCode();
		QilNode root = CompileRootExpression(compiler.StartApplyTemplates);
		foreach (ProtoTemplate allTemplate2 in compiler.AllTemplates)
		{
			foreach (QilParameter argument in allTemplate2.Function.Arguments)
			{
				if (!IsDebug || argument.Name.Equals(_nameNamespaces))
				{
					argument.DefaultValue = null;
				}
			}
		}
		Scripts.TrimSafeDictionary scriptClasses = compiler.Scripts.ScriptClasses;
		List<EarlyBoundInfo> list = new List<EarlyBoundInfo>(scriptClasses.Count);
		foreach (string key in scriptClasses.Keys)
		{
			Type type = scriptClasses[key];
			if (type != null)
			{
				list.Add(new EarlyBoundInfo(key, type));
			}
		}
		QilExpression qilExpression = _f.QilExpression(root, _f.BaseFactory);
		qilExpression.EarlyBoundTypes = list;
		qilExpression.FunctionList = _functions;
		qilExpression.GlobalParameterList = _extPars;
		qilExpression.GlobalVariableList = _gloVars;
		qilExpression.WhitespaceRules = compiler.WhitespaceRules;
		qilExpression.IsDebug = IsDebug;
		qilExpression.DefaultWriterSettings = compiler.Output.Settings;
		QilDepthChecker.Check(qilExpression);
		return qilExpression;
	}

	private QilNode InvokeOnCurrentNodeChanged()
	{
		QilIterator qilIterator;
		return _f.Loop(qilIterator = _f.Let(_f.InvokeOnCurrentNodeChanged(_curLoop.GetCurrent())), _f.Sequence());
	}

	private void CompileInitializationCode()
	{
		QilNode qilNode = _f.Int32(0);
		if (_formatNumberDynamicUsed || IsDebug)
		{
			bool flag = false;
			foreach (DecimalFormatDecl decimalFormat in _compiler.DecimalFormats)
			{
				qilNode = _f.Add(qilNode, _f.InvokeRegisterDecimalFormat(decimalFormat));
				flag |= decimalFormat.Name == DecimalFormatDecl.Default.Name;
			}
			if (!flag)
			{
				qilNode = _f.Add(qilNode, _f.InvokeRegisterDecimalFormat(DecimalFormatDecl.Default));
			}
		}
		foreach (string key in _compiler.Scripts.ScriptClasses.Keys)
		{
			qilNode = _f.Add(qilNode, _f.InvokeCheckScriptNamespace(key));
		}
		if (qilNode.NodeType == QilNodeType.Add)
		{
			QilFunction qilFunction = _f.Function(_f.FormalParameterList(), qilNode, _f.True());
			qilFunction.DebugName = "Init";
			_functions.Add(qilFunction);
			QilNode qilNode2 = _f.Invoke(qilFunction, _f.ActualParameterList());
			if (IsDebug)
			{
				qilNode2 = _f.TypeAssert(qilNode2, XmlQueryTypeFactory.ItemS);
			}
			QilIterator qilIterator = _f.Let(qilNode2);
			qilIterator.DebugName = _nameInit.ToString();
			_gloVars.Insert(0, qilIterator);
		}
	}

	private QilNode CompileRootExpression(XslNode applyTmpls)
	{
		_singlFocus.SetFocus(SingletonFocusType.InitialContextNode);
		QilNode child = GenerateApply(_compiler.Root, applyTmpls);
		_singlFocus.SetFocus(null);
		return _f.DocumentCtor(child);
	}

	private QilList EnterScope(XslNode node)
	{
		_lastScope = node;
		_xslVersion = node.XslVersion;
		if (_scope.EnterScope(node.Namespaces))
		{
			return BuildDebuggerNamespaces();
		}
		return null;
	}

	private void ExitScope()
	{
		_scope.ExitScope();
	}

	private QilList BuildDebuggerNamespaces()
	{
		if (IsDebug)
		{
			QilList qilList = _f.BaseFactory.Sequence();
			CompilerScopeManager<QilIterator>.NamespaceEnumerator enumerator = _scope.GetEnumerator();
			while (enumerator.MoveNext())
			{
				CompilerScopeManager<QilIterator>.ScopeRecord current = enumerator.Current;
				qilList.Add(_f.NamespaceDecl(_f.String(current.ncName), _f.String(current.nsUri)));
			}
			return qilList;
		}
		return null;
	}

	private QilNode GetCurrentNode()
	{
		if (_curLoop.IsFocusSet)
		{
			return _curLoop.GetCurrent();
		}
		if (_funcFocus.IsFocusSet)
		{
			return _funcFocus.GetCurrent();
		}
		return _singlFocus.GetCurrent();
	}

	private QilNode GetCurrentPosition()
	{
		if (_curLoop.IsFocusSet)
		{
			return _curLoop.GetPosition();
		}
		if (_funcFocus.IsFocusSet)
		{
			return _funcFocus.GetPosition();
		}
		return _singlFocus.GetPosition();
	}

	private QilNode GetLastPosition()
	{
		if (_curLoop.IsFocusSet)
		{
			return _curLoop.GetLast();
		}
		if (_funcFocus.IsFocusSet)
		{
			return _funcFocus.GetLast();
		}
		return _singlFocus.GetLast();
	}

	private XmlQueryType ChooseBestType(VarPar var)
	{
		if (IsDebug || !InferXPathTypes)
		{
			return XmlQueryTypeFactory.ItemS;
		}
		return (var.Flags & XslFlags.TypeFilter) switch
		{
			XslFlags.String => XmlQueryTypeFactory.StringX, 
			XslFlags.Number => XmlQueryTypeFactory.DoubleX, 
			XslFlags.Boolean => XmlQueryTypeFactory.BooleanX, 
			XslFlags.Node => XmlQueryTypeFactory.NodeNotRtf, 
			XslFlags.Nodeset => XmlQueryTypeFactory.NodeNotRtfS, 
			XslFlags.Rtf => XmlQueryTypeFactory.Node, 
			XslFlags.Node | XslFlags.Rtf => XmlQueryTypeFactory.Node, 
			XslFlags.Node | XslFlags.Nodeset => XmlQueryTypeFactory.NodeNotRtfS, 
			XslFlags.Nodeset | XslFlags.Rtf => XmlQueryTypeFactory.NodeS, 
			XslFlags.Node | XslFlags.Nodeset | XslFlags.Rtf => XmlQueryTypeFactory.NodeS, 
			_ => XmlQueryTypeFactory.ItemS, 
		};
	}

	private QilIterator GetNsVar(QilList nsList)
	{
		foreach (QilIterator nsVar in _nsVars)
		{
			QilList qilList = (QilList)nsVar.Binding;
			if (qilList.Count != nsList.Count)
			{
				continue;
			}
			bool flag = true;
			for (int i = 0; i < nsList.Count; i++)
			{
				if (((QilLiteral)((QilBinary)nsList[i]).Right).Value != ((QilLiteral)((QilBinary)qilList[i]).Right).Value || ((QilLiteral)((QilBinary)nsList[i]).Left).Value != ((QilLiteral)((QilBinary)qilList[i]).Left).Value)
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				return nsVar;
			}
		}
		QilIterator qilIterator2 = _f.Let(nsList);
		qilIterator2.DebugName = _f.QName("ns" + _nsVars.Count, "urn:schemas-microsoft-com:xslt-debug").ToString();
		_gloVars.Add(qilIterator2);
		_nsVars.Add(qilIterator2);
		return qilIterator2;
	}

	private void PrecompileProtoTemplatesHeaders()
	{
		List<VarPar> list = null;
		Dictionary<VarPar, Template> dictionary = null;
		Dictionary<VarPar, QilFunction> dictionary2 = null;
		foreach (ProtoTemplate allTemplate in _compiler.AllTemplates)
		{
			QilList qilList = _f.FormalParameterList();
			XslFlags xslFlags = ((!IsDebug) ? allTemplate.Flags : XslFlags.FocusFilter);
			QilList qilList2 = EnterScope(allTemplate);
			if ((xslFlags & XslFlags.Current) != 0)
			{
				qilList.Add(CreateXslParam(CloneName(_nameCurrent), XmlQueryTypeFactory.NodeNotRtf));
			}
			if ((xslFlags & XslFlags.Position) != 0)
			{
				qilList.Add(CreateXslParam(CloneName(_namePosition), XmlQueryTypeFactory.DoubleX));
			}
			if ((xslFlags & XslFlags.Last) != 0)
			{
				qilList.Add(CreateXslParam(CloneName(_nameLast), XmlQueryTypeFactory.DoubleX));
			}
			if (IsDebug && qilList2 != null)
			{
				QilParameter qilParameter = CreateXslParam(CloneName(_nameNamespaces), XmlQueryTypeFactory.NamespaceS);
				qilParameter.DefaultValue = GetNsVar(qilList2);
				qilList.Add(qilParameter);
			}
			if (allTemplate is Template template)
			{
				_funcFocus.StartFocus(qilList, xslFlags);
				for (int i = 0; i < allTemplate.Content.Count; i++)
				{
					XslNode xslNode = allTemplate.Content[i];
					if (xslNode.NodeType == XslNodeType.Text)
					{
						continue;
					}
					if (xslNode.NodeType != XslNodeType.Param)
					{
						break;
					}
					VarPar varPar = (VarPar)xslNode;
					EnterScope(varPar);
					if (_scope.IsLocalVariable(varPar.Name.LocalName, varPar.Name.NamespaceUri))
					{
						ReportError(System.SR.Xslt_DupLocalVariable, varPar.Name.QualifiedName);
					}
					QilParameter qilParameter2 = CreateXslParam(varPar.Name, ChooseBestType(varPar));
					if (IsDebug)
					{
						qilParameter2.Annotation = varPar;
					}
					else if ((varPar.DefValueFlags & XslFlags.HasCalls) == 0)
					{
						qilParameter2.DefaultValue = CompileVarParValue(varPar);
					}
					else
					{
						QilList qilList3 = _f.FormalParameterList();
						QilList qilList4 = _f.ActualParameterList();
						for (int j = 0; j < qilList.Count; j++)
						{
							QilParameter qilParameter3 = _f.Parameter(qilList[j].XmlType);
							qilParameter3.DebugName = ((QilParameter)qilList[j]).DebugName;
							qilParameter3.Name = CloneName(((QilParameter)qilList[j]).Name);
							SetLineInfo(qilParameter3, qilList[j].SourceLine);
							qilList3.Add(qilParameter3);
							qilList4.Add(qilList[j]);
						}
						varPar.Flags |= template.Flags & XslFlags.FocusFilter;
						QilFunction qilFunction = _f.Function(qilList3, _f.Boolean((varPar.DefValueFlags & XslFlags.SideEffects) != 0), ChooseBestType(varPar));
						qilFunction.SourceLine = SourceLineInfo.NoSource;
						qilFunction.DebugName = "<xsl:param name=\"" + varPar.Name.QualifiedName + "\">";
						qilParameter2.DefaultValue = _f.Invoke(qilFunction, qilList4);
						if (list == null)
						{
							list = new List<VarPar>();
							dictionary = new Dictionary<VarPar, Template>();
							dictionary2 = new Dictionary<VarPar, QilFunction>();
						}
						list.Add(varPar);
						dictionary.Add(varPar, template);
						dictionary2.Add(varPar, qilFunction);
					}
					SetLineInfo(qilParameter2, varPar.SourceLine);
					ExitScope();
					_scope.AddVariable(varPar.Name, qilParameter2);
					qilList.Add(qilParameter2);
				}
				_funcFocus.StopFocus();
			}
			ExitScope();
			allTemplate.Function = _f.Function(qilList, _f.Boolean((allTemplate.Flags & XslFlags.SideEffects) != 0), (allTemplate is AttributeSet) ? XmlQueryTypeFactory.AttributeS : XmlQueryTypeFactory.NodeNotRtfS);
			allTemplate.Function.DebugName = allTemplate.GetDebugName();
			SetLineInfo(allTemplate.Function, allTemplate.SourceLine ?? SourceLineInfo.NoSource);
			_functions.Add(allTemplate.Function);
		}
		if (list == null)
		{
			return;
		}
		foreach (VarPar item in list)
		{
			Template node = dictionary[item];
			QilFunction qilFunction2 = dictionary2[item];
			_funcFocus.StartFocus(qilFunction2.Arguments, item.Flags);
			EnterScope(node);
			EnterScope(item);
			foreach (QilParameter argument in qilFunction2.Arguments)
			{
				_scope.AddVariable(argument.Name, argument);
			}
			qilFunction2.Definition = CompileVarParValue(item);
			SetLineInfo(qilFunction2.Definition, item.SourceLine);
			ExitScope();
			ExitScope();
			_funcFocus.StopFocus();
			_functions.Add(qilFunction2);
		}
	}

	private QilParameter CreateXslParam(QilName name, XmlQueryType xt)
	{
		QilParameter qilParameter = _f.Parameter(xt);
		qilParameter.DebugName = name.ToString();
		qilParameter.Name = name;
		return qilParameter;
	}

	private void CompileProtoTemplate(ProtoTemplate tmpl)
	{
		EnterScope(tmpl);
		_funcFocus.StartFocus(tmpl.Function.Arguments, (!IsDebug) ? tmpl.Flags : XslFlags.FocusFilter);
		foreach (QilParameter argument in tmpl.Function.Arguments)
		{
			if (argument.Name.NamespaceUri != "urn:schemas-microsoft-com:xslt-debug")
			{
				if (IsDebug)
				{
					VarPar node = (VarPar)argument.Annotation;
					QilList nsList = EnterScope(node);
					argument.DefaultValue = CompileVarParValue(node);
					ExitScope();
					argument.DefaultValue = SetDebugNs(argument.DefaultValue, nsList);
				}
				_scope.AddVariable(argument.Name, argument);
			}
		}
		tmpl.Function.Definition = CompileInstructions(tmpl.Content);
		_funcFocus.StopFocus();
		ExitScope();
	}

	private QilList InstructionList()
	{
		return _f.BaseFactory.Sequence();
	}

	private QilNode CompileInstructions(IList<XslNode> instructions)
	{
		return CompileInstructions(instructions, 0, InstructionList());
	}

	private QilNode CompileInstructions(IList<XslNode> instructions, int from)
	{
		return CompileInstructions(instructions, from, InstructionList());
	}

	private QilNode CompileInstructions(IList<XslNode> instructions, QilList content)
	{
		return CompileInstructions(instructions, 0, content);
	}

	private QilNode CompileInstructions(IList<XslNode> instructions, int from, QilList content)
	{
		for (int i = from; i < instructions.Count; i++)
		{
			XslNode xslNode = instructions[i];
			XslNodeType nodeType = xslNode.NodeType;
			if (nodeType == XslNodeType.Param)
			{
				continue;
			}
			QilList nsList = EnterScope(xslNode);
			QilNode qilNode = nodeType switch
			{
				XslNodeType.ApplyImports => CompileApplyImports(xslNode), 
				XslNodeType.ApplyTemplates => CompileApplyTemplates((XslNodeEx)xslNode), 
				XslNodeType.Attribute => CompileAttribute((NodeCtor)xslNode), 
				XslNodeType.CallTemplate => CompileCallTemplate((XslNodeEx)xslNode), 
				XslNodeType.Choose => CompileChoose(xslNode), 
				XslNodeType.Comment => CompileComment(xslNode), 
				XslNodeType.Copy => CompileCopy(xslNode), 
				XslNodeType.CopyOf => CompileCopyOf(xslNode), 
				XslNodeType.Element => CompileElement((NodeCtor)xslNode), 
				XslNodeType.Error => CompileError(xslNode), 
				XslNodeType.ForEach => CompileForEach((XslNodeEx)xslNode), 
				XslNodeType.If => CompileIf(xslNode), 
				XslNodeType.List => CompileList(xslNode), 
				XslNodeType.LiteralAttribute => CompileLiteralAttribute(xslNode), 
				XslNodeType.LiteralElement => CompileLiteralElement(xslNode), 
				XslNodeType.Message => CompileMessage(xslNode), 
				XslNodeType.Nop => CompileNop(xslNode), 
				XslNodeType.Number => CompileNumber((Number)xslNode), 
				XslNodeType.PI => CompilePI(xslNode), 
				XslNodeType.Text => CompileText((Text)xslNode), 
				XslNodeType.UseAttributeSet => CompileUseAttributeSet(xslNode), 
				XslNodeType.ValueOf => CompileValueOf(xslNode), 
				XslNodeType.ValueOfDoe => CompileValueOfDoe(xslNode), 
				XslNodeType.Variable => CompileVariable(xslNode), 
				_ => null, 
			};
			ExitScope();
			if (qilNode.NodeType != QilNodeType.Sequence || qilNode.Count != 0)
			{
				if (nodeType != XslNodeType.LiteralAttribute && nodeType != XslNodeType.UseAttributeSet)
				{
					SetLineInfoCheck(qilNode, xslNode.SourceLine);
				}
				qilNode = SetDebugNs(qilNode, nsList);
				if (nodeType == XslNodeType.Variable)
				{
					QilIterator qilIterator = _f.Let(qilNode);
					qilIterator.DebugName = xslNode.Name.ToString();
					_scope.AddVariable(xslNode.Name, qilIterator);
					qilNode = _f.Loop(qilIterator, CompileInstructions(instructions, i + 1));
					i = instructions.Count;
				}
				content.Add(qilNode);
			}
		}
		if (!IsDebug && content.Count == 1)
		{
			return content[0];
		}
		return content;
	}

	private QilNode CompileList(XslNode node)
	{
		return CompileInstructions(node.Content);
	}

	private QilNode CompileNop(XslNode node)
	{
		return _f.Nop(_f.Sequence());
	}

	private void AddNsDecl(QilList content, string prefix, string nsUri)
	{
		if (!(_outputScope.LookupNamespace(prefix) == nsUri))
		{
			_outputScope.AddNamespace(prefix, nsUri);
			content.Add(_f.NamespaceDecl(_f.String(prefix), _f.String(nsUri)));
		}
	}

	private QilNode CompileLiteralElement(XslNode node)
	{
		bool flag = true;
		while (true)
		{
			_prefixesInUse.Clear();
			QilName name = node.Name;
			string prefix = name.Prefix;
			string nsUri = name.NamespaceUri;
			_compiler.ApplyNsAliases(ref prefix, ref nsUri);
			if (flag)
			{
				_prefixesInUse.Add(prefix, nsUri);
			}
			else
			{
				prefix = name.Prefix;
			}
			_outputScope.PushScope();
			QilList content = InstructionList();
			CompilerScopeManager<QilIterator>.NamespaceEnumerator enumerator = _scope.GetEnumerator();
			while (true)
			{
				if (enumerator.MoveNext())
				{
					CompilerScopeManager<QilIterator>.ScopeRecord current = enumerator.Current;
					string prefix2 = current.ncName;
					string nsUri2 = current.nsUri;
					if (!(nsUri2 != "http://www.w3.org/1999/XSL/Transform") || _scope.IsExNamespace(nsUri2))
					{
						continue;
					}
					_compiler.ApplyNsAliases(ref prefix2, ref nsUri2);
					if (flag)
					{
						if (_prefixesInUse.Contains(prefix2))
						{
							if ((string)_prefixesInUse[prefix2] != nsUri2)
							{
								break;
							}
						}
						else
						{
							_prefixesInUse.Add(prefix2, nsUri2);
						}
					}
					else
					{
						prefix2 = current.ncName;
					}
					AddNsDecl(content, prefix2, nsUri2);
					continue;
				}
				QilNode content2 = CompileInstructions(node.Content, content);
				_outputScope.PopScope();
				name.Prefix = prefix;
				name.NamespaceUri = nsUri;
				return _f.ElementCtor(name, content2);
			}
			_outputScope.PopScope();
			flag = false;
		}
	}

	private QilNode CompileElement(NodeCtor node)
	{
		QilNode qilNode = CompileStringAvt(node.NsAvt);
		QilNode qilNode2 = CompileStringAvt(node.NameAvt);
		QilNode name;
		if (qilNode2.NodeType != QilNodeType.LiteralString || (qilNode != null && qilNode.NodeType != QilNodeType.LiteralString))
		{
			name = ((qilNode == null) ? ResolveQNameDynamic(ignoreDefaultNs: false, qilNode2) : _f.StrParseQName(qilNode2, qilNode));
		}
		else
		{
			string qname = (QilLiteral)qilNode2;
			string prefix;
			string localName;
			bool flag = _compiler.ParseQName(qname, out prefix, out localName, this);
			string uri = ((qilNode != null) ? ((string)(QilLiteral)qilNode) : (flag ? ResolvePrefix(ignoreDefaultNs: false, prefix) : _compiler.CreatePhantomNamespace()));
			name = _f.QName(localName, uri, prefix);
		}
		_outputScope.PushScope();
		_outputScope.InvalidateAllPrefixes();
		QilNode content = CompileInstructions(node.Content);
		_outputScope.PopScope();
		return _f.ElementCtor(name, content);
	}

	private QilNode CompileLiteralAttribute(XslNode node)
	{
		QilName name = node.Name;
		string prefix = name.Prefix;
		string nsUri = name.NamespaceUri;
		if (prefix.Length != 0)
		{
			_compiler.ApplyNsAliases(ref prefix, ref nsUri);
		}
		name.Prefix = prefix;
		name.NamespaceUri = nsUri;
		return _f.AttributeCtor(name, CompileTextAvt(node.Select));
	}

	private QilNode CompileAttribute(NodeCtor node)
	{
		QilNode qilNode = CompileStringAvt(node.NsAvt);
		QilNode qilNode2 = CompileStringAvt(node.NameAvt);
		bool flag = false;
		QilNode name;
		if (qilNode2.NodeType != QilNodeType.LiteralString || (qilNode != null && qilNode.NodeType != QilNodeType.LiteralString))
		{
			name = ((qilNode == null) ? ResolveQNameDynamic(ignoreDefaultNs: true, qilNode2) : _f.StrParseQName(qilNode2, qilNode));
		}
		else
		{
			string text = (QilLiteral)qilNode2;
			string prefix;
			string localName;
			bool flag2 = _compiler.ParseQName(text, out prefix, out localName, this);
			string text2;
			if (qilNode == null)
			{
				text2 = (flag2 ? ResolvePrefix(ignoreDefaultNs: true, prefix) : _compiler.CreatePhantomNamespace());
			}
			else
			{
				text2 = (QilLiteral)qilNode;
				flag = true;
			}
			if (text == "xmlns" || (localName == "xmlns" && text2.Length == 0))
			{
				ReportError(System.SR.Xslt_XmlnsAttr, "name", text);
			}
			name = _f.QName(localName, text2, prefix);
		}
		if (flag)
		{
			_outputScope.InvalidateNonDefaultPrefixes();
		}
		return _f.AttributeCtor(name, CompileInstructions(node.Content));
	}

	private QilNode ExtractText(string source, ref int pos)
	{
		int num = pos;
		_unescapedText.Length = 0;
		int i;
		for (i = pos; i < source.Length; i++)
		{
			char c = source[i];
			if (c != '{' && c != '}')
			{
				continue;
			}
			if (i + 1 < source.Length && source[i + 1] == c)
			{
				i++;
				_unescapedText.Append(source, num, i - num);
				num = i + 1;
				continue;
			}
			if (c == '{')
			{
				break;
			}
			pos = source.Length;
			if (_xslVersion != XslVersion.ForwardsCompatible)
			{
				ReportError(System.SR.Xslt_SingleRightBraceInAvt, source);
				return null;
			}
			return _f.Error(_lastScope.SourceLine, System.SR.Xslt_SingleRightBraceInAvt, source);
		}
		pos = i;
		if (_unescapedText.Length == 0)
		{
			if (i <= num)
			{
				return null;
			}
			return _f.String(source.Substring(num, i - num));
		}
		_unescapedText.Append(source, num, i - num);
		return _f.String(_unescapedText.ToString());
	}

	private QilNode CompileAvt(string source)
	{
		QilList qilList = _f.BaseFactory.Sequence();
		int pos = 0;
		while (pos < source.Length)
		{
			QilNode qilNode = ExtractText(source, ref pos);
			if (qilNode != null)
			{
				qilList.Add(qilNode);
			}
			if (pos < source.Length)
			{
				pos++;
				QilNode n = CompileXPathExpressionWithinAvt(source, ref pos);
				qilList.Add(_f.ConvertToString(n));
			}
		}
		if (qilList.Count == 1)
		{
			return qilList[0];
		}
		return qilList;
	}

	[return: NotNullIfNotNull("avt")]
	private QilNode CompileStringAvt(string avt)
	{
		if (avt == null)
		{
			return null;
		}
		if (avt.IndexOfAny(s_curlyBraces) == -1)
		{
			return _f.String(avt);
		}
		return _f.StrConcat(CompileAvt(avt));
	}

	private QilNode CompileTextAvt(string avt)
	{
		if (avt.IndexOfAny(s_curlyBraces) == -1)
		{
			return _f.TextCtor(_f.String(avt));
		}
		QilNode qilNode = CompileAvt(avt);
		if (qilNode.NodeType == QilNodeType.Sequence)
		{
			QilList qilList = InstructionList();
			{
				foreach (QilNode item in qilNode)
				{
					qilList.Add(_f.TextCtor(item));
				}
				return qilList;
			}
		}
		return _f.TextCtor(qilNode);
	}

	private QilNode CompileText(Text node)
	{
		if (node.Hints == SerializationHints.None)
		{
			return _f.TextCtor(_f.String(node.Select));
		}
		return _f.RawTextCtor(_f.String(node.Select));
	}

	private QilNode CompilePI(XslNode node)
	{
		QilNode qilNode = CompileStringAvt(node.Select);
		if (qilNode.NodeType == QilNodeType.LiteralString)
		{
			string name = (QilLiteral)qilNode;
			_compiler.ValidatePiName(name, this);
		}
		return _f.PICtor(qilNode, CompileInstructions(node.Content));
	}

	private QilNode CompileComment(XslNode node)
	{
		return _f.CommentCtor(CompileInstructions(node.Content));
	}

	private QilNode CompileError(XslNode node)
	{
		return _f.Error(_f.String(node.Select));
	}

	private QilNode WrapLoopBody(ISourceLineInfo before, QilNode expr, ISourceLineInfo after)
	{
		if (IsDebug)
		{
			return _f.Sequence(SetLineInfo(InvokeOnCurrentNodeChanged(), before), expr, SetLineInfo(_f.Nop(_f.Sequence()), after));
		}
		return expr;
	}

	private QilNode CompileForEach(XslNodeEx node)
	{
		IList<XslNode> content = node.Content;
		LoopFocus parentLoop = _curLoop;
		QilIterator focus = _f.For(CompileNodeSetExpression(node.Select));
		_curLoop.SetFocus(focus);
		int varScope = _varHelper.StartVariables();
		_curLoop.Sort(CompileSorts(content, ref parentLoop));
		QilNode expr = CompileInstructions(content);
		expr = WrapLoopBody(node.ElemNameLi, expr, node.EndTagLi);
		expr = AddCurrentPositionLast(expr);
		expr = _curLoop.ConstructLoop(expr);
		expr = _varHelper.FinishVariables(expr, varScope);
		_curLoop = parentLoop;
		return expr;
	}

	private QilNode CompileApplyTemplates(XslNodeEx node)
	{
		IList<XslNode> content = node.Content;
		int varScope = _varHelper.StartVariables();
		QilIterator qilIterator = _f.Let(CompileNodeSetExpression(node.Select));
		_varHelper.AddVariable(qilIterator);
		for (int i = 0; i < content.Count; i++)
		{
			if (content[i] is VarPar varPar)
			{
				CompileWithParam(varPar);
				QilNode value = varPar.Value;
				if (IsDebug || (!(value is QilIterator) && !(value is QilLiteral)))
				{
					QilIterator qilIterator2 = _f.Let(value);
					qilIterator2.DebugName = _f.QName("with-param " + varPar.Name.QualifiedName, "urn:schemas-microsoft-com:xslt-debug").ToString();
					_varHelper.AddVariable(qilIterator2);
					varPar.Value = qilIterator2;
				}
			}
		}
		LoopFocus parentLoop = _curLoop;
		QilIterator focus = _f.For(qilIterator);
		_curLoop.SetFocus(focus);
		_curLoop.Sort(CompileSorts(content, ref parentLoop));
		QilNode expr = GenerateApply(_compiler.Root, node);
		expr = WrapLoopBody(node.ElemNameLi, expr, node.EndTagLi);
		expr = AddCurrentPositionLast(expr);
		expr = _curLoop.ConstructLoop(expr);
		_curLoop = parentLoop;
		return _varHelper.FinishVariables(expr, varScope);
	}

	private QilNode CompileApplyImports(XslNode node)
	{
		return GenerateApply((StylesheetLevel)node.Arg, node);
	}

	private QilNode CompileCallTemplate(XslNodeEx node)
	{
		int varScope = _varHelper.StartVariables();
		IList<XslNode> content = node.Content;
		foreach (VarPar item in content)
		{
			CompileWithParam(item);
			if (IsDebug)
			{
				QilNode value = item.Value;
				QilIterator qilIterator = _f.Let(value);
				qilIterator.DebugName = _f.QName("with-param " + item.Name.QualifiedName, "urn:schemas-microsoft-com:xslt-debug").ToString();
				_varHelper.AddVariable(qilIterator);
				item.Value = qilIterator;
			}
		}
		QilNode qilNode;
		if (_compiler.NamedTemplates.TryGetValue(node.Name, out var value2))
		{
			qilNode = _invkGen.GenerateInvoke(value2.Function, AddRemoveImplicitArgs(node.Content, value2.Flags));
		}
		else
		{
			if (!_compiler.IsPhantomName(node.Name))
			{
				_compiler.ReportError(node.SourceLine, System.SR.Xslt_InvalidCallTemplate, node.Name.QualifiedName);
			}
			qilNode = _f.Sequence();
		}
		if (content.Count > 0)
		{
			qilNode = SetLineInfo(qilNode, node.ElemNameLi);
		}
		qilNode = _varHelper.FinishVariables(qilNode, varScope);
		if (IsDebug)
		{
			return _f.Nop(qilNode);
		}
		return qilNode;
	}

	private QilNode CompileUseAttributeSet(XslNode node)
	{
		_outputScope.InvalidateAllPrefixes();
		if (_compiler.AttributeSets.TryGetValue(node.Name, out var value))
		{
			return _invkGen.GenerateInvoke(value.Function, AddRemoveImplicitArgs(node.Content, value.Flags));
		}
		if (!_compiler.IsPhantomName(node.Name))
		{
			_compiler.ReportError(node.SourceLine, System.SR.Xslt_NoAttributeSet, node.Name.QualifiedName);
		}
		return _f.Sequence();
	}

	private QilNode CompileCopy(XslNode copy)
	{
		QilNode currentNode = GetCurrentNode();
		if ((currentNode.XmlType.NodeKinds & (XmlNodeKindFlags.Attribute | XmlNodeKindFlags.Namespace)) != 0)
		{
			_outputScope.InvalidateAllPrefixes();
		}
		if (currentNode.XmlType.NodeKinds == XmlNodeKindFlags.Element)
		{
			QilList qilList = InstructionList();
			qilList.Add(_f.XPathNamespace(currentNode));
			_outputScope.PushScope();
			_outputScope.InvalidateAllPrefixes();
			QilNode content = CompileInstructions(copy.Content, qilList);
			_outputScope.PopScope();
			return _f.ElementCtor(_f.NameOf(currentNode), content);
		}
		if (currentNode.XmlType.NodeKinds == XmlNodeKindFlags.Document)
		{
			return CompileInstructions(copy.Content);
		}
		if ((currentNode.XmlType.NodeKinds & (XmlNodeKindFlags.Document | XmlNodeKindFlags.Element)) == 0)
		{
			return currentNode;
		}
		return _f.XsltCopy(currentNode, CompileInstructions(copy.Content));
	}

	private QilNode CompileCopyOf(XslNode node)
	{
		QilNode qilNode = CompileXPathExpression(node.Select);
		if (qilNode.XmlType.IsNode)
		{
			if ((qilNode.XmlType.NodeKinds & (XmlNodeKindFlags.Attribute | XmlNodeKindFlags.Namespace)) != 0)
			{
				_outputScope.InvalidateAllPrefixes();
			}
			if (qilNode.XmlType.IsNotRtf && (qilNode.XmlType.NodeKinds & XmlNodeKindFlags.Document) == 0)
			{
				return qilNode;
			}
			if (qilNode.XmlType.IsSingleton)
			{
				return _f.XsltCopyOf(qilNode);
			}
			QilIterator expr;
			return _f.Loop(expr = _f.For(qilNode), _f.XsltCopyOf(expr));
		}
		if (qilNode.XmlType.IsAtomicValue)
		{
			return _f.TextCtor(_f.ConvertToString(qilNode));
		}
		_outputScope.InvalidateAllPrefixes();
		QilIterator expr2;
		return _f.Loop(expr2 = _f.For(qilNode), _f.Conditional(_f.IsType(expr2, XmlQueryTypeFactory.Node), _f.XsltCopyOf(_f.TypeAssert(expr2, XmlQueryTypeFactory.Node)), _f.TextCtor(_f.XsltConvert(expr2, XmlQueryTypeFactory.StringX))));
	}

	private QilNode CompileValueOf(XslNode valueOf)
	{
		return _f.TextCtor(_f.ConvertToString(CompileXPathExpression(valueOf.Select)));
	}

	private QilNode CompileValueOfDoe(XslNode valueOf)
	{
		return _f.RawTextCtor(_f.ConvertToString(CompileXPathExpression(valueOf.Select)));
	}

	private QilNode CompileWhen(XslNode whenNode, QilNode otherwise)
	{
		return _f.Conditional(_f.ConvertToBoolean(CompileXPathExpression(whenNode.Select)), CompileInstructions(whenNode.Content), otherwise);
	}

	private QilNode CompileIf(XslNode ifNode)
	{
		return CompileWhen(ifNode, InstructionList());
	}

	private QilNode CompileChoose(XslNode node)
	{
		IList<XslNode> content = node.Content;
		QilNode qilNode = null;
		int num = content.Count - 1;
		while (0 <= num)
		{
			XslNode xslNode = content[num];
			QilList nsList = EnterScope(xslNode);
			qilNode = ((xslNode.NodeType != XslNodeType.Otherwise) ? CompileWhen(xslNode, qilNode ?? InstructionList()) : CompileInstructions(xslNode.Content));
			ExitScope();
			SetLineInfoCheck(qilNode, xslNode.SourceLine);
			qilNode = SetDebugNs(qilNode, nsList);
			num--;
		}
		if (qilNode == null)
		{
			return _f.Sequence();
		}
		if (!IsDebug)
		{
			return qilNode;
		}
		return _f.Sequence(qilNode);
	}

	private QilNode CompileMessage(XslNode node)
	{
		string uri = _lastScope.SourceLine.Uri;
		QilNode n = _f.RtfCtor(CompileInstructions(node.Content), _f.String(uri));
		n = _f.InvokeOuterXml(n);
		if (!(bool)node.Arg)
		{
			return _f.Warning(n);
		}
		QilIterator text;
		return _f.Loop(text = _f.Let(n), _f.Sequence(_f.Warning(text), _f.Error(text)));
	}

	private QilNode CompileVariable(XslNode node)
	{
		if (_scope.IsLocalVariable(node.Name.LocalName, node.Name.NamespaceUri))
		{
			ReportError(System.SR.Xslt_DupLocalVariable, node.Name.QualifiedName);
		}
		return CompileVarParValue(node);
	}

	private QilNode CompileVarParValue(XslNode node)
	{
		string uri = _lastScope.SourceLine.Uri;
		IList<XslNode> content = node.Content;
		string select = node.Select;
		QilNode qilNode;
		if (select != null)
		{
			QilList qilList = InstructionList();
			qilList.Add(CompileXPathExpression(select));
			qilNode = CompileInstructions(content, qilList);
		}
		else if (content.Count != 0)
		{
			_outputScope.PushScope();
			_outputScope.InvalidateAllPrefixes();
			qilNode = _f.RtfCtor(CompileInstructions(content), _f.String(uri));
			_outputScope.PopScope();
		}
		else
		{
			qilNode = _f.String(string.Empty);
		}
		if (IsDebug)
		{
			qilNode = _f.TypeAssert(qilNode, XmlQueryTypeFactory.ItemS);
		}
		return qilNode;
	}

	private void CompileWithParam(VarPar withParam)
	{
		QilList nsList = EnterScope(withParam);
		QilNode n = CompileVarParValue(withParam);
		ExitScope();
		SetLineInfo(n, withParam.SourceLine);
		n = SetDebugNs(n, nsList);
		withParam.Value = n;
	}

	private QilNode CompileSorts(IList<XslNode> content, ref LoopFocus parentLoop)
	{
		QilList qilList = _f.BaseFactory.SortKeyList();
		int num = 0;
		while (num < content.Count)
		{
			if (content[num] is Sort sort)
			{
				CompileSort(sort, qilList, ref parentLoop);
				content.RemoveAt(num);
			}
			else
			{
				num++;
			}
		}
		if (qilList.Count == 0)
		{
			return null;
		}
		return qilList;
	}

	private QilNode CompileLangAttribute(string attValue, bool fwdCompat)
	{
		QilNode qilNode = CompileStringAvt(attValue);
		if (qilNode != null)
		{
			if (qilNode.NodeType == QilNodeType.LiteralString)
			{
				string lang = (QilLiteral)qilNode;
				string text = XsltLibrary.LangToNameInternal(lang, fwdCompat, this);
				if (text == "")
				{
					qilNode = null;
				}
			}
			else
			{
				QilIterator qilIterator;
				qilNode = _f.Loop(qilIterator = _f.Let(qilNode), _f.Conditional(_f.Eq(_f.InvokeLangToLcid(qilIterator, fwdCompat), _f.Int32(127)), _f.String(string.Empty), qilIterator));
			}
		}
		return qilNode;
	}

	private QilNode CompileLangAttributeToLcid(string attValue, bool fwdCompat)
	{
		return CompileLangToLcid(CompileStringAvt(attValue), fwdCompat);
	}

	private QilNode CompileLangToLcid(QilNode lang, bool fwdCompat)
	{
		if (lang == null)
		{
			return _f.Double(127.0);
		}
		if (lang.NodeType == QilNodeType.LiteralString)
		{
			return _f.String(XsltLibrary.LangToNameInternal((QilLiteral)lang, fwdCompat, this));
		}
		return _f.XsltConvert(_f.InvokeLangToLcid(lang, fwdCompat), XmlQueryTypeFactory.DoubleX);
	}

	private void CompileDataTypeAttribute(string attValue, bool fwdCompat, ref QilNode select, out QilNode select2)
	{
		QilNode qilNode = CompileStringAvt(attValue);
		if (qilNode != null)
		{
			if (qilNode.NodeType != QilNodeType.LiteralString)
			{
				QilIterator qilIterator;
				QilIterator qilIterator2;
				qilNode = _f.Loop(qilIterator = _f.Let(qilNode), _f.Conditional(_f.Eq(qilIterator, _f.String("number")), _f.False(), _f.Conditional(_f.Eq(qilIterator, _f.String("text")), _f.True(), fwdCompat ? _f.True() : _f.Loop(qilIterator2 = _f.Let(ResolveQNameDynamic(ignoreDefaultNs: true, qilIterator)), _f.Error(_lastScope.SourceLine, System.SR.Xslt_BistateAttribute, "data-type", "text", "number")))));
				QilIterator qilIterator3 = _f.Let(qilNode);
				_varHelper.AddVariable(qilIterator3);
				select2 = select.DeepClone(_f.BaseFactory);
				select = _f.Conditional(qilIterator3, _f.ConvertToString(select), _f.String(string.Empty));
				select2 = _f.Conditional(qilIterator3, _f.Double(0.0), _f.ConvertToNumber(select2));
				return;
			}
			string text = (QilLiteral)qilNode;
			if (text == "number")
			{
				select = _f.ConvertToNumber(select);
				select2 = null;
				return;
			}
			if (!(text == "text") && !fwdCompat)
			{
				string prefix;
				string localName;
				string text2 = (_compiler.ParseQName(text, out prefix, out localName, this) ? ResolvePrefix(ignoreDefaultNs: true, prefix) : _compiler.CreatePhantomNamespace());
				_ = text2.Length;
				ReportError(System.SR.Xslt_BistateAttribute, "data-type", "text", "number");
			}
		}
		select = _f.ConvertToString(select);
		select2 = null;
	}

	[return: NotNullIfNotNull("attName")]
	private QilNode CompileOrderAttribute(string attName, string attValue, string value0, string value1, bool fwdCompat)
	{
		QilNode qilNode = CompileStringAvt(attValue);
		if (qilNode != null)
		{
			if (qilNode.NodeType == QilNodeType.LiteralString)
			{
				string text = (QilLiteral)qilNode;
				if (text == value1)
				{
					qilNode = _f.String("1");
				}
				else
				{
					if (text != value0 && !fwdCompat)
					{
						ReportError(System.SR.Xslt_BistateAttribute, attName, value0, value1);
					}
					qilNode = _f.String("0");
				}
			}
			else
			{
				QilIterator left;
				qilNode = _f.Loop(left = _f.Let(qilNode), _f.Conditional(_f.Eq(left, _f.String(value1)), _f.String("1"), fwdCompat ? _f.String("0") : _f.Conditional(_f.Eq(left, _f.String(value0)), _f.String("0"), _f.Error(_lastScope.SourceLine, System.SR.Xslt_BistateAttribute, attName, value0, value1))));
			}
		}
		return qilNode;
	}

	private void CompileSort(Sort sort, QilList keyList, ref LoopFocus parentLoop)
	{
		EnterScope(sort);
		bool forwardsCompatible = sort.ForwardsCompatible;
		QilNode select = CompileXPathExpression(sort.Select);
		QilNode value;
		QilNode select2;
		QilNode qilNode;
		QilNode qilNode2;
		if (sort.Lang != null || sort.DataType != null || sort.Order != null || sort.CaseOrder != null)
		{
			LoopFocus curLoop = _curLoop;
			_curLoop = parentLoop;
			value = CompileLangAttribute(sort.Lang, forwardsCompatible);
			CompileDataTypeAttribute(sort.DataType, forwardsCompatible, ref select, out select2);
			qilNode = CompileOrderAttribute("order", sort.Order, "ascending", "descending", forwardsCompatible);
			qilNode2 = CompileOrderAttribute("case-order", sort.CaseOrder, "lower-first", "upper-first", forwardsCompatible);
			_curLoop = curLoop;
		}
		else
		{
			select = _f.ConvertToString(select);
			select2 = (value = (qilNode = (qilNode2 = null)));
		}
		_strConcat.Reset();
		_strConcat.Append("http://collations.microsoft.com");
		_strConcat.Append('/');
		_strConcat.Append(value);
		char value2 = '?';
		if (qilNode != null)
		{
			_strConcat.Append(value2);
			_strConcat.Append("descendingOrder=");
			_strConcat.Append(qilNode);
			value2 = '&';
		}
		if (qilNode2 != null)
		{
			_strConcat.Append(value2);
			_strConcat.Append("upperFirst=");
			_strConcat.Append(qilNode2);
			value2 = '&';
		}
		QilNode qilNode3 = _strConcat.ToQil();
		QilSortKey node = _f.SortKey(select, qilNode3);
		keyList.Add(node);
		if (select2 != null)
		{
			node = _f.SortKey(select2, qilNode3.DeepClone(_f.BaseFactory));
			keyList.Add(node);
		}
		ExitScope();
	}

	private QilNode MatchPattern(QilNode pattern, QilIterator testNode)
	{
		if (pattern.NodeType == QilNodeType.Error)
		{
			return pattern;
		}
		QilList qilList;
		if (pattern.NodeType == QilNodeType.Sequence)
		{
			qilList = (QilList)pattern;
		}
		else
		{
			qilList = _f.BaseFactory.Sequence();
			qilList.Add(pattern);
		}
		QilNode qilNode = _f.False();
		int num = qilList.Count - 1;
		while (0 <= num)
		{
			QilLoop qilLoop = (QilLoop)qilList[num];
			qilNode = _f.Or(_refReplacer.Replace(qilLoop.Body, qilLoop.Variable, testNode), qilNode);
			num--;
		}
		return qilNode;
	}

	private QilNode MatchCountPattern(QilNode countPattern, QilIterator testNode)
	{
		if (countPattern != null)
		{
			return MatchPattern(countPattern, testNode);
		}
		QilNode currentNode = GetCurrentNode();
		XmlNodeKindFlags nodeKinds = currentNode.XmlType.NodeKinds;
		if ((nodeKinds & (nodeKinds - 1)) != 0)
		{
			return _f.InvokeIsSameNodeSort(testNode, currentNode);
		}
		QilNode left;
		switch (nodeKinds)
		{
		case XmlNodeKindFlags.Document:
			return _f.IsType(testNode, XmlQueryTypeFactory.Document);
		case XmlNodeKindFlags.Element:
			left = _f.IsType(testNode, XmlQueryTypeFactory.Element);
			break;
		case XmlNodeKindFlags.Attribute:
			left = _f.IsType(testNode, XmlQueryTypeFactory.Attribute);
			break;
		case XmlNodeKindFlags.Text:
			return _f.IsType(testNode, XmlQueryTypeFactory.Text);
		case XmlNodeKindFlags.Comment:
			return _f.IsType(testNode, XmlQueryTypeFactory.Comment);
		case XmlNodeKindFlags.PI:
			return _f.And(_f.IsType(testNode, XmlQueryTypeFactory.PI), _f.Eq(_f.LocalNameOf(testNode), _f.LocalNameOf(currentNode)));
		case XmlNodeKindFlags.Namespace:
			return _f.And(_f.IsType(testNode, XmlQueryTypeFactory.Namespace), _f.Eq(_f.LocalNameOf(testNode), _f.LocalNameOf(currentNode)));
		default:
			return _f.False();
		}
		return _f.And(left, _f.And(_f.Eq(_f.LocalNameOf(testNode), _f.LocalNameOf(currentNode)), _f.Eq(_f.NamespaceUriOf(testNode), _f.NamespaceUriOf(GetCurrentNode()))));
	}

	private QilNode PlaceMarker(QilNode countPattern, QilNode fromPattern, bool multiple)
	{
		QilNode countPattern2 = countPattern?.DeepClone(_f.BaseFactory);
		QilIterator testNode;
		QilNode qilNode = _f.Filter(testNode = _f.For(_f.AncestorOrSelf(GetCurrentNode())), MatchCountPattern(countPattern, testNode));
		QilNode qilNode2 = ((!multiple) ? _f.Filter(testNode = _f.For(qilNode), _f.Eq(_f.PositionOf(testNode), _f.Int32(1))) : _f.DocOrderDistinct(qilNode));
		QilNode binding;
		QilIterator right;
		if (fromPattern == null)
		{
			binding = qilNode2;
		}
		else
		{
			QilNode binding2 = _f.Filter(testNode = _f.For(_f.AncestorOrSelf(GetCurrentNode())), MatchPattern(fromPattern, testNode));
			QilNode binding3 = _f.Filter(testNode = _f.For(binding2), _f.Eq(_f.PositionOf(testNode), _f.Int32(1)));
			binding = _f.Loop(testNode = _f.For(binding3), _f.Filter(right = _f.For(qilNode2), _f.Before(testNode, right)));
		}
		return _f.Loop(right = _f.For(binding), _f.Add(_f.Int32(1), _f.Length(_f.Filter(testNode = _f.For(_f.PrecedingSibling(right)), MatchCountPattern(countPattern2, testNode)))));
	}

	private QilNode PlaceMarkerAny(QilNode countPattern, QilNode fromPattern)
	{
		QilNode child;
		QilIterator testNode2;
		if (fromPattern == null)
		{
			QilNode binding = _f.NodeRange(_f.Root(GetCurrentNode()), GetCurrentNode());
			QilIterator testNode;
			child = _f.Filter(testNode = _f.For(binding), MatchCountPattern(countPattern, testNode));
		}
		else
		{
			QilIterator testNode;
			QilNode binding2 = _f.Filter(testNode = _f.For(_f.Preceding(GetCurrentNode())), MatchPattern(fromPattern, testNode));
			QilNode binding3 = _f.Filter(testNode = _f.For(binding2), _f.Eq(_f.PositionOf(testNode), _f.Int32(1)));
			QilIterator right;
			child = _f.Loop(testNode = _f.For(binding3), _f.Filter(right = _f.For(_f.Filter(testNode2 = _f.For(_f.NodeRange(testNode, GetCurrentNode())), MatchCountPattern(countPattern, testNode2))), _f.Not(_f.Is(testNode, right))));
		}
		return _f.Loop(testNode2 = _f.Let(_f.Length(child)), _f.Conditional(_f.Eq(testNode2, _f.Int32(0)), _f.Sequence(), testNode2));
	}

	private QilNode CompileLetterValueAttribute(string attValue, bool fwdCompat)
	{
		QilNode qilNode = CompileStringAvt(attValue);
		if (qilNode != null)
		{
			if (qilNode.NodeType == QilNodeType.LiteralString)
			{
				string text = (QilLiteral)qilNode;
				if (text != "alphabetic" && text != "traditional")
				{
					if (fwdCompat)
					{
						return _f.String("default");
					}
					ReportError(System.SR.Xslt_BistateAttribute, "letter-value", "alphabetic", "traditional");
				}
				return qilNode;
			}
			QilIterator qilIterator = _f.Let(qilNode);
			return _f.Loop(qilIterator, _f.Conditional(_f.Or(_f.Eq(qilIterator, _f.String("alphabetic")), _f.Eq(qilIterator, _f.String("traditional"))), qilIterator, fwdCompat ? _f.String("default") : _f.Error(_lastScope.SourceLine, System.SR.Xslt_BistateAttribute, "letter-value", "alphabetic", "traditional")));
		}
		return _f.String("default");
	}

	private QilNode CompileGroupingSeparatorAttribute(string attValue, bool fwdCompat)
	{
		QilNode qilNode = CompileStringAvt(attValue);
		if (qilNode == null)
		{
			qilNode = _f.String(string.Empty);
		}
		else if (qilNode.NodeType == QilNodeType.LiteralString)
		{
			string text = (QilLiteral)qilNode;
			if (text.Length != 1)
			{
				if (!fwdCompat)
				{
					ReportError(System.SR.Xslt_CharAttribute, "grouping-separator");
				}
				qilNode = _f.String(string.Empty);
			}
		}
		else
		{
			QilIterator qilIterator = _f.Let(qilNode);
			qilNode = _f.Loop(qilIterator, _f.Conditional(_f.Eq(_f.StrLength(qilIterator), _f.Int32(1)), qilIterator, fwdCompat ? _f.String(string.Empty) : _f.Error(_lastScope.SourceLine, System.SR.Xslt_CharAttribute, "grouping-separator")));
		}
		return qilNode;
	}

	private QilNode CompileGroupingSizeAttribute(string attValue, bool fwdCompat)
	{
		QilNode qilNode = CompileStringAvt(attValue);
		if (qilNode == null)
		{
			return _f.Double(0.0);
		}
		if (qilNode.NodeType == QilNodeType.LiteralString)
		{
			string s = (QilLiteral)qilNode;
			double num = XsltFunctions.Round(XPathConvert.StringToDouble(s));
			if (0.0 <= num && num <= 2147483647.0)
			{
				return _f.Double(num);
			}
			return _f.Double(0.0);
		}
		QilIterator qilIterator = _f.Let(_f.ConvertToNumber(qilNode));
		return _f.Loop(qilIterator, _f.Conditional(_f.And(_f.Lt(_f.Double(0.0), qilIterator), _f.Lt(qilIterator, _f.Double(2147483647.0))), qilIterator, _f.Double(0.0)));
	}

	private QilNode CompileNumber(Number num)
	{
		QilNode value;
		if (num.Value != null)
		{
			value = _f.ConvertToNumber(CompileXPathExpression(num.Value));
		}
		else
		{
			QilNode countPattern = ((num.Count != null) ? CompileNumberPattern(num.Count) : null);
			QilNode fromPattern = ((num.From != null) ? CompileNumberPattern(num.From) : null);
			value = num.Level switch
			{
				NumberLevel.Single => PlaceMarker(countPattern, fromPattern, multiple: false), 
				NumberLevel.Multiple => PlaceMarker(countPattern, fromPattern, multiple: true), 
				_ => PlaceMarkerAny(countPattern, fromPattern), 
			};
		}
		bool forwardsCompatible = num.ForwardsCompatible;
		return _f.TextCtor(_f.InvokeNumberFormat(value, CompileStringAvt(num.Format), CompileLangAttributeToLcid(num.Lang, forwardsCompatible), CompileLetterValueAttribute(num.LetterValue, forwardsCompatible), CompileGroupingSeparatorAttribute(num.GroupingSeparator, forwardsCompatible), CompileGroupingSizeAttribute(num.GroupingSize, forwardsCompatible)));
	}

	private void CompileAndSortMatches(Stylesheet sheet)
	{
		foreach (Template template in sheet.Templates)
		{
			if (template.Match == null)
			{
				continue;
			}
			EnterScope(template);
			QilNode qilNode = CompileMatchPattern(template.Match);
			if (qilNode.NodeType == QilNodeType.Sequence)
			{
				QilList qilList = (QilList)qilNode;
				for (int i = 0; i < qilList.Count; i++)
				{
					sheet.AddTemplateMatch(template, (QilLoop)qilList[i]);
				}
			}
			else
			{
				sheet.AddTemplateMatch(template, (QilLoop)qilNode);
			}
			ExitScope();
		}
		sheet.SortTemplateMatches();
		Stylesheet[] imports = sheet.Imports;
		foreach (Stylesheet sheet2 in imports)
		{
			CompileAndSortMatches(sheet2);
		}
	}

	private void CompileKeys()
	{
		for (int i = 0; i < _compiler.Keys.Count; i++)
		{
			foreach (Key item in _compiler.Keys[i])
			{
				EnterScope(item);
				QilParameter qilParameter = _f.Parameter(XmlQueryTypeFactory.NodeNotRtf);
				_singlFocus.SetFocus(qilParameter);
				QilIterator qilIterator = _f.For(_f.OptimizeBarrier(CompileKeyMatch(item.Match)));
				_singlFocus.SetFocus(qilIterator);
				QilIterator qilIterator2 = _f.For(CompileKeyUse(item));
				qilIterator2 = _f.For(_f.OptimizeBarrier(_f.Loop(qilIterator2, _f.ConvertToString(qilIterator2))));
				QilParameter qilParameter2 = _f.Parameter(XmlQueryTypeFactory.StringX);
				QilFunction qilFunction = _f.Function(_f.FormalParameterList(qilParameter, qilParameter2), _f.Filter(qilIterator, _f.Not(_f.IsEmpty(_f.Filter(qilIterator2, _f.Eq(qilIterator2, qilParameter2))))), _f.False());
				qilFunction.DebugName = item.GetDebugName();
				SetLineInfo(qilFunction, item.SourceLine);
				item.Function = qilFunction;
				_functions.Add(qilFunction);
				ExitScope();
			}
		}
		_singlFocus.SetFocus(null);
	}

	private void CreateGlobalVarPars()
	{
		foreach (VarPar externalPar in _compiler.ExternalPars)
		{
			CreateGlobalVarPar(externalPar);
		}
		foreach (VarPar globalVar in _compiler.GlobalVars)
		{
			CreateGlobalVarPar(globalVar);
		}
	}

	private void CreateGlobalVarPar(VarPar varPar)
	{
		XmlQueryType t = ChooseBestType(varPar);
		QilIterator qilIterator = ((varPar.NodeType != XslNodeType.Variable) ? _f.Parameter(null, varPar.Name, t) : _f.Let(_f.Unknown(t)));
		qilIterator.DebugName = varPar.Name.ToString();
		varPar.Value = qilIterator;
		SetLineInfo(qilIterator, varPar.SourceLine);
		_scope.AddVariable(varPar.Name, qilIterator);
	}

	private void CompileGlobalVariables()
	{
		_singlFocus.SetFocus(SingletonFocusType.InitialDocumentNode);
		foreach (VarPar externalPar in _compiler.ExternalPars)
		{
			_extPars.Add(CompileGlobalVarPar(externalPar));
		}
		foreach (VarPar globalVar in _compiler.GlobalVars)
		{
			_gloVars.Add(CompileGlobalVarPar(globalVar));
		}
		_singlFocus.SetFocus(null);
	}

	private QilIterator CompileGlobalVarPar(VarPar varPar)
	{
		QilIterator qilIterator = (QilIterator)varPar.Value;
		QilList nsList = EnterScope(varPar);
		QilNode qilNode = CompileVarParValue(varPar);
		SetLineInfo(qilNode, qilIterator.SourceLine);
		qilNode = AddCurrentPositionLast(qilNode);
		qilNode = SetDebugNs(qilNode, nsList);
		qilIterator.SourceLine = SourceLineInfo.NoSource;
		qilIterator.Binding = qilNode;
		ExitScope();
		return qilIterator;
	}

	private void ReportErrorInXPath(XslLoadException e)
	{
		string text = ((e is XPathCompileException ex) ? ex.FormatDetailedMessage() : e.Message);
		_compiler.ReportError(_lastScope.SourceLine, System.SR.Xml_UserException, text);
	}

	private QilNode PhantomXPathExpression()
	{
		return _f.TypeAssert(_f.Sequence(), XmlQueryTypeFactory.ItemS);
	}

	private QilNode PhantomKeyMatch()
	{
		return _f.TypeAssert(_f.Sequence(), XmlQueryTypeFactory.NodeNotRtfS);
	}

	private QilNode CompileXPathExpression(string expr)
	{
		SetEnvironmentFlags(allowVariables: true, allowCurrent: true, allowKey: true);
		QilNode qilNode;
		if (expr == null)
		{
			qilNode = PhantomXPathExpression();
		}
		else
		{
			try
			{
				XPathScanner scanner = new XPathScanner(expr);
				qilNode = _xpathParser.Parse(scanner, _xpathBuilder, LexKind.Eof);
			}
			catch (XslLoadException ex)
			{
				if (_xslVersion != XslVersion.ForwardsCompatible)
				{
					ReportErrorInXPath(ex);
				}
				qilNode = _f.Error(_f.String(ex.Message));
			}
		}
		if (qilNode is QilIterator)
		{
			qilNode = _f.Nop(qilNode);
		}
		return qilNode;
	}

	private QilNode CompileNodeSetExpression(string expr)
	{
		QilNode qilNode = _f.TryEnsureNodeSet(CompileXPathExpression(expr));
		if (qilNode == null)
		{
			XPathCompileException ex = new XPathCompileException(expr, 0, expr.Length, System.SR.XPath_NodeSetExpected, (string[])null);
			if (_xslVersion != XslVersion.ForwardsCompatible)
			{
				ReportErrorInXPath(ex);
			}
			qilNode = _f.Error(_f.String(ex.Message));
		}
		return qilNode;
	}

	private QilNode CompileXPathExpressionWithinAvt(string expr, ref int pos)
	{
		SetEnvironmentFlags(allowVariables: true, allowCurrent: true, allowKey: true);
		QilNode qilNode;
		try
		{
			XPathScanner xPathScanner = new XPathScanner(expr, pos);
			qilNode = _xpathParser.Parse(xPathScanner, _xpathBuilder, LexKind.RBrace);
			pos = xPathScanner.LexStart + 1;
		}
		catch (XslLoadException ex)
		{
			if (_xslVersion != XslVersion.ForwardsCompatible)
			{
				ReportErrorInXPath(ex);
			}
			qilNode = _f.Error(_f.String(ex.Message));
			pos = expr.Length;
		}
		if (qilNode is QilIterator)
		{
			qilNode = _f.Nop(qilNode);
		}
		return qilNode;
	}

	private QilNode CompileMatchPattern(string pttrn)
	{
		SetEnvironmentFlags(allowVariables: false, allowCurrent: false, allowKey: true);
		QilNode qilNode;
		try
		{
			XPathScanner scanner = new XPathScanner(pttrn);
			qilNode = _ptrnParser.Parse(scanner, _ptrnBuilder);
		}
		catch (XslLoadException ex)
		{
			if (_xslVersion != XslVersion.ForwardsCompatible)
			{
				ReportErrorInXPath(ex);
			}
			qilNode = _f.Loop(_f.For(_ptrnBuilder.FixupNode), _f.Error(_f.String(ex.Message)));
			XPathPatternBuilder.SetPriority(qilNode, 0.5);
		}
		return qilNode;
	}

	private QilNode CompileNumberPattern(string pttrn)
	{
		SetEnvironmentFlags(allowVariables: true, allowCurrent: false, allowKey: true);
		try
		{
			XPathScanner scanner = new XPathScanner(pttrn);
			return _ptrnParser.Parse(scanner, _ptrnBuilder);
		}
		catch (XslLoadException ex)
		{
			if (_xslVersion != XslVersion.ForwardsCompatible)
			{
				ReportErrorInXPath(ex);
			}
			return _f.Error(_f.String(ex.Message));
		}
	}

	private QilNode CompileKeyMatch(string pttrn)
	{
		if (_keyMatchBuilder == null)
		{
			_keyMatchBuilder = new KeyMatchBuilder(this);
		}
		SetEnvironmentFlags(allowVariables: false, allowCurrent: false, allowKey: false);
		if (pttrn == null)
		{
			return PhantomKeyMatch();
		}
		try
		{
			XPathScanner scanner = new XPathScanner(pttrn);
			return _ptrnParser.Parse(scanner, _keyMatchBuilder);
		}
		catch (XslLoadException ex)
		{
			if (_xslVersion != XslVersion.ForwardsCompatible)
			{
				ReportErrorInXPath(ex);
			}
			return _f.Error(_f.String(ex.Message));
		}
	}

	private QilNode CompileKeyUse(Key key)
	{
		string use = key.Use;
		SetEnvironmentFlags(allowVariables: false, allowCurrent: true, allowKey: false);
		QilNode qilNode;
		if (use == null)
		{
			qilNode = _f.Error(_f.String(XslLoadException.CreateMessage(key.SourceLine, System.SR.Xslt_MissingAttribute, "use")));
		}
		else
		{
			try
			{
				XPathScanner scanner = new XPathScanner(use);
				qilNode = _xpathParser.Parse(scanner, _xpathBuilder, LexKind.Eof);
			}
			catch (XslLoadException ex)
			{
				if (_xslVersion != XslVersion.ForwardsCompatible)
				{
					ReportErrorInXPath(ex);
				}
				qilNode = _f.Error(_f.String(ex.Message));
			}
		}
		if (qilNode is QilIterator)
		{
			qilNode = _f.Nop(qilNode);
		}
		return qilNode;
	}

	private QilNode ResolveQNameDynamic(bool ignoreDefaultNs, QilNode qilName)
	{
		QilList qilList = _f.BaseFactory.Sequence();
		if (ignoreDefaultNs)
		{
			qilList.Add(_f.NamespaceDecl(_f.String(string.Empty), _f.String(string.Empty)));
		}
		CompilerScopeManager<QilIterator>.NamespaceEnumerator enumerator = _scope.GetEnumerator();
		while (enumerator.MoveNext())
		{
			CompilerScopeManager<QilIterator>.ScopeRecord current = enumerator.Current;
			string ncName = current.ncName;
			string nsUri = current.nsUri;
			if (!ignoreDefaultNs || ncName.Length != 0)
			{
				qilList.Add(_f.NamespaceDecl(_f.String(ncName), _f.String(nsUri)));
			}
		}
		return _f.StrParseQName(qilName, qilList);
	}

	private QilNode GenerateApply(StylesheetLevel sheet, XslNode node)
	{
		if (_compiler.Settings.CheckOnly)
		{
			return _f.Sequence();
		}
		return InvokeApplyFunction(sheet, node.Name, node.Content);
	}

	private void SetArg(IList<XslNode> args, int pos, QilName name, QilNode value)
	{
		VarPar varPar;
		if (args.Count <= pos || args[pos].Name != name)
		{
			varPar = AstFactory.WithParam(name);
			args.Insert(pos, varPar);
		}
		else
		{
			varPar = (VarPar)args[pos];
		}
		varPar.Value = value;
	}

	private IList<XslNode> AddRemoveImplicitArgs(IList<XslNode> args, XslFlags flags)
	{
		if (IsDebug)
		{
			flags = XslFlags.FocusFilter;
		}
		if ((flags & XslFlags.FocusFilter) != 0)
		{
			if (args == null || args.IsReadOnly)
			{
				args = new List<XslNode>(3);
			}
			int num = 0;
			if ((flags & XslFlags.Current) != 0)
			{
				SetArg(args, num++, _nameCurrent, GetCurrentNode());
			}
			if ((flags & XslFlags.Position) != 0)
			{
				SetArg(args, num++, _namePosition, GetCurrentPosition());
			}
			if ((flags & XslFlags.Last) != 0)
			{
				SetArg(args, num++, _nameLast, GetLastPosition());
			}
		}
		return args;
	}

	private bool FillupInvokeArgs(IList<QilNode> formalArgs, IList<XslNode> actualArgs, QilList invokeArgs)
	{
		if (actualArgs.Count != formalArgs.Count)
		{
			return false;
		}
		invokeArgs.Clear();
		for (int i = 0; i < formalArgs.Count; i++)
		{
			QilName name = ((QilParameter)formalArgs[i]).Name;
			XmlQueryType xmlType = formalArgs[i].XmlType;
			QilNode qilNode = null;
			for (int j = 0; j < actualArgs.Count; j++)
			{
				VarPar varPar = (VarPar)actualArgs[j];
				if (name.Equals(varPar.Name))
				{
					QilNode value = varPar.Value;
					XmlQueryType xmlType2 = value.XmlType;
					if (xmlType2 != xmlType && (!xmlType2.IsNode || !xmlType.IsNode || !xmlType2.IsSubtypeOf(xmlType)))
					{
						return false;
					}
					qilNode = value;
					break;
				}
			}
			if (qilNode == null)
			{
				return false;
			}
			invokeArgs.Add(qilNode);
		}
		return true;
	}

	private QilNode InvokeApplyFunction(StylesheetLevel sheet, QilName mode, IList<XslNode> actualArgs)
	{
		if (!sheet.ModeFlags.TryGetValue(mode, out var value))
		{
			value = XslFlags.None;
		}
		value |= XslFlags.Current;
		actualArgs = AddRemoveImplicitArgs(actualArgs, value);
		QilList qilList = _f.ActualParameterList();
		QilFunction qilFunction = null;
		if (!sheet.ApplyFunctions.TryGetValue(mode, out var value2))
		{
			List<QilFunction> list2 = (sheet.ApplyFunctions[mode] = new List<QilFunction>());
			value2 = list2;
		}
		foreach (QilFunction item in value2)
		{
			if (FillupInvokeArgs(item.Arguments, actualArgs, qilList))
			{
				qilFunction = item;
				break;
			}
		}
		if (qilFunction == null)
		{
			qilList.Clear();
			QilList qilList2 = _f.FormalParameterList();
			for (int i = 0; i < actualArgs.Count; i++)
			{
				VarPar varPar = (VarPar)actualArgs[i];
				qilList.Add(varPar.Value);
				QilParameter qilParameter = _f.Parameter((i == 0) ? XmlQueryTypeFactory.NodeNotRtf : varPar.Value.XmlType);
				qilParameter.Name = CloneName(varPar.Name);
				qilList2.Add(qilParameter);
				varPar.Value = qilParameter;
			}
			qilFunction = _f.Function(qilList2, _f.Boolean((value & XslFlags.SideEffects) != 0), XmlQueryTypeFactory.NodeNotRtfS);
			string text = ((mode.LocalName.Length == 0) ? string.Empty : (" mode=\"" + mode.QualifiedName + "\""));
			qilFunction.DebugName = ((sheet is RootLevel) ? "<xsl:apply-templates" : "<xsl:apply-imports") + text + ">";
			value2.Add(qilFunction);
			_functions.Add(qilFunction);
			QilIterator qilIterator = (QilIterator)qilList2[0];
			QilIterator qilIterator2 = _f.For(_f.Content(qilIterator));
			QilNode qilNode = _f.Filter(qilIterator2, _f.IsType(qilIterator2, XmlQueryTypeFactory.Content));
			qilNode.XmlType = XmlQueryTypeFactory.ContentS;
			LoopFocus curLoop = _curLoop;
			_curLoop.SetFocus(_f.For(qilNode));
			QilNode qilNode2 = InvokeApplyFunction(_compiler.Root, mode, null);
			if (IsDebug)
			{
				qilNode2 = _f.Sequence(InvokeOnCurrentNodeChanged(), qilNode2);
			}
			QilLoop center = _curLoop.ConstructLoop(qilNode2);
			_curLoop = curLoop;
			QilTernary otherwise = _f.BaseFactory.Conditional(_f.IsType(qilIterator, _elementOrDocumentType), center, _f.Conditional(_f.IsType(qilIterator, _textOrAttributeType), _f.TextCtor(_f.XPathNodeValue(qilIterator)), _f.Sequence()));
			_matcherBuilder.CollectPatterns(sheet, mode);
			qilFunction.Definition = _matcherBuilder.BuildMatcher(qilIterator, actualArgs, otherwise);
		}
		return _f.Invoke(qilFunction, qilList);
	}

	public void ReportError(string res, params string[] args)
	{
		_compiler.ReportError(_lastScope.SourceLine, res, args);
	}

	public void ReportWarning(string res, params string[] args)
	{
		_compiler.ReportWarning(_lastScope.SourceLine, res, args);
	}

	private string ResolvePrefix(bool ignoreDefaultNs, string prefix)
	{
		if (ignoreDefaultNs && prefix.Length == 0)
		{
			return string.Empty;
		}
		string text = _scope.LookupNamespace(prefix);
		if (text == null)
		{
			if (prefix.Length == 0)
			{
				text = string.Empty;
			}
			else
			{
				ReportError(System.SR.Xslt_InvalidPrefix, prefix);
				text = _compiler.CreatePhantomNamespace();
			}
		}
		return text;
	}

	private void SetLineInfoCheck(QilNode n, ISourceLineInfo lineInfo)
	{
		if (n.SourceLine == null)
		{
			SetLineInfo(n, lineInfo);
		}
	}

	private static QilNode SetLineInfo(QilNode n, ISourceLineInfo lineInfo)
	{
		if (lineInfo != null && 0 < lineInfo.Start.Line && lineInfo.Start.LessOrEqual(lineInfo.End))
		{
			n.SourceLine = lineInfo;
		}
		return n;
	}

	private QilNode AddDebugVariable(QilName name, QilNode value, QilNode content)
	{
		QilIterator qilIterator = _f.Let(value);
		qilIterator.DebugName = name.ToString();
		return _f.Loop(qilIterator, content);
	}

	private QilNode SetDebugNs(QilNode n, QilList nsList)
	{
		if (n != null && nsList != null)
		{
			QilNode qilNode = GetNsVar(nsList);
			if (qilNode.XmlType.Cardinality == XmlQueryCardinality.One)
			{
				qilNode = _f.TypeAssert(qilNode, XmlQueryTypeFactory.NamespaceS);
			}
			n = AddDebugVariable(CloneName(_nameNamespaces), qilNode, n);
		}
		return n;
	}

	private QilNode AddCurrentPositionLast(QilNode content)
	{
		if (IsDebug)
		{
			content = AddDebugVariable(CloneName(_nameLast), GetLastPosition(), content);
			content = AddDebugVariable(CloneName(_namePosition), GetCurrentPosition(), content);
			content = AddDebugVariable(CloneName(_nameCurrent), GetCurrentNode(), content);
		}
		return content;
	}

	private QilName CloneName(QilName name)
	{
		return (QilName)name.ShallowClone(_f.BaseFactory);
	}

	private void SetEnvironmentFlags(bool allowVariables, bool allowCurrent, bool allowKey)
	{
		_allowVariables = allowVariables;
		_allowCurrent = allowCurrent;
		_allowKey = allowKey;
	}

	QilNode IFocus.GetCurrent()
	{
		return GetCurrentNode();
	}

	QilNode IFocus.GetPosition()
	{
		return GetCurrentPosition();
	}

	QilNode IFocus.GetLast()
	{
		return GetLastPosition();
	}

	string IXPathEnvironment.ResolvePrefix(string prefix)
	{
		return ResolvePrefixThrow(ignoreDefaultNs: true, prefix);
	}

	QilNode IXPathEnvironment.ResolveVariable(string prefix, string name)
	{
		if (!_allowVariables)
		{
			throw new XslLoadException(System.SR.Xslt_VariablesNotAllowed);
		}
		string uri = ResolvePrefixThrow(ignoreDefaultNs: true, prefix);
		QilNode qilNode = _scope.LookupVariable(name, uri);
		if (qilNode == null)
		{
			throw new XslLoadException(System.SR.Xslt_InvalidVariable, Compiler.ConstructQName(prefix, name));
		}
		XmlQueryType xmlType = qilNode.XmlType;
		if (qilNode.NodeType == QilNodeType.Parameter && xmlType.IsNode && xmlType.IsNotRtf && xmlType.MaybeMany && !xmlType.IsDod)
		{
			qilNode = _f.TypeAssert(qilNode, XmlQueryTypeFactory.NodeSDod);
		}
		return qilNode;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Suppressing the warning for the ResolveFunction call on the Scripts since Scripts functionality is not supported by .NET Core")]
	QilNode IXPathEnvironment.ResolveFunction(string prefix, string name, IList<QilNode> args, IFocus env)
	{
		if (prefix.Length == 0)
		{
			if (FunctionTable.TryGetValue(name, out var value))
			{
				value.CastArguments(args, name, _f);
				switch (value.id)
				{
				case FuncId.Current:
					if (!_allowCurrent)
					{
						throw new XslLoadException(System.SR.Xslt_CurrentNotAllowed);
					}
					return ((IFocus)this).GetCurrent();
				case FuncId.Key:
					if (!_allowKey)
					{
						throw new XslLoadException(System.SR.Xslt_KeyNotAllowed);
					}
					return CompileFnKey(args[0], args[1], env);
				case FuncId.Document:
					return CompileFnDocument(args[0], (args.Count > 1) ? args[1] : null);
				case FuncId.FormatNumber:
					return CompileFormatNumber(args[0], args[1], (args.Count > 2) ? args[2] : null);
				case FuncId.UnparsedEntityUri:
					return CompileUnparsedEntityUri(args[0]);
				case FuncId.GenerateId:
					return CompileGenerateId((args.Count > 0) ? args[0] : env.GetCurrent());
				case FuncId.SystemProperty:
					return CompileSystemProperty(args[0]);
				case FuncId.ElementAvailable:
					return CompileElementAvailable(args[0]);
				case FuncId.FunctionAvailable:
					return CompileFunctionAvailable(args[0]);
				default:
					return null;
				}
			}
			throw new XslLoadException(System.SR.Xslt_UnknownXsltFunction, Compiler.ConstructQName(prefix, name));
		}
		string text = ResolvePrefixThrow(ignoreDefaultNs: true, prefix);
		if (text == "urn:schemas-microsoft-com:xslt")
		{
			switch (name)
			{
			case "node-set":
				XPathBuilder.FunctionInfo<FuncId>.CheckArity(1, 1, name, args.Count);
				return CompileMsNodeSet(args[0]);
			case "string-compare":
				XPathBuilder.FunctionInfo<FuncId>.CheckArity(2, 4, name, args.Count);
				return _f.InvokeMsStringCompare(_f.ConvertToString(args[0]), _f.ConvertToString(args[1]), (2 < args.Count) ? _f.ConvertToString(args[2]) : _f.String(string.Empty), (3 < args.Count) ? _f.ConvertToString(args[3]) : _f.String(string.Empty));
			case "utc":
				XPathBuilder.FunctionInfo<FuncId>.CheckArity(1, 1, name, args.Count);
				return _f.InvokeMsUtc(_f.ConvertToString(args[0]));
			case "format-date":
			case "format-time":
				XPathBuilder.FunctionInfo<FuncId>.CheckArity(1, 3, name, args.Count);
				return _f.InvokeMsFormatDateTime(_f.ConvertToString(args[0]), (1 < args.Count) ? _f.ConvertToString(args[1]) : _f.String(string.Empty), (2 < args.Count) ? _f.ConvertToString(args[2]) : _f.String(string.Empty), _f.Boolean(name == "format-date"));
			case "local-name":
				XPathBuilder.FunctionInfo<FuncId>.CheckArity(1, 1, name, args.Count);
				return _f.InvokeMsLocalName(_f.ConvertToString(args[0]));
			case "namespace-uri":
				XPathBuilder.FunctionInfo<FuncId>.CheckArity(1, 1, name, args.Count);
				return _f.InvokeMsNamespaceUri(_f.ConvertToString(args[0]), env.GetCurrent());
			case "number":
				XPathBuilder.FunctionInfo<FuncId>.CheckArity(1, 1, name, args.Count);
				return _f.InvokeMsNumber(args[0]);
			}
		}
		if (text == "http://exslt.org/common")
		{
			if (name == "node-set")
			{
				XPathBuilder.FunctionInfo<FuncId>.CheckArity(1, 1, name, args.Count);
				return CompileMsNodeSet(args[0]);
			}
			if (name == "object-type")
			{
				XPathBuilder.FunctionInfo<FuncId>.CheckArity(1, 1, name, args.Count);
				return EXslObjectType(args[0]);
			}
		}
		for (int i = 0; i < args.Count; i++)
		{
			args[i] = _f.SafeDocOrderDistinct(args[i]);
		}
		if (_compiler.Settings.EnableScript)
		{
			XmlExtensionFunction xmlExtensionFunction = _compiler.Scripts.ResolveFunction(name, text, args.Count, this);
			if (xmlExtensionFunction != null)
			{
				return GenerateScriptCall(_f.QName(name, text, prefix), xmlExtensionFunction, args);
			}
		}
		else if (_compiler.Scripts.ScriptClasses.ContainsKey(text))
		{
			ReportWarning(System.SR.Xslt_ScriptsProhibited);
			return _f.Error(_lastScope.SourceLine, System.SR.Xslt_ScriptsProhibited);
		}
		return _f.XsltInvokeLateBound(_f.QName(name, text, prefix), args);
	}

	private QilNode GenerateScriptCall(QilName name, XmlExtensionFunction scrFunc, IList<QilNode> args)
	{
		for (int i = 0; i < args.Count; i++)
		{
			XmlQueryType xmlArgumentType = scrFunc.GetXmlArgumentType(i);
			switch (xmlArgumentType.TypeCode)
			{
			case XmlTypeCode.Boolean:
				args[i] = _f.ConvertToBoolean(args[i]);
				break;
			case XmlTypeCode.Double:
				args[i] = _f.ConvertToNumber(args[i]);
				break;
			case XmlTypeCode.String:
				args[i] = _f.ConvertToString(args[i]);
				break;
			case XmlTypeCode.Node:
				args[i] = (xmlArgumentType.IsSingleton ? _f.ConvertToNode(args[i]) : _f.ConvertToNodeSet(args[i]));
				break;
			}
		}
		return _f.XsltInvokeEarlyBound(name, scrFunc.Method, scrFunc.XmlReturnType, args);
	}

	private string ResolvePrefixThrow(bool ignoreDefaultNs, string prefix)
	{
		if (ignoreDefaultNs && prefix.Length == 0)
		{
			return string.Empty;
		}
		string text = _scope.LookupNamespace(prefix);
		if (text == null)
		{
			if (prefix.Length != 0)
			{
				throw new XslLoadException(System.SR.Xslt_InvalidPrefix, prefix);
			}
			text = string.Empty;
		}
		return text;
	}

	private static Dictionary<string, XPathBuilder.FunctionInfo<FuncId>> CreateFunctionTable()
	{
		Dictionary<string, XPathBuilder.FunctionInfo<FuncId>> dictionary = new Dictionary<string, XPathBuilder.FunctionInfo<FuncId>>(16);
		dictionary.Add("current", new XPathBuilder.FunctionInfo<FuncId>(FuncId.Current, 0, 0, null));
		dictionary.Add("document", new XPathBuilder.FunctionInfo<FuncId>(FuncId.Document, 1, 2, s_argFnDocument));
		dictionary.Add("key", new XPathBuilder.FunctionInfo<FuncId>(FuncId.Key, 2, 2, s_argFnKey));
		dictionary.Add("format-number", new XPathBuilder.FunctionInfo<FuncId>(FuncId.FormatNumber, 2, 3, s_argFnFormatNumber));
		dictionary.Add("unparsed-entity-uri", new XPathBuilder.FunctionInfo<FuncId>(FuncId.UnparsedEntityUri, 1, 1, XPathBuilder.argString));
		dictionary.Add("generate-id", new XPathBuilder.FunctionInfo<FuncId>(FuncId.GenerateId, 0, 1, XPathBuilder.argNodeSet));
		dictionary.Add("system-property", new XPathBuilder.FunctionInfo<FuncId>(FuncId.SystemProperty, 1, 1, XPathBuilder.argString));
		dictionary.Add("element-available", new XPathBuilder.FunctionInfo<FuncId>(FuncId.ElementAvailable, 1, 1, XPathBuilder.argString));
		dictionary.Add("function-available", new XPathBuilder.FunctionInfo<FuncId>(FuncId.FunctionAvailable, 1, 1, XPathBuilder.argString));
		return dictionary;
	}

	public static bool IsFunctionAvailable(string localName, string nsUri)
	{
		if (XPathBuilder.IsFunctionAvailable(localName, nsUri))
		{
			return true;
		}
		if (nsUri.Length == 0)
		{
			if (FunctionTable.ContainsKey(localName))
			{
				return localName != "unparsed-entity-uri";
			}
			return false;
		}
		if (nsUri == "urn:schemas-microsoft-com:xslt")
		{
			switch (localName)
			{
			default:
				return localName == "utc";
			case "node-set":
			case "format-date":
			case "format-time":
			case "local-name":
			case "namespace-uri":
			case "number":
			case "string-compare":
				return true;
			}
		}
		if (nsUri == "http://exslt.org/common")
		{
			if (!(localName == "node-set"))
			{
				return localName == "object-type";
			}
			return true;
		}
		return false;
	}

	public static bool IsElementAvailable(XmlQualifiedName name)
	{
		if (name.Namespace == "http://www.w3.org/1999/XSL/Transform")
		{
			string name2 = name.Name;
			switch (name2)
			{
			default:
				return name2 == "variable";
			case "apply-imports":
			case "apply-templates":
			case "attribute":
			case "call-template":
			case "choose":
			case "comment":
			case "copy":
			case "copy-of":
			case "element":
			case "fallback":
			case "for-each":
			case "if":
			case "message":
			case "number":
			case "processing-instruction":
			case "text":
			case "value-of":
				return true;
			}
		}
		return false;
	}

	private QilNode CompileFnKey(QilNode name, QilNode keys, IFocus env)
	{
		QilIterator name2;
		QilIterator expr;
		QilIterator n;
		QilNode collection = (keys.XmlType.IsNode ? ((!keys.XmlType.IsSingleton) ? _f.Loop(n = _f.For(keys), CompileSingleKey(name, _f.ConvertToString(n), env)) : CompileSingleKey(name, _f.ConvertToString(keys), env)) : ((!keys.XmlType.IsAtomicValue) ? _f.Loop(name2 = _f.Let(name), _f.Loop(expr = _f.Let(keys), _f.Conditional(_f.Not(_f.IsType(expr, XmlQueryTypeFactory.AnyAtomicType)), _f.Loop(n = _f.For(_f.TypeAssert(expr, XmlQueryTypeFactory.NodeS)), CompileSingleKey(name2, _f.ConvertToString(n), env)), CompileSingleKey(name2, _f.XsltConvert(expr, XmlQueryTypeFactory.StringX), env)))) : CompileSingleKey(name, _f.ConvertToString(keys), env)));
		return _f.DocOrderDistinct(collection);
	}

	private QilNode CompileSingleKey(QilNode name, QilNode key, IFocus env)
	{
		if (name.NodeType == QilNodeType.LiteralString)
		{
			string text = (QilLiteral)name;
			_compiler.ParseQName(text, out var prefix, out var localName, default(ThrowErrorHelper));
			string uri = ResolvePrefixThrow(ignoreDefaultNs: true, prefix);
			QilName key2 = _f.QName(localName, uri, prefix);
			if (!_compiler.Keys.Contains(key2))
			{
				throw new XslLoadException(System.SR.Xslt_UndefinedKey, text);
			}
			return CompileSingleKey(_compiler.Keys[key2], key, env);
		}
		if (_generalKey == null)
		{
			_generalKey = CreateGeneralKeyFunction();
		}
		QilIterator qilIterator = _f.Let(name);
		QilNode qilNode = ResolveQNameDynamic(ignoreDefaultNs: true, qilIterator);
		QilNode body = _f.Invoke(_generalKey, _f.ActualParameterList(qilIterator, qilNode, key, env.GetCurrent()));
		return _f.Loop(qilIterator, body);
	}

	private QilNode CompileSingleKey(List<Key> defList, QilNode key, IFocus env)
	{
		if (defList.Count == 1)
		{
			return _f.Invoke(defList[0].Function, _f.ActualParameterList(env.GetCurrent(), key));
		}
		QilIterator qilIterator = _f.Let(key);
		QilNode qilNode = _f.Sequence();
		foreach (Key def in defList)
		{
			qilNode.Add(_f.Invoke(def.Function, _f.ActualParameterList(env.GetCurrent(), qilIterator)));
		}
		return _f.Loop(qilIterator, qilNode);
	}

	private QilNode CompileSingleKey(List<Key> defList, QilIterator key, QilIterator context)
	{
		QilList qilList = _f.BaseFactory.Sequence();
		QilNode qilNode = null;
		foreach (Key def in defList)
		{
			qilNode = _f.Invoke(def.Function, _f.ActualParameterList(context, key));
			qilList.Add(qilNode);
		}
		if (defList.Count != 1)
		{
			return qilList;
		}
		return qilNode;
	}

	private QilFunction CreateGeneralKeyFunction()
	{
		QilIterator qilIterator = _f.Parameter(XmlQueryTypeFactory.StringX);
		QilIterator qilIterator2 = _f.Parameter(XmlQueryTypeFactory.QNameX);
		QilIterator qilIterator3 = _f.Parameter(XmlQueryTypeFactory.StringX);
		QilIterator qilIterator4 = _f.Parameter(XmlQueryTypeFactory.NodeNotRtf);
		QilNode qilNode = _f.Error(System.SR.Xslt_UndefinedKey, qilIterator);
		for (int i = 0; i < _compiler.Keys.Count; i++)
		{
			qilNode = _f.Conditional(_f.Eq(qilIterator2, _compiler.Keys[i][0].Name.DeepClone(_f.BaseFactory)), CompileSingleKey(_compiler.Keys[i], qilIterator3, qilIterator4), qilNode);
		}
		QilFunction qilFunction = _f.Function(_f.FormalParameterList(qilIterator, qilIterator2, qilIterator3, qilIterator4), qilNode, _f.False());
		qilFunction.DebugName = "key";
		_functions.Add(qilFunction);
		return qilFunction;
	}

	private QilNode CompileFnDocument(QilNode uris, QilNode baseNode)
	{
		if (!_compiler.Settings.EnableDocumentFunction)
		{
			ReportWarning(System.SR.Xslt_DocumentFuncProhibited);
			return _f.Error(_lastScope.SourceLine, System.SR.Xslt_DocumentFuncProhibited);
		}
		QilIterator qilIterator;
		if (uris.XmlType.IsNode)
		{
			return _f.DocOrderDistinct(_f.Loop(qilIterator = _f.For(uris), CompileSingleDocument(_f.ConvertToString(qilIterator), baseNode ?? qilIterator)));
		}
		if (uris.XmlType.IsAtomicValue)
		{
			return CompileSingleDocument(_f.ConvertToString(uris), baseNode);
		}
		QilIterator qilIterator2 = _f.Let(uris);
		QilIterator qilIterator3 = ((baseNode != null) ? _f.Let(baseNode) : null);
		QilNode qilNode = _f.Conditional(_f.Not(_f.IsType(qilIterator2, XmlQueryTypeFactory.AnyAtomicType)), _f.DocOrderDistinct(_f.Loop(qilIterator = _f.For(_f.TypeAssert(qilIterator2, XmlQueryTypeFactory.NodeS)), CompileSingleDocument(_f.ConvertToString(qilIterator), qilIterator3 ?? qilIterator))), CompileSingleDocument(_f.XsltConvert(qilIterator2, XmlQueryTypeFactory.StringX), qilIterator3));
		qilNode = ((baseNode != null) ? _f.Loop(qilIterator3, qilNode) : qilNode);
		return _f.Loop(qilIterator2, qilNode);
	}

	private QilNode CompileSingleDocument(QilNode uri, QilNode baseNode)
	{
		QilIterator n;
		QilNode baseUri = ((baseNode == null) ? _f.String(_lastScope.SourceLine.Uri) : ((!baseNode.XmlType.IsSingleton) ? _f.StrConcat(_f.Loop(n = _f.FirstNode(baseNode), _f.InvokeBaseUri(n))) : _f.InvokeBaseUri(baseNode)));
		return _f.DataSource(uri, baseUri);
	}

	private QilNode CompileFormatNumber(QilNode value, QilNode formatPicture, QilNode formatName)
	{
		XmlQualifiedName xmlQualifiedName;
		if (formatName != null)
		{
			xmlQualifiedName = ((formatName.NodeType != QilNodeType.LiteralString) ? null : ResolveQNameThrow(ignoreDefaultNs: true, formatName));
		}
		else
		{
			xmlQualifiedName = new XmlQualifiedName();
			formatName = _f.String(string.Empty);
		}
		if (xmlQualifiedName != null)
		{
			DecimalFormatDecl format;
			if (_compiler.DecimalFormats.Contains(xmlQualifiedName))
			{
				format = _compiler.DecimalFormats[xmlQualifiedName];
			}
			else
			{
				if (xmlQualifiedName != DecimalFormatDecl.Default.Name)
				{
					throw new XslLoadException(System.SR.Xslt_NoDecimalFormat, (QilLiteral)formatName);
				}
				format = DecimalFormatDecl.Default;
			}
			if (formatPicture.NodeType == QilNodeType.LiteralString)
			{
				QilIterator qilIterator = _f.Let(_f.InvokeRegisterDecimalFormatter(formatPicture, format));
				qilIterator.DebugName = _f.QName("formatter" + _formatterCnt++, "urn:schemas-microsoft-com:xslt-debug").ToString();
				_gloVars.Add(qilIterator);
				return _f.InvokeFormatNumberStatic(value, qilIterator);
			}
			_formatNumberDynamicUsed = true;
			QilNode decimalFormatName = _f.QName(xmlQualifiedName.Name, xmlQualifiedName.Namespace);
			return _f.InvokeFormatNumberDynamic(value, formatPicture, decimalFormatName, formatName);
		}
		_formatNumberDynamicUsed = true;
		QilIterator qilIterator2 = _f.Let(formatName);
		QilNode decimalFormatName2 = ResolveQNameDynamic(ignoreDefaultNs: true, qilIterator2);
		return _f.Loop(qilIterator2, _f.InvokeFormatNumberDynamic(value, formatPicture, decimalFormatName2, qilIterator2));
	}

	private QilNode CompileUnparsedEntityUri(QilNode n)
	{
		return _f.Error(_lastScope.SourceLine, System.SR.Xslt_UnsupportedXsltFunction, "unparsed-entity-uri");
	}

	private QilNode CompileGenerateId(QilNode n)
	{
		if (n.XmlType.IsSingleton)
		{
			return _f.XsltGenerateId(n);
		}
		QilIterator expr;
		return _f.StrConcat(_f.Loop(expr = _f.FirstNode(n), _f.XsltGenerateId(expr)));
	}

	private XmlQualifiedName ResolveQNameThrow(bool ignoreDefaultNs, QilNode qilName)
	{
		string qname = (QilLiteral)qilName;
		_compiler.ParseQName(qname, out var prefix, out var localName, default(ThrowErrorHelper));
		string ns = ResolvePrefixThrow(ignoreDefaultNs, prefix);
		return new XmlQualifiedName(localName, ns);
	}

	private QilNode CompileSystemProperty(QilNode name)
	{
		if (name.NodeType == QilNodeType.LiteralString)
		{
			XmlQualifiedName xmlQualifiedName = ResolveQNameThrow(ignoreDefaultNs: true, name);
			if (EvaluateFuncCalls)
			{
				XPathItem xPathItem = XsltFunctions.SystemProperty(xmlQualifiedName);
				if (xPathItem.ValueType == XsltConvert.StringType)
				{
					return _f.String(xPathItem.Value);
				}
				return _f.Double(xPathItem.ValueAsDouble);
			}
			name = _f.QName(xmlQualifiedName.Name, xmlQualifiedName.Namespace);
		}
		else
		{
			name = ResolveQNameDynamic(ignoreDefaultNs: true, name);
		}
		return _f.InvokeSystemProperty(name);
	}

	private QilNode CompileElementAvailable(QilNode name)
	{
		if (name.NodeType == QilNodeType.LiteralString)
		{
			XmlQualifiedName xmlQualifiedName = ResolveQNameThrow(ignoreDefaultNs: false, name);
			if (EvaluateFuncCalls)
			{
				return _f.Boolean(IsElementAvailable(xmlQualifiedName));
			}
			name = _f.QName(xmlQualifiedName.Name, xmlQualifiedName.Namespace);
		}
		else
		{
			name = ResolveQNameDynamic(ignoreDefaultNs: false, name);
		}
		return _f.InvokeElementAvailable(name);
	}

	private QilNode CompileFunctionAvailable(QilNode name)
	{
		if (name.NodeType == QilNodeType.LiteralString)
		{
			XmlQualifiedName xmlQualifiedName = ResolveQNameThrow(ignoreDefaultNs: true, name);
			if (EvaluateFuncCalls && (xmlQualifiedName.Namespace.Length == 0 || xmlQualifiedName.Namespace == "http://www.w3.org/1999/XSL/Transform"))
			{
				return _f.Boolean(IsFunctionAvailable(xmlQualifiedName.Name, xmlQualifiedName.Namespace));
			}
			name = _f.QName(xmlQualifiedName.Name, xmlQualifiedName.Namespace);
		}
		else
		{
			name = ResolveQNameDynamic(ignoreDefaultNs: true, name);
		}
		return _f.InvokeFunctionAvailable(name);
	}

	private QilNode CompileMsNodeSet(QilNode n)
	{
		if (n.XmlType.IsNode && n.XmlType.IsNotRtf)
		{
			return n;
		}
		return _f.XsltConvert(n, XmlQueryTypeFactory.NodeSDod);
	}

	private QilNode EXslObjectType(QilNode n)
	{
		if (EvaluateFuncCalls)
		{
			switch (n.XmlType.TypeCode)
			{
			case XmlTypeCode.Boolean:
				return _f.String("boolean");
			case XmlTypeCode.Double:
				return _f.String("number");
			case XmlTypeCode.String:
				return _f.String("string");
			}
			if (n.XmlType.IsNode && n.XmlType.IsNotRtf)
			{
				return _f.String("node-set");
			}
		}
		return _f.InvokeEXslObjectType(n);
	}
}

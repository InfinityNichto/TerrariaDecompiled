using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Xml.XPath;
using System.Xml.Xsl.Qil;

namespace System.Xml.Xsl.Xslt;

internal sealed class XsltLoader : IErrorHelper
{
	private enum InstructionFlags
	{
		None = 0,
		AllowParam = 1,
		AllowSort = 2,
		AllowFallback = 4
	}

	private Compiler _compiler;

	private XmlResolver _xmlResolver;

	private QueryReaderSettings _readerSettings;

	private KeywordsTable _atoms;

	private XsltInput _input;

	private Stylesheet _curStylesheet;

	private Template _curTemplate;

	internal static QilName nullMode = AstFactory.QName(string.Empty);

	public static int V1Opt = 1;

	public static int V1Req = 2;

	public static int V2Opt = 4;

	public static int V2Req = 8;

	private readonly HybridDictionary _documentUriInUse = new HybridDictionary();

	private readonly XsltInput.XsltAttribute[] _stylesheetAttributes = new XsltInput.XsltAttribute[4]
	{
		new XsltInput.XsltAttribute("version", V1Req | V2Req),
		new XsltInput.XsltAttribute("id", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("default-validation", V2Opt),
		new XsltInput.XsltAttribute("input-type-annotations", V2Opt)
	};

	private readonly XsltInput.XsltAttribute[] _importIncludeAttributes = new XsltInput.XsltAttribute[1]
	{
		new XsltInput.XsltAttribute("href", V1Req | V2Req)
	};

	private readonly XsltInput.XsltAttribute[] _loadStripSpaceAttributes = new XsltInput.XsltAttribute[1]
	{
		new XsltInput.XsltAttribute("elements", V1Req | V2Req)
	};

	private readonly XsltInput.XsltAttribute[] _outputAttributes = new XsltInput.XsltAttribute[17]
	{
		new XsltInput.XsltAttribute("name", V2Opt),
		new XsltInput.XsltAttribute("method", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("byte-order-mark", V2Opt),
		new XsltInput.XsltAttribute("cdata-section-elements", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("doctype-public", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("doctype-system", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("encoding", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("escape-uri-attributes", V2Opt),
		new XsltInput.XsltAttribute("include-content-type", V2Opt),
		new XsltInput.XsltAttribute("indent", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("media-type", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("normalization-form", V2Opt),
		new XsltInput.XsltAttribute("omit-xml-declaration", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("standalone", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("undeclare-prefixes", V2Opt),
		new XsltInput.XsltAttribute("use-character-maps", V2Opt),
		new XsltInput.XsltAttribute("version", V1Opt | V2Opt)
	};

	private readonly XsltInput.XsltAttribute[] _keyAttributes = new XsltInput.XsltAttribute[4]
	{
		new XsltInput.XsltAttribute("name", V1Req | V2Req),
		new XsltInput.XsltAttribute("match", V1Req | V2Req),
		new XsltInput.XsltAttribute("use", V1Req | V2Opt),
		new XsltInput.XsltAttribute("collation", V2Opt)
	};

	private readonly XsltInput.XsltAttribute[] _decimalFormatAttributes = new XsltInput.XsltAttribute[11]
	{
		new XsltInput.XsltAttribute("name", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("infinity", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("NaN", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("decimal-separator", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("grouping-separator", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("percent", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("per-mille", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("zero-digit", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("digit", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("pattern-separator", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("minus-sign", V1Opt | V2Opt)
	};

	private readonly XsltInput.XsltAttribute[] _namespaceAliasAttributes = new XsltInput.XsltAttribute[2]
	{
		new XsltInput.XsltAttribute("stylesheet-prefix", V1Req | V2Req),
		new XsltInput.XsltAttribute("result-prefix", V1Req | V2Req)
	};

	private readonly XsltInput.XsltAttribute[] _attributeSetAttributes = new XsltInput.XsltAttribute[2]
	{
		new XsltInput.XsltAttribute("name", V1Req | V2Req),
		new XsltInput.XsltAttribute("use-attribute-sets", V1Opt | V2Opt)
	};

	private readonly XsltInput.XsltAttribute[] _templateAttributes = new XsltInput.XsltAttribute[5]
	{
		new XsltInput.XsltAttribute("match", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("name", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("priority", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("mode", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("as", V2Opt)
	};

	private readonly XsltInput.XsltAttribute[] _scriptAttributes = new XsltInput.XsltAttribute[2]
	{
		new XsltInput.XsltAttribute("implements-prefix", V1Req | V2Req),
		new XsltInput.XsltAttribute("language", V1Opt | V2Opt)
	};

	private readonly XsltInput.XsltAttribute[] _assemblyAttributes = new XsltInput.XsltAttribute[2]
	{
		new XsltInput.XsltAttribute("name", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("href", V1Opt | V2Opt)
	};

	private readonly XsltInput.XsltAttribute[] _usingAttributes = new XsltInput.XsltAttribute[1]
	{
		new XsltInput.XsltAttribute("namespace", V1Req | V2Req)
	};

	private int _loadInstructionsDepth;

	private readonly XsltInput.XsltAttribute[] _applyTemplatesAttributes = new XsltInput.XsltAttribute[2]
	{
		new XsltInput.XsltAttribute("select", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("mode", V1Opt | V2Opt)
	};

	private readonly XsltInput.XsltAttribute[] _callTemplateAttributes = new XsltInput.XsltAttribute[1]
	{
		new XsltInput.XsltAttribute("name", V1Req | V2Req)
	};

	private readonly XsltInput.XsltAttribute[] _copyAttributes = new XsltInput.XsltAttribute[5]
	{
		new XsltInput.XsltAttribute("copy-namespaces", V2Opt),
		new XsltInput.XsltAttribute("inherit-namespaces", V2Opt),
		new XsltInput.XsltAttribute("use-attribute-sets", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("type", V2Opt),
		new XsltInput.XsltAttribute("validation", V2Opt)
	};

	private readonly XsltInput.XsltAttribute[] _copyOfAttributes = new XsltInput.XsltAttribute[4]
	{
		new XsltInput.XsltAttribute("select", V1Req | V2Req),
		new XsltInput.XsltAttribute("copy-namespaces", V2Opt),
		new XsltInput.XsltAttribute("type", V2Opt),
		new XsltInput.XsltAttribute("validation", V2Opt)
	};

	private readonly XsltInput.XsltAttribute[] _ifAttributes = new XsltInput.XsltAttribute[1]
	{
		new XsltInput.XsltAttribute("test", V1Req | V2Req)
	};

	private readonly XsltInput.XsltAttribute[] _forEachAttributes = new XsltInput.XsltAttribute[1]
	{
		new XsltInput.XsltAttribute("select", V1Req | V2Req)
	};

	private readonly XsltInput.XsltAttribute[] _messageAttributes = new XsltInput.XsltAttribute[2]
	{
		new XsltInput.XsltAttribute("select", V2Opt),
		new XsltInput.XsltAttribute("terminate", V1Opt | V2Opt)
	};

	private readonly XsltInput.XsltAttribute[] _numberAttributes = new XsltInput.XsltAttribute[11]
	{
		new XsltInput.XsltAttribute("value", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("select", V2Opt),
		new XsltInput.XsltAttribute("level", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("count", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("from", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("format", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("lang", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("letter-value", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("ordinal", V2Opt),
		new XsltInput.XsltAttribute("grouping-separator", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("grouping-size", V1Opt | V2Opt)
	};

	private readonly XsltInput.XsltAttribute[] _valueOfAttributes = new XsltInput.XsltAttribute[3]
	{
		new XsltInput.XsltAttribute("select", V1Req | V2Opt),
		new XsltInput.XsltAttribute("separator", V2Opt),
		new XsltInput.XsltAttribute("disable-output-escaping", V1Opt | V2Opt)
	};

	private readonly XsltInput.XsltAttribute[] _variableAttributes = new XsltInput.XsltAttribute[5]
	{
		new XsltInput.XsltAttribute("name", V1Req | V2Req),
		new XsltInput.XsltAttribute("select", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("as", V2Opt),
		new XsltInput.XsltAttribute("required", 0),
		new XsltInput.XsltAttribute("tunnel", 0)
	};

	private readonly XsltInput.XsltAttribute[] _paramAttributes = new XsltInput.XsltAttribute[5]
	{
		new XsltInput.XsltAttribute("name", V1Req | V2Req),
		new XsltInput.XsltAttribute("select", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("as", V2Opt),
		new XsltInput.XsltAttribute("required", V2Opt),
		new XsltInput.XsltAttribute("tunnel", V2Opt)
	};

	private readonly XsltInput.XsltAttribute[] _withParamAttributes = new XsltInput.XsltAttribute[5]
	{
		new XsltInput.XsltAttribute("name", V1Req | V2Req),
		new XsltInput.XsltAttribute("select", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("as", V2Opt),
		new XsltInput.XsltAttribute("required", 0),
		new XsltInput.XsltAttribute("tunnel", V2Opt)
	};

	private readonly XsltInput.XsltAttribute[] _commentAttributes = new XsltInput.XsltAttribute[1]
	{
		new XsltInput.XsltAttribute("select", V2Opt)
	};

	private readonly XsltInput.XsltAttribute[] _processingInstructionAttributes = new XsltInput.XsltAttribute[2]
	{
		new XsltInput.XsltAttribute("name", V1Req | V2Req),
		new XsltInput.XsltAttribute("select", V2Opt)
	};

	private readonly XsltInput.XsltAttribute[] _textAttributes = new XsltInput.XsltAttribute[1]
	{
		new XsltInput.XsltAttribute("disable-output-escaping", V1Opt | V2Opt)
	};

	private readonly XsltInput.XsltAttribute[] _elementAttributes = new XsltInput.XsltAttribute[6]
	{
		new XsltInput.XsltAttribute("name", V1Req | V2Req),
		new XsltInput.XsltAttribute("namespace", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("inherit-namespaces", V2Opt),
		new XsltInput.XsltAttribute("use-attribute-sets", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("type", V2Opt),
		new XsltInput.XsltAttribute("validation", V2Opt)
	};

	private readonly XsltInput.XsltAttribute[] _attributeAttributes = new XsltInput.XsltAttribute[6]
	{
		new XsltInput.XsltAttribute("name", V1Req | V2Req),
		new XsltInput.XsltAttribute("namespace", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("select", V2Opt),
		new XsltInput.XsltAttribute("separator", V2Opt),
		new XsltInput.XsltAttribute("type", V2Opt),
		new XsltInput.XsltAttribute("validation", V2Opt)
	};

	private readonly XsltInput.XsltAttribute[] _sortAttributes = new XsltInput.XsltAttribute[7]
	{
		new XsltInput.XsltAttribute("select", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("lang", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("order", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("collation", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("stable", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("case-order", V1Opt | V2Opt),
		new XsltInput.XsltAttribute("data-type", V1Opt | V2Opt)
	};

	private bool V1 => _compiler.Version == 1;

	public void Load(Compiler compiler, object stylesheet, XmlResolver xmlResolver)
	{
		_compiler = compiler;
		_xmlResolver = xmlResolver ?? XmlNullResolver.Singleton;
		if (stylesheet is XmlReader reader)
		{
			_readerSettings = new QueryReaderSettings(reader);
			Load(reader);
		}
		else if (stylesheet is string text)
		{
			XmlResolver xmlResolver2 = xmlResolver;
			if (xmlResolver == null || xmlResolver == XmlNullResolver.Singleton)
			{
				xmlResolver2 = new XmlUrlResolver();
			}
			Uri uri = xmlResolver2.ResolveUri(null, text);
			if (uri == null)
			{
				throw new XslLoadException(System.SR.Xslt_CantResolve, text);
			}
			_readerSettings = new QueryReaderSettings(new NameTable());
			XmlReader reader2;
			using (reader2 = CreateReader(uri, xmlResolver2))
			{
				Load(reader2);
			}
		}
		else if (stylesheet is IXPathNavigable iXPathNavigable)
		{
			XmlReader reader2 = XPathNavigatorReader.Create(iXPathNavigable.CreateNavigator());
			_readerSettings = new QueryReaderSettings(reader2.NameTable);
			Load(reader2);
		}
		compiler.StartApplyTemplates = AstFactory.ApplyTemplates(nullMode);
		ProcessOutputSettings();
		foreach (AttributeSet value in compiler.AttributeSets.Values)
		{
			CheckAttributeSetsDfs(value);
		}
	}

	private void Load(XmlReader reader)
	{
		_atoms = new KeywordsTable(reader.NameTable);
		AtomizeAttributes();
		LoadStylesheet(reader, include: false);
	}

	private void AtomizeAttributes(XsltInput.XsltAttribute[] attributes)
	{
		for (int i = 0; i < attributes.Length; i++)
		{
			attributes[i].name = _atoms.NameTable.Add(attributes[i].name);
		}
	}

	private void AtomizeAttributes()
	{
		AtomizeAttributes(_stylesheetAttributes);
		AtomizeAttributes(_importIncludeAttributes);
		AtomizeAttributes(_loadStripSpaceAttributes);
		AtomizeAttributes(_outputAttributes);
		AtomizeAttributes(_keyAttributes);
		AtomizeAttributes(_decimalFormatAttributes);
		AtomizeAttributes(_namespaceAliasAttributes);
		AtomizeAttributes(_attributeSetAttributes);
		AtomizeAttributes(_templateAttributes);
		AtomizeAttributes(_scriptAttributes);
		AtomizeAttributes(_assemblyAttributes);
		AtomizeAttributes(_usingAttributes);
		AtomizeAttributes(_applyTemplatesAttributes);
		AtomizeAttributes(_callTemplateAttributes);
		AtomizeAttributes(_copyAttributes);
		AtomizeAttributes(_copyOfAttributes);
		AtomizeAttributes(_ifAttributes);
		AtomizeAttributes(_forEachAttributes);
		AtomizeAttributes(_messageAttributes);
		AtomizeAttributes(_numberAttributes);
		AtomizeAttributes(_valueOfAttributes);
		AtomizeAttributes(_variableAttributes);
		AtomizeAttributes(_paramAttributes);
		AtomizeAttributes(_withParamAttributes);
		AtomizeAttributes(_commentAttributes);
		AtomizeAttributes(_processingInstructionAttributes);
		AtomizeAttributes(_textAttributes);
		AtomizeAttributes(_elementAttributes);
		AtomizeAttributes(_attributeAttributes);
		AtomizeAttributes(_sortAttributes);
	}

	private Uri ResolveUri(string relativeUri, string baseUri)
	{
		Uri baseUri2 = ((baseUri.Length != 0) ? _xmlResolver.ResolveUri(null, baseUri) : null);
		Uri uri = _xmlResolver.ResolveUri(baseUri2, relativeUri);
		if (uri == null)
		{
			throw new XslLoadException(System.SR.Xslt_CantResolve, relativeUri);
		}
		return uri;
	}

	private XmlReader CreateReader(Uri uri, XmlResolver xmlResolver)
	{
		object entity = xmlResolver.GetEntity(uri, null, null);
		if (entity is Stream stream)
		{
			return _readerSettings.CreateReader(stream, uri.ToString());
		}
		if (entity is XmlReader result)
		{
			return result;
		}
		if (entity is IXPathNavigable iXPathNavigable)
		{
			return XPathNavigatorReader.Create(iXPathNavigable.CreateNavigator());
		}
		throw new XslLoadException(System.SR.Xslt_CannotLoadStylesheet, uri.ToString(), (entity == null) ? "null" : entity.GetType().ToString());
	}

	private Stylesheet LoadStylesheet(Uri uri, bool include)
	{
		using XmlReader reader = CreateReader(uri, _xmlResolver);
		return LoadStylesheet(reader, include);
	}

	private Stylesheet LoadStylesheet(XmlReader reader, bool include)
	{
		string baseURI = reader.BaseURI;
		_documentUriInUse.Add(baseURI, null);
		_compiler.AddModule(baseURI);
		Stylesheet curStylesheet = _curStylesheet;
		XsltInput input = _input;
		Stylesheet stylesheet = (include ? _curStylesheet : _compiler.CreateStylesheet());
		_input = new XsltInput(reader, _compiler, _atoms);
		_curStylesheet = stylesheet;
		try
		{
			LoadDocument();
			if (!include)
			{
				_compiler.MergeWithStylesheet(_curStylesheet);
				List<Uri> importHrefs = _curStylesheet.ImportHrefs;
				_curStylesheet.Imports = new Stylesheet[importHrefs.Count];
				int num = importHrefs.Count;
				while (0 <= --num)
				{
					_curStylesheet.Imports[num] = LoadStylesheet(importHrefs[num], include: false);
				}
			}
		}
		catch (XslLoadException)
		{
			throw;
		}
		catch (Exception ex2)
		{
			if (!XmlException.IsCatchableException(ex2))
			{
				throw;
			}
			ISourceLineInfo sourceLineInfo;
			if (!(ex2 is XmlException { SourceUri: not null } ex3))
			{
				sourceLineInfo = _input.BuildReaderLineInfo();
			}
			else
			{
				ISourceLineInfo sourceLineInfo2 = new SourceLineInfo(ex3.SourceUri, ex3.LineNumber, ex3.LinePosition, ex3.LineNumber, ex3.LinePosition);
				sourceLineInfo = sourceLineInfo2;
			}
			ISourceLineInfo lineInfo = sourceLineInfo;
			throw new XslLoadException(ex2, lineInfo);
		}
		finally
		{
			_documentUriInUse.Remove(baseURI);
			_input = input;
			_curStylesheet = curStylesheet;
		}
		return stylesheet;
	}

	private void LoadDocument()
	{
		if (!_input.FindStylesheetElement())
		{
			ReportError(System.SR.Xslt_WrongStylesheetElement);
			return;
		}
		if (_input.IsXsltNamespace())
		{
			if (_input.IsKeyword(_atoms.Stylesheet) || _input.IsKeyword(_atoms.Transform))
			{
				LoadRealStylesheet();
			}
			else
			{
				ReportError(System.SR.Xslt_WrongStylesheetElement);
				_input.SkipNode();
			}
		}
		else
		{
			LoadSimplifiedStylesheet();
		}
		_input.Finish();
	}

	private void LoadSimplifiedStylesheet()
	{
		_curTemplate = AstFactory.Template(null, "/", nullMode, double.NaN, _input.XslVersion);
		_input.CanHaveApplyImports = true;
		XslNode xslNode = LoadLiteralResultElement(asStylesheet: true);
		if (xslNode != null)
		{
			SetLineInfo(_curTemplate, xslNode.SourceLine);
			List<XslNode> list = new List<XslNode>();
			list.Add(xslNode);
			SetContent(_curTemplate, list);
			_curStylesheet.AddTemplate(_curTemplate);
		}
		_curTemplate = null;
	}

	private void LoadRealStylesheet()
	{
		XsltInput.ContextInfo attributes = _input.GetAttributes(_stylesheetAttributes);
		ParseValidationAttribute(2, defVal: true);
		ParseInputTypeAnnotationsAttribute(3);
		XsltInput.DelayedQName elementName = _input.ElementName;
		if (!_input.MoveToFirstChild())
		{
			return;
		}
		bool flag = true;
		do
		{
			bool flag2 = false;
			switch (_input.NodeType)
			{
			case XmlNodeType.Element:
				if (_input.IsXsltNamespace())
				{
					if (_input.IsKeyword(_atoms.Import))
					{
						if (!flag)
						{
							ReportError(System.SR.Xslt_NotAtTop, _input.QualifiedName, elementName);
							_input.SkipNode();
						}
						else
						{
							flag2 = true;
							LoadImport();
						}
					}
					else if (_input.IsKeyword(_atoms.Include))
					{
						LoadInclude();
					}
					else if (_input.IsKeyword(_atoms.StripSpace))
					{
						LoadStripSpace(attributes.nsList);
					}
					else if (_input.IsKeyword(_atoms.PreserveSpace))
					{
						LoadPreserveSpace(attributes.nsList);
					}
					else if (_input.IsKeyword(_atoms.Output))
					{
						LoadOutput();
					}
					else if (_input.IsKeyword(_atoms.Key))
					{
						LoadKey(attributes.nsList);
					}
					else if (_input.IsKeyword(_atoms.DecimalFormat))
					{
						LoadDecimalFormat(attributes.nsList);
					}
					else if (_input.IsKeyword(_atoms.NamespaceAlias))
					{
						LoadNamespaceAlias(attributes.nsList);
					}
					else if (_input.IsKeyword(_atoms.AttributeSet))
					{
						LoadAttributeSet(attributes.nsList);
					}
					else if (_input.IsKeyword(_atoms.Variable))
					{
						LoadGlobalVariableOrParameter(attributes.nsList, XslNodeType.Variable);
					}
					else if (_input.IsKeyword(_atoms.Param))
					{
						LoadGlobalVariableOrParameter(attributes.nsList, XslNodeType.Param);
					}
					else if (_input.IsKeyword(_atoms.Template))
					{
						LoadTemplate(attributes.nsList);
					}
					else
					{
						_input.GetVersionAttribute();
						if (!_input.ForwardCompatibility)
						{
							ReportError(System.SR.Xslt_UnexpectedElement, _input.QualifiedName, elementName);
						}
						_input.SkipNode();
					}
				}
				else if (_input.IsNs(_atoms.UrnMsxsl) && _input.IsKeyword(_atoms.Script))
				{
					LoadMsScript(attributes.nsList);
				}
				else
				{
					if (_input.IsNullNamespace())
					{
						ReportError(System.SR.Xslt_NullNsAtTopLevel, _input.LocalName);
					}
					_input.SkipNode();
				}
				flag = flag2;
				break;
			default:
				ReportError(System.SR.Xslt_TextNodesNotAllowed, elementName);
				break;
			case XmlNodeType.Whitespace:
			case XmlNodeType.SignificantWhitespace:
				break;
			}
		}
		while (_input.MoveToNextSibling());
	}

	private void LoadImport()
	{
		_input.GetAttributes(_importIncludeAttributes);
		if (_input.MoveToXsltAttribute(0, "href"))
		{
			Uri uri = ResolveUri(_input.Value, _input.BaseUri);
			if (_documentUriInUse.Contains(uri.ToString()))
			{
				ReportError(System.SR.Xslt_CircularInclude, _input.Value);
			}
			else
			{
				_curStylesheet.ImportHrefs.Add(uri);
			}
		}
		CheckNoContent();
	}

	private void LoadInclude()
	{
		_input.GetAttributes(_importIncludeAttributes);
		if (_input.MoveToXsltAttribute(0, "href"))
		{
			Uri uri = ResolveUri(_input.Value, _input.BaseUri);
			if (_documentUriInUse.Contains(uri.ToString()))
			{
				ReportError(System.SR.Xslt_CircularInclude, _input.Value);
			}
			else
			{
				LoadStylesheet(uri, include: true);
			}
		}
		CheckNoContent();
	}

	private void LoadStripSpace(NsDecl stylesheetNsList)
	{
		XsltInput.ContextInfo attributes = _input.GetAttributes(_loadStripSpaceAttributes);
		attributes.nsList = MergeNamespaces(attributes.nsList, stylesheetNsList);
		if (_input.MoveToXsltAttribute(0, _atoms.Elements))
		{
			ParseWhitespaceRules(_input.Value, preserveSpace: false);
		}
		CheckNoContent();
	}

	private void LoadPreserveSpace(NsDecl stylesheetNsList)
	{
		XsltInput.ContextInfo attributes = _input.GetAttributes(_loadStripSpaceAttributes);
		attributes.nsList = MergeNamespaces(attributes.nsList, stylesheetNsList);
		if (_input.MoveToXsltAttribute(0, _atoms.Elements))
		{
			ParseWhitespaceRules(_input.Value, preserveSpace: true);
		}
		CheckNoContent();
	}

	private void LoadOutput()
	{
		_input.GetAttributes(_outputAttributes);
		Output output = _compiler.Output;
		XmlWriterSettings settings = output.Settings;
		int currentPrecedence = _compiler.CurrentPrecedence;
		QilName qilName = ParseQNameAttribute(0);
		if (qilName != null)
		{
			ReportNYI("xsl:output/@name");
		}
		if (_input.MoveToXsltAttribute(1, "method") && output.MethodPrec <= currentPrecedence)
		{
			_compiler.EnterForwardsCompatible();
			XmlOutputMethod method;
			XmlQualifiedName xmlQualifiedName = ParseOutputMethod(_input.Value, out method);
			if (_compiler.ExitForwardsCompatible(_input.ForwardCompatibility) && xmlQualifiedName != null)
			{
				if (currentPrecedence == output.MethodPrec && !output.Method.Equals(xmlQualifiedName))
				{
					ReportWarning(System.SR.Xslt_AttributeRedefinition, "method");
				}
				settings.OutputMethod = method;
				output.Method = xmlQualifiedName;
				output.MethodPrec = currentPrecedence;
			}
		}
		TriState triState = ParseYesNoAttribute(2, "byte-order-mark");
		if (triState != TriState.Unknown)
		{
			ReportNYI("xsl:output/@byte-order-mark");
		}
		if (_input.MoveToXsltAttribute(3, "cdata-section-elements"))
		{
			_compiler.EnterForwardsCompatible();
			string[] array = XmlConvert.SplitString(_input.Value);
			List<XmlQualifiedName> list = new List<XmlQualifiedName>();
			for (int i = 0; i < array.Length; i++)
			{
				list.Add(ResolveQName(ignoreDefaultNs: false, array[i]));
			}
			if (_compiler.ExitForwardsCompatible(_input.ForwardCompatibility))
			{
				settings.CDataSectionElements.AddRange(list);
			}
		}
		if (_input.MoveToXsltAttribute(4, "doctype-public") && output.DocTypePublicPrec <= currentPrecedence)
		{
			if (currentPrecedence == output.DocTypePublicPrec && settings.DocTypePublic != _input.Value)
			{
				ReportWarning(System.SR.Xslt_AttributeRedefinition, "doctype-public");
			}
			settings.DocTypePublic = _input.Value;
			output.DocTypePublicPrec = currentPrecedence;
		}
		if (_input.MoveToXsltAttribute(5, "doctype-system") && output.DocTypeSystemPrec <= currentPrecedence)
		{
			if (currentPrecedence == output.DocTypeSystemPrec && settings.DocTypeSystem != _input.Value)
			{
				ReportWarning(System.SR.Xslt_AttributeRedefinition, "doctype-system");
			}
			settings.DocTypeSystem = _input.Value;
			output.DocTypeSystemPrec = currentPrecedence;
		}
		if (_input.MoveToXsltAttribute(6, "encoding") && output.EncodingPrec <= currentPrecedence)
		{
			try
			{
				Encoding encoding = Encoding.GetEncoding(_input.Value);
				if (currentPrecedence == output.EncodingPrec && output.Encoding != _input.Value)
				{
					ReportWarning(System.SR.Xslt_AttributeRedefinition, "encoding");
				}
				settings.Encoding = encoding;
				output.Encoding = _input.Value;
				output.EncodingPrec = currentPrecedence;
			}
			catch (ArgumentException)
			{
				if (!_input.ForwardCompatibility)
				{
					ReportWarning(System.SR.Xslt_InvalidEncoding, _input.Value);
				}
			}
		}
		if (ParseYesNoAttribute(7, "escape-uri-attributes") == TriState.False)
		{
			ReportNYI("xsl:output/@escape-uri-attributes == flase()");
		}
		if (ParseYesNoAttribute(8, "include-content-type") == TriState.False)
		{
			ReportNYI("xsl:output/@include-content-type == flase()");
		}
		TriState triState2 = ParseYesNoAttribute(9, "indent");
		if (triState2 != TriState.Unknown && output.IndentPrec <= currentPrecedence)
		{
			bool flag = triState2 == TriState.True;
			if (currentPrecedence == output.IndentPrec && settings.Indent != flag)
			{
				ReportWarning(System.SR.Xslt_AttributeRedefinition, "indent");
			}
			settings.Indent = flag;
			output.IndentPrec = currentPrecedence;
		}
		if (_input.MoveToXsltAttribute(10, "media-type") && output.MediaTypePrec <= currentPrecedence)
		{
			if (currentPrecedence == output.MediaTypePrec && settings.MediaType != _input.Value)
			{
				ReportWarning(System.SR.Xslt_AttributeRedefinition, "media-type");
			}
			settings.MediaType = _input.Value;
			output.MediaTypePrec = currentPrecedence;
		}
		if (_input.MoveToXsltAttribute(11, "normalization-form"))
		{
			ReportNYI("xsl:output/@normalization-form");
		}
		triState2 = ParseYesNoAttribute(12, "omit-xml-declaration");
		if (triState2 != TriState.Unknown && output.OmitXmlDeclarationPrec <= currentPrecedence)
		{
			bool flag2 = triState2 == TriState.True;
			if (currentPrecedence == output.OmitXmlDeclarationPrec && settings.OmitXmlDeclaration != flag2)
			{
				ReportWarning(System.SR.Xslt_AttributeRedefinition, "omit-xml-declaration");
			}
			settings.OmitXmlDeclaration = flag2;
			output.OmitXmlDeclarationPrec = currentPrecedence;
		}
		triState2 = ParseYesNoAttribute(13, "standalone");
		if (triState2 != TriState.Unknown && output.StandalonePrec <= currentPrecedence)
		{
			XmlStandalone xmlStandalone = ((triState2 == TriState.True) ? XmlStandalone.Yes : XmlStandalone.No);
			if (currentPrecedence == output.StandalonePrec && settings.Standalone != xmlStandalone)
			{
				ReportWarning(System.SR.Xslt_AttributeRedefinition, "standalone");
			}
			settings.Standalone = xmlStandalone;
			output.StandalonePrec = currentPrecedence;
		}
		if (ParseYesNoAttribute(14, "undeclare-prefixes") == TriState.True)
		{
			ReportNYI("xsl:output/@undeclare-prefixes == true()");
		}
		List<QilName> list2 = ParseUseCharacterMaps(15);
		if (list2.Count != 0)
		{
			ReportNYI("xsl:output/@use-character-maps");
		}
		if (_input.MoveToXsltAttribute(16, "version") && output.VersionPrec <= currentPrecedence)
		{
			if (currentPrecedence == output.VersionPrec && output.Version != _input.Value)
			{
				ReportWarning(System.SR.Xslt_AttributeRedefinition, "version");
			}
			output.Version = _input.Value;
			output.VersionPrec = currentPrecedence;
		}
		CheckNoContent();
	}

	private void ProcessOutputSettings()
	{
		Output output = _compiler.Output;
		XmlWriterSettings settings = output.Settings;
		if (settings.OutputMethod == XmlOutputMethod.Html && output.IndentPrec == int.MinValue)
		{
			settings.Indent = true;
		}
		if (output.MediaTypePrec == int.MinValue)
		{
			settings.MediaType = ((settings.OutputMethod == XmlOutputMethod.Xml) ? "text/xml" : ((settings.OutputMethod == XmlOutputMethod.Html) ? "text/html" : ((settings.OutputMethod == XmlOutputMethod.Text) ? "text/plain" : null)));
		}
	}

	private void CheckUseAttrubuteSetInList(IList<XslNode> list)
	{
		foreach (XslNode item in list)
		{
			switch (item.NodeType)
			{
			case XslNodeType.UseAttributeSet:
			{
				if (_compiler.AttributeSets.TryGetValue(item.Name, out var value))
				{
					CheckAttributeSetsDfs(value);
				}
				break;
			}
			case XslNodeType.List:
				CheckUseAttrubuteSetInList(item.Content);
				break;
			}
		}
	}

	private void CheckAttributeSetsDfs(AttributeSet attSet)
	{
		switch (attSet.CycleCheck)
		{
		case CycleCheck.NotStarted:
			attSet.CycleCheck = CycleCheck.Processing;
			CheckUseAttrubuteSetInList(attSet.Content);
			attSet.CycleCheck = CycleCheck.Completed;
			break;
		default:
			_compiler.ReportError(attSet.Content[0].SourceLine, System.SR.Xslt_CircularAttributeSet, attSet.Name.QualifiedName);
			break;
		case CycleCheck.Completed:
			break;
		}
	}

	private void LoadKey(NsDecl stylesheetNsList)
	{
		XsltInput.ContextInfo attributes = _input.GetAttributes(_keyAttributes);
		attributes.nsList = MergeNamespaces(attributes.nsList, stylesheetNsList);
		QilName qilName = ParseQNameAttribute(0);
		string match = ParseStringAttribute(1, "match");
		string text = ParseStringAttribute(2, "use");
		ParseCollationAttribute(3);
		_input.MoveToElement();
		List<XslNode> list = null;
		if (V1)
		{
			if (text == null)
			{
				_input.SkipNode();
			}
			else
			{
				CheckNoContent();
			}
		}
		else
		{
			list = LoadInstructions();
			if (list.Count != 0)
			{
				list = LoadEndTag(list);
			}
			if (text == null == (list.Count == 0))
			{
				ReportError(System.SR.Xslt_KeyCntUse);
			}
			else if (text == null)
			{
				ReportNYI("xsl:key[count(@use) = 0]");
			}
		}
		Key item = (Key)SetInfo(AstFactory.Key(qilName, match, text, _input.XslVersion), null, attributes);
		if (_compiler.Keys.Contains(qilName))
		{
			_compiler.Keys[qilName].Add(item);
			return;
		}
		List<Key> list2 = new List<Key>();
		list2.Add(item);
		_compiler.Keys.Add(list2);
	}

	private void LoadDecimalFormat(NsDecl stylesheetNsList)
	{
		XsltInput.ContextInfo attributes = _input.GetAttributes(_decimalFormatAttributes);
		attributes.nsList = MergeNamespaces(attributes.nsList, stylesheetNsList);
		XmlQualifiedName xmlQualifiedName;
		if (_input.MoveToXsltAttribute(0, "name"))
		{
			_compiler.EnterForwardsCompatible();
			xmlQualifiedName = ResolveQName(ignoreDefaultNs: true, _input.Value);
			if (!_compiler.ExitForwardsCompatible(_input.ForwardCompatibility))
			{
				xmlQualifiedName = new XmlQualifiedName();
			}
		}
		else
		{
			xmlQualifiedName = new XmlQualifiedName();
		}
		string text = DecimalFormatDecl.Default.InfinitySymbol;
		if (_input.MoveToXsltAttribute(1, "infinity"))
		{
			text = _input.Value;
		}
		string text2 = DecimalFormatDecl.Default.NanSymbol;
		if (_input.MoveToXsltAttribute(2, "NaN"))
		{
			text2 = _input.Value;
		}
		char[] characters = DecimalFormatDecl.Default.Characters;
		char[] array = new char[8];
		for (int i = 0; i < 8; i++)
		{
			array[i] = ParseCharAttribute(3 + i, _decimalFormatAttributes[3 + i].name, characters[i]);
		}
		for (int j = 0; j < 7; j++)
		{
			for (int k = j + 1; k < 7; k++)
			{
				if (array[j] == array[k])
				{
					bool flag = _input.MoveToXsltAttribute(3 + k, _decimalFormatAttributes[3 + k].name) || _input.MoveToXsltAttribute(3 + j, _decimalFormatAttributes[3 + j].name);
					ReportError(System.SR.Xslt_DecimalFormatSignsNotDistinct, _decimalFormatAttributes[3 + j].name, _decimalFormatAttributes[3 + k].name);
					break;
				}
			}
		}
		if (_compiler.DecimalFormats.Contains(xmlQualifiedName))
		{
			DecimalFormatDecl decimalFormatDecl = _compiler.DecimalFormats[xmlQualifiedName];
			_input.MoveToXsltAttribute(1, "infinity");
			CheckError(text != decimalFormatDecl.InfinitySymbol, System.SR.Xslt_DecimalFormatRedefined, "infinity", text);
			_input.MoveToXsltAttribute(2, "NaN");
			CheckError(text2 != decimalFormatDecl.NanSymbol, System.SR.Xslt_DecimalFormatRedefined, "NaN", text2);
			for (int l = 0; l < 8; l++)
			{
				_input.MoveToXsltAttribute(3 + l, _decimalFormatAttributes[3 + l].name);
				CheckError(array[l] != decimalFormatDecl.Characters[l], System.SR.Xslt_DecimalFormatRedefined, _decimalFormatAttributes[3 + l].name, char.ToString(array[l]));
			}
		}
		else
		{
			DecimalFormatDecl item = new DecimalFormatDecl(xmlQualifiedName, text, text2, new string(array));
			_compiler.DecimalFormats.Add(item);
		}
		CheckNoContent();
	}

	private void LoadNamespaceAlias(NsDecl stylesheetNsList)
	{
		XsltInput.ContextInfo attributes = _input.GetAttributes(_namespaceAliasAttributes);
		attributes.nsList = MergeNamespaces(attributes.nsList, stylesheetNsList);
		string text = null;
		string text2 = null;
		string text3 = null;
		if (_input.MoveToXsltAttribute(0, "stylesheet-prefix"))
		{
			if (_input.Value.Length == 0)
			{
				ReportError(System.SR.Xslt_EmptyNsAlias, "stylesheet-prefix");
			}
			else
			{
				text = _input.LookupXmlNamespace((_input.Value == "#default") ? string.Empty : _input.Value);
			}
		}
		if (_input.MoveToXsltAttribute(1, "result-prefix"))
		{
			if (_input.Value.Length == 0)
			{
				ReportError(System.SR.Xslt_EmptyNsAlias, "result-prefix");
			}
			else
			{
				text2 = ((_input.Value == "#default") ? string.Empty : _input.Value);
				text3 = _input.LookupXmlNamespace(text2);
			}
		}
		CheckNoContent();
		if (text != null && text3 != null && _compiler.SetNsAlias(text, text3, text2, _curStylesheet.ImportPrecedence))
		{
			_input.MoveToElement();
			ReportWarning(System.SR.Xslt_DupNsAlias, text);
		}
	}

	private void LoadAttributeSet(NsDecl stylesheetNsList)
	{
		XsltInput.ContextInfo attributes = _input.GetAttributes(_attributeSetAttributes);
		attributes.nsList = MergeNamespaces(attributes.nsList, stylesheetNsList);
		QilName qilName = ParseQNameAttribute(0);
		if (!_curStylesheet.AttributeSets.TryGetValue(qilName, out var value))
		{
			value = AstFactory.AttributeSet(qilName);
			_curStylesheet.AttributeSets[qilName] = value;
			if (!_compiler.AttributeSets.ContainsKey(qilName))
			{
				_compiler.AllTemplates.Add(value);
			}
		}
		List<XslNode> list = new List<XslNode>();
		if (_input.MoveToXsltAttribute(1, "use-attribute-sets"))
		{
			AddUseAttributeSets(list);
		}
		XsltInput.DelayedQName elementName = _input.ElementName;
		if (_input.MoveToFirstChild())
		{
			do
			{
				switch (_input.NodeType)
				{
				case XmlNodeType.Element:
					if (_input.IsXsltKeyword(_atoms.Attribute))
					{
						AddInstruction(list, XslAttribute());
						break;
					}
					ReportError(System.SR.Xslt_UnexpectedElement, _input.QualifiedName, elementName);
					_input.SkipNode();
					break;
				default:
					ReportError(System.SR.Xslt_TextNodesNotAllowed, elementName);
					break;
				case XmlNodeType.Whitespace:
				case XmlNodeType.SignificantWhitespace:
					break;
				}
			}
			while (_input.MoveToNextSibling());
		}
		value.AddContent(SetInfo(AstFactory.List(), LoadEndTag(list), attributes));
	}

	private void LoadGlobalVariableOrParameter(NsDecl stylesheetNsList, XslNodeType nodeType)
	{
		VarPar varPar = XslVarPar();
		varPar.Namespaces = MergeNamespaces(varPar.Namespaces, stylesheetNsList);
		CheckError(!_curStylesheet.AddVarPar(varPar), System.SR.Xslt_DupGlobalVariable, varPar.Name.QualifiedName);
	}

	private void LoadTemplate(NsDecl stylesheetNsList)
	{
		XsltInput.ContextInfo attributes = _input.GetAttributes(_templateAttributes);
		attributes.nsList = MergeNamespaces(attributes.nsList, stylesheetNsList);
		string text = ParseStringAttribute(0, "match");
		QilName name = ParseQNameAttribute(1);
		double num = double.NaN;
		if (_input.MoveToXsltAttribute(2, "priority"))
		{
			num = XPathConvert.StringToDouble(_input.Value);
			if (double.IsNaN(num) && !_input.ForwardCompatibility)
			{
				ReportError(System.SR.Xslt_InvalidAttrValue, "priority", _input.Value);
			}
		}
		QilName mode = (V1 ? ParseModeAttribute(3) : ParseModeListAttribute(3));
		if (text == null)
		{
			CheckError(!_input.AttributeExists(1, "name"), System.SR.Xslt_BothMatchNameAbsent);
			CheckError(_input.AttributeExists(3, "mode"), System.SR.Xslt_ModeWithoutMatch);
			mode = nullMode;
			if (_input.AttributeExists(2, "priority"))
			{
				if (V1)
				{
					ReportWarning(System.SR.Xslt_PriorityWithoutMatch);
				}
				else
				{
					ReportError(System.SR.Xslt_PriorityWithoutMatch);
				}
			}
		}
		if (_input.MoveToXsltAttribute(4, "as"))
		{
			ReportNYI("xsl:template/@as");
		}
		_curTemplate = AstFactory.Template(name, text, mode, num, _input.XslVersion);
		_input.CanHaveApplyImports = text != null;
		SetInfo(_curTemplate, LoadEndTag(LoadInstructions(InstructionFlags.AllowParam)), attributes);
		if (!_curStylesheet.AddTemplate(_curTemplate))
		{
			ReportError(System.SR.Xslt_DupTemplateName, _curTemplate.Name.QualifiedName);
		}
		_curTemplate = null;
	}

	private void LoadMsScript(NsDecl stylesheetNsList)
	{
		XsltInput.ContextInfo attributes = _input.GetAttributes(_scriptAttributes);
		attributes.nsList = MergeNamespaces(attributes.nsList, stylesheetNsList);
		string text = null;
		if (_input.MoveToXsltAttribute(0, "implements-prefix"))
		{
			if (_input.Value.Length == 0)
			{
				ReportError(System.SR.Xslt_EmptyAttrValue, "implements-prefix", _input.Value);
			}
			else
			{
				text = _input.LookupXmlNamespace(_input.Value);
				if (text == "http://www.w3.org/1999/XSL/Transform")
				{
					ReportError(System.SR.Xslt_ScriptXsltNamespace);
					text = null;
				}
			}
		}
		if (text == null)
		{
			text = _compiler.CreatePhantomNamespace();
		}
		string text2 = ParseStringAttribute(1, "language");
		if (text2 == null)
		{
			text2 = "jscript";
		}
		if (!_compiler.Settings.EnableScript)
		{
			_compiler.Scripts.ScriptClasses[text] = null;
			_input.SkipNode();
			return;
		}
		throw new PlatformNotSupportedException(System.SR.CompilingScriptsNotSupported);
	}

	private List<XslNode> LoadInstructions()
	{
		return LoadInstructions(new List<XslNode>(), InstructionFlags.None);
	}

	private List<XslNode> LoadInstructions(InstructionFlags flags)
	{
		return LoadInstructions(new List<XslNode>(), flags);
	}

	private List<XslNode> LoadInstructions(List<XslNode> content)
	{
		return LoadInstructions(content, InstructionFlags.None);
	}

	private List<XslNode> LoadInstructions(List<XslNode> content, InstructionFlags flags)
	{
		if (++_loadInstructionsDepth > 1024 && System.LocalAppContextSwitches.LimitXPathComplexity)
		{
			throw XsltException.Create(System.SR.Xslt_InputTooComplex);
		}
		XsltInput.DelayedQName elementName = _input.ElementName;
		if (_input.MoveToFirstChild())
		{
			bool flag = true;
			int num = 0;
			do
			{
				XmlNodeType nodeType = _input.NodeType;
				XslNode instruction;
				if (nodeType != XmlNodeType.Element)
				{
					if (nodeType == XmlNodeType.Whitespace)
					{
						continue;
					}
					if (nodeType != XmlNodeType.SignificantWhitespace)
					{
						flag = false;
					}
					instruction = SetLineInfo(AstFactory.Text(_input.Value), _input.BuildLineInfo());
				}
				else
				{
					string namespaceUri = _input.NamespaceUri;
					string localName = _input.LocalName;
					if (namespaceUri == _atoms.UriXsl)
					{
						InstructionFlags instructionFlags = (Ref.Equal(localName, _atoms.Param) ? InstructionFlags.AllowParam : (Ref.Equal(localName, _atoms.Sort) ? InstructionFlags.AllowSort : InstructionFlags.None));
						if (instructionFlags != 0)
						{
							string text = (((flags & instructionFlags) == 0) ? System.SR.Xslt_UnexpectedElement : ((!flag) ? System.SR.Xslt_NotAtTop : null));
							if (text != null)
							{
								ReportError(text, _input.QualifiedName, elementName);
								flag = false;
								_input.SkipNode();
								continue;
							}
						}
						else
						{
							flag = false;
						}
						instruction = (Ref.Equal(localName, _atoms.ApplyImports) ? XslApplyImports() : (Ref.Equal(localName, _atoms.ApplyTemplates) ? XslApplyTemplates() : (Ref.Equal(localName, _atoms.CallTemplate) ? XslCallTemplate() : (Ref.Equal(localName, _atoms.Copy) ? XslCopy() : (Ref.Equal(localName, _atoms.CopyOf) ? XslCopyOf() : (Ref.Equal(localName, _atoms.Fallback) ? XslFallback() : (Ref.Equal(localName, _atoms.If) ? XslIf() : (Ref.Equal(localName, _atoms.Choose) ? XslChoose() : (Ref.Equal(localName, _atoms.ForEach) ? XslForEach() : (Ref.Equal(localName, _atoms.Message) ? XslMessage() : (Ref.Equal(localName, _atoms.Number) ? XslNumber() : (Ref.Equal(localName, _atoms.ValueOf) ? XslValueOf() : (Ref.Equal(localName, _atoms.Comment) ? XslComment() : (Ref.Equal(localName, _atoms.ProcessingInstruction) ? XslProcessingInstruction() : (Ref.Equal(localName, _atoms.Text) ? XslText() : (Ref.Equal(localName, _atoms.Element) ? XslElement() : (Ref.Equal(localName, _atoms.Attribute) ? XslAttribute() : (Ref.Equal(localName, _atoms.Variable) ? XslVarPar() : (Ref.Equal(localName, _atoms.Param) ? XslVarPar() : (Ref.Equal(localName, _atoms.Sort) ? XslSort(num++) : LoadUnknownXsltInstruction(elementName)))))))))))))))))))));
					}
					else
					{
						flag = false;
						instruction = LoadLiteralResultElement(asStylesheet: false);
					}
				}
				AddInstruction(content, instruction);
			}
			while (_input.MoveToNextSibling());
		}
		_loadInstructionsDepth--;
		return content;
	}

	private List<XslNode> LoadWithParams(InstructionFlags flags)
	{
		XsltInput.DelayedQName elementName = _input.ElementName;
		List<XslNode> list = new List<XslNode>();
		if (_input.MoveToFirstChild())
		{
			int num = 0;
			do
			{
				switch (_input.NodeType)
				{
				case XmlNodeType.Element:
					if (_input.IsXsltKeyword(_atoms.WithParam))
					{
						XslNode xslNode = XslVarPar();
						CheckWithParam(list, xslNode);
						AddInstruction(list, xslNode);
					}
					else if (flags == InstructionFlags.AllowSort && _input.IsXsltKeyword(_atoms.Sort))
					{
						AddInstruction(list, XslSort(num++));
					}
					else if (flags == InstructionFlags.AllowFallback && _input.IsXsltKeyword(_atoms.Fallback))
					{
						XslFallback();
					}
					else
					{
						ReportError(System.SR.Xslt_UnexpectedElement, _input.QualifiedName, elementName);
						_input.SkipNode();
					}
					break;
				default:
					ReportError(System.SR.Xslt_TextNodesNotAllowed, elementName);
					break;
				case XmlNodeType.Whitespace:
				case XmlNodeType.SignificantWhitespace:
					break;
				}
			}
			while (_input.MoveToNextSibling());
		}
		return list;
	}

	private XslNode XslApplyImports()
	{
		XsltInput.ContextInfo attributes = _input.GetAttributes();
		if (!_input.CanHaveApplyImports)
		{
			ReportError(System.SR.Xslt_InvalidApplyImports);
			_input.SkipNode();
			return null;
		}
		List<XslNode> list = LoadWithParams(InstructionFlags.None);
		attributes.SaveExtendedLineInfo(_input);
		if (V1)
		{
			if (list.Count != 0)
			{
				ISourceLineInfo sourceLine = list[0].SourceLine;
				if (_input.ForwardCompatibility)
				{
					return SetInfo(AstFactory.Error(XslLoadException.CreateMessage(sourceLine, System.SR.Xslt_NotEmptyContents, _atoms.ApplyImports)), null, attributes);
				}
				_compiler.ReportError(sourceLine, System.SR.Xslt_NotEmptyContents, _atoms.ApplyImports);
			}
			list = null;
		}
		else
		{
			if (list.Count != 0)
			{
				ReportNYI("xsl:apply-imports/xsl:with-param");
			}
			list = null;
		}
		return SetInfo(AstFactory.ApplyImports(_curTemplate.Mode, _curStylesheet, _input.XslVersion), list, attributes);
	}

	private XslNode XslApplyTemplates()
	{
		XsltInput.ContextInfo attributes = _input.GetAttributes(_applyTemplatesAttributes);
		string text = ParseStringAttribute(0, "select");
		if (text == null)
		{
			text = "node()";
		}
		QilName mode = ParseModeAttribute(1);
		List<XslNode> content = LoadWithParams(InstructionFlags.AllowSort);
		attributes.SaveExtendedLineInfo(_input);
		return SetInfo(AstFactory.ApplyTemplates(mode, text, attributes, _input.XslVersion), content, attributes);
	}

	private XslNode XslCallTemplate()
	{
		XsltInput.ContextInfo attributes = _input.GetAttributes(_callTemplateAttributes);
		QilName name = ParseQNameAttribute(0);
		List<XslNode> content = LoadWithParams(InstructionFlags.None);
		attributes.SaveExtendedLineInfo(_input);
		return SetInfo(AstFactory.CallTemplate(name, attributes), content, attributes);
	}

	private XslNode XslCopy()
	{
		XsltInput.ContextInfo attributes = _input.GetAttributes(_copyAttributes);
		bool flag = ParseYesNoAttribute(0, "copy-namespaces") != TriState.False;
		bool flag2 = ParseYesNoAttribute(1, "inherit-namespaces") != TriState.False;
		if (!flag)
		{
			ReportNYI("xsl:copy[@copy-namespaces    = 'no']");
		}
		if (!flag2)
		{
			ReportNYI("xsl:copy[@inherit-namespaces = 'no']");
		}
		List<XslNode> list = new List<XslNode>();
		if (_input.MoveToXsltAttribute(2, "use-attribute-sets"))
		{
			AddUseAttributeSets(list);
		}
		ParseTypeAttribute(3);
		ParseValidationAttribute(4, defVal: false);
		return SetInfo(AstFactory.Copy(), LoadEndTag(LoadInstructions(list)), attributes);
	}

	private XslNode XslCopyOf()
	{
		XsltInput.ContextInfo attributes = _input.GetAttributes(_copyOfAttributes);
		string select = ParseStringAttribute(0, "select");
		if (ParseYesNoAttribute(1, "copy-namespaces") == TriState.False)
		{
			ReportNYI("xsl:copy-of[@copy-namespaces    = 'no']");
		}
		ParseTypeAttribute(2);
		ParseValidationAttribute(3, defVal: false);
		CheckNoContent();
		return SetInfo(AstFactory.CopyOf(select, _input.XslVersion), null, attributes);
	}

	private XslNode XslFallback()
	{
		_input.GetAttributes();
		_input.SkipNode();
		return null;
	}

	private XslNode XslIf()
	{
		XsltInput.ContextInfo attributes = _input.GetAttributes(_ifAttributes);
		string test = ParseStringAttribute(0, "test");
		return SetInfo(AstFactory.If(test, _input.XslVersion), LoadInstructions(), attributes);
	}

	private XslNode XslChoose()
	{
		XsltInput.ContextInfo attributes = _input.GetAttributes();
		List<XslNode> content = new List<XslNode>();
		bool flag = false;
		bool flag2 = false;
		XsltInput.DelayedQName elementName = _input.ElementName;
		if (_input.MoveToFirstChild())
		{
			do
			{
				switch (_input.NodeType)
				{
				case XmlNodeType.Element:
				{
					XslNode xslNode = null;
					if (Ref.Equal(_input.NamespaceUri, _atoms.UriXsl))
					{
						if (Ref.Equal(_input.LocalName, _atoms.When))
						{
							if (flag)
							{
								ReportError(System.SR.Xslt_WhenAfterOtherwise);
								_input.SkipNode();
								break;
							}
							flag2 = true;
							xslNode = XslIf();
						}
						else if (Ref.Equal(_input.LocalName, _atoms.Otherwise))
						{
							if (flag)
							{
								ReportError(System.SR.Xslt_DupOtherwise);
								_input.SkipNode();
								break;
							}
							flag = true;
							xslNode = XslOtherwise();
						}
					}
					if (xslNode == null)
					{
						ReportError(System.SR.Xslt_UnexpectedElement, _input.QualifiedName, elementName);
						_input.SkipNode();
					}
					else
					{
						AddInstruction(content, xslNode);
					}
					break;
				}
				default:
					ReportError(System.SR.Xslt_TextNodesNotAllowed, elementName);
					break;
				case XmlNodeType.Whitespace:
				case XmlNodeType.SignificantWhitespace:
					break;
				}
			}
			while (_input.MoveToNextSibling());
		}
		CheckError(!flag2, System.SR.Xslt_NoWhen);
		return SetInfo(AstFactory.Choose(), content, attributes);
	}

	private XslNode XslOtherwise()
	{
		XsltInput.ContextInfo attributes = _input.GetAttributes();
		return SetInfo(AstFactory.Otherwise(), LoadInstructions(), attributes);
	}

	private XslNode XslForEach()
	{
		XsltInput.ContextInfo attributes = _input.GetAttributes(_forEachAttributes);
		string select = ParseStringAttribute(0, "select");
		_input.CanHaveApplyImports = false;
		List<XslNode> content = LoadInstructions(InstructionFlags.AllowSort);
		attributes.SaveExtendedLineInfo(_input);
		return SetInfo(AstFactory.ForEach(select, attributes, _input.XslVersion), content, attributes);
	}

	private XslNode XslMessage()
	{
		XsltInput.ContextInfo attributes = _input.GetAttributes(_messageAttributes);
		string text = ParseStringAttribute(0, "select");
		bool term = ParseYesNoAttribute(1, "terminate") == TriState.True;
		List<XslNode> list = LoadInstructions();
		if (list.Count != 0)
		{
			list = LoadEndTag(list);
		}
		if (text != null)
		{
			list.Insert(0, AstFactory.CopyOf(text, _input.XslVersion));
		}
		return SetInfo(AstFactory.Message(term), list, attributes);
	}

	private XslNode XslNumber()
	{
		XsltInput.ContextInfo attributes = _input.GetAttributes(_numberAttributes);
		string value = ParseStringAttribute(0, "value");
		string text = ParseStringAttribute(1, "select");
		if (text != null)
		{
			ReportNYI("xsl:number/@select");
		}
		NumberLevel level = NumberLevel.Single;
		if (_input.MoveToXsltAttribute(2, "level"))
		{
			switch (_input.Value)
			{
			case "single":
				level = NumberLevel.Single;
				break;
			case "multiple":
				level = NumberLevel.Multiple;
				break;
			case "any":
				level = NumberLevel.Any;
				break;
			default:
				if (!_input.ForwardCompatibility)
				{
					ReportError(System.SR.Xslt_InvalidAttrValue, "level", _input.Value);
				}
				break;
			}
		}
		string count = ParseStringAttribute(3, "count");
		string from = ParseStringAttribute(4, "from");
		string text2 = ParseStringAttribute(5, "format");
		string lang = ParseStringAttribute(6, "lang");
		string letterValue = ParseStringAttribute(7, "letter-value");
		string value2 = ParseStringAttribute(8, "ordinal");
		if (!string.IsNullOrEmpty(value2))
		{
			ReportNYI("xsl:number/@ordinal");
		}
		string groupingSeparator = ParseStringAttribute(9, "grouping-separator");
		string groupingSize = ParseStringAttribute(10, "grouping-size");
		if (text2 == null)
		{
			text2 = "1";
		}
		CheckNoContent();
		return SetInfo(AstFactory.Number(level, count, from, value, text2, lang, letterValue, groupingSeparator, groupingSize, _input.XslVersion), null, attributes);
	}

	private XslNode XslValueOf()
	{
		XsltInput.ContextInfo attributes = _input.GetAttributes(_valueOfAttributes);
		string text = ParseStringAttribute(0, "select");
		string text2 = ParseStringAttribute(1, "separator");
		bool flag = ParseYesNoAttribute(2, "disable-output-escaping") == TriState.True;
		if (text2 == null)
		{
			if (!_input.BackwardCompatibility)
			{
				text2 = ((text != null) ? " " : string.Empty);
			}
		}
		else
		{
			ReportNYI("xsl:value-of/@separator");
		}
		List<XslNode> list = null;
		if (V1)
		{
			if (text == null)
			{
				_input.SkipNode();
				return SetInfo(AstFactory.Error(XslLoadException.CreateMessage(attributes.lineInfo, System.SR.Xslt_MissingAttribute, "select")), null, attributes);
			}
			CheckNoContent();
		}
		else
		{
			list = LoadContent(text != null);
			CheckError(text == null && list.Count == 0, System.SR.Xslt_NoSelectNoContent, _input.ElementName);
			if (list.Count != 0)
			{
				ReportNYI("xsl:value-of/*");
				list = null;
			}
		}
		return SetInfo(AstFactory.XslNode(flag ? XslNodeType.ValueOfDoe : XslNodeType.ValueOf, null, text, _input.XslVersion), null, attributes);
	}

	private VarPar XslVarPar()
	{
		string localName = _input.LocalName;
		XslNodeType xslNodeType = (Ref.Equal(localName, _atoms.Variable) ? XslNodeType.Variable : (Ref.Equal(localName, _atoms.Param) ? XslNodeType.Param : (Ref.Equal(localName, _atoms.WithParam) ? XslNodeType.WithParam : XslNodeType.Unknown)));
		XsltInput.ContextInfo attributes = _input.GetAttributes(xslNodeType switch
		{
			XslNodeType.Param => _paramAttributes, 
			XslNodeType.Variable => _variableAttributes, 
			_ => _withParamAttributes, 
		});
		QilName qilName = ParseQNameAttribute(0);
		string text = ParseStringAttribute(1, "select");
		string text2 = ParseStringAttribute(2, "as");
		TriState triState = ParseYesNoAttribute(3, "required");
		if (triState == TriState.True)
		{
			ReportNYI("xsl:param/@required == true()");
		}
		if (text2 != null)
		{
			ReportNYI("xsl:param/@as");
		}
		TriState triState2 = ParseYesNoAttribute(4, "tunnel");
		if (triState2 != TriState.Unknown)
		{
			if (xslNodeType == XslNodeType.Param && _curTemplate == null)
			{
				if (!_input.ForwardCompatibility)
				{
					ReportError(System.SR.Xslt_NonTemplateTunnel, qilName.ToString());
				}
			}
			else if (triState2 == TriState.True)
			{
				ReportNYI("xsl:param/@tunnel == true()");
			}
		}
		List<XslNode> list = LoadContent(text != null);
		CheckError(triState == TriState.True && (text != null || list.Count != 0), System.SR.Xslt_RequiredAndSelect, qilName.ToString());
		VarPar varPar = AstFactory.VarPar(xslNodeType, qilName, text, _input.XslVersion);
		SetInfo(varPar, list, attributes);
		return varPar;
	}

	private XslNode XslComment()
	{
		XsltInput.ContextInfo attributes = _input.GetAttributes(_commentAttributes);
		string text = ParseStringAttribute(0, "select");
		if (text != null)
		{
			ReportNYI("xsl:comment/@select");
		}
		return SetInfo(AstFactory.Comment(), LoadContent(text != null), attributes);
	}

	private List<XslNode> LoadContent(bool hasSelect)
	{
		XsltInput.DelayedQName elementName = _input.ElementName;
		List<XslNode> list = LoadInstructions();
		CheckError(hasSelect && list.Count != 0, System.SR.Xslt_ElementCntSel, elementName);
		if (list.Count != 0)
		{
			list = LoadEndTag(list);
		}
		return list;
	}

	private XslNode XslProcessingInstruction()
	{
		XsltInput.ContextInfo attributes = _input.GetAttributes(_processingInstructionAttributes);
		string name = ParseNCNameAttribute(0);
		string text = ParseStringAttribute(1, "select");
		if (text != null)
		{
			ReportNYI("xsl:processing-instruction/@select");
		}
		return SetInfo(AstFactory.PI(name, _input.XslVersion), LoadContent(text != null), attributes);
	}

	private XslNode XslText()
	{
		XsltInput.ContextInfo attributes = _input.GetAttributes(_textAttributes);
		SerializationHints hints = ((ParseYesNoAttribute(0, "disable-output-escaping") == TriState.True) ? SerializationHints.DisableOutputEscaping : SerializationHints.None);
		List<XslNode> list = new List<XslNode>();
		XsltInput.DelayedQName elementName = _input.ElementName;
		if (_input.MoveToFirstChild())
		{
			do
			{
				XmlNodeType nodeType = _input.NodeType;
				if (nodeType == XmlNodeType.Text || (uint)(nodeType - 13) <= 1u)
				{
					list.Add(AstFactory.Text(_input.Value, hints));
					continue;
				}
				ReportError(System.SR.Xslt_UnexpectedElement, _input.QualifiedName, elementName);
				_input.SkipNode();
			}
			while (_input.MoveToNextSibling());
		}
		return SetInfo(AstFactory.List(), list, attributes);
	}

	private XslNode XslElement()
	{
		XsltInput.ContextInfo attributes = _input.GetAttributes(_elementAttributes);
		string nameAvt = ParseNCNameAttribute(0);
		string text = ParseStringAttribute(1, "namespace");
		CheckError(text == "http://www.w3.org/2000/xmlns/", System.SR.Xslt_ReservedNS, text);
		if (ParseYesNoAttribute(2, "inherit-namespaces") == TriState.False)
		{
			ReportNYI("xsl:copy[@inherit-namespaces = 'no']");
		}
		ParseTypeAttribute(4);
		ParseValidationAttribute(5, defVal: false);
		List<XslNode> list = new List<XslNode>();
		if (_input.MoveToXsltAttribute(3, "use-attribute-sets"))
		{
			AddUseAttributeSets(list);
		}
		return SetInfo(AstFactory.Element(nameAvt, text, _input.XslVersion), LoadEndTag(LoadInstructions(list)), attributes);
	}

	private XslNode XslAttribute()
	{
		XsltInput.ContextInfo attributes = _input.GetAttributes(_attributeAttributes);
		string nameAvt = ParseNCNameAttribute(0);
		string text = ParseStringAttribute(1, "namespace");
		CheckError(text == "http://www.w3.org/2000/xmlns/", System.SR.Xslt_ReservedNS, text);
		string text2 = ParseStringAttribute(2, "select");
		if (text2 != null)
		{
			ReportNYI("xsl:attribute/@select");
		}
		string text3 = ParseStringAttribute(3, "separator");
		if (text3 != null)
		{
			ReportNYI("xsl:attribute/@separator");
		}
		text3 = ((text3 != null) ? text3 : ((text2 != null) ? " " : string.Empty));
		ParseTypeAttribute(4);
		ParseValidationAttribute(5, defVal: false);
		return SetInfo(AstFactory.Attribute(nameAvt, text, _input.XslVersion), LoadContent(text2 != null), attributes);
	}

	private XslNode XslSort(int sortNumber)
	{
		XsltInput.ContextInfo attributes = _input.GetAttributes(_sortAttributes);
		string text = ParseStringAttribute(0, "select");
		string lang = ParseStringAttribute(1, "lang");
		string order = ParseStringAttribute(2, "order");
		ParseCollationAttribute(3);
		TriState triState = ParseYesNoAttribute(4, "stable");
		string caseOrder = ParseStringAttribute(5, "case-order");
		string dataType = ParseStringAttribute(6, "data-type");
		if (triState != TriState.Unknown)
		{
			CheckError(sortNumber != 0, System.SR.Xslt_SortStable);
		}
		List<XslNode> list = null;
		if (V1)
		{
			CheckNoContent();
		}
		else
		{
			list = LoadContent(text != null);
			if (list.Count != 0)
			{
				ReportNYI("xsl:sort/*");
				list = null;
			}
		}
		if (text == null)
		{
			text = ".";
		}
		return SetInfo(AstFactory.Sort(text, lang, dataType, order, caseOrder, _input.XslVersion), null, attributes);
	}

	private XslNode LoadLiteralResultElement(bool asStylesheet)
	{
		string prefix = _input.Prefix;
		string localName = _input.LocalName;
		string namespaceUri = _input.NamespaceUri;
		XsltInput.ContextInfo literalAttributes = _input.GetLiteralAttributes(asStylesheet);
		if (_input.IsExtensionNamespace(namespaceUri))
		{
			return SetInfo(AstFactory.List(), LoadFallbacks(localName), literalAttributes);
		}
		List<XslNode> list = new List<XslNode>();
		for (int i = 1; _input.MoveToLiteralAttribute(i); i++)
		{
			if (_input.IsXsltNamespace() && _input.IsKeyword(_atoms.UseAttributeSets))
			{
				AddUseAttributeSets(list);
			}
		}
		for (int j = 1; _input.MoveToLiteralAttribute(j); j++)
		{
			if (!_input.IsXsltNamespace())
			{
				XslNode node = AstFactory.LiteralAttribute(AstFactory.QName(_input.LocalName, _input.NamespaceUri, _input.Prefix), _input.Value, _input.XslVersion);
				AddInstruction(list, SetLineInfo(node, literalAttributes.lineInfo));
			}
		}
		list = LoadEndTag(LoadInstructions(list));
		return SetInfo(AstFactory.LiteralElement(AstFactory.QName(localName, namespaceUri, prefix)), list, literalAttributes);
	}

	private void CheckWithParam(List<XslNode> content, XslNode withParam)
	{
		foreach (XslNode item in content)
		{
			if (item.NodeType == XslNodeType.WithParam && item.Name.Equals(withParam.Name))
			{
				ReportError(System.SR.Xslt_DuplicateWithParam, withParam.Name.QualifiedName);
				break;
			}
		}
	}

	private static void AddInstruction(List<XslNode> content, XslNode instruction)
	{
		if (instruction != null)
		{
			content.Add(instruction);
		}
	}

	private List<XslNode> LoadEndTag(List<XslNode> content)
	{
		if (_compiler.IsDebug && !_input.IsEmptyElement)
		{
			AddInstruction(content, SetLineInfo(AstFactory.Nop(), _input.BuildLineInfo()));
		}
		return content;
	}

	private XslNode LoadUnknownXsltInstruction(string parentName)
	{
		_input.GetVersionAttribute();
		if (!_input.ForwardCompatibility)
		{
			ReportError(System.SR.Xslt_UnexpectedElement, _input.QualifiedName, parentName);
			_input.SkipNode();
			return null;
		}
		XsltInput.ContextInfo attributes = _input.GetAttributes();
		List<XslNode> content = LoadFallbacks(_input.LocalName);
		return SetInfo(AstFactory.List(), content, attributes);
	}

	private List<XslNode> LoadFallbacks(string instrName)
	{
		_input.MoveToElement();
		ISourceLineInfo lineInfo = _input.BuildNameLineInfo();
		List<XslNode> list = new List<XslNode>();
		if (_input.MoveToFirstChild())
		{
			do
			{
				if (_input.IsXsltKeyword(_atoms.Fallback))
				{
					XsltInput.ContextInfo attributes = _input.GetAttributes();
					list.Add(SetInfo(AstFactory.List(), LoadInstructions(), attributes));
				}
				else
				{
					_input.SkipNode();
				}
			}
			while (_input.MoveToNextSibling());
		}
		if (list.Count == 0)
		{
			list.Add(AstFactory.Error(XslLoadException.CreateMessage(lineInfo, System.SR.Xslt_UnknownExtensionElement, instrName)));
		}
		return list;
	}

	private QilName ParseModeAttribute(int attNum)
	{
		if (!_input.MoveToXsltAttribute(attNum, "mode"))
		{
			return nullMode;
		}
		_compiler.EnterForwardsCompatible();
		string value = _input.Value;
		QilName result;
		if (!V1 && value == "#default")
		{
			result = nullMode;
		}
		else if (!V1 && value == "#current")
		{
			ReportNYI("xsl:apply-templates[@mode='#current']");
			result = nullMode;
		}
		else if (!V1 && value == "#all")
		{
			ReportError(System.SR.Xslt_ModeListAll);
			result = nullMode;
		}
		else
		{
			result = CreateXPathQName(value);
		}
		if (!_compiler.ExitForwardsCompatible(_input.ForwardCompatibility))
		{
			result = nullMode;
		}
		return result;
	}

	private QilName ParseModeListAttribute(int attNum)
	{
		if (!_input.MoveToXsltAttribute(attNum, "mode"))
		{
			return nullMode;
		}
		string value = _input.Value;
		if (value == "#all")
		{
			ReportNYI("xsl:template[@mode='#all']");
			return nullMode;
		}
		string[] array = XmlConvert.SplitString(value);
		List<QilName> list = new List<QilName>(array.Length);
		_compiler.EnterForwardsCompatible();
		if (array.Length == 0)
		{
			ReportError(System.SR.Xslt_ModeListEmpty);
		}
		else
		{
			string[] array2 = array;
			foreach (string text in array2)
			{
				QilName qilName;
				bool flag;
				switch (text)
				{
				case "#default":
					qilName = nullMode;
					goto IL_00e6;
				case "#current":
					ReportNYI("xsl:apply-templates[@mode='#current']");
					break;
				case "#all":
					ReportError(System.SR.Xslt_ModeListAll);
					break;
				default:
					{
						qilName = CreateXPathQName(text);
						goto IL_00e6;
					}
					IL_00e6:
					flag = false;
					foreach (QilName item in list)
					{
						flag |= item.Equals(qilName);
					}
					if (flag)
					{
						ReportError(System.SR.Xslt_ModeListDup, text);
					}
					else
					{
						list.Add(qilName);
					}
					continue;
				}
				break;
			}
		}
		if (!_compiler.ExitForwardsCompatible(_input.ForwardCompatibility))
		{
			list.Clear();
			list.Add(nullMode);
		}
		if (1 < list.Count)
		{
			ReportNYI("Multipe modes");
			return nullMode;
		}
		if (list.Count == 0)
		{
			return nullMode;
		}
		return list[0];
	}

	private string ParseCollationAttribute(int attNum)
	{
		if (_input.MoveToXsltAttribute(attNum, "collation"))
		{
			ReportNYI("@collation");
		}
		return null;
	}

	private bool ResolveQName(bool ignoreDefaultNs, string qname, out string localName, out string namespaceName, out string prefix)
	{
		if (qname == null)
		{
			prefix = _compiler.PhantomNCName;
			localName = _compiler.PhantomNCName;
			namespaceName = _compiler.CreatePhantomNamespace();
			return false;
		}
		if (!_compiler.ParseQName(qname, out prefix, out localName, this))
		{
			namespaceName = _compiler.CreatePhantomNamespace();
			return false;
		}
		if (ignoreDefaultNs && prefix.Length == 0)
		{
			namespaceName = string.Empty;
		}
		else
		{
			namespaceName = _input.LookupXmlNamespace(prefix);
			if (namespaceName == null)
			{
				namespaceName = _compiler.CreatePhantomNamespace();
				return false;
			}
		}
		return true;
	}

	private QilName ParseQNameAttribute(int attNum)
	{
		bool flag = _input.IsRequiredAttribute(attNum);
		QilName qilName = null;
		if (!flag)
		{
			_compiler.EnterForwardsCompatible();
		}
		if (_input.MoveToXsltAttribute(attNum, "name") && ResolveQName(ignoreDefaultNs: true, _input.Value, out var localName, out var namespaceName, out var prefix))
		{
			qilName = AstFactory.QName(localName, namespaceName, prefix);
		}
		if (!flag)
		{
			_compiler.ExitForwardsCompatible(_input.ForwardCompatibility);
		}
		if (qilName == null && flag)
		{
			qilName = AstFactory.QName(_compiler.PhantomNCName, _compiler.CreatePhantomNamespace(), _compiler.PhantomNCName);
		}
		return qilName;
	}

	private string ParseNCNameAttribute(int attNum)
	{
		if (_input.MoveToXsltAttribute(attNum, "name"))
		{
			return _input.Value;
		}
		return _compiler.PhantomNCName;
	}

	private QilName CreateXPathQName(string qname)
	{
		ResolveQName(ignoreDefaultNs: true, qname, out var localName, out var namespaceName, out var prefix);
		return AstFactory.QName(localName, namespaceName, prefix);
	}

	private XmlQualifiedName ResolveQName(bool ignoreDefaultNs, string qname)
	{
		ResolveQName(ignoreDefaultNs, qname, out var localName, out var namespaceName, out var _);
		return new XmlQualifiedName(localName, namespaceName);
	}

	private void ParseWhitespaceRules(string elements, bool preserveSpace)
	{
		if (elements == null || elements.Length == 0)
		{
			return;
		}
		string[] array = XmlConvert.SplitString(elements);
		for (int i = 0; i < array.Length; i++)
		{
			string text;
			if (!_compiler.ParseNameTest(array[i], out var prefix, out var localName, this))
			{
				text = _compiler.CreatePhantomNamespace();
			}
			else if (prefix == null || prefix.Length == 0)
			{
				text = prefix;
			}
			else
			{
				text = _input.LookupXmlNamespace(prefix);
				if (text == null)
				{
					text = _compiler.CreatePhantomNamespace();
				}
			}
			int index = ((localName == null) ? 1 : 0) + ((text == null) ? 1 : 0);
			_curStylesheet.AddWhitespaceRule(index, new WhitespaceRule(localName, text, preserveSpace));
		}
	}

	private XmlQualifiedName ParseOutputMethod(string attValue, out XmlOutputMethod method)
	{
		ResolveQName(ignoreDefaultNs: true, attValue, out var localName, out var namespaceName, out var prefix);
		method = XmlOutputMethod.AutoDetect;
		if (_compiler.IsPhantomNamespace(namespaceName))
		{
			return null;
		}
		if (prefix.Length == 0)
		{
			switch (localName)
			{
			case "xml":
				method = XmlOutputMethod.Xml;
				break;
			case "html":
				method = XmlOutputMethod.Html;
				break;
			case "text":
				method = XmlOutputMethod.Text;
				break;
			default:
				ReportError(System.SR.Xslt_InvalidAttrValue, "method", attValue);
				return null;
			}
		}
		else if (!_input.ForwardCompatibility)
		{
			ReportWarning(System.SR.Xslt_InvalidMethod, attValue);
		}
		return new XmlQualifiedName(localName, namespaceName);
	}

	private void AddUseAttributeSets(List<XslNode> list)
	{
		_compiler.EnterForwardsCompatible();
		string[] array = XmlConvert.SplitString(_input.Value);
		foreach (string qname in array)
		{
			AddInstruction(list, SetLineInfo(AstFactory.UseAttributeSet(CreateXPathQName(qname)), _input.BuildLineInfo()));
		}
		if (!_compiler.ExitForwardsCompatible(_input.ForwardCompatibility))
		{
			list.Clear();
		}
	}

	private List<QilName> ParseUseCharacterMaps(int attNum)
	{
		List<QilName> list = new List<QilName>();
		if (_input.MoveToXsltAttribute(attNum, "use-character-maps"))
		{
			_compiler.EnterForwardsCompatible();
			string[] array = XmlConvert.SplitString(_input.Value);
			foreach (string qname in array)
			{
				list.Add(CreateXPathQName(qname));
			}
			if (!_compiler.ExitForwardsCompatible(_input.ForwardCompatibility))
			{
				list.Clear();
			}
		}
		return list;
	}

	private string ParseStringAttribute(int attNum, string attName)
	{
		if (_input.MoveToXsltAttribute(attNum, attName))
		{
			return _input.Value;
		}
		return null;
	}

	private char ParseCharAttribute(int attNum, string attName, char defVal)
	{
		if (_input.MoveToXsltAttribute(attNum, attName))
		{
			if (_input.Value.Length == 1)
			{
				return _input.Value[0];
			}
			if (_input.IsRequiredAttribute(attNum) || !_input.ForwardCompatibility)
			{
				ReportError(System.SR.Xslt_CharAttribute, attName);
			}
		}
		return defVal;
	}

	private TriState ParseYesNoAttribute(int attNum, string attName)
	{
		if (_input.MoveToXsltAttribute(attNum, attName))
		{
			string value = _input.Value;
			if (value == "yes")
			{
				return TriState.True;
			}
			if (value == "no")
			{
				return TriState.False;
			}
			if (!_input.ForwardCompatibility)
			{
				ReportError(System.SR.Xslt_BistateAttribute, attName, "yes", "no");
			}
		}
		return TriState.Unknown;
	}

	private void ParseTypeAttribute(int attNum)
	{
		if (_input.MoveToXsltAttribute(attNum, "type"))
		{
			CheckError(true, System.SR.Xslt_SchemaAttribute, "type");
		}
	}

	private void ParseValidationAttribute(int attNum, bool defVal)
	{
		string text = (defVal ? _atoms.DefaultValidation : "validation");
		if (!_input.MoveToXsltAttribute(attNum, text))
		{
			return;
		}
		string value = _input.Value;
		switch (value)
		{
		case "strict":
			if (defVal)
			{
				goto default;
			}
			goto case "preserve";
		default:
			if (!(value == "lax") || defVal)
			{
				break;
			}
			goto case "preserve";
		case "preserve":
			ReportError(System.SR.Xslt_SchemaAttributeValue, text, value);
			return;
		}
		if (!_input.ForwardCompatibility)
		{
			ReportError(System.SR.Xslt_InvalidAttrValue, text, value);
		}
	}

	private void ParseInputTypeAnnotationsAttribute(int attNum)
	{
		if (!_input.MoveToXsltAttribute(attNum, "input-type-annotations"))
		{
			return;
		}
		string value = _input.Value;
		switch (value)
		{
		case "strip":
		case "preserve":
			if (_compiler.inputTypeAnnotations == null)
			{
				_compiler.inputTypeAnnotations = value;
			}
			else
			{
				CheckError(_compiler.inputTypeAnnotations != value, System.SR.Xslt_InputTypeAnnotations);
			}
			return;
		}
		if (!_input.ForwardCompatibility)
		{
			ReportError(System.SR.Xslt_InvalidAttrValue, "input-type-annotations", value);
		}
	}

	private void CheckNoContent()
	{
		_input.MoveToElement();
		XsltInput.DelayedQName elementName = _input.ElementName;
		ISourceLineInfo sourceLineInfo = SkipEmptyContent();
		if (sourceLineInfo != null)
		{
			_compiler.ReportError(sourceLineInfo, System.SR.Xslt_NotEmptyContents, elementName);
		}
	}

	private ISourceLineInfo SkipEmptyContent()
	{
		ISourceLineInfo sourceLineInfo = null;
		if (_input.MoveToFirstChild())
		{
			do
			{
				if (_input.NodeType != XmlNodeType.Whitespace)
				{
					if (sourceLineInfo == null)
					{
						sourceLineInfo = _input.BuildNameLineInfo();
					}
					_input.SkipNode();
				}
			}
			while (_input.MoveToNextSibling());
		}
		return sourceLineInfo;
	}

	private static XslNode SetLineInfo(XslNode node, ISourceLineInfo lineInfo)
	{
		node.SourceLine = lineInfo;
		return node;
	}

	private static void SetContent(XslNode node, List<XslNode> content)
	{
		if (content != null && content.Count == 0)
		{
			content = null;
		}
		node.SetContent(content);
	}

	internal static XslNode SetInfo(XslNode to, List<XslNode> content, XsltInput.ContextInfo info)
	{
		to.Namespaces = info.nsList;
		SetContent(to, content);
		SetLineInfo(to, info.lineInfo);
		return to;
	}

	private static NsDecl MergeNamespaces(NsDecl thisList, NsDecl parentList)
	{
		if (parentList == null)
		{
			return thisList;
		}
		if (thisList == null)
		{
			return parentList;
		}
		while (parentList != null)
		{
			bool flag = false;
			for (NsDecl nsDecl = thisList; nsDecl != null; nsDecl = nsDecl.Prev)
			{
				if (Ref.Equal(nsDecl.Prefix, parentList.Prefix) && (nsDecl.Prefix != null || nsDecl.NsUri == parentList.NsUri))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				thisList = new NsDecl(thisList, parentList.Prefix, parentList.NsUri);
			}
			parentList = parentList.Prev;
		}
		return thisList;
	}

	public void ReportError(string res, params string[] args)
	{
		_compiler.ReportError(_input.BuildNameLineInfo(), res, args);
	}

	public void ReportWarning(string res, params string[] args)
	{
		_compiler.ReportWarning(_input.BuildNameLineInfo(), res, args);
	}

	private void ReportNYI(string arg)
	{
		if (!_input.ForwardCompatibility)
		{
			ReportError(System.SR.Xslt_NotYetImplemented, arg);
		}
	}

	public void CheckError(bool cond, string res, params string[] args)
	{
		if (cond)
		{
			_compiler.ReportError(_input.BuildNameLineInfo(), res, args);
		}
	}
}

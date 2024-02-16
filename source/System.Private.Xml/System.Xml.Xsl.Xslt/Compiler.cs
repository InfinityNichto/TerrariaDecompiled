using System.Collections.Generic;
using System.Xml.XPath;
using System.Xml.Xsl.Qil;

namespace System.Xml.Xsl.Xslt;

internal sealed class Compiler
{
	private sealed class CompilerErrorComparer : IComparer<CompilerError>
	{
		private readonly Dictionary<string, int> _moduleOrder;

		public CompilerErrorComparer(Dictionary<string, int> moduleOrder)
		{
			_moduleOrder = moduleOrder;
		}

		public int Compare(CompilerError x, CompilerError y)
		{
			if (x == y)
			{
				return 0;
			}
			if (x == null)
			{
				return -1;
			}
			if (y == null)
			{
				return 1;
			}
			int num = _moduleOrder[x.FileName].CompareTo(_moduleOrder[y.FileName]);
			if (num != 0)
			{
				return num;
			}
			num = x.Line.CompareTo(y.Line);
			if (num != 0)
			{
				return num;
			}
			num = x.Column.CompareTo(y.Column);
			if (num != 0)
			{
				return num;
			}
			num = x.IsWarning.CompareTo(y.IsWarning);
			if (num != 0)
			{
				return num;
			}
			num = string.CompareOrdinal(x.ErrorNumber, y.ErrorNumber);
			if (num != 0)
			{
				return num;
			}
			return string.CompareOrdinal(x.ErrorText, y.ErrorText);
		}
	}

	public XsltSettings Settings;

	public bool IsDebug;

	public string ScriptAssemblyPath;

	public int Version;

	public string inputTypeAnnotations;

	public CompilerErrorCollection CompilerErrorColl;

	public int CurrentPrecedence;

	public XslNode StartApplyTemplates;

	public RootLevel Root;

	public Scripts Scripts;

	public Output Output = new Output();

	public List<VarPar> ExternalPars = new List<VarPar>();

	public List<VarPar> GlobalVars = new List<VarPar>();

	public List<WhitespaceRule> WhitespaceRules = new List<WhitespaceRule>();

	public DecimalFormats DecimalFormats = new DecimalFormats();

	public Keys Keys = new Keys();

	public List<ProtoTemplate> AllTemplates = new List<ProtoTemplate>();

	public Dictionary<QilName, VarPar> AllGlobalVarPars = new Dictionary<QilName, VarPar>();

	public Dictionary<QilName, Template> NamedTemplates = new Dictionary<QilName, Template>();

	public Dictionary<QilName, AttributeSet> AttributeSets = new Dictionary<QilName, AttributeSet>();

	public Dictionary<string, NsAlias> NsAliases = new Dictionary<string, NsAlias>();

	private readonly Dictionary<string, int> _moduleOrder = new Dictionary<string, int>();

	public readonly string PhantomNCName = "error";

	private int _phantomNsCounter;

	private int _savedErrorCount = -1;

	private int ErrorCount
	{
		get
		{
			return CompilerErrorColl.Count;
		}
		set
		{
			for (int num = ErrorCount - 1; num >= value; num--)
			{
				CompilerErrorColl.RemoveAt(num);
			}
		}
	}

	public Compiler(XsltSettings settings, bool debug, string scriptAssemblyPath)
	{
		Settings = settings;
		IsDebug = settings.IncludeDebugInformation || debug;
		ScriptAssemblyPath = scriptAssemblyPath;
		CompilerErrorColl = new CompilerErrorCollection();
		Scripts = new Scripts(this);
	}

	public CompilerErrorCollection Compile(object stylesheet, XmlResolver xmlResolver, out QilExpression qil)
	{
		new XsltLoader().Load(this, stylesheet, xmlResolver);
		qil = QilGenerator.CompileStylesheet(this);
		SortErrors();
		return CompilerErrorColl;
	}

	public Stylesheet CreateStylesheet()
	{
		Stylesheet stylesheet = new Stylesheet(this, CurrentPrecedence);
		if (CurrentPrecedence-- == 0)
		{
			Root = new RootLevel(stylesheet);
		}
		return stylesheet;
	}

	public void AddModule(string baseUri)
	{
		if (!_moduleOrder.ContainsKey(baseUri))
		{
			_moduleOrder[baseUri] = _moduleOrder.Count;
		}
	}

	public void ApplyNsAliases(ref string prefix, ref string nsUri)
	{
		if (NsAliases.TryGetValue(nsUri, out var value))
		{
			nsUri = value.ResultNsUri;
			prefix = value.ResultPrefix;
		}
	}

	public bool SetNsAlias(string ssheetNsUri, string resultNsUri, string resultPrefix, int importPrecedence)
	{
		if (NsAliases.TryGetValue(ssheetNsUri, out var value) && (importPrecedence < value.ImportPrecedence || resultNsUri == value.ResultNsUri))
		{
			return false;
		}
		NsAliases[ssheetNsUri] = new NsAlias(resultNsUri, resultPrefix, importPrecedence);
		return value != null;
	}

	private void MergeWhitespaceRules(Stylesheet sheet)
	{
		for (int i = 0; i <= 2; i++)
		{
			sheet.WhitespaceRules[i].Reverse();
			WhitespaceRules.AddRange(sheet.WhitespaceRules[i]);
		}
		sheet.WhitespaceRules = null;
	}

	private void MergeAttributeSets(Stylesheet sheet)
	{
		foreach (QilName key in sheet.AttributeSets.Keys)
		{
			if (!AttributeSets.TryGetValue(key, out var value))
			{
				AttributeSets[key] = sheet.AttributeSets[key];
			}
			else
			{
				value.MergeContent(sheet.AttributeSets[key]);
			}
		}
		sheet.AttributeSets = null;
	}

	private void MergeGlobalVarPars(Stylesheet sheet)
	{
		foreach (VarPar globalVarPar in sheet.GlobalVarPars)
		{
			if (!AllGlobalVarPars.ContainsKey(globalVarPar.Name))
			{
				if (globalVarPar.NodeType == XslNodeType.Variable)
				{
					GlobalVars.Add(globalVarPar);
				}
				else
				{
					ExternalPars.Add(globalVarPar);
				}
				AllGlobalVarPars[globalVarPar.Name] = globalVarPar;
			}
		}
		sheet.GlobalVarPars = null;
	}

	public void MergeWithStylesheet(Stylesheet sheet)
	{
		MergeWhitespaceRules(sheet);
		MergeAttributeSets(sheet);
		MergeGlobalVarPars(sheet);
	}

	public static string ConstructQName(string prefix, string localName)
	{
		if (prefix.Length == 0)
		{
			return localName;
		}
		return prefix + ":" + localName;
	}

	public bool ParseQName(string qname, out string prefix, out string localName, IErrorHelper errorHelper)
	{
		try
		{
			ValidateNames.ParseQNameThrow(qname, out prefix, out localName);
			return true;
		}
		catch (XmlException ex)
		{
			errorHelper.ReportError(ex.Message, (string[])null);
			prefix = PhantomNCName;
			localName = PhantomNCName;
			return false;
		}
	}

	public bool ParseNameTest(string nameTest, out string prefix, out string localName, IErrorHelper errorHelper)
	{
		try
		{
			ValidateNames.ParseNameTestThrow(nameTest, out prefix, out localName);
			return true;
		}
		catch (XmlException ex)
		{
			errorHelper.ReportError(ex.Message, (string[])null);
			prefix = PhantomNCName;
			localName = PhantomNCName;
			return false;
		}
	}

	public void ValidatePiName(string name, IErrorHelper errorHelper)
	{
		try
		{
			ValidateNames.ValidateNameThrow(string.Empty, name, string.Empty, XPathNodeType.ProcessingInstruction, ValidateNames.Flags.AllExceptPrefixMapping);
		}
		catch (XmlException ex)
		{
			errorHelper.ReportError(ex.Message, (string[])null);
		}
	}

	public string CreatePhantomNamespace()
	{
		return "\0namespace" + _phantomNsCounter++;
	}

	public bool IsPhantomNamespace(string namespaceName)
	{
		if (namespaceName.Length > 0)
		{
			return namespaceName[0] == '\0';
		}
		return false;
	}

	public bool IsPhantomName(QilName qname)
	{
		string namespaceUri = qname.NamespaceUri;
		if (namespaceUri.Length > 0)
		{
			return namespaceUri[0] == '\0';
		}
		return false;
	}

	public void EnterForwardsCompatible()
	{
		_savedErrorCount = ErrorCount;
	}

	public bool ExitForwardsCompatible(bool fwdCompat)
	{
		if (fwdCompat && ErrorCount > _savedErrorCount)
		{
			ErrorCount = _savedErrorCount;
			return false;
		}
		return true;
	}

	public CompilerError CreateError(ISourceLineInfo lineInfo, string res, params string[] args)
	{
		AddModule(lineInfo.Uri);
		return new CompilerError(lineInfo.Uri, lineInfo.Start.Line, lineInfo.Start.Pos, string.Empty, XslTransformException.CreateMessage(res, args));
	}

	public void ReportError(ISourceLineInfo lineInfo, string res, params string[] args)
	{
		CompilerError value = CreateError(lineInfo, res, args);
		CompilerErrorColl.Add(value);
	}

	public void ReportWarning(ISourceLineInfo lineInfo, string res, params string[] args)
	{
		int num = 1;
		if (0 > Settings.WarningLevel || Settings.WarningLevel >= num)
		{
			CompilerError compilerError = CreateError(lineInfo, res, args);
			if (Settings.TreatWarningsAsErrors)
			{
				compilerError.ErrorText = XslTransformException.CreateMessage(System.SR.Xslt_WarningAsError, compilerError.ErrorText);
				CompilerErrorColl.Add(compilerError);
			}
			else
			{
				compilerError.IsWarning = true;
				CompilerErrorColl.Add(compilerError);
			}
		}
	}

	private void SortErrors()
	{
		CompilerErrorCollection compilerErrorColl = CompilerErrorColl;
		if (compilerErrorColl.Count > 1)
		{
			CompilerError[] array = new CompilerError[compilerErrorColl.Count];
			compilerErrorColl.CopyTo(array, 0);
			Array.Sort(array, new CompilerErrorComparer(_moduleOrder));
			compilerErrorColl.Clear();
			compilerErrorColl.AddRange(array);
		}
	}
}

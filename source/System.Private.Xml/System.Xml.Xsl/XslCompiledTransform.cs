using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml.XPath;
using System.Xml.Xsl.Qil;
using System.Xml.Xsl.Runtime;
using System.Xml.Xsl.Xslt;

namespace System.Xml.Xsl;

public sealed class XslCompiledTransform
{
	private static readonly Version s_version = typeof(XslCompiledTransform).Assembly.GetName().Version;

	private readonly bool _enableDebug;

	private CompilerErrorCollection _compilerErrorColl;

	private XmlWriterSettings _outputSettings;

	private QilExpression _qil;

	private XmlILCommand _command;

	public XmlWriterSettings? OutputSettings => _outputSettings;

	public XslCompiledTransform()
	{
	}

	public XslCompiledTransform(bool enableDebug)
	{
		_enableDebug = enableDebug;
	}

	private void Reset()
	{
		_compilerErrorColl = null;
		_outputSettings = null;
		_qil = null;
		_command = null;
	}

	public void Load(XmlReader stylesheet)
	{
		Reset();
		LoadInternal(stylesheet, XsltSettings.Default, CreateDefaultResolver());
	}

	public void Load(XmlReader stylesheet, XsltSettings? settings, XmlResolver? stylesheetResolver)
	{
		Reset();
		LoadInternal(stylesheet, settings, stylesheetResolver);
	}

	public void Load(IXPathNavigable stylesheet)
	{
		Reset();
		LoadInternal(stylesheet, XsltSettings.Default, CreateDefaultResolver());
	}

	public void Load(IXPathNavigable stylesheet, XsltSettings? settings, XmlResolver? stylesheetResolver)
	{
		Reset();
		LoadInternal(stylesheet, settings, stylesheetResolver);
	}

	public void Load(string stylesheetUri)
	{
		Reset();
		if (stylesheetUri == null)
		{
			throw new ArgumentNullException("stylesheetUri");
		}
		LoadInternal(stylesheetUri, XsltSettings.Default, CreateDefaultResolver());
	}

	public void Load(string stylesheetUri, XsltSettings? settings, XmlResolver? stylesheetResolver)
	{
		Reset();
		if (stylesheetUri == null)
		{
			throw new ArgumentNullException("stylesheetUri");
		}
		LoadInternal(stylesheetUri, settings, stylesheetResolver);
	}

	private CompilerErrorCollection LoadInternal(object stylesheet, XsltSettings settings, XmlResolver stylesheetResolver)
	{
		if (stylesheet == null)
		{
			throw new ArgumentNullException("stylesheet");
		}
		if (settings == null)
		{
			settings = XsltSettings.Default;
		}
		CompileXsltToQil(stylesheet, settings, stylesheetResolver);
		CompilerError firstError = GetFirstError();
		if (firstError != null)
		{
			throw new XslLoadException(firstError);
		}
		if (!settings.CheckOnly)
		{
			CompileQilToMsil(settings);
		}
		return _compilerErrorColl;
	}

	[MemberNotNull("_compilerErrorColl")]
	[MemberNotNull("_qil")]
	private void CompileXsltToQil(object stylesheet, XsltSettings settings, XmlResolver stylesheetResolver)
	{
		_compilerErrorColl = new Compiler(settings, _enableDebug, null).Compile(stylesheet, stylesheetResolver, out _qil);
	}

	private CompilerError GetFirstError()
	{
		foreach (CompilerError item in _compilerErrorColl)
		{
			if (!item.IsWarning)
			{
				return item;
			}
		}
		return null;
	}

	private void CompileQilToMsil(XsltSettings settings)
	{
		_command = new XmlILGenerator().Generate(_qil, null);
		_outputSettings = _command.StaticData.DefaultWriterSettings;
		_qil = null;
	}

	[RequiresUnreferencedCode("This method will get fields and types from the assembly of the passed in compiledStylesheet and call their constructors which cannot be statically analyzed")]
	public void Load(Type compiledStylesheet)
	{
		Reset();
		if (compiledStylesheet == null)
		{
			throw new ArgumentNullException("compiledStylesheet");
		}
		object[] customAttributes = compiledStylesheet.GetCustomAttributes(typeof(GeneratedCodeAttribute), inherit: false);
		GeneratedCodeAttribute generatedCodeAttribute = ((customAttributes.Length != 0) ? ((GeneratedCodeAttribute)customAttributes[0]) : null);
		if (generatedCodeAttribute != null && generatedCodeAttribute.Tool == typeof(XslCompiledTransform).FullName)
		{
			if (s_version < Version.Parse(generatedCodeAttribute.Version))
			{
				throw new ArgumentException(System.SR.Format(System.SR.Xslt_IncompatibleCompiledStylesheetVersion, generatedCodeAttribute.Version, s_version), "compiledStylesheet");
			}
			FieldInfo field = compiledStylesheet.GetField("staticData", BindingFlags.Static | BindingFlags.NonPublic);
			FieldInfo field2 = compiledStylesheet.GetField("ebTypes", BindingFlags.Static | BindingFlags.NonPublic);
			if (field != null && field2 != null && field.GetValue(null) is byte[] queryData)
			{
				MethodInfo method = compiledStylesheet.GetMethod("Execute", BindingFlags.Static | BindingFlags.NonPublic);
				Type[] earlyBoundTypes = (Type[])field2.GetValue(null);
				Load(method, queryData, earlyBoundTypes);
				return;
			}
		}
		if (_command == null)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Xslt_NotCompiledStylesheet, compiledStylesheet.FullName), "compiledStylesheet");
		}
	}

	[RequiresUnreferencedCode("This method will call into constructors of the earlyBoundTypes array which cannot be statically analyzed.")]
	public void Load(MethodInfo executeMethod, byte[] queryData, Type[]? earlyBoundTypes)
	{
		Reset();
		if (executeMethod == null)
		{
			throw new ArgumentNullException("executeMethod");
		}
		if (queryData == null)
		{
			throw new ArgumentNullException("queryData");
		}
		DynamicMethod dynamicMethod = executeMethod as DynamicMethod;
		Delegate @delegate = ((dynamicMethod != null) ? dynamicMethod.CreateDelegate(typeof(ExecuteDelegate)) : executeMethod.CreateDelegate(typeof(ExecuteDelegate)));
		_command = new XmlILCommand((ExecuteDelegate)@delegate, new XmlQueryStaticData(queryData, earlyBoundTypes));
		_outputSettings = _command.StaticData.DefaultWriterSettings;
	}

	public void Transform(IXPathNavigable input, XmlWriter results)
	{
		CheckArguments(input, results);
		Transform(input, null, results, CreateDefaultResolver());
	}

	public void Transform(IXPathNavigable input, XsltArgumentList? arguments, XmlWriter results)
	{
		CheckArguments(input, results);
		Transform(input, arguments, results, CreateDefaultResolver());
	}

	public void Transform(IXPathNavigable input, XsltArgumentList? arguments, TextWriter results)
	{
		CheckArguments(input, results);
		using XmlWriter xmlWriter = XmlWriter.Create(results, OutputSettings);
		Transform(input, arguments, xmlWriter, CreateDefaultResolver());
		xmlWriter.Close();
	}

	public void Transform(IXPathNavigable input, XsltArgumentList? arguments, Stream results)
	{
		CheckArguments(input, results);
		using XmlWriter xmlWriter = XmlWriter.Create(results, OutputSettings);
		Transform(input, arguments, xmlWriter, CreateDefaultResolver());
		xmlWriter.Close();
	}

	public void Transform(XmlReader input, XmlWriter results)
	{
		CheckArguments(input, results);
		Transform(input, null, results, CreateDefaultResolver());
	}

	public void Transform(XmlReader input, XsltArgumentList? arguments, XmlWriter results)
	{
		CheckArguments(input, results);
		Transform(input, arguments, results, CreateDefaultResolver());
	}

	public void Transform(XmlReader input, XsltArgumentList? arguments, TextWriter results)
	{
		CheckArguments(input, results);
		using XmlWriter xmlWriter = XmlWriter.Create(results, OutputSettings);
		Transform(input, arguments, xmlWriter, CreateDefaultResolver());
		xmlWriter.Close();
	}

	public void Transform(XmlReader input, XsltArgumentList? arguments, Stream results)
	{
		CheckArguments(input, results);
		using XmlWriter xmlWriter = XmlWriter.Create(results, OutputSettings);
		Transform(input, arguments, xmlWriter, CreateDefaultResolver());
		xmlWriter.Close();
	}

	public void Transform(string inputUri, XmlWriter results)
	{
		CheckArguments(inputUri, results);
		using XmlReader input = XmlReader.Create(inputUri);
		Transform(input, null, results, CreateDefaultResolver());
	}

	public void Transform(string inputUri, XsltArgumentList? arguments, XmlWriter results)
	{
		CheckArguments(inputUri, results);
		using XmlReader input = XmlReader.Create(inputUri);
		Transform(input, arguments, results, CreateDefaultResolver());
	}

	public void Transform(string inputUri, XsltArgumentList? arguments, TextWriter results)
	{
		CheckArguments(inputUri, results);
		using XmlReader input = XmlReader.Create(inputUri);
		using XmlWriter xmlWriter = XmlWriter.Create(results, OutputSettings);
		Transform(input, arguments, xmlWriter, CreateDefaultResolver());
		xmlWriter.Close();
	}

	public void Transform(string inputUri, XsltArgumentList? arguments, Stream results)
	{
		CheckArguments(inputUri, results);
		using XmlReader input = XmlReader.Create(inputUri);
		using XmlWriter xmlWriter = XmlWriter.Create(results, OutputSettings);
		Transform(input, arguments, xmlWriter, CreateDefaultResolver());
		xmlWriter.Close();
	}

	public void Transform(string inputUri, string resultsFile)
	{
		if (inputUri == null)
		{
			throw new ArgumentNullException("inputUri");
		}
		if (resultsFile == null)
		{
			throw new ArgumentNullException("resultsFile");
		}
		using XmlReader input = XmlReader.Create(inputUri);
		using XmlWriter xmlWriter = XmlWriter.Create(resultsFile, OutputSettings);
		Transform(input, null, xmlWriter, CreateDefaultResolver());
		xmlWriter.Close();
	}

	public void Transform(XmlReader input, XsltArgumentList? arguments, XmlWriter results, XmlResolver? documentResolver)
	{
		CheckArguments(input, results);
		CheckCommand();
		_command.Execute(input, documentResolver, arguments, results);
	}

	public void Transform(IXPathNavigable input, XsltArgumentList? arguments, XmlWriter results, XmlResolver? documentResolver)
	{
		CheckArguments(input, results);
		CheckCommand();
		_command.Execute(input.CreateNavigator(), documentResolver, arguments, results);
	}

	private static void CheckArguments(object input, object results)
	{
		if (input == null)
		{
			throw new ArgumentNullException("input");
		}
		if (results == null)
		{
			throw new ArgumentNullException("results");
		}
	}

	private static void CheckArguments(string inputUri, object results)
	{
		if (inputUri == null)
		{
			throw new ArgumentNullException("inputUri");
		}
		if (results == null)
		{
			throw new ArgumentNullException("results");
		}
	}

	[MemberNotNull("_command")]
	private void CheckCommand()
	{
		if (_command == null)
		{
			throw new InvalidOperationException(System.SR.Xslt_NoStylesheetLoaded);
		}
	}

	private static XmlResolver CreateDefaultResolver()
	{
		if (System.LocalAppContextSwitches.AllowDefaultResolver)
		{
			return new XmlUrlResolver();
		}
		return XmlNullResolver.Singleton;
	}
}

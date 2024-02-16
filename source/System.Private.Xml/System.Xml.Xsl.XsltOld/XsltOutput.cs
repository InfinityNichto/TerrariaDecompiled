using System.Collections;
using System.Text;

namespace System.Xml.Xsl.XsltOld;

internal sealed class XsltOutput : CompiledAction
{
	internal enum OutputMethod
	{
		Xml,
		Html,
		Text,
		Other,
		Unknown
	}

	private OutputMethod _method = OutputMethod.Unknown;

	private int _methodSId = int.MaxValue;

	private Encoding _encoding = Encoding.UTF8;

	private int _encodingSId = int.MaxValue;

	private string _version;

	private int _versionSId = int.MaxValue;

	private bool _omitXmlDecl;

	private int _omitXmlDeclSId = int.MaxValue;

	private bool _standalone;

	private int _standaloneSId = int.MaxValue;

	private string _doctypePublic;

	private int _doctypePublicSId = int.MaxValue;

	private string _doctypeSystem;

	private int _doctypeSystemSId = int.MaxValue;

	private bool _indent;

	private int _indentSId = int.MaxValue;

	private string _mediaType = "text/html";

	private int _mediaTypeSId = int.MaxValue;

	private Hashtable _cdataElements;

	internal OutputMethod Method => _method;

	internal bool OmitXmlDeclaration => _omitXmlDecl;

	internal bool HasStandalone => _standaloneSId != int.MaxValue;

	internal bool Standalone => _standalone;

	internal string DoctypePublic => _doctypePublic;

	internal string DoctypeSystem => _doctypeSystem;

	internal Hashtable CDataElements => _cdataElements;

	internal bool Indent => _indent;

	internal Encoding Encoding => _encoding;

	internal string MediaType => _mediaType;

	internal XsltOutput CreateDerivedOutput(OutputMethod method)
	{
		XsltOutput xsltOutput = (XsltOutput)MemberwiseClone();
		xsltOutput._method = method;
		if (method == OutputMethod.Html && _indentSId == int.MaxValue)
		{
			xsltOutput._indent = true;
		}
		return xsltOutput;
	}

	internal override void Compile(Compiler compiler)
	{
		CompileAttributes(compiler);
		CheckEmpty(compiler);
	}

	internal override bool CompileAttribute(Compiler compiler)
	{
		string localName = compiler.Input.LocalName;
		string value = compiler.Input.Value;
		if (Ref.Equal(localName, compiler.Atoms.Method))
		{
			if (compiler.Stylesheetid <= _methodSId)
			{
				_method = ParseOutputMethod(value, compiler);
				_methodSId = compiler.Stylesheetid;
				if (_indentSId == int.MaxValue)
				{
					_indent = _method == OutputMethod.Html;
				}
			}
		}
		else if (Ref.Equal(localName, compiler.Atoms.Version))
		{
			if (compiler.Stylesheetid <= _versionSId)
			{
				_version = value;
				_versionSId = compiler.Stylesheetid;
			}
		}
		else if (Ref.Equal(localName, compiler.Atoms.Encoding))
		{
			if (compiler.Stylesheetid <= _encodingSId)
			{
				try
				{
					_encoding = Encoding.GetEncoding(value);
					_encodingSId = compiler.Stylesheetid;
				}
				catch (NotSupportedException)
				{
				}
				catch (ArgumentException)
				{
				}
			}
		}
		else if (Ref.Equal(localName, compiler.Atoms.OmitXmlDeclaration))
		{
			if (compiler.Stylesheetid <= _omitXmlDeclSId)
			{
				_omitXmlDecl = compiler.GetYesNo(value);
				_omitXmlDeclSId = compiler.Stylesheetid;
			}
		}
		else if (Ref.Equal(localName, compiler.Atoms.Standalone))
		{
			if (compiler.Stylesheetid <= _standaloneSId)
			{
				_standalone = compiler.GetYesNo(value);
				_standaloneSId = compiler.Stylesheetid;
			}
		}
		else if (Ref.Equal(localName, compiler.Atoms.DocTypePublic))
		{
			if (compiler.Stylesheetid <= _doctypePublicSId)
			{
				_doctypePublic = value;
				_doctypePublicSId = compiler.Stylesheetid;
			}
		}
		else if (Ref.Equal(localName, compiler.Atoms.DocTypeSystem))
		{
			if (compiler.Stylesheetid <= _doctypeSystemSId)
			{
				_doctypeSystem = value;
				_doctypeSystemSId = compiler.Stylesheetid;
			}
		}
		else if (Ref.Equal(localName, compiler.Atoms.Indent))
		{
			if (compiler.Stylesheetid <= _indentSId)
			{
				_indent = compiler.GetYesNo(value);
				_indentSId = compiler.Stylesheetid;
			}
		}
		else if (Ref.Equal(localName, compiler.Atoms.MediaType))
		{
			if (compiler.Stylesheetid <= _mediaTypeSId)
			{
				_mediaType = value;
				_mediaTypeSId = compiler.Stylesheetid;
			}
		}
		else
		{
			if (!Ref.Equal(localName, compiler.Atoms.CDataSectionElements))
			{
				return false;
			}
			string[] array = XmlConvert.SplitString(value);
			if (_cdataElements == null)
			{
				_cdataElements = new Hashtable(array.Length);
			}
			for (int i = 0; i < array.Length; i++)
			{
				XmlQualifiedName xmlQualifiedName = compiler.CreateXmlQName(array[i]);
				_cdataElements[xmlQualifiedName] = xmlQualifiedName;
			}
		}
		return true;
	}

	internal override void Execute(Processor processor, ActionFrame frame)
	{
	}

	private static OutputMethod ParseOutputMethod(string value, Compiler compiler)
	{
		XmlQualifiedName xmlQualifiedName = compiler.CreateXPathQName(value);
		if (xmlQualifiedName.Namespace.Length != 0)
		{
			return OutputMethod.Other;
		}
		switch (xmlQualifiedName.Name)
		{
		case "xml":
			return OutputMethod.Xml;
		case "html":
			return OutputMethod.Html;
		case "text":
			return OutputMethod.Text;
		default:
			if (compiler.ForwardCompatibility)
			{
				return OutputMethod.Unknown;
			}
			throw XsltException.Create(System.SR.Xslt_InvalidAttrValue, "method", value);
		}
	}
}

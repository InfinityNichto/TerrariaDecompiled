using System.Data.Common;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace System.Data.SqlTypes;

[XmlSchemaProvider("GetXsdType")]
public sealed class SqlXml : INullable, IXmlSerializable
{
	private static readonly Func<Stream, XmlReaderSettings, XmlParserContext, XmlReader> s_sqlReaderDelegate = CreateSqlReaderDelegate();

	private static readonly XmlReaderSettings s_defaultXmlReaderSettings = new XmlReaderSettings
	{
		ConformanceLevel = ConformanceLevel.Fragment
	};

	private static readonly XmlReaderSettings s_defaultXmlReaderSettingsCloseInput = new XmlReaderSettings
	{
		ConformanceLevel = ConformanceLevel.Fragment,
		CloseInput = true
	};

	private static MethodInfo s_createSqlReaderMethodInfo;

	private MethodInfo _createSqlReaderMethodInfo;

	private bool _fNotNull;

	private Stream _stream;

	private bool _firstCreateReader;

	private static MethodInfo CreateSqlReaderMethodInfo
	{
		get
		{
			if (s_createSqlReaderMethodInfo == null)
			{
				s_createSqlReaderMethodInfo = typeof(XmlReader).GetMethod("CreateSqlReader", BindingFlags.Static | BindingFlags.NonPublic);
			}
			return s_createSqlReaderMethodInfo;
		}
	}

	public bool IsNull => !_fNotNull;

	public string Value
	{
		get
		{
			if (IsNull)
			{
				throw new SqlNullValueException();
			}
			StringWriter stringWriter = new StringWriter((IFormatProvider?)null);
			XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
			xmlWriterSettings.CloseOutput = false;
			xmlWriterSettings.ConformanceLevel = ConformanceLevel.Fragment;
			XmlWriter xmlWriter = XmlWriter.Create(stringWriter, xmlWriterSettings);
			XmlReader xmlReader = CreateReader();
			if (xmlReader.ReadState == ReadState.Initial)
			{
				xmlReader.Read();
			}
			while (!xmlReader.EOF)
			{
				xmlWriter.WriteNode(xmlReader, defattr: true);
			}
			xmlWriter.Flush();
			return stringWriter.ToString();
		}
	}

	public static SqlXml Null => new SqlXml(fNull: true);

	public SqlXml()
	{
		SetNull();
	}

	private SqlXml(bool fNull)
	{
		SetNull();
	}

	public SqlXml(XmlReader? value)
	{
		if (value == null)
		{
			SetNull();
			return;
		}
		_fNotNull = true;
		_firstCreateReader = true;
		_stream = CreateMemoryStreamFromXmlReader(value);
	}

	public SqlXml(Stream? value)
	{
		if (value == null)
		{
			SetNull();
			return;
		}
		_firstCreateReader = true;
		_fNotNull = true;
		_stream = value;
	}

	public XmlReader CreateReader()
	{
		if (IsNull)
		{
			throw new SqlNullValueException();
		}
		SqlXmlStreamWrapper sqlXmlStreamWrapper = new SqlXmlStreamWrapper(_stream);
		if ((!_firstCreateReader || sqlXmlStreamWrapper.CanSeek) && sqlXmlStreamWrapper.Position != 0L)
		{
			sqlXmlStreamWrapper.Seek(0L, SeekOrigin.Begin);
		}
		if (_createSqlReaderMethodInfo == null)
		{
			_createSqlReaderMethodInfo = CreateSqlReaderMethodInfo;
		}
		XmlReader result = CreateSqlXmlReader(sqlXmlStreamWrapper);
		_firstCreateReader = false;
		return result;
	}

	internal static XmlReader CreateSqlXmlReader(Stream stream, bool closeInput = false, bool throwTargetInvocationExceptions = false)
	{
		XmlReaderSettings arg = (closeInput ? s_defaultXmlReaderSettingsCloseInput : s_defaultXmlReaderSettings);
		try
		{
			return s_sqlReaderDelegate(stream, arg, null);
		}
		catch (Exception ex)
		{
			if (!throwTargetInvocationExceptions || !ADP.IsCatchableExceptionType(ex))
			{
				throw;
			}
			throw new TargetInvocationException(ex);
		}
	}

	private static Func<Stream, XmlReaderSettings, XmlParserContext, XmlReader> CreateSqlReaderDelegate()
	{
		return CreateSqlReaderMethodInfo.CreateDelegate<Func<Stream, XmlReaderSettings, XmlParserContext, XmlReader>>();
	}

	private void SetNull()
	{
		_fNotNull = false;
		_stream = null;
		_firstCreateReader = true;
	}

	private Stream CreateMemoryStreamFromXmlReader(XmlReader reader)
	{
		XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
		xmlWriterSettings.CloseOutput = false;
		xmlWriterSettings.ConformanceLevel = ConformanceLevel.Fragment;
		xmlWriterSettings.Encoding = Encoding.GetEncoding("utf-16");
		xmlWriterSettings.OmitXmlDeclaration = true;
		MemoryStream memoryStream = new MemoryStream();
		XmlWriter xmlWriter = XmlWriter.Create(memoryStream, xmlWriterSettings);
		if (reader.ReadState == ReadState.Closed)
		{
			throw new InvalidOperationException(SQLResource.ClosedXmlReaderMessage);
		}
		if (reader.ReadState == ReadState.Initial)
		{
			reader.Read();
		}
		while (!reader.EOF)
		{
			xmlWriter.WriteNode(reader, defattr: true);
		}
		xmlWriter.Flush();
		memoryStream.Seek(0L, SeekOrigin.Begin);
		return memoryStream;
	}

	XmlSchema IXmlSerializable.GetSchema()
	{
		return null;
	}

	void IXmlSerializable.ReadXml(XmlReader r)
	{
		string attribute = r.GetAttribute("nil", "http://www.w3.org/2001/XMLSchema-instance");
		if (attribute != null && XmlConvert.ToBoolean(attribute))
		{
			r.ReadInnerXml();
			SetNull();
			return;
		}
		_fNotNull = true;
		_firstCreateReader = true;
		_stream = new MemoryStream();
		StreamWriter streamWriter = new StreamWriter(_stream);
		streamWriter.Write(r.ReadInnerXml());
		streamWriter.Flush();
		if (_stream.CanSeek)
		{
			_stream.Seek(0L, SeekOrigin.Begin);
		}
	}

	void IXmlSerializable.WriteXml(XmlWriter writer)
	{
		if (IsNull)
		{
			writer.WriteAttributeString("xsi", "nil", "http://www.w3.org/2001/XMLSchema-instance", "true");
		}
		else
		{
			XmlReader xmlReader = CreateReader();
			if (xmlReader.ReadState == ReadState.Initial)
			{
				xmlReader.Read();
			}
			while (!xmlReader.EOF)
			{
				writer.WriteNode(xmlReader, defattr: true);
			}
		}
		writer.Flush();
	}

	public static XmlQualifiedName GetXsdType(XmlSchemaSet schemaSet)
	{
		return new XmlQualifiedName("anyType", "http://www.w3.org/2001/XMLSchema");
	}
}

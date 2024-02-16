using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Xml.Schema;

[Serializable]
[TypeForwardedFrom("System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class XmlSchemaException : SystemException
{
	private readonly string _res;

	private readonly string[] _args;

	private string _sourceUri;

	private int _lineNumber;

	private int _linePosition;

	private XmlSchemaObject _sourceSchemaObject;

	private readonly string _message;

	internal string? GetRes => _res;

	internal string?[]? Args => _args;

	public string? SourceUri => _sourceUri;

	public int LineNumber => _lineNumber;

	public int LinePosition => _linePosition;

	public XmlSchemaObject? SourceSchemaObject => _sourceSchemaObject;

	public override string Message
	{
		get
		{
			if (_message != null)
			{
				return _message;
			}
			return base.Message;
		}
	}

	protected XmlSchemaException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		_res = (string)info.GetValue("res", typeof(string));
		_args = (string[])info.GetValue("args", typeof(string[]));
		_sourceUri = (string)info.GetValue("sourceUri", typeof(string));
		_lineNumber = (int)info.GetValue("lineNumber", typeof(int));
		_linePosition = (int)info.GetValue("linePosition", typeof(int));
		string text = null;
		SerializationInfoEnumerator enumerator = info.GetEnumerator();
		while (enumerator.MoveNext())
		{
			SerializationEntry current = enumerator.Current;
			if (current.Name == "version")
			{
				text = (string)current.Value;
			}
		}
		if (text == null)
		{
			_message = CreateMessage(_res, _args);
		}
		else
		{
			_message = null;
		}
	}

	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		base.GetObjectData(info, context);
		info.AddValue("res", _res);
		info.AddValue("args", _args);
		info.AddValue("sourceUri", _sourceUri);
		info.AddValue("lineNumber", _lineNumber);
		info.AddValue("linePosition", _linePosition);
		info.AddValue("version", "2.0");
	}

	public XmlSchemaException()
		: this(null)
	{
	}

	public XmlSchemaException(string? message)
		: this(message, (Exception?)null, 0, 0)
	{
	}

	public XmlSchemaException(string? message, Exception? innerException)
		: this(message, innerException, 0, 0)
	{
	}

	public XmlSchemaException(string? message, Exception? innerException, int lineNumber, int linePosition)
		: this((message == null) ? System.SR.Sch_DefaultException : System.SR.Xml_UserException, new string[1] { message }, innerException, null, lineNumber, linePosition, null)
	{
	}

	internal XmlSchemaException(string res, string[] args)
		: this(res, args, null, null, 0, 0, null)
	{
	}

	internal XmlSchemaException(string res, string arg)
		: this(res, new string[1] { arg }, null, null, 0, 0, null)
	{
	}

	internal XmlSchemaException(string res, string arg, string sourceUri, int lineNumber, int linePosition)
		: this(res, new string[1] { arg }, null, sourceUri, lineNumber, linePosition, null)
	{
	}

	internal XmlSchemaException(string res, string sourceUri, int lineNumber, int linePosition)
		: this(res, null, null, sourceUri, lineNumber, linePosition, null)
	{
	}

	internal XmlSchemaException(string res, string[] args, string sourceUri, int lineNumber, int linePosition)
		: this(res, args, null, sourceUri, lineNumber, linePosition, null)
	{
	}

	internal XmlSchemaException(string res, XmlSchemaObject source)
		: this(res, (string[])null, source)
	{
	}

	internal XmlSchemaException(string res, string arg, XmlSchemaObject source)
		: this(res, new string[1] { arg }, source)
	{
	}

	internal XmlSchemaException(string res, string[] args, XmlSchemaObject source)
		: this(res, args, null, source.SourceUri, source.LineNumber, source.LinePosition, source)
	{
	}

	internal XmlSchemaException(string res, string[] args, Exception innerException, string sourceUri, int lineNumber, int linePosition, XmlSchemaObject source)
		: base(CreateMessage(res, args), innerException)
	{
		base.HResult = -2146231999;
		_res = res;
		_args = args;
		_sourceUri = sourceUri;
		_lineNumber = lineNumber;
		_linePosition = linePosition;
		_sourceSchemaObject = source;
	}

	internal static string CreateMessage(string res, string[] args)
	{
		try
		{
			if (args == null)
			{
				return res;
			}
			return string.Format(res ?? string.Empty, args);
		}
		catch (MissingManifestResourceException)
		{
			return "UNKNOWN(" + res + ")";
		}
	}

	internal void SetSource(string sourceUri, int lineNumber, int linePosition)
	{
		_sourceUri = sourceUri;
		_lineNumber = lineNumber;
		_linePosition = linePosition;
	}

	internal void SetSchemaObject(XmlSchemaObject source)
	{
		_sourceSchemaObject = source;
	}

	internal void SetSource(XmlSchemaObject source)
	{
		_sourceSchemaObject = source;
		_sourceUri = source.SourceUri;
		_lineNumber = source.LineNumber;
		_linePosition = source.LinePosition;
	}
}

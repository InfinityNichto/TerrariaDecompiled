using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Xml.Xsl;

[Serializable]
[TypeForwardedFrom("System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class XsltException : SystemException
{
	private readonly string _res;

	private readonly string[] _args;

	private readonly string _sourceUri;

	private readonly int _lineNumber;

	private readonly int _linePosition;

	private readonly string _message;

	public virtual string? SourceUri => _sourceUri;

	public virtual int LineNumber => _lineNumber;

	public virtual int LinePosition => _linePosition;

	public override string Message => _message ?? base.Message;

	protected XsltException(SerializationInfo info, StreamingContext context)
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
			_message = CreateMessage(_res, _args, _sourceUri, _lineNumber, _linePosition);
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

	public XsltException()
		: this(string.Empty, null)
	{
	}

	public XsltException(string message)
		: this(message, null)
	{
	}

	public XsltException(string message, Exception? innerException)
		: this(System.SR.Xml_UserException, new string[1] { message }, null, 0, 0, innerException)
	{
	}

	internal static XsltException Create(string res, params string[] args)
	{
		return new XsltException(res, args, null, 0, 0, null);
	}

	internal static XsltException Create(string res, string[] args, Exception inner)
	{
		return new XsltException(res, args, null, 0, 0, inner);
	}

	internal XsltException(string res, string[] args, string sourceUri, int lineNumber, int linePosition, Exception inner)
		: base(CreateMessage(res, args, sourceUri, lineNumber, linePosition), inner)
	{
		base.HResult = -2146231998;
		_res = res;
		_sourceUri = sourceUri;
		_lineNumber = lineNumber;
		_linePosition = linePosition;
	}

	private static string CreateMessage(string res, string[] args, string sourceUri, int lineNumber, int linePosition)
	{
		try
		{
			string text = FormatMessage(res, args);
			if (res != System.SR.Xslt_CompileError && lineNumber != 0)
			{
				text = text + " " + FormatMessage(System.SR.Xml_ErrorFilePosition, sourceUri, lineNumber.ToString(CultureInfo.InvariantCulture), linePosition.ToString(CultureInfo.InvariantCulture));
			}
			return text;
		}
		catch (MissingManifestResourceException)
		{
			return "UNKNOWN(" + res + ")";
		}
	}

	[return: NotNullIfNotNull("key")]
	private static string FormatMessage(string key, params string[] args)
	{
		string text = key;
		if (text != null && args != null)
		{
			text = string.Format(CultureInfo.InvariantCulture, text, args);
		}
		return text;
	}
}

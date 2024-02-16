using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Xml;

[Serializable]
[TypeForwardedFrom("System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class XmlException : SystemException
{
	private readonly string _res;

	private readonly string[] _args;

	private readonly int _lineNumber;

	private readonly int _linePosition;

	private readonly string _sourceUri;

	private readonly string _message;

	public int LineNumber => _lineNumber;

	public int LinePosition => _linePosition;

	public string? SourceUri => _sourceUri;

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

	internal string ResString => _res;

	protected XmlException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		_res = (string)info.GetValue("res", typeof(string));
		_args = (string[])info.GetValue("args", typeof(string[]));
		_lineNumber = (int)info.GetValue("lineNumber", typeof(int));
		_linePosition = (int)info.GetValue("linePosition", typeof(int));
		_sourceUri = string.Empty;
		string text = null;
		SerializationInfoEnumerator enumerator = info.GetEnumerator();
		while (enumerator.MoveNext())
		{
			SerializationEntry current = enumerator.Current;
			string name = current.Name;
			if (!(name == "sourceUri"))
			{
				if (name == "version")
				{
					text = (string)current.Value;
				}
			}
			else
			{
				_sourceUri = (string)current.Value;
			}
		}
		if (text == null)
		{
			_message = CreateMessage(_res, _args, _lineNumber, _linePosition);
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
		info.AddValue("lineNumber", _lineNumber);
		info.AddValue("linePosition", _linePosition);
		info.AddValue("sourceUri", _sourceUri);
		info.AddValue("version", "2.0");
	}

	public XmlException()
		: this(null)
	{
	}

	public XmlException(string? message)
		: this(message, (Exception?)null, 0, 0)
	{
	}

	public XmlException(string? message, Exception? innerException)
		: this(message, innerException, 0, 0)
	{
	}

	public XmlException(string? message, Exception? innerException, int lineNumber, int linePosition)
		: this(message, innerException, lineNumber, linePosition, null)
	{
	}

	internal XmlException(string message, Exception innerException, int lineNumber, int linePosition, string sourceUri)
		: base(FormatUserMessage(message, lineNumber, linePosition), innerException)
	{
		base.HResult = -2146232000;
		_res = ((message == null) ? System.SR.Xml_DefaultException : System.SR.Xml_UserException);
		_args = new string[1] { message };
		_sourceUri = sourceUri;
		_lineNumber = lineNumber;
		_linePosition = linePosition;
	}

	internal XmlException(string res, string[] args)
		: this(res, args, null, 0, 0, null)
	{
	}

	internal XmlException(string res, string arg)
		: this(res, new string[1] { arg }, null, 0, 0, null)
	{
	}

	internal XmlException(string res, string arg, string sourceUri)
		: this(res, new string[1] { arg }, null, 0, 0, sourceUri)
	{
	}

	internal XmlException(string res, string arg, IXmlLineInfo lineInfo)
		: this(res, new string[1] { arg }, lineInfo, null)
	{
	}

	internal XmlException(string res, string arg, Exception innerException, IXmlLineInfo lineInfo)
		: this(res, new string[1] { arg }, innerException, lineInfo?.LineNumber ?? 0, lineInfo?.LinePosition ?? 0, null)
	{
	}

	internal XmlException(string res, string[] args, IXmlLineInfo lineInfo)
		: this(res, args, lineInfo, null)
	{
	}

	internal XmlException(string res, string[] args, IXmlLineInfo lineInfo, string sourceUri)
		: this(res, args, null, lineInfo?.LineNumber ?? 0, lineInfo?.LinePosition ?? 0, sourceUri)
	{
	}

	internal XmlException(string res, string arg, int lineNumber, int linePosition)
		: this(res, new string[1] { arg }, null, lineNumber, linePosition, null)
	{
	}

	internal XmlException(string res, string arg, int lineNumber, int linePosition, string sourceUri)
		: this(res, new string[1] { arg }, null, lineNumber, linePosition, sourceUri)
	{
	}

	internal XmlException(string res, string[] args, int lineNumber, int linePosition)
		: this(res, args, null, lineNumber, linePosition, null)
	{
	}

	internal XmlException(string res, string[] args, int lineNumber, int linePosition, string sourceUri)
		: this(res, args, null, lineNumber, linePosition, sourceUri)
	{
	}

	internal XmlException(string res, string[] args, Exception innerException, int lineNumber, int linePosition)
		: this(res, args, innerException, lineNumber, linePosition, null)
	{
	}

	internal XmlException(string res, string[] args, Exception innerException, int lineNumber, int linePosition, string sourceUri)
		: base(CreateMessage(res, args, lineNumber, linePosition), innerException)
	{
		base.HResult = -2146232000;
		_res = res;
		_args = args;
		_sourceUri = sourceUri;
		_lineNumber = lineNumber;
		_linePosition = linePosition;
	}

	private static string FormatUserMessage(string message, int lineNumber, int linePosition)
	{
		if (message == null)
		{
			return CreateMessage(System.SR.Xml_DefaultException, null, lineNumber, linePosition);
		}
		if (lineNumber == 0 && linePosition == 0)
		{
			return message;
		}
		return CreateMessage(System.SR.Xml_UserException, new string[1] { message }, lineNumber, linePosition);
	}

	private static string CreateMessage(string res, string[] args, int lineNumber, int linePosition)
	{
		try
		{
			string text;
			if (args != null)
			{
				object[] args2 = args;
				text = string.Format(res, args2);
			}
			else
			{
				text = res;
			}
			string text2 = text;
			if (lineNumber != 0)
			{
				string text3 = lineNumber.ToString(CultureInfo.InvariantCulture);
				string text4 = linePosition.ToString(CultureInfo.InvariantCulture);
				string xml_MessageWithErrorPosition = System.SR.Xml_MessageWithErrorPosition;
				object[] args2 = new string[3] { text2, text3, text4 };
				text2 = System.SR.Format(xml_MessageWithErrorPosition, args2);
			}
			return text2;
		}
		catch (MissingManifestResourceException)
		{
			return "UNKNOWN(" + res + ")";
		}
	}

	internal static string[] BuildCharExceptionArgs(string data, int invCharIndex)
	{
		return BuildCharExceptionArgs(data[invCharIndex], (invCharIndex + 1 < data.Length) ? data[invCharIndex + 1] : '\0');
	}

	internal static string[] BuildCharExceptionArgs(char[] data, int length, int invCharIndex)
	{
		return BuildCharExceptionArgs(data[invCharIndex], (invCharIndex + 1 < length) ? data[invCharIndex + 1] : '\0');
	}

	internal static string[] BuildCharExceptionArgs(char invChar, char nextChar)
	{
		string[] array = new string[2];
		if (XmlCharType.IsHighSurrogate(invChar) && nextChar != 0)
		{
			int value = XmlCharType.CombineSurrogateChar(nextChar, invChar);
			string[] array2 = array;
			Span<char> span = stackalloc char[2] { invChar, nextChar };
			array2[0] = new string(span);
			array[1] = $"0x{value:X2}";
		}
		else
		{
			if (invChar == '\0')
			{
				array[0] = ".";
			}
			else
			{
				array[0] = invChar.ToString();
			}
			array[1] = $"0x{invChar:X2}";
		}
		return array;
	}

	internal static bool IsCatchableException(Exception e)
	{
		if (!(e is OutOfMemoryException))
		{
			return !(e is NullReferenceException);
		}
		return false;
	}
}

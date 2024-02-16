using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Xml.XPath;

[Serializable]
[TypeForwardedFrom("System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class XPathException : SystemException
{
	private readonly string _res;

	private readonly string[] _args;

	private readonly string _message;

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

	protected XPathException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		_res = (string)info.GetValue("res", typeof(string));
		_args = (string[])info.GetValue("args", typeof(string[]));
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
		info.AddValue("version", "2.0");
	}

	public XPathException()
		: this(string.Empty, (Exception?)null)
	{
	}

	public XPathException(string? message)
		: this(message, (Exception?)null)
	{
	}

	public XPathException(string? message, Exception? innerException)
		: this(System.SR.Xml_UserException, new string[1] { message }, innerException)
	{
	}

	internal static XPathException Create(string res)
	{
		return new XPathException(res, (string[])null);
	}

	internal static XPathException Create(string res, string arg)
	{
		return new XPathException(res, new string[1] { arg });
	}

	internal static XPathException Create(string res, string arg, string arg2)
	{
		return new XPathException(res, new string[2] { arg, arg2 });
	}

	internal static XPathException Create(string res, string arg, Exception innerException)
	{
		return new XPathException(res, new string[1] { arg }, innerException);
	}

	private XPathException(string res, string[] args)
		: this(res, args, null)
	{
	}

	private XPathException(string res, string[] args, Exception inner)
		: base(CreateMessage(res, args), inner)
	{
		base.HResult = -2146231997;
		_res = res;
		_args = args;
	}

	private static string CreateMessage(string res, string[] args)
	{
		try
		{
			string text;
			if (args != null)
			{
				text = string.Format(res, args);
			}
			else
			{
				text = res;
			}
			string text2 = text;
			if (text2 == null)
			{
				text2 = "UNKNOWN(" + res + ")";
			}
			return text2;
		}
		catch (MissingManifestResourceException)
		{
			return "UNKNOWN(" + res + ")";
		}
	}
}

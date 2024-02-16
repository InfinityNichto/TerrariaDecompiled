using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class BadImageFormatException : SystemException
{
	private readonly string _fileName;

	private readonly string _fusionLog;

	public override string Message
	{
		get
		{
			SetMessageField();
			return _message;
		}
	}

	public string? FileName => _fileName;

	public string? FusionLog => _fusionLog;

	private BadImageFormatException(string fileName, int hResult)
		: base(null)
	{
		base.HResult = hResult;
		_fileName = fileName;
		SetMessageField();
	}

	public BadImageFormatException()
		: base(SR.Arg_BadImageFormatException)
	{
		base.HResult = -2147024885;
	}

	public BadImageFormatException(string? message)
		: base(message)
	{
		base.HResult = -2147024885;
	}

	public BadImageFormatException(string? message, Exception? inner)
		: base(message, inner)
	{
		base.HResult = -2147024885;
	}

	public BadImageFormatException(string? message, string? fileName)
		: base(message)
	{
		base.HResult = -2147024885;
		_fileName = fileName;
	}

	public BadImageFormatException(string? message, string? fileName, Exception? inner)
		: base(message, inner)
	{
		base.HResult = -2147024885;
		_fileName = fileName;
	}

	protected BadImageFormatException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		_fileName = info.GetString("BadImageFormat_FileName");
		_fusionLog = info.GetString("BadImageFormat_FusionLog");
	}

	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		base.GetObjectData(info, context);
		info.AddValue("BadImageFormat_FileName", _fileName, typeof(string));
		info.AddValue("BadImageFormat_FusionLog", _fusionLog, typeof(string));
	}

	private void SetMessageField()
	{
		if (_message == null)
		{
			if (_fileName == null && base.HResult == -2146233088)
			{
				_message = SR.Arg_BadImageFormatException;
			}
			else
			{
				_message = FileLoadException.FormatFileLoadExceptionMessage(_fileName, base.HResult);
			}
		}
	}

	public override string ToString()
	{
		string text = GetType().ToString() + ": " + Message;
		if (!string.IsNullOrEmpty(_fileName))
		{
			text = text + "\r\n" + SR.Format(SR.IO_FileName_Name, _fileName);
		}
		if (base.InnerException != null)
		{
			text = text + " ---> " + base.InnerException.ToString();
		}
		if (StackTrace != null)
		{
			text = text + "\r\n" + StackTrace;
		}
		if (_fusionLog != null)
		{
			if (text == null)
			{
				text = " ";
			}
			text = text + "\r\n\r\n" + _fusionLog;
		}
		return text;
	}
}

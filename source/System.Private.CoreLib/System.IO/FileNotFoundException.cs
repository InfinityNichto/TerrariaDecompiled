using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.IO;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class FileNotFoundException : IOException
{
	public override string Message
	{
		get
		{
			SetMessageField();
			return _message;
		}
	}

	public string? FileName { get; }

	public string? FusionLog { get; }

	private FileNotFoundException(string fileName, int hResult)
		: base(null)
	{
		base.HResult = hResult;
		FileName = fileName;
		SetMessageField();
	}

	public FileNotFoundException()
		: base(SR.IO_FileNotFound)
	{
		base.HResult = -2147024894;
	}

	public FileNotFoundException(string? message)
		: base(message)
	{
		base.HResult = -2147024894;
	}

	public FileNotFoundException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2147024894;
	}

	public FileNotFoundException(string? message, string? fileName)
		: base(message)
	{
		base.HResult = -2147024894;
		FileName = fileName;
	}

	public FileNotFoundException(string? message, string? fileName, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2147024894;
		FileName = fileName;
	}

	private void SetMessageField()
	{
		if (_message == null)
		{
			if (FileName == null && base.HResult == -2146233088)
			{
				_message = SR.IO_FileNotFound;
			}
			else if (FileName != null)
			{
				_message = FileLoadException.FormatFileLoadExceptionMessage(FileName, base.HResult);
			}
		}
	}

	public override string ToString()
	{
		string text = GetType().ToString() + ": " + Message;
		if (!string.IsNullOrEmpty(FileName))
		{
			text = text + "\r\n" + SR.Format(SR.IO_FileName_Name, FileName);
		}
		if (base.InnerException != null)
		{
			text = text + "\r\n ---> " + base.InnerException.ToString();
		}
		if (StackTrace != null)
		{
			text = text + "\r\n" + StackTrace;
		}
		if (FusionLog != null)
		{
			if (text == null)
			{
				text = " ";
			}
			text = text + "\r\n\r\n" + FusionLog;
		}
		return text;
	}

	protected FileNotFoundException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		FileName = info.GetString("FileNotFound_FileName");
		FusionLog = info.GetString("FileNotFound_FusionLog");
	}

	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		base.GetObjectData(info, context);
		info.AddValue("FileNotFound_FileName", FileName, typeof(string));
		info.AddValue("FileNotFound_FusionLog", FusionLog, typeof(string));
	}
}

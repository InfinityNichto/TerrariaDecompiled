using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class ArgumentException : SystemException
{
	private readonly string _paramName;

	public override string Message
	{
		get
		{
			SetMessageField();
			string text = base.Message;
			if (!string.IsNullOrEmpty(_paramName))
			{
				text = text + " " + SR.Format(SR.Arg_ParamName_Name, _paramName);
			}
			return text;
		}
	}

	public virtual string? ParamName => _paramName;

	public ArgumentException()
		: base(SR.Arg_ArgumentException)
	{
		base.HResult = -2147024809;
	}

	public ArgumentException(string? message)
		: base(message)
	{
		base.HResult = -2147024809;
	}

	public ArgumentException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2147024809;
	}

	public ArgumentException(string? message, string? paramName, Exception? innerException)
		: base(message, innerException)
	{
		_paramName = paramName;
		base.HResult = -2147024809;
	}

	public ArgumentException(string? message, string? paramName)
		: base(message)
	{
		_paramName = paramName;
		base.HResult = -2147024809;
	}

	protected ArgumentException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		_paramName = info.GetString("ParamName");
	}

	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		base.GetObjectData(info, context);
		info.AddValue("ParamName", _paramName, typeof(string));
	}

	private void SetMessageField()
	{
		if (_message == null && base.HResult == -2147024809)
		{
			_message = SR.Arg_ArgumentException;
		}
	}
}

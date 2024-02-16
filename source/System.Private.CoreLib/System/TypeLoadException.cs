using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class TypeLoadException : SystemException, ISerializable
{
	private string _className;

	private string _assemblyName;

	private readonly string _messageArg;

	private readonly int _resourceId;

	public override string Message
	{
		get
		{
			SetMessageField();
			return _message;
		}
	}

	public string TypeName => _className ?? string.Empty;

	private TypeLoadException(string className, string assemblyName, string messageArg, int resourceId)
		: base(null)
	{
		base.HResult = -2146233054;
		_className = className;
		_assemblyName = assemblyName;
		_messageArg = messageArg;
		_resourceId = resourceId;
		SetMessageField();
	}

	private void SetMessageField()
	{
		if (_message != null)
		{
			return;
		}
		if (_className == null && _resourceId == 0)
		{
			_message = SR.Arg_TypeLoadException;
			return;
		}
		if (_assemblyName == null)
		{
			_assemblyName = SR.IO_UnknownFileName;
		}
		if (_className == null)
		{
			_className = SR.IO_UnknownFileName;
		}
		string s = null;
		GetTypeLoadExceptionMessage(_resourceId, new StringHandleOnStack(ref s));
		_message = string.Format(s, _className, _assemblyName, _messageArg);
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void GetTypeLoadExceptionMessage(int resourceId, StringHandleOnStack retString);

	public TypeLoadException()
		: base(SR.Arg_TypeLoadException)
	{
		base.HResult = -2146233054;
	}

	public TypeLoadException(string? message)
		: base(message)
	{
		base.HResult = -2146233054;
	}

	public TypeLoadException(string? message, Exception? inner)
		: base(message, inner)
	{
		base.HResult = -2146233054;
	}

	protected TypeLoadException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		_className = info.GetString("TypeLoadClassName");
		_assemblyName = info.GetString("TypeLoadAssemblyName");
		_messageArg = info.GetString("TypeLoadMessageArg");
		_resourceId = info.GetInt32("TypeLoadResourceID");
	}

	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		base.GetObjectData(info, context);
		info.AddValue("TypeLoadClassName", _className, typeof(string));
		info.AddValue("TypeLoadAssemblyName", _assemblyName, typeof(string));
		info.AddValue("TypeLoadMessageArg", _messageArg, typeof(string));
		info.AddValue("TypeLoadResourceID", _resourceId);
	}
}

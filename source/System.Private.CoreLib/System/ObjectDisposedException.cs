using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class ObjectDisposedException : InvalidOperationException
{
	private readonly string _objectName;

	public override string Message
	{
		get
		{
			string objectName = ObjectName;
			if (string.IsNullOrEmpty(objectName))
			{
				return base.Message;
			}
			string text = SR.Format(SR.ObjectDisposed_ObjectName_Name, objectName);
			return base.Message + "\r\n" + text;
		}
	}

	public string ObjectName => _objectName ?? string.Empty;

	private ObjectDisposedException()
		: this(null, SR.ObjectDisposed_Generic)
	{
	}

	public ObjectDisposedException(string? objectName)
		: this(objectName, SR.ObjectDisposed_Generic)
	{
	}

	public ObjectDisposedException(string? objectName, string? message)
		: base(message)
	{
		base.HResult = -2146232798;
		_objectName = objectName;
	}

	public ObjectDisposedException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146232798;
	}

	protected ObjectDisposedException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		_objectName = info.GetString("ObjectName");
	}

	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		base.GetObjectData(info, context);
		info.AddValue("ObjectName", ObjectName, typeof(string));
	}
}

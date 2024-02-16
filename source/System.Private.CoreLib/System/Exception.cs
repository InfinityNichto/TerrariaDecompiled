using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class Exception : ISerializable
{
	internal enum ExceptionMessageKind
	{
		ThreadAbort = 1,
		ThreadInterrupted,
		OutOfMemory
	}

	internal readonly struct DispatchState
	{
		public readonly byte[] StackTrace;

		public readonly object[] DynamicMethods;

		public readonly string RemoteStackTrace;

		public readonly UIntPtr IpForWatsonBuckets;

		public readonly byte[] WatsonBuckets;

		public DispatchState(byte[] stackTrace, object[] dynamicMethods, string remoteStackTrace, UIntPtr ipForWatsonBuckets, byte[] watsonBuckets)
		{
			StackTrace = stackTrace;
			DynamicMethods = dynamicMethods;
			RemoteStackTrace = remoteStackTrace;
			IpForWatsonBuckets = ipForWatsonBuckets;
			WatsonBuckets = watsonBuckets;
		}
	}

	private MethodBase _exceptionMethod;

	internal string _message;

	private IDictionary _data;

	private readonly Exception _innerException;

	private string _helpURL;

	private byte[] _stackTrace;

	private byte[] _watsonBuckets;

	private string _stackTraceString;

	private string _remoteStackTraceString;

	private readonly object[] _dynamicMethods;

	private string _source;

	private UIntPtr _ipForWatsonBuckets;

	private readonly IntPtr _xptrs;

	private readonly int _xcode = -532462766;

	private int _HResult;

	private const int _COMPlusExceptionCode = -532462766;

	private protected const string InnerExceptionPrefix = " ---> ";

	public MethodBase? TargetSite
	{
		get
		{
			if (_exceptionMethod != null)
			{
				return _exceptionMethod;
			}
			if (_stackTrace == null)
			{
				return null;
			}
			_exceptionMethod = GetExceptionMethodFromStackTrace();
			return _exceptionMethod;
		}
	}

	private bool HasBeenThrown => _stackTrace != null;

	private object? SerializationWatsonBuckets => _watsonBuckets;

	public virtual string Message => _message ?? SR.Format(SR.Exception_WasThrown, GetClassName());

	public virtual IDictionary Data => _data ?? (_data = CreateDataContainer());

	public Exception? InnerException => _innerException;

	public virtual string? HelpLink
	{
		get
		{
			return _helpURL;
		}
		set
		{
			_helpURL = value;
		}
	}

	public virtual string? Source
	{
		get
		{
			return _source ?? (_source = CreateSourceName());
		}
		set
		{
			_source = value;
		}
	}

	public int HResult
	{
		get
		{
			return _HResult;
		}
		set
		{
			_HResult = value;
		}
	}

	public virtual string? StackTrace
	{
		get
		{
			string stackTraceString = _stackTraceString;
			string remoteStackTraceString = _remoteStackTraceString;
			if (stackTraceString != null)
			{
				return remoteStackTraceString + stackTraceString;
			}
			if (!HasBeenThrown)
			{
				return remoteStackTraceString;
			}
			return remoteStackTraceString + GetStackTrace();
		}
	}

	private string? SerializationStackTraceString
	{
		get
		{
			string text = _stackTraceString;
			if (text == null && HasBeenThrown)
			{
				text = GetStackTrace();
			}
			return text;
		}
	}

	[Obsolete("BinaryFormatter serialization is obsolete and should not be used. See https://aka.ms/binaryformatter for more information.", DiagnosticId = "SYSLIB0011", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	protected event EventHandler<SafeSerializationEventArgs>? SerializeObjectState
	{
		add
		{
			throw new PlatformNotSupportedException(SR.PlatformNotSupported_SecureBinarySerialization);
		}
		remove
		{
			throw new PlatformNotSupportedException(SR.PlatformNotSupported_SecureBinarySerialization);
		}
	}

	private IDictionary CreateDataContainer()
	{
		if (IsImmutableAgileException(this))
		{
			return new EmptyReadOnlyDictionaryInternal();
		}
		return new ListDictionaryInternal();
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern bool IsImmutableAgileException(Exception e);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern IRuntimeMethodInfo GetMethodFromStackTrace(object stackTrace);

	private MethodBase GetExceptionMethodFromStackTrace()
	{
		IRuntimeMethodInfo methodFromStackTrace = GetMethodFromStackTrace(_stackTrace);
		if (methodFromStackTrace == null)
		{
			return null;
		}
		return RuntimeType.GetMethodBase(methodFromStackTrace);
	}

	private string CreateSourceName()
	{
		StackTrace stackTrace = new StackTrace(this, fNeedFileInfo: false);
		if (stackTrace.FrameCount > 0)
		{
			StackFrame frame = stackTrace.GetFrame(0);
			MethodBase method = frame.GetMethod();
			Module module = method.Module;
			RuntimeModule runtimeModule = module as RuntimeModule;
			if ((object)runtimeModule == null)
			{
				if (!(module is ModuleBuilder moduleBuilder))
				{
					throw new ArgumentException(SR.Argument_MustBeRuntimeReflectionObject);
				}
				runtimeModule = moduleBuilder.InternalModule;
			}
			return runtimeModule.GetRuntimeAssembly().GetSimpleName();
		}
		return null;
	}

	[OnDeserialized]
	private void OnDeserialized(StreamingContext context)
	{
		_stackTrace = null;
		_ipForWatsonBuckets = UIntPtr.Zero;
	}

	internal void InternalPreserveStackTrace()
	{
		_ = Source;
		string stackTrace = StackTrace;
		if (!string.IsNullOrEmpty(stackTrace))
		{
			_remoteStackTraceString = stackTrace + "\r\n";
		}
		_stackTrace = null;
		_stackTraceString = null;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void PrepareForForeignExceptionRaise();

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void GetStackTracesDeepCopy(Exception exception, out byte[] currentStackTrace, out object[] dynamicMethodArray);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void SaveStackTracesFromDeepCopy(Exception exception, byte[] currentStackTrace, object[] dynamicMethodArray);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern uint GetExceptionCount();

	internal void RestoreDispatchState(in DispatchState dispatchState)
	{
		if (!IsImmutableAgileException(this))
		{
			byte[] currentStackTrace = (byte[])dispatchState.StackTrace?.Clone();
			object[] dynamicMethodArray = (object[])dispatchState.DynamicMethods?.Clone();
			_watsonBuckets = dispatchState.WatsonBuckets;
			_ipForWatsonBuckets = dispatchState.IpForWatsonBuckets;
			_remoteStackTraceString = dispatchState.RemoteStackTrace;
			SaveStackTracesFromDeepCopy(this, currentStackTrace, dynamicMethodArray);
			_stackTraceString = null;
			PrepareForForeignExceptionRaise();
		}
	}

	internal static string GetMessageFromNativeResources(ExceptionMessageKind kind)
	{
		string s = null;
		GetMessageFromNativeResources(kind, new StringHandleOnStack(ref s));
		return s;
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void GetMessageFromNativeResources(ExceptionMessageKind kind, StringHandleOnStack retMesg);

	internal DispatchState CaptureDispatchState()
	{
		GetStackTracesDeepCopy(this, out var currentStackTrace, out var dynamicMethodArray);
		return new DispatchState(currentStackTrace, dynamicMethodArray, _remoteStackTraceString, _ipForWatsonBuckets, _watsonBuckets);
	}

	private bool CanSetRemoteStackTrace()
	{
		if (IsImmutableAgileException(this))
		{
			return false;
		}
		if (_stackTrace != null || _stackTraceString != null || _remoteStackTraceString != null)
		{
			ThrowHelper.ThrowInvalidOperationException();
		}
		return true;
	}

	public Exception()
	{
		_HResult = -2146233088;
	}

	public Exception(string? message)
		: this()
	{
		_message = message;
	}

	public Exception(string? message, Exception? innerException)
		: this()
	{
		_message = message;
		_innerException = innerException;
	}

	protected Exception(SerializationInfo info, StreamingContext context)
	{
		if (info == null)
		{
			throw new ArgumentNullException("info");
		}
		_message = info.GetString("Message");
		_data = (IDictionary)info.GetValueNoThrow("Data", typeof(IDictionary));
		_innerException = (Exception)info.GetValue("InnerException", typeof(Exception));
		_helpURL = info.GetString("HelpURL");
		_stackTraceString = info.GetString("StackTraceString");
		_remoteStackTraceString = info.GetString("RemoteStackTraceString");
		_HResult = info.GetInt32("HResult");
		_source = info.GetString("Source");
		RestoreRemoteStackTrace(info, context);
	}

	private string GetClassName()
	{
		return GetType().ToString();
	}

	public virtual Exception GetBaseException()
	{
		Exception innerException = InnerException;
		Exception result = this;
		while (innerException != null)
		{
			result = innerException;
			innerException = innerException.InnerException;
		}
		return result;
	}

	public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		if (info == null)
		{
			throw new ArgumentNullException("info");
		}
		if (_source == null)
		{
			_source = Source;
		}
		info.AddValue("ClassName", GetClassName(), typeof(string));
		info.AddValue("Message", _message, typeof(string));
		info.AddValue("Data", _data, typeof(IDictionary));
		info.AddValue("InnerException", _innerException, typeof(Exception));
		info.AddValue("HelpURL", _helpURL, typeof(string));
		info.AddValue("StackTraceString", SerializationStackTraceString, typeof(string));
		info.AddValue("RemoteStackTraceString", _remoteStackTraceString, typeof(string));
		info.AddValue("RemoteStackIndex", 0, typeof(int));
		info.AddValue("ExceptionMethod", null, typeof(string));
		info.AddValue("HResult", _HResult);
		info.AddValue("Source", _source, typeof(string));
		info.AddValue("WatsonBuckets", SerializationWatsonBuckets, typeof(byte[]));
	}

	public override string ToString()
	{
		string className = GetClassName();
		string message = Message;
		string text = _innerException?.ToString() ?? "";
		string exception_EndOfInnerExceptionStack = SR.Exception_EndOfInnerExceptionStack;
		string stackTrace = StackTrace;
		int num = className.Length;
		checked
		{
			if (!string.IsNullOrEmpty(message))
			{
				num += 2 + message.Length;
			}
			if (_innerException != null)
			{
				num += "\r\n".Length + " ---> ".Length + text.Length + "\r\n".Length + 3 + exception_EndOfInnerExceptionStack.Length;
			}
			if (stackTrace != null)
			{
				num += "\r\n".Length + stackTrace.Length;
			}
			string text2 = string.FastAllocateString(num);
			Span<char> dest2 = new Span<char>(ref text2.GetRawStringData(), text2.Length);
			Write(className, ref dest2);
			if (!string.IsNullOrEmpty(message))
			{
				Write(": ", ref dest2);
				Write(message, ref dest2);
			}
			if (_innerException != null)
			{
				Write("\r\n", ref dest2);
				Write(" ---> ", ref dest2);
				Write(text, ref dest2);
				Write("\r\n", ref dest2);
				Write("   ", ref dest2);
				Write(exception_EndOfInnerExceptionStack, ref dest2);
			}
			if (stackTrace != null)
			{
				Write("\r\n", ref dest2);
				Write(stackTrace, ref dest2);
			}
			return text2;
		}
		static void Write(string source, ref Span<char> dest)
		{
			source.CopyTo(dest);
			dest = dest.Slice(source.Length);
		}
	}

	public new Type GetType()
	{
		return base.GetType();
	}

	private void RestoreRemoteStackTrace(SerializationInfo info, StreamingContext context)
	{
		_watsonBuckets = (byte[])info.GetValueNoThrow("WatsonBuckets", typeof(byte[]));
		if (context.State == StreamingContextStates.CrossAppDomain)
		{
			_remoteStackTraceString += _stackTraceString;
			_stackTraceString = null;
		}
	}

	private string GetStackTrace()
	{
		return new StackTrace(this, fNeedFileInfo: true).ToString(System.Diagnostics.StackTrace.TraceFormat.Normal);
	}

	[StackTraceHidden]
	internal void SetCurrentStackTrace()
	{
		if (CanSetRemoteStackTrace())
		{
			StringBuilder stringBuilder = new StringBuilder(256);
			new StackTrace(fNeedFileInfo: true).ToString(System.Diagnostics.StackTrace.TraceFormat.TrailingNewLine, stringBuilder);
			stringBuilder.AppendLine(SR.Exception_EndStackTraceFromPreviousThrow);
			_remoteStackTraceString = stringBuilder.ToString();
		}
	}

	internal void SetRemoteStackTrace(string stackTrace)
	{
		if (CanSetRemoteStackTrace())
		{
			_remoteStackTraceString = stackTrace + "\r\n" + SR.Exception_EndStackTraceFromPreviousThrow + "\r\n";
		}
	}
}

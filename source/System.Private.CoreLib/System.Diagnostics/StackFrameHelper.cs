using System.Reflection;
using System.Threading;

namespace System.Diagnostics;

internal sealed class StackFrameHelper
{
	private delegate void GetSourceLineInfoDelegate(Assembly assembly, string assemblyPath, IntPtr loadedPeAddress, int loadedPeSize, bool isFileLayout, IntPtr inMemoryPdbAddress, int inMemoryPdbSize, int methodToken, int ilOffset, out string sourceFile, out int sourceLine, out int sourceColumn);

	private Thread targetThread;

	private int[] rgiOffset;

	private int[] rgiILOffset;

	private object dynamicMethods;

	private IntPtr[] rgMethodHandle;

	private string[] rgAssemblyPath;

	private Assembly[] rgAssembly;

	private IntPtr[] rgLoadedPeAddress;

	private int[] rgiLoadedPeSize;

	private bool[] rgiIsFileLayout;

	private IntPtr[] rgInMemoryPdbAddress;

	private int[] rgiInMemoryPdbSize;

	private int[] rgiMethodToken;

	private string[] rgFilename;

	private int[] rgiLineNumber;

	private int[] rgiColumnNumber;

	private bool[] rgiLastFrameFromForeignExceptionStackTrace;

	private int iFrameCount;

	private static GetSourceLineInfoDelegate s_getSourceLineInfo;

	[ThreadStatic]
	private static int t_reentrancy;

	public StackFrameHelper(Thread target)
	{
		targetThread = target;
		rgMethodHandle = null;
		rgiMethodToken = null;
		rgiOffset = null;
		rgiILOffset = null;
		rgAssemblyPath = null;
		rgAssembly = null;
		rgLoadedPeAddress = null;
		rgiLoadedPeSize = null;
		rgiIsFileLayout = null;
		rgInMemoryPdbAddress = null;
		rgiInMemoryPdbSize = null;
		dynamicMethods = null;
		rgFilename = null;
		rgiLineNumber = null;
		rgiColumnNumber = null;
		rgiLastFrameFromForeignExceptionStackTrace = null;
		iFrameCount = 0;
	}

	internal void InitializeSourceInfo(int iSkip, bool fNeedFileInfo, Exception exception)
	{
		StackTrace.GetStackFramesInternal(this, iSkip, fNeedFileInfo, exception);
		if (!fNeedFileInfo || t_reentrancy > 0)
		{
			return;
		}
		t_reentrancy++;
		try
		{
			if (s_getSourceLineInfo == null)
			{
				Type type = Type.GetType("System.Diagnostics.StackTraceSymbols, System.Diagnostics.StackTrace, Version=4.0.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", throwOnError: false);
				if (type == null)
				{
					return;
				}
				Type[] types = new Type[12]
				{
					typeof(Assembly),
					typeof(string),
					typeof(IntPtr),
					typeof(int),
					typeof(bool),
					typeof(IntPtr),
					typeof(int),
					typeof(int),
					typeof(int),
					typeof(string).MakeByRefType(),
					typeof(int).MakeByRefType(),
					typeof(int).MakeByRefType()
				};
				MethodInfo method = type.GetMethod("GetSourceLineInfo", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, types, null);
				if (method == null)
				{
					return;
				}
				object target = Activator.CreateInstance(type);
				GetSourceLineInfoDelegate value = method.CreateDelegate<GetSourceLineInfoDelegate>(target);
				Interlocked.CompareExchange(ref s_getSourceLineInfo, value, null);
			}
			for (int i = 0; i < iFrameCount; i++)
			{
				if (rgiMethodToken[i] != 0)
				{
					s_getSourceLineInfo(rgAssembly[i], rgAssemblyPath[i], rgLoadedPeAddress[i], rgiLoadedPeSize[i], rgiIsFileLayout[i], rgInMemoryPdbAddress[i], rgiInMemoryPdbSize[i], rgiMethodToken[i], rgiILOffset[i], out rgFilename[i], out rgiLineNumber[i], out rgiColumnNumber[i]);
				}
			}
		}
		catch
		{
		}
		finally
		{
			t_reentrancy--;
		}
	}

	public MethodBase GetMethodBase(int i)
	{
		IntPtr intPtr = rgMethodHandle[i];
		if (intPtr == IntPtr.Zero)
		{
			return null;
		}
		IRuntimeMethodInfo typicalMethodDefinition = RuntimeMethodHandle.GetTypicalMethodDefinition(new RuntimeMethodInfoStub(intPtr, this));
		return RuntimeType.GetMethodBase(typicalMethodDefinition);
	}

	public int GetOffset(int i)
	{
		return rgiOffset[i];
	}

	public int GetILOffset(int i)
	{
		return rgiILOffset[i];
	}

	public string GetFilename(int i)
	{
		string[] array = rgFilename;
		if (array == null)
		{
			return null;
		}
		return array[i];
	}

	public int GetLineNumber(int i)
	{
		if (rgiLineNumber != null)
		{
			return rgiLineNumber[i];
		}
		return 0;
	}

	public int GetColumnNumber(int i)
	{
		if (rgiColumnNumber != null)
		{
			return rgiColumnNumber[i];
		}
		return 0;
	}

	public bool IsLastFrameFromForeignExceptionStackTrace(int i)
	{
		if (rgiLastFrameFromForeignExceptionStackTrace != null)
		{
			return rgiLastFrameFromForeignExceptionStackTrace[i];
		}
		return false;
	}

	public int GetNumberOfFrames()
	{
		return iFrameCount;
	}
}

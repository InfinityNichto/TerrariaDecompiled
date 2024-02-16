using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using _003CCppImplementationDetails_003E;

namespace Microsoft.Xna.Framework;

internal class WindowsManagedCallbackHandler : IDisposable
{
	private unsafe void* _parentExitEvent = null;

	private unsafe void* _callManagedPlease = null;

	private unsafe void* _managedFunctionDataCanBeWritten = null;

	private unsafe void* _proxyProcessNeedsToChange = null;

	private unsafe void* _newCallbackRegistered;

	private IProxyProcessHandler _proxyProcessHandler;

	private object _proxyProcessHandlerSyncObject;

	private static WindowsManagedCallbackHandler instance;

	private ManagedCallType _managedCallType;

	private uint _managedCallArgs;

	private List<NativeToManagedCallback> nativeToManagedCallbacks;

	public static WindowsManagedCallbackHandler Instance => instance;

	public unsafe static int Initialize(void* parentExitEvent)
	{
		if (parentExitEvent == null)
		{
			return -2147024809;
		}
		instance = new WindowsManagedCallbackHandler();
		int num = instance.InitializeInstance(parentExitEvent);
		if (num < 0)
		{
			instance = null;
		}
		return num;
	}

	public unsafe int WaitForAsyncOperationToFinish(out ManagedCallType managedCallType, out uint managedCallArgs)
	{
		int num = 0;
		System.Runtime.CompilerServices.Unsafe.SkipInit(out _0024ArrayType_0024_0024_0024BY0EA_0040PAX _0024ArrayType_0024_0024_0024BY0EA_0040PAX);
		while (true)
		{
			bool flag = false;
			uint num2 = 0u;
			uint num3 = uint.MaxValue;
			uint num4 = uint.MaxValue;
			uint num5 = uint.MaxValue;
			uint num6 = uint.MaxValue;
			uint num7 = uint.MaxValue;
			bool lockTaken = false;
			try
			{
				Monitor.Enter(nativeToManagedCallbacks, ref lockTaken);
				num2 = (uint)(nativeToManagedCallbacks.Count + 5);
				if (num2 > 64)
				{
					return -2147220991;
				}
				for (int i = 0; i < nativeToManagedCallbacks.Count; i++)
				{
					*(int*)((ref *(_003F*)(i * 4)) + (ref *(_003F*)(&_0024ArrayType_0024_0024_0024BY0EA_0040PAX))) = (int)nativeToManagedCallbacks[i].dupedWaitHandle;
				}
				num3 = num2 - 5;
				*(int*)((ref *(_003F*)(num3 * 4)) + (ref *(_003F*)(&_0024ArrayType_0024_0024_0024BY0EA_0040PAX))) = (int)_newCallbackRegistered;
				num4 = num2 - 4;
				*(int*)((ref *(_003F*)(num4 * 4)) + (ref *(_003F*)(&_0024ArrayType_0024_0024_0024BY0EA_0040PAX))) = (int)_callManagedPlease;
				num5 = num2 - 3;
				*(int*)((ref *(_003F*)(num5 * 4)) + (ref *(_003F*)(&_0024ArrayType_0024_0024_0024BY0EA_0040PAX))) = (int)_proxyProcessNeedsToChange;
				num6 = num2 - 2;
				*(int*)((ref *(_003F*)(num6 * 4)) + (ref *(_003F*)(&_0024ArrayType_0024_0024_0024BY0EA_0040PAX))) = (int)_parentExitEvent;
			}
			finally
			{
				if (lockTaken)
				{
					Monitor.Exit(nativeToManagedCallbacks);
				}
			}
			bool lockTaken2 = false;
			try
			{
				Monitor.Enter(_proxyProcessHandlerSyncObject, ref lockTaken2);
				IProxyProcessHandler proxyProcessHandler = _proxyProcessHandler;
				if (proxyProcessHandler != null)
				{
					IntPtr proxyProcessWantsToTalk = proxyProcessHandler.ProxyProcessWantsToTalk;
					IntPtr intPtr = proxyProcessWantsToTalk;
					IntPtr intPtr2 = proxyProcessWantsToTalk;
					num7 = num2 - 1;
					*(int*)((ref *(_003F*)(num7 * 4)) + (ref *(_003F*)(&_0024ArrayType_0024_0024_0024BY0EA_0040PAX))) = (int)intPtr2.ToPointer();
				}
				else
				{
					num2--;
				}
				uint num8 = global::_003CModule_003E.WaitForMultipleObjects(num2, (void**)(&_0024ArrayType_0024_0024_0024BY0EA_0040PAX), 0, uint.MaxValue);
				if (num8 == uint.MaxValue)
				{
					uint lastError = global::_003CModule_003E.GetLastError();
					int num9 = (((int)lastError > 0) ? ((int)(lastError & 0xFFFF) | -2147024896) : ((int)lastError));
					num = num9;
				}
				num8 = num8;
				if (num8 < num3)
				{
					bool flag2 = false;
					NativeToManagedCallback nativeToManagedCallback = nativeToManagedCallbacks[(int)num8];
					ManagedCallType managedCallType2 = managedCallType;
					uint num10 = managedCallArgs;
					num = nativeToManagedCallback.eventSignalledFunction(nativeToManagedCallback.originalWaitHandle, nativeToManagedCallback.pContext, &managedCallType2, &num10, &flag2);
					if (num >= 0)
					{
						managedCallType = managedCallType2;
						managedCallArgs = num10;
						if (flag2)
						{
							void* dupedWaitHandle = nativeToManagedCallback.dupedWaitHandle;
							if (dupedWaitHandle != null && dupedWaitHandle != (void*)(-1))
							{
								global::_003CModule_003E.CloseHandle(dupedWaitHandle);
								nativeToManagedCallback.dupedWaitHandle = (void*)(-1);
							}
							nativeToManagedCallbacks.RemoveAt((int)num8);
						}
					}
				}
				else if (num8 == num4)
				{
					managedCallType = _managedCallType;
					managedCallArgs = _managedCallArgs;
					global::_003CModule_003E.SetEvent(_managedFunctionDataCanBeWritten);
				}
				else if (num8 != num5 && num8 != num3)
				{
					if (num8 == num6)
					{
						Thread.CurrentThread.Abort();
					}
					else if (num7 != uint.MaxValue && num8 == num7)
					{
						managedCallType = _proxyProcessHandler.AsyncManagedCallType;
						managedCallArgs = _proxyProcessHandler.AsyncManagedCallArgument;
						num = (int)_proxyProcessHandler.AsyncHResult;
						global::_003CModule_003E.SetEvent(_proxyProcessHandler.SharedAsyncDataSafeToWrite.ToPointer());
					}
				}
				else
				{
					flag = true;
				}
			}
			finally
			{
				if (lockTaken2)
				{
					Monitor.Exit(_proxyProcessHandlerSyncObject);
				}
			}
			if (!flag)
			{
				break;
			}
			global::_003CModule_003E.Sleep(0u);
		}
		return num;
	}

	public unsafe int SetProxyProcessHandler(IProxyProcessHandler proxyProcessHandler)
	{
		if (global::_003CModule_003E.SetEvent(_proxyProcessNeedsToChange) == 0)
		{
			uint lastError = global::_003CModule_003E.GetLastError();
			return ((int)lastError > 0) ? ((int)(lastError & 0xFFFF) | -2147024896) : ((int)lastError);
		}
		bool lockTaken = false;
		try
		{
			Monitor.Enter(_proxyProcessHandlerSyncObject, ref lockTaken);
			_proxyProcessHandler = proxyProcessHandler;
		}
		finally
		{
			if (lockTaken)
			{
				Monitor.Exit(_proxyProcessHandlerSyncObject);
			}
		}
		if (global::_003CModule_003E.ResetEvent(_proxyProcessNeedsToChange) == 0)
		{
			uint lastError2 = global::_003CModule_003E.GetLastError();
			return ((int)lastError2 > 0) ? ((int)(lastError2 & 0xFFFF) | -2147024896) : ((int)lastError2);
		}
		return 0;
	}

	public static int CallManagedFunctionForMe(ManagedCallType managedCallType, uint args)
	{
		return instance.CallManagedFunctionForMeImpl(managedCallType, args);
	}

	public unsafe static int RegisterNativeToManagedCallback(void* waitForThisHandle, delegate*<void*, void*, ManagedCallType*, uint*, bool*, int> eventSignalledFunc, void* pContext)
	{
		return instance.RegisterNativeToManagedCallbackImpl(waitForThisHandle, eventSignalledFunc, pContext);
	}

	private void _007EWindowsManagedCallbackHandler()
	{
		_0021WindowsManagedCallbackHandler();
	}

	private unsafe void _0021WindowsManagedCallbackHandler()
	{
		void* callManagedPlease = _callManagedPlease;
		if (callManagedPlease != null)
		{
			global::_003CModule_003E.CloseHandle(callManagedPlease);
			_callManagedPlease = null;
		}
		void* managedFunctionDataCanBeWritten = _managedFunctionDataCanBeWritten;
		if (managedFunctionDataCanBeWritten != null)
		{
			global::_003CModule_003E.CloseHandle(managedFunctionDataCanBeWritten);
			_managedFunctionDataCanBeWritten = null;
		}
		void* proxyProcessNeedsToChange = _proxyProcessNeedsToChange;
		if (proxyProcessNeedsToChange != null)
		{
			global::_003CModule_003E.CloseHandle(proxyProcessNeedsToChange);
			_proxyProcessNeedsToChange = null;
		}
		void* newCallbackRegistered = _newCallbackRegistered;
		if (newCallbackRegistered != null)
		{
			global::_003CModule_003E.CloseHandle(newCallbackRegistered);
			_newCallbackRegistered = null;
		}
		if (nativeToManagedCallbacks.Count > 0)
		{
			do
			{
				global::_003CModule_003E.CloseHandle(nativeToManagedCallbacks[0].dupedWaitHandle);
				nativeToManagedCallbacks.RemoveAt(0);
			}
			while (nativeToManagedCallbacks.Count > 0);
		}
	}

	private unsafe WindowsManagedCallbackHandler()
	{
		_proxyProcessHandlerSyncObject = new object();
		nativeToManagedCallbacks = new List<NativeToManagedCallback>();
	}

	private unsafe int InitializeInstance(void* parentExitEvent)
	{
		_parentExitEvent = parentExitEvent;
		if ((_callManagedPlease = global::_003CModule_003E.CreateEventW(null, 0, 0, null)) == null)
		{
			uint lastError = global::_003CModule_003E.GetLastError();
			return ((int)lastError > 0) ? ((int)(lastError & 0xFFFF) | -2147024896) : ((int)lastError);
		}
		if ((_managedFunctionDataCanBeWritten = global::_003CModule_003E.CreateEventW(null, 0, 1, null)) == null)
		{
			uint lastError2 = global::_003CModule_003E.GetLastError();
			return ((int)lastError2 > 0) ? ((int)(lastError2 & 0xFFFF) | -2147024896) : ((int)lastError2);
		}
		if ((_proxyProcessNeedsToChange = global::_003CModule_003E.CreateEventW(null, 1, 0, null)) == null)
		{
			uint lastError3 = global::_003CModule_003E.GetLastError();
			return ((int)lastError3 > 0) ? ((int)(lastError3 & 0xFFFF) | -2147024896) : ((int)lastError3);
		}
		if ((_newCallbackRegistered = global::_003CModule_003E.CreateEventW(null, 0, 0, null)) == null)
		{
			uint lastError4 = global::_003CModule_003E.GetLastError();
			return ((int)lastError4 > 0) ? ((int)(lastError4 & 0xFFFF) | -2147024896) : ((int)lastError4);
		}
		return 0;
	}

	private unsafe int CallManagedFunctionForMeImpl(ManagedCallType managedCallType, uint args)
	{
		void* managedFunctionDataCanBeWritten = _managedFunctionDataCanBeWritten;
		if (managedFunctionDataCanBeWritten != null && _callManagedPlease != null)
		{
			System.Runtime.CompilerServices.Unsafe.SkipInit(out _0024ArrayType_0024_0024_0024BY01PAX _0024ArrayType_0024_0024_0024BY01PAX);
			*(int*)(&_0024ArrayType_0024_0024_0024BY01PAX) = (int)_parentExitEvent;
			System.Runtime.CompilerServices.Unsafe.As<_0024ArrayType_0024_0024_0024BY01PAX, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref _0024ArrayType_0024_0024_0024BY01PAX, 4)) = (int)managedFunctionDataCanBeWritten;
			int result = 0;
			switch (global::_003CModule_003E.WaitForMultipleObjects(2u, (void**)(&_0024ArrayType_0024_0024_0024BY01PAX), 0, uint.MaxValue))
			{
			case 0u:
				result = 0;
				break;
			case 1u:
				_managedCallType = managedCallType;
				_managedCallArgs = args;
				global::_003CModule_003E.SetEvent(_callManagedPlease);
				break;
			case uint.MaxValue:
			{
				uint lastError = global::_003CModule_003E.GetLastError();
				result = (((int)lastError > 0) ? ((int)(lastError & 0xFFFF) | -2147024896) : ((int)lastError));
				break;
			}
			}
			return result;
		}
		return -2147467259;
	}

	private unsafe int RegisterNativeToManagedCallbackImpl(void* waitForThisHandle, delegate*<void*, void*, ManagedCallType*, uint*, bool*, int> eventSignalledFunc, void* pContext)
	{
		if (waitForThisHandle != null && eventSignalledFunc != (delegate*<void*, void*, ManagedCallType*, uint*, bool*, int>)null)
		{
			void* ptr = null;
			int num = ((global::_003CModule_003E.DuplicateHandle(global::_003CModule_003E.GetCurrentProcess(), waitForThisHandle, global::_003CModule_003E.GetCurrentProcess(), &ptr, 0u, 0, 2u) == 0) ? (-2147467259) : 0);
			int num2 = num;
			if (num < 0)
			{
				if (ptr != null && ptr != (void*)(-1))
				{
					global::_003CModule_003E.CloseHandle(ptr);
				}
				return num;
			}
			bool lockTaken = false;
			try
			{
				Monitor.Enter(nativeToManagedCallbacks, ref lockTaken);
				num2 = HasRoomForAnotherWaitHandle();
				if (num2 >= 0)
				{
					NativeToManagedCallback item = new NativeToManagedCallback(waitForThisHandle, ptr, eventSignalledFunc, pContext);
					nativeToManagedCallbacks.Add(item);
					if (global::_003CModule_003E.SetEvent(_newCallbackRegistered) == 0)
					{
						uint lastError = global::_003CModule_003E.GetLastError();
						num2 = (((int)lastError > 0) ? ((int)(lastError & 0xFFFF) | -2147024896) : ((int)lastError));
					}
					if (num2 < 0)
					{
						nativeToManagedCallbacks.RemoveAt(nativeToManagedCallbacks.Count - 1);
					}
				}
			}
			finally
			{
				if (lockTaken)
				{
					Monitor.Exit(nativeToManagedCallbacks);
				}
			}
			return num2;
		}
		return -2147024809;
	}

	private int HasRoomForAnotherWaitHandle()
	{
		return (nativeToManagedCallbacks.Count + 6 > 64) ? (-2147220991) : 0;
	}

	[HandleProcessCorruptedStateExceptions]
	protected virtual void Dispose([MarshalAs(UnmanagedType.U1)] bool P_0)
	{
		if (P_0)
		{
			_0021WindowsManagedCallbackHandler();
			return;
		}
		try
		{
			_0021WindowsManagedCallbackHandler();
		}
		finally
		{
			base.Finalize();
		}
	}

	public virtual sealed void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	~WindowsManagedCallbackHandler()
	{
		Dispose(false);
	}
}

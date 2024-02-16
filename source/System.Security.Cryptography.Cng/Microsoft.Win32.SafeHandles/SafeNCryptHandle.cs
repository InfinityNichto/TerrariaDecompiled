using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles;

public abstract class SafeNCryptHandle : SafeHandleZeroOrMinusOneIsInvalid
{
	private enum OwnershipState
	{
		Owner,
		Duplicate,
		Holder
	}

	private OwnershipState _ownershipState;

	private SafeNCryptHandle _holder;

	private SafeHandle _parentHandle;

	private SafeNCryptHandle? Holder
	{
		get
		{
			return _holder;
		}
		[param: DisallowNull]
		set
		{
			_holder = value;
			_ownershipState = OwnershipState.Duplicate;
		}
	}

	protected SafeNCryptHandle()
		: base(ownsHandle: true)
	{
	}

	protected SafeNCryptHandle(IntPtr handle, SafeHandle parentHandle)
		: base(ownsHandle: true)
	{
		if (parentHandle == null)
		{
			throw new ArgumentNullException("parentHandle");
		}
		if (parentHandle.IsClosed || parentHandle.IsInvalid)
		{
			throw new ArgumentException(System.SR.Argument_Invalid_SafeHandleInvalidOrClosed, "parentHandle");
		}
		bool success = false;
		parentHandle.DangerousAddRef(ref success);
		_parentHandle = parentHandle;
		SetHandle(handle);
		if (IsInvalid)
		{
			_parentHandle.DangerousRelease();
			_parentHandle = null;
		}
	}

	internal T Duplicate<T>() where T : SafeNCryptHandle, new()
	{
		if (_ownershipState == OwnershipState.Owner)
		{
			return DuplicateOwnerHandle<T>();
		}
		return DuplicateDuplicatedHandle<T>();
	}

	private T DuplicateDuplicatedHandle<T>() where T : SafeNCryptHandle, new()
	{
		bool success = false;
		T val = new T();
		Holder.DangerousAddRef(ref success);
		val.SetHandle(Holder.DangerousGetHandle());
		val.Holder = Holder;
		return val;
	}

	private T DuplicateOwnerHandle<T>() where T : SafeNCryptHandle, new()
	{
		bool success = false;
		T val = new T();
		T val2 = new T();
		val._ownershipState = OwnershipState.Holder;
		val.SetHandle(DangerousGetHandle());
		GC.SuppressFinalize(val);
		if (_parentHandle != null)
		{
			val._parentHandle = _parentHandle;
			_parentHandle = null;
		}
		Holder = val;
		val.DangerousAddRef(ref success);
		val2.SetHandle(val.DangerousGetHandle());
		val2.Holder = val;
		return val2;
	}

	protected override bool ReleaseHandle()
	{
		if (_ownershipState == OwnershipState.Duplicate)
		{
			Holder.DangerousRelease();
			return true;
		}
		if (_parentHandle != null)
		{
			_parentHandle.DangerousRelease();
			return true;
		}
		return ReleaseNativeHandle();
	}

	protected abstract bool ReleaseNativeHandle();

	internal bool ReleaseNativeWithNCryptFreeObject()
	{
		global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptFreeObject(handle);
		return errorCode == global::Interop.NCrypt.ErrorCode.ERROR_SUCCESS;
	}
}

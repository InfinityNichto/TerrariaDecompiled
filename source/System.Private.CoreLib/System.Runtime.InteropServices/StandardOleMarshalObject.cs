namespace System.Runtime.InteropServices;

public class StandardOleMarshalObject : MarshalByRefObject, IMarshal
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int GetMarshalSizeMaxDelegate(IntPtr _this, ref Guid riid, IntPtr pv, int dwDestContext, IntPtr pvDestContext, int mshlflags, out int pSize);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int MarshalInterfaceDelegate(IntPtr _this, IntPtr pStm, ref Guid riid, IntPtr pv, int dwDestContext, IntPtr pvDestContext, int mshlflags);

	private static readonly Guid CLSID_StdMarshal = new Guid("00000017-0000-0000-c000-000000000046");

	protected StandardOleMarshalObject()
	{
	}

	private IntPtr GetStdMarshaler(ref Guid riid, int dwDestContext, int mshlflags)
	{
		IntPtr iUnknownForObject = Marshal.GetIUnknownForObject(this);
		if (iUnknownForObject != IntPtr.Zero)
		{
			try
			{
				IntPtr ppMarshal = IntPtr.Zero;
				if (Interop.Ole32.CoGetStandardMarshal(ref riid, iUnknownForObject, dwDestContext, IntPtr.Zero, mshlflags, out ppMarshal) == 0)
				{
					return ppMarshal;
				}
			}
			finally
			{
				Marshal.Release(iUnknownForObject);
			}
		}
		throw new InvalidOperationException(SR.Format(SR.StandardOleMarshalObjectGetMarshalerFailed, riid));
	}

	int IMarshal.GetUnmarshalClass(ref Guid riid, IntPtr pv, int dwDestContext, IntPtr pvDestContext, int mshlflags, out Guid pCid)
	{
		pCid = CLSID_StdMarshal;
		return 0;
	}

	unsafe int IMarshal.GetMarshalSizeMax(ref Guid riid, IntPtr pv, int dwDestContext, IntPtr pvDestContext, int mshlflags, out int pSize)
	{
		IntPtr stdMarshaler = GetStdMarshaler(ref riid, dwDestContext, mshlflags);
		try
		{
			IntPtr intPtr = *(IntPtr*)stdMarshaler.ToPointer();
			IntPtr ptr = *(IntPtr*)((byte*)intPtr.ToPointer() + (nint)4 * (nint)sizeof(IntPtr));
			GetMarshalSizeMaxDelegate getMarshalSizeMaxDelegate = (GetMarshalSizeMaxDelegate)Marshal.GetDelegateForFunctionPointer(ptr, typeof(GetMarshalSizeMaxDelegate));
			return getMarshalSizeMaxDelegate(stdMarshaler, ref riid, pv, dwDestContext, pvDestContext, mshlflags, out pSize);
		}
		finally
		{
			Marshal.Release(stdMarshaler);
		}
	}

	unsafe int IMarshal.MarshalInterface(IntPtr pStm, ref Guid riid, IntPtr pv, int dwDestContext, IntPtr pvDestContext, int mshlflags)
	{
		IntPtr stdMarshaler = GetStdMarshaler(ref riid, dwDestContext, mshlflags);
		try
		{
			IntPtr intPtr = *(IntPtr*)stdMarshaler.ToPointer();
			IntPtr ptr = *(IntPtr*)((byte*)intPtr.ToPointer() + (nint)5 * (nint)sizeof(IntPtr));
			MarshalInterfaceDelegate marshalInterfaceDelegate = (MarshalInterfaceDelegate)Marshal.GetDelegateForFunctionPointer(ptr, typeof(MarshalInterfaceDelegate));
			return marshalInterfaceDelegate(stdMarshaler, pStm, ref riid, pv, dwDestContext, pvDestContext, mshlflags);
		}
		finally
		{
			Marshal.Release(stdMarshaler);
		}
	}

	int IMarshal.UnmarshalInterface(IntPtr pStm, ref Guid riid, out IntPtr ppv)
	{
		ppv = IntPtr.Zero;
		return -2147467263;
	}

	int IMarshal.ReleaseMarshalData(IntPtr pStm)
	{
		return -2147467263;
	}

	int IMarshal.DisconnectObject(int dwReserved)
	{
		return -2147467263;
	}
}

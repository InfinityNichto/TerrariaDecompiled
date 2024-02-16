using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;

namespace System.Runtime.InteropServices.CustomMarshalers;

internal sealed class EnumerableViewOfDispatch : ICustomAdapter, System.Collections.IEnumerable
{
	private const int DISPID_NEWENUM = -4;

	private const int LCID_DEFAULT = 1;

	private readonly object _dispatch;

	private IDispatch Dispatch => (IDispatch)_dispatch;

	public EnumerableViewOfDispatch(object dispatch)
	{
		_dispatch = dispatch;
	}

	public unsafe System.Collections.IEnumerator GetEnumerator()
	{
		System.Runtime.CompilerServices.Unsafe.SkipInit(out Variant variant);
		void* value = &variant;
		DISPPARAMS pDispParams = default(DISPPARAMS);
		Guid riid = Guid.Empty;
		Dispatch.Invoke(-4, ref riid, 1, InvokeFlags.DISPATCH_METHOD | InvokeFlags.DISPATCH_PROPERTYGET, ref pDispParams, new IntPtr(value), IntPtr.Zero, IntPtr.Zero);
		IntPtr intPtr = IntPtr.Zero;
		try
		{
			object obj = variant.ToObject();
			if (!(obj is IEnumVARIANT o))
			{
				throw new InvalidOperationException(SR.InvalidOp_InvalidNewEnumVariant);
			}
			intPtr = Marshal.GetIUnknownForObject(o);
			return (System.Collections.IEnumerator)EnumeratorToEnumVariantMarshaler.GetInstance(null).MarshalNativeToManaged(intPtr);
		}
		finally
		{
			variant.Clear();
			if (intPtr != IntPtr.Zero)
			{
				Marshal.Release(intPtr);
			}
		}
	}

	public object GetUnderlyingObject()
	{
		return _dispatch;
	}
}

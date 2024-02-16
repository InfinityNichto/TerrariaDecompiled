using System.Collections;
using System.Runtime.InteropServices.ComTypes;

namespace System.Runtime.InteropServices.CustomMarshalers;

internal sealed class EnumVariantViewOfEnumerator : IEnumVARIANT, ICustomAdapter
{
	public System.Collections.IEnumerator Enumerator { get; }

	public EnumVariantViewOfEnumerator(System.Collections.IEnumerator enumerator)
	{
		if (enumerator == null)
		{
			throw new ArgumentNullException("enumerator");
		}
		Enumerator = enumerator;
	}

	public IEnumVARIANT Clone()
	{
		if (Enumerator is ICloneable cloneable)
		{
			return new EnumVariantViewOfEnumerator((System.Collections.IEnumerator)cloneable.Clone());
		}
		throw new COMException(SR.Arg_EnumNotCloneable, -2147467259);
	}

	public int Next(int celt, object[] rgVar, IntPtr pceltFetched)
	{
		int num = 0;
		try
		{
			if (celt > 0 && rgVar == null)
			{
				return -2147024809;
			}
			while (num < celt && Enumerator.MoveNext())
			{
				rgVar[num++] = Enumerator.Current;
			}
			if (pceltFetched != IntPtr.Zero)
			{
				Marshal.WriteInt32(pceltFetched, num);
			}
		}
		catch (Exception ex)
		{
			return ex.HResult;
		}
		if (num != celt)
		{
			return 1;
		}
		return 0;
	}

	public int Reset()
	{
		try
		{
			Enumerator.Reset();
		}
		catch (Exception ex)
		{
			return ex.HResult;
		}
		return 0;
	}

	public int Skip(int celt)
	{
		try
		{
			while (celt > 0 && Enumerator.MoveNext())
			{
				celt--;
			}
		}
		catch (Exception ex)
		{
			return ex.HResult;
		}
		if (celt != 0)
		{
			return 1;
		}
		return 0;
	}

	public object GetUnderlyingObject()
	{
		return Enumerator;
	}
}

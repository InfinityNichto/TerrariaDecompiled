using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class EffectAnnotation
{
	internal unsafe ID3DXBaseEffect* pEffect;

	internal unsafe sbyte* _handle;

	internal string _name;

	internal string _semantic;

	internal int _rows;

	internal int _columns;

	internal EffectParameterClass _paramClass;

	internal EffectParameterType _paramType;

	public EffectParameterType ParameterType => _paramType;

	public EffectParameterClass ParameterClass => _paramClass;

	public int ColumnCount => _columns;

	public int RowCount => _rows;

	public string Semantic => _semantic;

	public string Name => _name;

	internal unsafe EffectAnnotation(ID3DXBaseEffect* parent, sbyte* handle)
	{
		pEffect = parent;
		_handle = handle;
		base._002Ector();
		ID3DXBaseEffect* ptr = pEffect;
		System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DXPARAMETER_DESC d3DXPARAMETER_DESC);
		int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, _D3DXPARAMETER_DESC*, int>)(int)(*(uint*)(*(int*)ptr + 16)))((nint)ptr, _handle, &d3DXPARAMETER_DESC);
		if (num < 0)
		{
			throw GraphicsHelpers.GetExceptionFromResult((uint)num);
		}
		IntPtr ptr2 = (IntPtr)(void*)(int)(*(uint*)(&d3DXPARAMETER_DESC));
		_name = Marshal.PtrToStringAnsi(ptr2);
		_semantic = ((System.Runtime.CompilerServices.Unsafe.As<_D3DXPARAMETER_DESC, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXPARAMETER_DESC, 4)) == 0) ? string.Empty : Marshal.PtrToStringAnsi((IntPtr)(void*)(int)System.Runtime.CompilerServices.Unsafe.As<_D3DXPARAMETER_DESC, uint>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXPARAMETER_DESC, 4))));
		_rows = System.Runtime.CompilerServices.Unsafe.As<_D3DXPARAMETER_DESC, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXPARAMETER_DESC, 16));
		_columns = System.Runtime.CompilerServices.Unsafe.As<_D3DXPARAMETER_DESC, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXPARAMETER_DESC, 20));
		_paramClass = _003CModule_003E.ConvertDxParameterClassToXna(System.Runtime.CompilerServices.Unsafe.As<_D3DXPARAMETER_DESC, _D3DXPARAMETER_CLASS>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXPARAMETER_DESC, 8)));
		_paramType = _003CModule_003E.ConvertDxParameterTypeToXna(System.Runtime.CompilerServices.Unsafe.As<_D3DXPARAMETER_DESC, _D3DXPARAMETER_TYPE>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXPARAMETER_DESC, 12)));
	}

	internal unsafe void UpdateHandle(ID3DXBaseEffect* parent, sbyte* handle)
	{
		pEffect = parent;
		_handle = handle;
	}

	[return: MarshalAs(UnmanagedType.U1)]
	public unsafe bool GetValueBoolean()
	{
		try
		{
			return new EffectParameter(pEffect, null, _handle, -1).GetValueBoolean();
		}
		finally
		{
		}
	}

	public unsafe int GetValueInt32()
	{
		try
		{
			return new EffectParameter(pEffect, null, _handle, -1).GetValueInt32();
		}
		finally
		{
		}
	}

	public unsafe float GetValueSingle()
	{
		try
		{
			return new EffectParameter(pEffect, null, _handle, -1).GetValueSingle();
		}
		finally
		{
		}
	}

	public unsafe Vector2 GetValueVector2()
	{
		try
		{
			return new EffectParameter(pEffect, null, _handle, -1).GetValueVector2();
		}
		finally
		{
		}
	}

	public unsafe Vector3 GetValueVector3()
	{
		try
		{
			return new EffectParameter(pEffect, null, _handle, -1).GetValueVector3();
		}
		finally
		{
		}
	}

	public unsafe Vector4 GetValueVector4()
	{
		try
		{
			return new EffectParameter(pEffect, null, _handle, -1).GetValueVector4();
		}
		finally
		{
		}
	}

	public unsafe Matrix GetValueMatrix()
	{
		try
		{
			return new EffectParameter(pEffect, null, _handle, -1).GetValueMatrix();
		}
		finally
		{
		}
	}

	public unsafe string GetValueString()
	{
		try
		{
			return new EffectParameter(pEffect, null, _handle, -1).GetValueString();
		}
		finally
		{
		}
	}
}

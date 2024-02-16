using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class EffectTechnique
{
	internal Effect _parent;

	internal EffectPassCollection pPasses;

	internal EffectAnnotationCollection pAnnotations;

	internal unsafe ID3DXBaseEffect* pEffect;

	internal unsafe sbyte* _handle;

	internal string _name;

	public EffectAnnotationCollection Annotations => pAnnotations;

	public EffectPassCollection Passes => pPasses;

	public string Name => _name;

	internal unsafe EffectTechnique(ID3DXBaseEffect* parent, Effect effect, sbyte* technique)
	{
		_parent = effect;
		pEffect = parent;
		_handle = technique;
		base._002Ector();
		ID3DXBaseEffect* ptr = pEffect;
		System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DXTECHNIQUE_DESC d3DXTECHNIQUE_DESC);
		int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, _D3DXTECHNIQUE_DESC*, int>)(int)(*(uint*)(*(int*)ptr + 20)))((nint)ptr, _handle, &d3DXTECHNIQUE_DESC);
		if (num < 0)
		{
			throw GraphicsHelpers.GetExceptionFromResult((uint)num);
		}
		IntPtr ptr2 = (IntPtr)(void*)(int)(*(uint*)(&d3DXTECHNIQUE_DESC));
		_name = Marshal.PtrToStringAnsi(ptr2);
		pPasses = new EffectPassCollection(pEffect, this, System.Runtime.CompilerServices.Unsafe.As<_D3DXTECHNIQUE_DESC, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXTECHNIQUE_DESC, 4)));
		pAnnotations = new EffectAnnotationCollection(pEffect, _handle, System.Runtime.CompilerServices.Unsafe.As<_D3DXTECHNIQUE_DESC, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXTECHNIQUE_DESC, 8)));
	}

	internal unsafe void UpdateHandle(ID3DXBaseEffect* parent, sbyte* handle)
	{
		pEffect = parent;
		_handle = handle;
		pPasses.UpdateParent(parent, handle);
		pAnnotations.UpdateParent(pEffect, _handle);
	}
}

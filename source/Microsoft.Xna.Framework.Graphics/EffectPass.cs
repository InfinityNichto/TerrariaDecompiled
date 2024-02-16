using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class EffectPass
{
	internal EffectAnnotationCollection pAnnotations;

	internal EffectTechnique _technique;

	internal unsafe ID3DXBaseEffect* pEffect;

	internal unsafe sbyte* _handle;

	internal int _index;

	internal string _name;

	internal EffectStateFlags _stateFlags;

	internal uint _textureFlags;

	internal unsafe uint* pVertexShaderCode;

	internal unsafe uint* pPixelShaderCode;

	public EffectAnnotationCollection Annotations => pAnnotations;

	public string Name => _name;

	internal unsafe EffectPass(ID3DXBaseEffect* parent, EffectTechnique technique, sbyte* Pass, int index)
	{
		_technique = technique;
		pEffect = parent;
		_handle = Pass;
		_index = index;
		base._002Ector();
		ID3DXBaseEffect* ptr = pEffect;
		System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DXPASS_DESC d3DXPASS_DESC);
		int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, sbyte*, _D3DXPASS_DESC*, int>)(int)(*(uint*)(*(int*)ptr + 24)))((nint)ptr, _handle, &d3DXPASS_DESC);
		if (num < 0)
		{
			throw GraphicsHelpers.GetExceptionFromResult((uint)num);
		}
		IntPtr ptr2 = (IntPtr)(void*)(int)(*(uint*)(&d3DXPASS_DESC));
		_name = Marshal.PtrToStringAnsi(ptr2);
		pVertexShaderCode = (uint*)(int)System.Runtime.CompilerServices.Unsafe.As<_D3DXPASS_DESC, uint>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXPASS_DESC, 8));
		pPixelShaderCode = (uint*)(int)System.Runtime.CompilerServices.Unsafe.As<_D3DXPASS_DESC, uint>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXPASS_DESC, 12));
		pAnnotations = new EffectAnnotationCollection(pEffect, _handle, System.Runtime.CompilerServices.Unsafe.As<_D3DXPASS_DESC, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DXPASS_DESC, 4)));
	}

	internal unsafe void UpdateHandle(ID3DXBaseEffect* parent, sbyte* handle)
	{
		pEffect = parent;
		_handle = handle;
		pAnnotations.UpdateParent(parent, handle);
	}

	internal unsafe void EndPass()
	{
		bool lockTaken = false;
		try
		{
			Monitor.Enter(Effect.pSyncObject, ref lockTaken);
			ID3DXEffect* pComPtr = _technique._parent.pComPtr;
			if (pComPtr != null)
			{
				((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, int>)(int)(*(uint*)(*(int*)pComPtr + 264)))((nint)pComPtr);
				((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, int>)(int)(*(uint*)(*(int*)pComPtr + 268)))((nint)pComPtr);
			}
		}
		finally
		{
			if (lockTaken)
			{
				Monitor.Exit(Effect.pSyncObject);
			}
		}
	}

	public unsafe void Apply()
	{
		Effect parent = _technique._parent;
		IntPtr pComPtr = (IntPtr)parent.pComPtr;
		Helpers.CheckDisposed(parent, pComPtr);
		if (parent._currentTechnique != _technique)
		{
			throw new InvalidOperationException(FrameworkResources.NotCurrentTechnique);
		}
		parent.OnApply();
		ID3DXEffect* pComPtr2 = parent.pComPtr;
		GraphicsDevice graphicsDevice = parent.GraphicsDevice;
		EffectPass activePass = graphicsDevice.activePass;
		int num;
		if (activePass == this)
		{
			num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, int>)(int)(*(uint*)(*(int*)pComPtr2 + 260)))((nint)pComPtr2);
		}
		else
		{
			activePass?.EndPass();
			System.Runtime.CompilerServices.Unsafe.SkipInit(out uint num2);
			num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint*, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 252)))((nint)pComPtr2, &num2, 1u);
			if (num >= 0)
			{
				num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 256)))((nint)pComPtr2, (uint)_index);
				if (num >= 0)
				{
					graphicsDevice.activePass = this;
					if (_stateFlags != 0)
					{
						SyncEffectState();
					}
				}
				else
				{
					((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, int>)(int)(*(uint*)(*(int*)pComPtr2 + 268)))((nint)pComPtr2);
					graphicsDevice.activePass = null;
				}
			}
			else
			{
				graphicsDevice.activePass = null;
			}
		}
		StateTrackerDevice* pStateTracker = graphicsDevice.pStateTracker;
		if (((byte*)pStateTracker)[104] != 0)
		{
			((byte*)pStateTracker)[104] = 0;
			graphicsDevice._profileCapabilities.ThrowNotSupportedException(FrameworkResources.ProfileVertexTextureFormatNotSupported, *(SurfaceFormat*)((byte*)pStateTracker + 108));
		}
		if (num < 0)
		{
			throw GraphicsHelpers.GetExceptionFromResult((uint)num);
		}
	}

	private void SyncEffectState()
	{
		GraphicsDevice graphicsDevice = _technique._parent.GraphicsDevice;
		if (graphicsDevice == null)
		{
			throw new ArgumentNullException("graphicsDevice", FrameworkResources.DeviceCannotBeNullOnResourceCreate);
		}
		if ((_stateFlags & EffectStateFlags.Blend) != 0)
		{
			graphicsDevice.ClearBlendState();
		}
		if ((_stateFlags & EffectStateFlags.DepthStencil) != 0)
		{
			graphicsDevice.ClearDepthStencilState();
		}
		if ((_stateFlags & EffectStateFlags.Rasterizer) != 0)
		{
			graphicsDevice.ClearRasterizerState();
		}
		if ((_stateFlags & EffectStateFlags.AllSamplers) != 0)
		{
			int num = 0;
			do
			{
				if (((uint)_stateFlags & (uint)(8 << num)) != 0)
				{
					graphicsDevice.SamplerStates.ClearState(num);
				}
				num++;
			}
			while (num < 16);
		}
		if ((_stateFlags & EffectStateFlags.AllVertexSamplers) == 0)
		{
			return;
		}
		int num2 = 0;
		do
		{
			if (((uint)_stateFlags & (uint)(524288 << num2)) != 0)
			{
				graphicsDevice.VertexSamplerStates.ClearState(num2);
			}
			num2++;
		}
		while (num2 < 4);
	}
}

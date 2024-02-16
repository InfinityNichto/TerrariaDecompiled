using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class TextureCollection
{
	private GraphicsDevice _parent;

	private int _textureOffset;

	internal int _maxTextures;

	public unsafe Texture this[int index]
	{
		get
		{
			IntPtr pComPtr = (IntPtr)_parent.pComPtr;
			Helpers.CheckDisposed(_parent, pComPtr);
			if (index >= 0 && index < _maxTextures)
			{
				IDirect3DBaseTexture9* ptr = null;
				IDirect3DTexture9* ptr2 = null;
				IDirect3DCubeTexture9* ptr3 = null;
				IDirect3DVolumeTexture9* ptr4 = null;
				try
				{
					IDirect3DDevice9* pComPtr2 = _parent.pComPtr;
					int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, IDirect3DBaseTexture9**, int>)(int)(*(uint*)(*(int*)pComPtr2 + 256)))((nint)pComPtr2, (uint)(_textureOffset + index), &ptr);
					if (num < 0)
					{
						throw GraphicsHelpers.GetExceptionFromResult((uint)num);
					}
					if (ptr == null)
					{
						return null;
					}
					int num2;
					if (((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _GUID*, void**, int>)(int)(*(uint*)(int)(*(uint*)ptr)))((nint)ptr, (_GUID*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _003CModule_003E.IID_IDirect3DTexture9), (void**)(&ptr2)) >= 0)
					{
						System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DSURFACE_DESC d3DSURFACE_DESC);
						num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DSURFACE_DESC*, int>)(int)(*(uint*)(*(int*)ptr2 + 68)))((nint)ptr2, 0u, &d3DSURFACE_DESC);
						if (num2 >= 0)
						{
							return Texture2D.GetManagedObject(ptr2, _parent, System.Runtime.CompilerServices.Unsafe.As<_D3DSURFACE_DESC, uint>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DSURFACE_DESC, 12)));
						}
					}
					else if (((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _GUID*, void**, int>)(int)(*(uint*)(int)(*(uint*)ptr)))((nint)ptr, (_GUID*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _003CModule_003E.IID_IDirect3DCubeTexture9), (void**)(&ptr3)) >= 0)
					{
						System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DSURFACE_DESC d3DSURFACE_DESC2);
						num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DSURFACE_DESC*, int>)(int)(*(uint*)(*(int*)ptr3 + 68)))((nint)ptr3, 0u, &d3DSURFACE_DESC2);
						if (num2 >= 0)
						{
							return TextureCube.GetManagedObject(ptr3, _parent, System.Runtime.CompilerServices.Unsafe.As<_D3DSURFACE_DESC, uint>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DSURFACE_DESC2, 12)));
						}
					}
					else
					{
						num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _GUID*, void**, int>)(int)(*(uint*)(int)(*(uint*)ptr)))((nint)ptr, (_GUID*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _003CModule_003E.IID_IDirect3DVolumeTexture9), (void**)(&ptr4));
						if (num2 >= 0)
						{
							System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DVOLUME_DESC d3DVOLUME_DESC);
							num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DVOLUME_DESC*, int>)(int)(*(uint*)(*(int*)ptr4 + 68)))((nint)ptr4, 0u, &d3DVOLUME_DESC);
							if (num2 >= 0)
							{
								return Texture3D.GetManagedObject(ptr4, _parent, System.Runtime.CompilerServices.Unsafe.As<_D3DVOLUME_DESC, uint>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DVOLUME_DESC, 12)));
							}
						}
					}
					throw GraphicsHelpers.GetExceptionFromResult((uint)num2);
				}
				finally
				{
					if (ptr != null)
					{
						IDirect3DBaseTexture9* intPtr = ptr;
						((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)intPtr + 8)))((nint)intPtr);
						ptr = null;
					}
				}
			}
			throw new ArgumentOutOfRangeException("index");
		}
		set
		{
			IntPtr pComPtr = (IntPtr)_parent.pComPtr;
			Helpers.CheckDisposed(_parent, pComPtr);
			IDirect3DBaseTexture9* ptr = null;
			if (value != null)
			{
				ptr = value.GetComPtr();
				IntPtr pComPtr2 = (IntPtr)ptr;
				Helpers.CheckDisposed(value, pComPtr2);
				if (value.isActiveRenderTarget)
				{
					throw new InvalidOperationException(FrameworkResources.MustResolveRenderTarget);
				}
				if (_textureOffset > 0 && ((byte*)value.pStateTracker)[16] == 0)
				{
					_parent._profileCapabilities.ThrowNotSupportedException(FrameworkResources.ProfileVertexTextureFormatNotSupported, value.Format);
				}
			}
			if (index >= 0 && index < _maxTextures)
			{
				int num = _textureOffset + index;
				int num2 = ((num < 257) ? num : (num - 241));
				uint num3 = (uint)(1 << num2);
				EffectPass activePass = _parent.activePass;
				if (activePass != null && (activePass._textureFlags & num3) != 0)
				{
					activePass.EndPass();
					_parent.activePass = null;
				}
				IDirect3DDevice9* pComPtr3 = _parent.pComPtr;
				int num4 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, IDirect3DBaseTexture9*, int>)(int)(*(uint*)(*(int*)pComPtr3 + 260)))((nint)pComPtr3, (uint)num, ptr);
				if (num4 < 0)
				{
					if (value != null && value.GraphicsDevice != _parent)
					{
						throw new InvalidOperationException(FrameworkResources.InvalidDevice);
					}
					throw GraphicsHelpers.GetExceptionFromResult((uint)num4);
				}
				StateTrackerDevice* pStateTracker = _parent.pStateTracker;
				uint num5 = ~num3;
				if (value != null)
				{
					StateTrackerTexture* pStateTracker2 = value.pStateTracker;
					*(int*)((byte*)pStateTracker + 136) = (*(int*)((byte*)pStateTracker2 + 20) << num2) | (*(int*)((byte*)pStateTracker + 136) & (int)num5);
					*(int*)((byte*)pStateTracker + 140) = (*(int*)((byte*)pStateTracker2 + 24) << num2) | (*(int*)((byte*)pStateTracker + 140) & (int)num5);
				}
				else
				{
					*(int*)((byte*)pStateTracker + 136) &= (int)num5;
					*(int*)((byte*)pStateTracker + 140) &= (int)num5;
				}
				return;
			}
			throw new ArgumentOutOfRangeException("index");
		}
	}

	internal TextureCollection(GraphicsDevice parent, int textureOffset, int maxTextures)
	{
		_parent = parent;
		_textureOffset = textureOffset;
		_maxTextures = maxTextures;
		base._002Ector();
	}

	internal unsafe void ResetState()
	{
		int num = 0;
		if (0 >= _maxTextures)
		{
			return;
		}
		while (true)
		{
			IntPtr pComPtr = (IntPtr)_parent.pComPtr;
			Helpers.CheckDisposed(_parent, pComPtr);
			if (num < 0 || num >= _maxTextures)
			{
				break;
			}
			int num2 = _textureOffset + num;
			int num3 = ((num2 < 257) ? num2 : (num2 - 241));
			uint num4 = (uint)(1 << num3);
			EffectPass activePass = _parent.activePass;
			if (activePass != null && (activePass._textureFlags & num4) != 0)
			{
				activePass.EndPass();
				_parent.activePass = null;
			}
			IDirect3DDevice9* pComPtr2 = _parent.pComPtr;
			int num5 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, IDirect3DBaseTexture9*, int>)(int)(*(uint*)(*(int*)pComPtr2 + 260)))((nint)pComPtr2, (uint)num2, null);
			if (num5 >= 0)
			{
				StateTrackerDevice* pStateTracker = _parent.pStateTracker;
				uint num6 = ~num4;
				*(int*)((byte*)pStateTracker + 136) &= (int)num6;
				*(int*)((byte*)pStateTracker + 140) &= (int)num6;
				num++;
				if (num >= _maxTextures)
				{
					return;
				}
				continue;
			}
			throw GraphicsHelpers.GetExceptionFromResult((uint)num5);
		}
		throw new ArgumentOutOfRangeException("index");
	}
}

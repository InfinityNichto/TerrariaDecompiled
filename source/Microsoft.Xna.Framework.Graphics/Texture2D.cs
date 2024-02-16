using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using Microsoft.Xna.Framework.Media;
using SharedConstants;

namespace Microsoft.Xna.Framework.Graphics;

public class Texture2D : Texture, IGraphicsResource
{
	private protected int _width;

	private protected int _height;

	private protected bool _shouldNotRecreate;

	private IntPtr[] pFaceData;

	internal unsafe IDirect3DTexture9* pComPtr;

	private protected override bool MustClamp
	{
		[return: MarshalAs(UnmanagedType.U1)]
		get
		{
			if (_parent._profileCapabilities.NonPow2Unconditional)
			{
				return false;
			}
			if (Texture.IsPowerOfTwo((uint)_width) && Texture.IsPowerOfTwo((uint)_height))
			{
				return false;
			}
			return true;
		}
	}

	public Rectangle Bounds => new Rectangle(0, 0, _width, _height);

	public int Height => _height;

	public int Width => _width;

	private unsafe Texture2D(IDirect3DTexture9* pInterface, GraphicsDevice pDevice)
	{
		pComPtr = pInterface;
		((object)this)._002Ector();
		try
		{
			_parent = pDevice;
			InitializeDescription(null);
			return;
		}
		catch
		{
			//try-fault
			base.Dispose(true);
			throw;
		}
	}

	private protected Texture2D()
	{
	}

	public Texture2D(GraphicsDevice graphicsDevice, int width, int height)
	{
		try
		{
			CreateTexture(graphicsDevice, width, height, mipMap: false, 0u, (_D3DPOOL)1, SurfaceFormat.Color);
			return;
		}
		catch
		{
			//try-fault
			base.Dispose(true);
			throw;
		}
	}

	public Texture2D(GraphicsDevice graphicsDevice, int width, int height, [MarshalAs(UnmanagedType.U1)] bool mipMap, SurfaceFormat format)
	{
		try
		{
			CreateTexture(graphicsDevice, width, height, mipMap, 0u, (_D3DPOOL)1, format);
			return;
		}
		catch
		{
			//try-fault
			base.Dispose(true);
			throw;
		}
	}

	internal Texture2D(GraphicsDevice graphicsDevice, int width, int height, [MarshalAs(UnmanagedType.U1)] bool mipMap, SurfaceFormat format, [MarshalAs(UnmanagedType.U1)] bool __unnamed005)
	{
		try
		{
			CreateTexture(graphicsDevice, width, height, mipMap, 0u, (_D3DPOOL)1, format);
			return;
		}
		catch
		{
			//try-fault
			base.Dispose(true);
			throw;
		}
	}

	internal unsafe Texture2D(GraphicsDevice graphicsDevice, Stream stream, int width, int height, SharedConstants.XnaImageOperation operation)
	{
		try
		{
			if (graphicsDevice == null)
			{
				throw new ArgumentNullException("graphicsDevice", FrameworkResources.DeviceCannotBeNullOnResourceCreate);
			}
			if (stream == null)
			{
				throw new ArgumentNullException("stream", FrameworkResources.NullNotAllowed);
			}
			if (!stream.CanSeek)
			{
				throw new ArgumentException(FrameworkResources.StreamNotSeekable, "stream");
			}
			ValidateCreationParameters(graphicsDevice._profileCapabilities, width, height, SurfaceFormat.Color, mipMap: false);
			ImageStream imageStream = stream as ImageStream;
			try
			{
				if (imageStream == null)
				{
					imageStream = ImageStream.FromStream(stream);
				}
				void* ptr = null;
				Helpers.ThrowExceptionFromErrorCode(Microsoft.Xna.Framework.Media.UnsafeNativeMethods.DecodeStreamToTexture(graphicsDevice.pComPtr, imageStream.Handle, &width, &height, (XnaImageOperation)operation, graphicsDevice._profileCapabilities.MaxTextureAspectRatio, &ptr));
				pComPtr = (IDirect3DTexture9*)ptr;
			}
			finally
			{
				if (imageStream != null && imageStream != stream)
				{
					((IDisposable)imageStream).Dispose();
				}
			}
			isDisposed = false;
			_parent = graphicsDevice;
			SurfaceFormat? format = SurfaceFormat.Color;
			InitializeDescription(format);
			graphicsDevice.Resources.AddTrackedObject(this, pComPtr, 1u, _internalHandle, ref _internalHandle);
			return;
		}
		catch
		{
			//try-fault
			base.Dispose(true);
			throw;
		}
	}

	public static Texture2D FromStream(GraphicsDevice graphicsDevice, Stream stream, int width, int height, [MarshalAs(UnmanagedType.U1)] bool zoom)
	{
		SharedConstants.XnaImageOperation operation = ((!zoom) ? ((SharedConstants.XnaImageOperation)1) : ((SharedConstants.XnaImageOperation)3));
		return new Texture2D(graphicsDevice, stream, width, height, operation);
	}

	public static Texture2D FromStream(GraphicsDevice graphicsDevice, Stream stream)
	{
		int maxTextureSize = graphicsDevice._profileCapabilities.MaxTextureSize;
		return new Texture2D(graphicsDevice, stream, maxTextureSize, maxTextureSize, (SharedConstants.XnaImageOperation)0);
	}

	public void SaveAsJpeg(Stream stream, int width, int height)
	{
		SaveAsImage(stream, (SharedConstants.XnaImageFormat)0, width, height);
	}

	public void SaveAsPng(Stream stream, int width, int height)
	{
		SaveAsImage(stream, (SharedConstants.XnaImageFormat)2, width, height);
	}

	public void SetData<T>(T[] data) where T : struct
	{
		Rectangle? rect = null;
		int elementCount = ((data != null) ? data.Length : 0);
		SetData(0, rect, data, 0, elementCount);
	}

	public void SetData<T>(T[] data, int startIndex, int elementCount) where T : struct
	{
		SetData(0, null, data, startIndex, elementCount);
	}

	public void SetData<T>(int level, Rectangle? rect, T[] data, int startIndex, int elementCount) where T : struct
	{
		CopyData(level, rect, data, startIndex, elementCount, 0u, isSetting: true);
	}

	public void GetData<T>(T[] data) where T : struct
	{
		Rectangle? rect = null;
		int elementCount = ((data != null) ? data.Length : 0);
		GetData(0, rect, data, 0, elementCount);
	}

	public void GetData<T>(T[] data, int startIndex, int elementCount) where T : struct
	{
		GetData(0, null, data, startIndex, elementCount);
	}

	public void GetData<T>(int level, Rectangle? rect, T[] data, int startIndex, int elementCount) where T : struct
	{
		CopyData(level, rect, data, startIndex, elementCount, 16u, isSetting: false);
	}

	private unsafe void CopyData<T>(int level, Rectangle? rect, T[] data, int startIndex, int elementCount, uint options, [MarshalAs(UnmanagedType.U1)] bool isSetting) where T : struct
	{
		IntPtr intPtr = (IntPtr)pComPtr;
		Helpers.CheckDisposed(this, intPtr);
		if (data != null && data.Length != 0)
		{
			if (isActiveRenderTarget)
			{
				throw new InvalidOperationException(FrameworkResources.MustResolveRenderTarget);
			}
			if (isSetting)
			{
				int num = 0;
				if (0 < _parent.Textures._maxTextures)
				{
					do
					{
						if (_parent.Textures[num] != this)
						{
							num++;
							continue;
						}
						throw GraphicsHelpers.GetExceptionFromResult(2147500036u);
					}
					while (num < _parent.Textures._maxTextures);
				}
				int num2 = 0;
				if (0 < _parent.VertexTextures._maxTextures)
				{
					do
					{
						if (_parent.VertexTextures[num2] != this)
						{
							num2++;
							continue;
						}
						throw GraphicsHelpers.GetExceptionFromResult(2147500036u);
					}
					while (num2 < _parent.VertexTextures._maxTextures);
				}
			}
			System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DSURFACE_DESC d3DSURFACE_DESC);
			// IL initblk instruction
			System.Runtime.CompilerServices.Unsafe.InitBlock(ref d3DSURFACE_DESC, 0, 32);
			IDirect3DTexture9* ptr = pComPtr;
			int num3 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DSURFACE_DESC*, int>)(int)(*(uint*)(*(int*)ptr + 68)))((nint)ptr, (uint)level, &d3DSURFACE_DESC);
			if (num3 < 0)
			{
				throw GraphicsHelpers.GetExceptionFromResult((uint)num3);
			}
			Helpers.ValidateCopyParameters(data.Length, startIndex, elementCount);
			System.Runtime.CompilerServices.Unsafe.SkipInit(out uint dwFormatSize);
			System.Runtime.CompilerServices.Unsafe.SkipInit(out uint dwElementSize);
			Texture.GetAndValidateSizes<T>(&d3DSURFACE_DESC, &dwFormatSize, &dwElementSize);
			System.Runtime.CompilerServices.Unsafe.SkipInit(out uint dwLockWidth);
			System.Runtime.CompilerServices.Unsafe.SkipInit(out uint dwLockHeight);
			Texture.GetAndValidateRect(&d3DSURFACE_DESC, &dwLockWidth, &dwLockHeight, rect);
			Texture.ValidateTotalSize(&d3DSURFACE_DESC, dwLockWidth, dwLockHeight, dwFormatSize, dwElementSize, (uint)elementCount);
			tagRECT* ptr2 = null;
			Rectangle rectangle = default(Rectangle);
			if (rect.HasValue)
			{
				rectangle = rect.Value;
				ptr2 = (tagRECT*)(int)(ref rectangle);
				if (ptr2 != null)
				{
					*(int*)((byte*)ptr2 + 8) += *(int*)ptr2;
					*(int*)((byte*)ptr2 + 12) += *(int*)((byte*)ptr2 + 4);
				}
			}
			int num5;
			System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DLOCKED_RECT d3DLOCKED_RECT);
			if (System.Runtime.CompilerServices.Unsafe.As<_D3DSURFACE_DESC, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DSURFACE_DESC, 12)) == 0)
			{
				if (!isSetting)
				{
					GraphicsDevice parent = _parent;
					GraphicsDevice graphicsDevice = parent;
					GraphicsDevice graphicsDevice2 = parent;
					GraphicsDevice graphicsDevice3 = parent;
					int num4 = *(int*)GraphicsAdapter.pComPtr + 40;
					if (((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DDEVTYPE, _D3DFORMAT, uint, _D3DRESOURCETYPE, _D3DFORMAT, int>)(int)(*(uint*)num4))((nint)GraphicsAdapter.pComPtr, graphicsDevice3.Adapter.adapter, graphicsDevice2._deviceType, _003CModule_003E.ConvertXnaFormatToWindowsAdapterFormat(graphicsDevice.Adapter.CurrentDisplayMode.Format), 1u, (_D3DRESOURCETYPE)3, *(_D3DFORMAT*)(&d3DSURFACE_DESC)) < 0)
					{
						throw new InvalidOperationException(FrameworkResources.CannotUseFormatTypeAsManualWhenLocking);
					}
				}
				IDirect3DSurface9* ptr3 = null;
				IDirect3DTexture9* ptr4 = null;
				IDirect3DSurface9* ptr5 = null;
				IDirect3DDevice9* ptr6 = _parent.pComPtr;
				num5 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, uint, uint, uint, _D3DFORMAT, _D3DPOOL, IDirect3DTexture9**, void**, int>)(int)(*(uint*)(*(int*)ptr6 + 92)))((nint)ptr6, System.Runtime.CompilerServices.Unsafe.As<_D3DSURFACE_DESC, uint>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DSURFACE_DESC, 24)), System.Runtime.CompilerServices.Unsafe.As<_D3DSURFACE_DESC, uint>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DSURFACE_DESC, 28)), 1u, 0u, *(_D3DFORMAT*)(&d3DSURFACE_DESC), (_D3DPOOL)2, &ptr4, null);
				if (num5 >= 0)
				{
					num5 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, IDirect3DSurface9**, int>)(int)(*(uint*)(*(int*)ptr4 + 72)))((nint)ptr4, 0u, &ptr3);
					if (num5 >= 0)
					{
						ptr = pComPtr;
						num5 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, IDirect3DSurface9**, int>)(int)(*(uint*)(*(int*)ptr + 72)))((nint)ptr, (uint)level, &ptr5);
						if (num5 >= 0)
						{
							if (isSetting)
							{
								if (ptr2 != null && (*(int*)ptr2 != 0 || *(int*)((byte*)ptr2 + 4) != 0 || *(int*)((byte*)ptr2 + 8) != System.Runtime.CompilerServices.Unsafe.As<_D3DSURFACE_DESC, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DSURFACE_DESC, 24)) || *(int*)((byte*)ptr2 + 12) != System.Runtime.CompilerServices.Unsafe.As<_D3DSURFACE_DESC, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DSURFACE_DESC, 28))))
								{
									num5 = _003CModule_003E.D3DXLoadSurfaceFromSurface(ptr3, null, null, ptr5, null, null, 1u, 0u);
								}
								if (num5 >= 0)
								{
									num5 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DLOCKED_RECT*, tagRECT*, uint, int>)(int)(*(uint*)(*(int*)ptr3 + 52)))((nint)ptr3, &d3DLOCKED_RECT, ptr2, 0u);
									if (num5 >= 0)
									{
										try
										{
											Texture.CopyData((void*)(int)System.Runtime.CompilerServices.Unsafe.As<_D3DLOCKED_RECT, uint>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DLOCKED_RECT, 4)), *(int*)(&d3DLOCKED_RECT), data, startIndex, elementCount, &d3DSURFACE_DESC, dwLockWidth, dwLockHeight, isSetting);
										}
										finally
										{
											IDirect3DSurface9* intPtr2 = ptr3;
											num5 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, int>)(int)(*(uint*)(*(int*)intPtr2 + 56)))((nint)intPtr2);
										}
										if (num5 >= 0)
										{
											num5 = _003CModule_003E.D3DXLoadSurfaceFromSurface(ptr5, null, ptr2, ptr3, null, ptr2, 1u, 0u);
										}
									}
								}
							}
							else
							{
								num5 = _003CModule_003E.D3DXLoadSurfaceFromSurface(ptr3, null, ptr2, ptr5, null, ptr2, 1u, 0u);
								if (num5 >= 0)
								{
									num5 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DLOCKED_RECT*, tagRECT*, uint, int>)(int)(*(uint*)(*(int*)ptr3 + 52)))((nint)ptr3, &d3DLOCKED_RECT, ptr2, 0u);
									if (num5 >= 0)
									{
										try
										{
											Texture.CopyData((void*)(int)System.Runtime.CompilerServices.Unsafe.As<_D3DLOCKED_RECT, uint>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DLOCKED_RECT, 4)), *(int*)(&d3DLOCKED_RECT), data, startIndex, elementCount, &d3DSURFACE_DESC, dwLockWidth, dwLockHeight, isSetting: false);
										}
										finally
										{
											IDirect3DSurface9* intPtr3 = ptr3;
											num5 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, int>)(int)(*(uint*)(*(int*)intPtr3 + 56)))((nint)intPtr3);
										}
									}
								}
							}
						}
					}
				}
				if (ptr5 != null)
				{
					IDirect3DSurface9* intPtr4 = ptr5;
					((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)intPtr4 + 8)))((nint)intPtr4);
					ptr5 = null;
				}
				if (ptr3 != null)
				{
					IDirect3DSurface9* intPtr5 = ptr3;
					((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)intPtr5 + 8)))((nint)intPtr5);
					ptr3 = null;
				}
				if (ptr4 != null)
				{
					IDirect3DTexture9* intPtr6 = ptr4;
					((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)intPtr6 + 8)))((nint)intPtr6);
					ptr4 = null;
				}
			}
			else
			{
				ptr = pComPtr;
				num5 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DLOCKED_RECT*, tagRECT*, uint, int>)(int)(*(uint*)(*(int*)ptr + 76)))((nint)ptr, (uint)level, &d3DLOCKED_RECT, ptr2, options);
				if (num5 >= 0)
				{
					try
					{
						Texture.CopyData((void*)(int)System.Runtime.CompilerServices.Unsafe.As<_D3DLOCKED_RECT, uint>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DLOCKED_RECT, 4)), *(int*)(&d3DLOCKED_RECT), data, startIndex, elementCount, &d3DSURFACE_DESC, dwLockWidth, dwLockHeight, isSetting);
					}
					finally
					{
						ptr = pComPtr;
						num5 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, int>)(int)(*(uint*)(*(int*)ptr + 80)))((nint)ptr, (uint)level);
					}
				}
			}
			if (_parent.IsDeviceLost)
			{
				if (isSetting)
				{
					return;
				}
				fixed (T* ptr7 = &data[startIndex])
				{
					try
					{
						uint num6 = (uint)(System.Runtime.CompilerServices.Unsafe.SizeOf<T>() * elementCount);
						// IL initblk instruction
						System.Runtime.CompilerServices.Unsafe.InitBlock(ptr7, 0, num6);
					}
					catch
					{
						//try-fault
						ptr7 = null;
						throw;
					}
				}
				return;
			}
			if (num5 < 0)
			{
				throw GraphicsHelpers.GetExceptionFromResult((uint)num5);
			}
			if (isSetting)
			{
				if (this is IDynamicGraphicsResource dynamicGraphicsResource)
				{
					dynamicGraphicsResource.SetContentLost(isContentLost: false);
				}
				renderTargetContentsDirty = true;
			}
			return;
		}
		throw new ArgumentNullException("data", FrameworkResources.NullNotAllowed);
	}

	private void SaveAsImage(Stream stream, SharedConstants.XnaImageFormat __unnamed001, int width, int height)
	{
		if (stream == null)
		{
			throw new ArgumentNullException("stream", FrameworkResources.NullNotAllowed);
		}
		if (!stream.CanWrite)
		{
			throw new ArgumentException("stream");
		}
		if (__unnamed001 != 0 && __unnamed001 != (SharedConstants.XnaImageFormat)2)
		{
			throw new ArgumentException("format");
		}
		SurfaceFormat format = _format;
		Color[] array2;
		int num;
		int height3;
		int width3;
		switch (format)
		{
		case SurfaceFormat.Color:
		{
			int width2 = _width;
			array2 = new Color[_height * width2];
			GetData(array2);
			goto IL_01da;
		}
		case SurfaceFormat.Bgr565:
			array2 = GetDataAsColor<Bgr565>();
			goto IL_01da;
		case SurfaceFormat.Bgra5551:
			array2 = GetDataAsColor<Bgra5551>();
			goto IL_01da;
		case SurfaceFormat.Bgra4444:
			array2 = GetDataAsColor<Bgra4444>();
			goto IL_01da;
		case SurfaceFormat.NormalizedByte2:
			array2 = GetDataAsColor<NormalizedByte2>();
			goto IL_01da;
		case SurfaceFormat.NormalizedByte4:
			array2 = GetDataAsColor<NormalizedByte4>();
			goto IL_01da;
		case SurfaceFormat.Rgba1010102:
			array2 = GetDataAsColor<Rgba1010102>();
			goto IL_01da;
		case SurfaceFormat.Rg32:
			array2 = GetDataAsColor<Rg32>();
			goto IL_01da;
		case SurfaceFormat.Rgba64:
			array2 = GetDataAsColor<Rgba64>();
			goto IL_01da;
		case SurfaceFormat.Alpha8:
			array2 = GetDataAsColor<Alpha8>();
			goto IL_01da;
		case SurfaceFormat.HalfSingle:
			array2 = GetDataAsColor<HalfSingle>();
			goto IL_01da;
		case SurfaceFormat.HalfVector2:
			array2 = GetDataAsColor<HalfVector2>();
			goto IL_01da;
		case SurfaceFormat.HalfVector4:
			array2 = GetDataAsColor<HalfVector4>();
			goto IL_01da;
		case SurfaceFormat.HdrBlendable:
			array2 = GetDataAsColor<HalfVector4>();
			goto IL_01da;
		case SurfaceFormat.Single:
			array2 = GetDataAsColor<float>(_003CModule_003E._003FA0x8419bba8_002ESingleToColor);
			goto IL_01da;
		case SurfaceFormat.Vector2:
			array2 = GetDataAsColor<Vector2>(_003CModule_003E._003FA0x8419bba8_002EVector2ToColor);
			goto IL_01da;
		case SurfaceFormat.Vector4:
			array2 = GetDataAsColor<Vector4>(_003CModule_003E._003FA0x8419bba8_002EVector4ToColor);
			goto IL_01da;
		case SurfaceFormat.Dxt1:
		case SurfaceFormat.Dxt3:
		case SurfaceFormat.Dxt5:
		{
			SurfaceFormat format2 = format;
			int height2 = _height;
			DxtDecoder dxtDecoder = new DxtDecoder(_width, height2, format2);
			byte[] array = new byte[dxtDecoder.PackedDataSize];
			GetData(array);
			array2 = dxtDecoder.Decode(array);
			goto IL_01da;
		}
		default:
			{
				throw new InvalidOperationException();
			}
			IL_01da:
			num = 0;
			if (0 < (nint)array2.LongLength)
			{
				do
				{
					if (array2[num].A == 0)
					{
						Color transparent = Color.Transparent;
						array2[num] = transparent;
					}
					num++;
				}
				while (num < (nint)array2.LongLength);
			}
			height3 = _height;
			width3 = _width;
			using (ImageStream imageStream = ImageStream.FromColors(array2, width3, height3, (XnaImageFormat)__unnamed001, width, height))
			{
				byte[] array3 = new BinaryReader(imageStream).ReadBytes((int)imageStream.Length);
				stream.Write(array3, 0, array3.Length);
				break;
			}
		}
	}

	private Color[] GetDataAsColor<T>(Converter<T, Color> toColor) where T : struct
	{
		int num = _height * _width;
		T[] array = new T[num];
		GetData(array);
		Color[] array2 = new Color[num];
		int num2 = 0;
		if (0 < num)
		{
			do
			{
				Color color = toColor(array[num2]);
				array2[num2] = color;
				num2++;
			}
			while (num2 < num);
		}
		return array2;
	}

	private Color[] GetDataAsColor<T>() where T : struct, IPackedVector
	{
		return GetDataAsColor<T>(_003CModule_003E._003FA0x8419bba8_002EPackedVectorToColor);
	}

	private protected unsafe void InitializeDescription(SurfaceFormat? format)
	{
		IDirect3DTexture9* ptr = pComPtr;
		System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DSURFACE_DESC d3DSURFACE_DESC);
		int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DSURFACE_DESC*, int>)(int)(*(uint*)(*(int*)ptr + 68)))((nint)ptr, 0u, &d3DSURFACE_DESC);
		if (num < 0)
		{
			throw GraphicsHelpers.GetExceptionFromResult((uint)num);
		}
		if (!format.HasValue)
		{
			format = _003CModule_003E.ConvertWindowsFormatToXna(*(_D3DFORMAT*)(&d3DSURFACE_DESC));
		}
		_width = System.Runtime.CompilerServices.Unsafe.As<_D3DSURFACE_DESC, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DSURFACE_DESC, 24));
		_height = System.Runtime.CompilerServices.Unsafe.As<_D3DSURFACE_DESC, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DSURFACE_DESC, 28));
		int shouldNotRecreate = ((System.Runtime.CompilerServices.Unsafe.As<_D3DSURFACE_DESC, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DSURFACE_DESC, 12)) == 0) ? 1 : 0);
		_shouldNotRecreate = (byte)shouldNotRecreate != 0;
		base.InitializeDescription(format.Value);
	}

	private protected unsafe void CreateTexture(GraphicsDevice graphicsDevice, int width, int height, [MarshalAs(UnmanagedType.U1)] bool mipMap, uint usage, _D3DPOOL pool, SurfaceFormat format)
	{
		if (graphicsDevice == null)
		{
			throw new ArgumentNullException("graphicsDevice", FrameworkResources.DeviceCannotBeNullOnResourceCreate);
		}
		ValidateCreationParameters(graphicsDevice._profileCapabilities, width, height, format, mipMap);
		int num = ((!mipMap) ? 1 : 0);
		fixed (IDirect3DTexture9** ptr = &pComPtr)
		{
			int num2 = *(int*)graphicsDevice.pComPtr + 92;
			int num3 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, uint, uint, uint, _D3DFORMAT, _D3DPOOL, IDirect3DTexture9**, void**, int>)(int)(*(uint*)num2))((nint)graphicsDevice.pComPtr, (uint)width, (uint)height, (uint)num, usage, _003CModule_003E.ConvertXnaFormatToWindows(format), pool, ptr, null);
			if (num3 < 0)
			{
				throw GraphicsHelpers.GetExceptionFromResult((uint)num3);
			}
			isDisposed = false;
			_parent = graphicsDevice;
			SurfaceFormat? format2 = format;
			InitializeDescription(format2);
			graphicsDevice.Resources.AddTrackedObject(this, pComPtr, (uint)pool, _internalHandle, ref _internalHandle);
		}
	}

	private protected static void ValidateCreationParameters(ProfileCapabilities profile, int width, int height, SurfaceFormat format, [MarshalAs(UnmanagedType.U1)] bool mipMap)
	{
		if (width <= 0)
		{
			throw new ArgumentOutOfRangeException("width", FrameworkResources.ResourcesMustBeGreaterThanZeroSize);
		}
		if (height <= 0)
		{
			throw new ArgumentOutOfRangeException("height", FrameworkResources.ResourcesMustBeGreaterThanZeroSize);
		}
		bool flag = Texture.CheckCompressedTexture(_003CModule_003E.ConvertXnaFormatToWindows(format));
		if (!profile.ValidTextureFormats.Contains(format))
		{
			profile.ThrowNotSupportedException(FrameworkResources.ProfileFormatNotSupported, typeof(Texture2D).Name, format);
		}
		int maxTextureSize = profile.MaxTextureSize;
		if (width > maxTextureSize || height > maxTextureSize)
		{
			profile.ThrowNotSupportedException(FrameworkResources.ProfileTooBig, typeof(Texture2D).Name, maxTextureSize);
		}
		int num = Math.Max(width, height);
		int num2 = Math.Min(width, height);
		int num3 = (num + num2 - 1) / num2;
		int maxTextureAspectRatio = profile.MaxTextureAspectRatio;
		if (num3 > maxTextureAspectRatio)
		{
			profile.ThrowNotSupportedException(FrameworkResources.ProfileAspectRatio, typeof(Texture2D).Name, maxTextureAspectRatio);
		}
		if (!profile.NonPow2Unconditional && (!Texture.IsPowerOfTwo((uint)width) || !Texture.IsPowerOfTwo((uint)height)))
		{
			if (mipMap)
			{
				profile.ThrowNotSupportedException(FrameworkResources.ProfileNotPowerOfTwoMipped, typeof(Texture2D).Name);
			}
			if (!flag)
			{
				return;
			}
			profile.ThrowNotSupportedException(FrameworkResources.ProfileNotPowerOfTwoDXT, typeof(Texture2D).Name);
		}
		if (flag && (((uint)width & 3u) != 0 || ((uint)height & 3u) != 0))
		{
			throw new ArgumentException(FrameworkResources.DxtNotMultipleOfFour);
		}
	}

	internal unsafe virtual int SaveDataForRecreation()
	{
		if (pComPtr == null)
		{
			return 0;
		}
		CleanupSavedData();
		int num = CopyOrRestoreData(isStoring: true);
		if (num >= 0)
		{
			ReleaseNativeObject(disposeManagedResource: false);
		}
		else
		{
			CleanupSavedData();
		}
		alreadyRecreated = false;
		return num;
	}

	int IGraphicsResource.SaveDataForRecreation()
	{
		//ILSpy generated this explicit interface implementation from .override directive in SaveDataForRecreation
		return this.SaveDataForRecreation();
	}

	internal unsafe virtual int RecreateAndPopulateObject()
	{
		if (_shouldNotRecreate)
		{
			return 0;
		}
		if (alreadyRecreated)
		{
			return 0;
		}
		if (pComPtr != null)
		{
			return -2147467259;
		}
		fixed (IDirect3DTexture9** ptr2 = &pComPtr)
		{
			IDirect3DDevice9* ptr = _parent.pComPtr;
			int num = *(int*)ptr + 92;
			int num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, uint, uint, uint, _D3DFORMAT, _D3DPOOL, IDirect3DTexture9**, void**, int>)(int)(*(uint*)num))((nint)ptr, (uint)_width, (uint)_height, (uint)_levelCount, 0u, _003CModule_003E.ConvertXnaFormatToWindows(_format), (_D3DPOOL)1, ptr2, null);
			if (num2 >= 0)
			{
				num2 = CreateStateWrapper();
				if (num2 >= 0)
				{
					num2 = CopyOrRestoreData(isStoring: false);
					if (num2 >= 0)
					{
						_parent.Resources.AddTrackedObject(this, pComPtr, 1u, _internalHandle, ref _internalHandle);
					}
				}
				CleanupSavedData();
			}
			alreadyRecreated = true;
			return num2;
		}
	}

	int IGraphicsResource.RecreateAndPopulateObject()
	{
		//ILSpy generated this explicit interface implementation from .override directive in RecreateAndPopulateObject
		return this.RecreateAndPopulateObject();
	}

	internal unsafe virtual void ReleaseNativeObject([MarshalAs(UnmanagedType.U1)] bool disposeManagedResource)
	{
		StateTrackerTexture* ptr = pStateTracker;
		if (ptr != null)
		{
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)ptr + 8)))((nint)ptr);
			pStateTracker = null;
		}
		GraphicsDevice parent = _parent;
		if (parent != null && pComPtr != null)
		{
			parent.Resources.ReleaseAllReferences(_internalHandle, disposeManagedResource);
		}
		pComPtr = null;
	}

	void IGraphicsResource.ReleaseNativeObject([MarshalAs(UnmanagedType.U1)] bool disposeManagedResource)
	{
		//ILSpy generated this explicit interface implementation from .override directive in ReleaseNativeObject
		this.ReleaseNativeObject(disposeManagedResource);
	}

	internal unsafe int CopyOrRestoreData([MarshalAs(UnmanagedType.U1)] bool isStoring)
	{
		IDirect3DTexture9* ptr = pComPtr;
		if (ptr == null)
		{
			return 0;
		}
		System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DSURFACE_DESC d3DSURFACE_DESC);
		int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DSURFACE_DESC*, int>)(int)(*(uint*)(*(int*)ptr + 68)))((nint)ptr, 0u, &d3DSURFACE_DESC);
		if (num >= 0)
		{
			if (isStoring)
			{
				pFaceData = new IntPtr[_levelCount];
			}
			uint num2 = System.Runtime.CompilerServices.Unsafe.As<_D3DSURFACE_DESC, uint>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DSURFACE_DESC, 24));
			uint num3 = System.Runtime.CompilerServices.Unsafe.As<_D3DSURFACE_DESC, uint>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DSURFACE_DESC, 28));
			bool flag = Texture.CheckCompressedTexture(*(_D3DFORMAT*)(&d3DSURFACE_DESC));
			byte b = ((!flag) ? Texture.GetExpectedByteSizeFromFormat(*(_D3DFORMAT*)(&d3DSURFACE_DESC)) : ((byte)((*(int*)(&d3DSURFACE_DESC) == 827611204) ? 8u : 16u)));
			int num4 = 0;
			int num5 = 0;
			if (0 < _levelCount)
			{
				System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DLOCKED_RECT d3DLOCKED_RECT);
				do
				{
					byte* ptr2 = null;
					int num6;
					if (!isStoring)
					{
						ref IntPtr reference = ref pFaceData[num4];
						num4++;
						ptr2 = (byte*)reference.ToPointer();
						num6 = 0;
					}
					else
					{
						num6 = 16;
					}
					ptr = pComPtr;
					num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DLOCKED_RECT*, tagRECT*, uint, int>)(int)(*(uint*)(*(int*)ptr + 76)))((nint)ptr, (uint)num5, &d3DLOCKED_RECT, null, (uint)num6);
					if (num < 0)
					{
						break;
					}
					uint num7 = num2;
					uint num8 = num3;
					if (flag)
					{
						num7 = num2 + 3 >> 2;
						num8 = num3 + 3 >> 2;
					}
					uint num9 = b * num7;
					byte* ptr3 = (byte*)(int)System.Runtime.CompilerServices.Unsafe.As<_D3DLOCKED_RECT, uint>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DLOCKED_RECT, 4));
					if (isStoring)
					{
						ptr2 = (byte*)_003CModule_003E.new_005B_005D(num9 * num8);
					}
					byte* ptr4 = ptr2;
					if (0 < num8)
					{
						uint num10 = num8;
						do
						{
							if (isStoring)
							{
								_003CModule_003E.memcpy_s(ptr4, num9 * num8, ptr3, num9);
							}
							else
							{
								_003CModule_003E.memcpy_s(ptr3, *(uint*)(&d3DLOCKED_RECT), ptr4, num9);
							}
							ptr3 = *(int*)(&d3DLOCKED_RECT) + ptr3;
							ptr4 = (int)num9 + ptr4;
							num10--;
						}
						while (num10 != 0);
					}
					ptr = pComPtr;
					num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, int>)(int)(*(uint*)(*(int*)ptr + 80)))((nint)ptr, (uint)num5);
					if (num < 0)
					{
						break;
					}
					if (isStoring)
					{
						IntPtr intPtr = (IntPtr)ptr2;
						ref IntPtr reference2 = ref pFaceData[num4];
						num4++;
						reference2 = intPtr;
					}
					num2 = ((num2 <= 1) ? 1u : (num2 >> 1));
					num3 = ((num3 <= 1) ? 1u : (num3 >> 1));
					num5++;
				}
				while (num5 < _levelCount);
			}
		}
		return num;
	}

	internal unsafe void CleanupSavedData()
	{
		IntPtr[] array = pFaceData;
		if (array == null)
		{
			return;
		}
		int num = 0;
		if (0 < (nint)array.LongLength)
		{
			do
			{
				void* ptr = pFaceData[num].ToPointer();
				if (ptr != null)
				{
					_003CModule_003E.delete_005B_005D(ptr);
				}
				ref IntPtr reference = ref pFaceData[num];
				reference = IntPtr.Zero;
				num++;
			}
			while (num < (nint)pFaceData.LongLength);
		}
		pFaceData = null;
	}

	internal unsafe override IDirect3DBaseTexture9* GetComPtr()
	{
		return (IDirect3DBaseTexture9*)pComPtr;
	}

	internal unsafe static Texture2D GetManagedObject(IDirect3DTexture9* pInterface, GraphicsDevice pDevice, uint pool)
	{
		Texture2D texture2D = pDevice.Resources.GetCachedObject(pInterface) as Texture2D;
		if (texture2D != null)
		{
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)pInterface + 8)))((nint)pInterface);
			texture2D.isDisposed = false;
			GC.ReRegisterForFinalize(texture2D);
		}
		else
		{
			texture2D = new Texture2D(pInterface, pDevice);
			pDevice.Resources.AddTrackedObject(texture2D, pInterface, pool, 0uL, ref texture2D._internalHandle);
		}
		return texture2D;
	}

	private void OnObjectCreation()
	{
		InitializeDescription(null);
	}

	private void _0021Texture2D()
	{
		if (!isDisposed)
		{
			ReleaseNativeObject(disposeManagedResource: true);
			CleanupSavedData();
		}
	}

	private void _007ETexture2D()
	{
		_0021Texture2D();
	}

	[HandleProcessCorruptedStateExceptions]
	protected override void Dispose([MarshalAs(UnmanagedType.U1)] bool P_0)
	{
		if (P_0)
		{
			try
			{
				_007ETexture2D();
				return;
			}
			finally
			{
				base.Dispose(true);
			}
		}
		try
		{
			_0021Texture2D();
		}
		finally
		{
			base.Dispose(false);
		}
	}
}

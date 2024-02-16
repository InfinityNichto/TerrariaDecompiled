using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using _003CCppImplementationDetails_003E;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class GraphicsAdapter
{
	private class CleanupHelper : IDisposable
	{
		private unsafe void _0021CleanupHelper()
		{
			if (pComPtr != null)
			{
				int num = *(int*)pComPtr + 8;
				((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)num))((nint)pComPtr);
				pComPtr = null;
			}
		}

		private void _007ECleanupHelper()
		{
		}

		[HandleProcessCorruptedStateExceptions]
		protected virtual void Dispose([MarshalAs(UnmanagedType.U1)] bool P_0)
		{
			if (!P_0)
			{
				try
				{
					_0021CleanupHelper();
				}
				finally
				{
					base.Finalize();
				}
			}
		}

		public virtual sealed void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~CleanupHelper()
		{
			Dispose(false);
		}
	}

	internal unsafe static IDirect3D9* pComPtr;

	private static ReadOnlyCollection<GraphicsAdapter> pAdapterList;

	private static CleanupHelper pCleanupHelper;

	private DisplayMode _currentDisplayMode;

	private DisplayModeCollection _supportedDisplayModes;

	private string _description;

	private string _deviceName;

	private int _vendorId;

	private int _deviceId;

	private int _subSystemId;

	private int _revision;

	internal uint adapter;

	private static bool _003Cbacking_store_003EUseNullDevice;

	private static bool _003Cbacking_store_003EUseReferenceDevice;

	internal static _D3DDEVTYPE CurrentDeviceType
	{
		get
		{
			if (_003Cbacking_store_003EUseNullDevice)
			{
				return (_D3DDEVTYPE)4;
			}
			return (!_003Cbacking_store_003EUseReferenceDevice) ? ((_D3DDEVTYPE)1) : ((_D3DDEVTYPE)2);
		}
	}

	public static bool UseReferenceDevice
	{
		[return: MarshalAs(UnmanagedType.U1)]
		get
		{
			return _003Cbacking_store_003EUseReferenceDevice;
		}
		[param: MarshalAs(UnmanagedType.U1)]
		set
		{
			_003Cbacking_store_003EUseReferenceDevice = value;
		}
	}

	public static bool UseNullDevice
	{
		[return: MarshalAs(UnmanagedType.U1)]
		get
		{
			return _003Cbacking_store_003EUseNullDevice;
		}
		[param: MarshalAs(UnmanagedType.U1)]
		set
		{
			_003Cbacking_store_003EUseNullDevice = value;
		}
	}

	public unsafe IntPtr MonitorHandle
	{
		get
		{
			int num = *(int*)pComPtr + 60;
			return (IntPtr)((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, HMONITOR__*>)(int)(*(uint*)num))((nint)pComPtr, adapter);
		}
	}

	public unsafe DisplayModeCollection SupportedDisplayModes
	{
		get
		{
			if (_supportedDisplayModes == null)
			{
				List<DisplayMode> list = new List<DisplayMode>();
				if (pComPtr != null)
				{
					List<SurfaceFormat>.Enumerator enumerator = ProfileCapabilities.HiDef.ValidTextureFormats.GetEnumerator();
					if (enumerator.MoveNext())
					{
						System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DDISPLAYMODE d3DDISPLAYMODE);
						do
						{
							SurfaceFormat current = enumerator.Current;
							_D3DFORMAT d3DFORMAT = _003CModule_003E.ConvertXnaFormatToWindowsAdapterFormat(current);
							int num = *(int*)pComPtr + 24;
							int num2 = (int)((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DFORMAT, uint>)(int)(*(uint*)num))((nint)pComPtr, adapter, d3DFORMAT);
							if (num2 <= 0)
							{
								continue;
							}
							Dictionary<Point, bool> dictionary = new Dictionary<Point, bool>();
							int num3 = 0;
							if (0 >= num2)
							{
								continue;
							}
							do
							{
								int num4 = *(int*)pComPtr + 28;
								if (((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DFORMAT, uint, _D3DDISPLAYMODE*, int>)(int)(*(uint*)num4))((nint)pComPtr, adapter, d3DFORMAT, (uint)num3, &d3DDISPLAYMODE) >= 0)
								{
									Point key = new Point(*(int*)(&d3DDISPLAYMODE), System.Runtime.CompilerServices.Unsafe.As<_D3DDISPLAYMODE, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DDISPLAYMODE, 4)));
									if (!dictionary.ContainsKey(key))
									{
										dictionary.Add(key, value: true);
										list.Add(new DisplayMode(*(int*)(&d3DDISPLAYMODE), System.Runtime.CompilerServices.Unsafe.As<_D3DDISPLAYMODE, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DDISPLAYMODE, 4)), current));
									}
								}
								num3++;
							}
							while (num3 < num2);
						}
						while (enumerator.MoveNext());
					}
				}
				_supportedDisplayModes = new DisplayModeCollection(list);
			}
			return _supportedDisplayModes;
		}
	}

	public unsafe DisplayMode CurrentDisplayMode
	{
		get
		{
			if (_currentDisplayMode == null)
			{
				int num = *(int*)pComPtr + 32;
				System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DDISPLAYMODE d3DDISPLAYMODE);
				((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DDISPLAYMODE*, int>)(int)(*(uint*)num))((nint)pComPtr, adapter, &d3DDISPLAYMODE);
				DisplayMode displayMode = (_currentDisplayMode = new DisplayMode(*(int*)(&d3DDISPLAYMODE), System.Runtime.CompilerServices.Unsafe.As<_D3DDISPLAYMODE, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DDISPLAYMODE, 4)), _003CModule_003E.ConvertWindowsFormatToXna(System.Runtime.CompilerServices.Unsafe.As<_D3DDISPLAYMODE, _D3DFORMAT>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DDISPLAYMODE, 12)))));
				if (displayMode._format < SurfaceFormat.Color)
				{
					displayMode._format = SurfaceFormat.Color;
				}
			}
			return _currentDisplayMode;
		}
	}

	public bool IsWideScreen
	{
		[return: MarshalAs(UnmanagedType.U1)]
		get
		{
			return (double)CurrentDisplayMode.AspectRatio > 1.600000023841858;
		}
	}

	public bool IsDefaultAdapter
	{
		[return: MarshalAs(UnmanagedType.U1)]
		get
		{
			return adapter == 0;
		}
	}

	public int Revision => _revision;

	public int SubSystemId => _subSystemId;

	public int DeviceId => _deviceId;

	public int VendorId => _vendorId;

	public string DeviceName => _deviceName;

	public string Description => _description;

	public static GraphicsAdapter DefaultAdapter => pAdapterList[0];

	public static ReadOnlyCollection<GraphicsAdapter> Adapters => pAdapterList;

	static GraphicsAdapter()
	{
		InitalizeGraphics();
		InitializeAdapterList();
	}

	private unsafe static void InitalizeGraphics()
	{
		HINSTANCE__* ptr = _003CModule_003E.LoadLibraryW((ushort*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _003CModule_003E._003F_003F_C_0040_1BK_0040FPBCHPIH_0040_003F_0024AAd_003F_0024AA3_003F_0024AAd_003F_0024AAx_003F_0024AA9_003F_0024AA__003F_0024AA4_003F_0024AA1_003F_0024AA_003F4_003F_0024AAd_003F_0024AAl_003F_0024AAl_003F_0024AA_003F_0024AA_0040));
		try
		{
			if (ptr == null)
			{
				throw new FileNotFoundException(string.Format(args: new object[1] { "d3dx9_41.dll" }, provider: CultureInfo.CurrentCulture, format: FrameworkResources.MissingNativeDependency));
			}
			pComPtr = _003CModule_003E.Direct3DCreate9(32u);
			pCleanupHelper = new CleanupHelper();
		}
		finally
		{
			if (ptr != null)
			{
				_003CModule_003E.FreeLibrary(ptr);
			}
		}
		_003CModule_003E.D3DPERF_SetOptions(2u);
	}

	private unsafe static void InitializeAdapterList()
	{
		List<GraphicsAdapter> list = new List<GraphicsAdapter>();
		int num = *(int*)pComPtr + 16;
		uint num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)num))((nint)pComPtr);
		uint num3 = 0u;
		if (0 < num2)
		{
			do
			{
				list.Add(new GraphicsAdapter(num3));
				num3++;
			}
			while (num3 < num2);
		}
		pAdapterList = new ReadOnlyCollection<GraphicsAdapter>(list);
	}

	private unsafe GraphicsAdapter(uint adapterOrdinal)
	{
		adapter = adapterOrdinal;
		base._002Ector();
		int num = *(int*)pComPtr + 20;
		System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DADAPTER_IDENTIFIER9 d3DADAPTER_IDENTIFIER);
		((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, uint, _D3DADAPTER_IDENTIFIER9*, int>)(int)(*(uint*)num))((nint)pComPtr, adapter, 0u, &d3DADAPTER_IDENTIFIER);
		IntPtr ptr = (IntPtr)System.Runtime.CompilerServices.Unsafe.AsPointer(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DADAPTER_IDENTIFIER, 512));
		_description = Marshal.PtrToStringAnsi(ptr);
		IntPtr ptr2 = (IntPtr)System.Runtime.CompilerServices.Unsafe.AsPointer(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DADAPTER_IDENTIFIER, 1024));
		_deviceName = Marshal.PtrToStringAnsi(ptr2);
		_vendorId = System.Runtime.CompilerServices.Unsafe.As<_D3DADAPTER_IDENTIFIER9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DADAPTER_IDENTIFIER, 1064));
		_deviceId = System.Runtime.CompilerServices.Unsafe.As<_D3DADAPTER_IDENTIFIER9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DADAPTER_IDENTIFIER, 1068));
		_subSystemId = System.Runtime.CompilerServices.Unsafe.As<_D3DADAPTER_IDENTIFIER9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DADAPTER_IDENTIFIER, 1072));
		_revision = System.Runtime.CompilerServices.Unsafe.As<_D3DADAPTER_IDENTIFIER9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DADAPTER_IDENTIFIER, 1076));
	}

	[return: MarshalAs(UnmanagedType.U1)]
	public bool QueryBackBufferFormat(GraphicsProfile graphicsProfile, SurfaceFormat format, DepthFormat depthFormat, int multiSampleCount, out SurfaceFormat selectedFormat, out DepthFormat selectedDepthFormat, out int selectedMultiSampleCount)
	{
		_D3DDEVTYPE deviceType = ((!_003Cbacking_store_003EUseNullDevice) ? ((!_003Cbacking_store_003EUseReferenceDevice) ? ((_D3DDEVTYPE)1) : ((_D3DDEVTYPE)2)) : ((_D3DDEVTYPE)4));
		return QueryFormat(isBackBuffer: true, deviceType, graphicsProfile, format, depthFormat, multiSampleCount, out selectedFormat, out selectedDepthFormat, out selectedMultiSampleCount);
	}

	[return: MarshalAs(UnmanagedType.U1)]
	public bool QueryRenderTargetFormat(GraphicsProfile graphicsProfile, SurfaceFormat format, DepthFormat depthFormat, int multiSampleCount, out SurfaceFormat selectedFormat, out DepthFormat selectedDepthFormat, out int selectedMultiSampleCount)
	{
		_D3DDEVTYPE deviceType = ((!_003Cbacking_store_003EUseNullDevice) ? ((!_003Cbacking_store_003EUseReferenceDevice) ? ((_D3DDEVTYPE)1) : ((_D3DDEVTYPE)2)) : ((_D3DDEVTYPE)4));
		return QueryFormat(isBackBuffer: false, deviceType, graphicsProfile, format, depthFormat, multiSampleCount, out selectedFormat, out selectedDepthFormat, out selectedMultiSampleCount);
	}

	[return: MarshalAs(UnmanagedType.U1)]
	internal bool QueryFormat([MarshalAs(UnmanagedType.U1)] bool isBackBuffer, _D3DDEVTYPE deviceType, GraphicsProfile graphicsProfile, SurfaceFormat format, DepthFormat depthFormat, int multiSampleCount, out SurfaceFormat selectedFormat, out DepthFormat selectedDepthFormat, out int selectedMultiSampleCount)
	{
		ProfileCapabilities instance = ProfileCapabilities.GetInstance(graphicsProfile);
		selectedDepthFormat = QueryDepthFormat(depthFormat, selectedFormat = QuerySurfaceFormat(format, isBackBuffer, deviceType, instance), deviceType, instance);
		int num = (selectedMultiSampleCount = QueryMultiSampleCount(multiSampleCount, selectedFormat, deviceType));
		int num2 = ((selectedFormat == format && selectedDepthFormat == depthFormat && num == multiSampleCount) ? 1 : 0);
		return (byte)num2 != 0;
	}

	private unsafe SurfaceFormat QuerySurfaceFormat(SurfaceFormat format, [MarshalAs(UnmanagedType.U1)] bool isBackBuffer, _D3DDEVTYPE deviceType, ProfileCapabilities profileCapabilities)
	{
		if (IsValidSurfaceFormat(format, isBackBuffer, deviceType, profileCapabilities))
		{
			return format;
		}
		if ((_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA & 1) == 0)
		{
			_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA |= 1u;
			try
			{
				System.Runtime.CompilerServices.Unsafe.As<_0024ArrayType_0024_0024_0024BY05W4SurfaceFormat_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref _003CModule_003E._003FA0x49afb68d_002E_003FcolorFallbacks_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404PAW473456_0040A, 20)) = -1;
			}
			catch
			{
				//try-fault
				_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA &= 4294967294u;
				throw;
			}
		}
		if ((_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA & 2) == 0)
		{
			_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA |= 2u;
			try
			{
				System.Runtime.CompilerServices.Unsafe.As<_0024ArrayType_0024_0024_0024BY05W4SurfaceFormat_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref _003CModule_003E._003FA0x49afb68d_002E_003Fbgr565Fallbacks_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404PAW473456_0040A, 20)) = -1;
			}
			catch
			{
				//try-fault
				_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA &= 4294967293u;
				throw;
			}
		}
		if ((_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA & 4) == 0)
		{
			_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA |= 4u;
			try
			{
				System.Runtime.CompilerServices.Unsafe.As<_0024ArrayType_0024_0024_0024BY05W4SurfaceFormat_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref _003CModule_003E._003FA0x49afb68d_002E_003Fbgra5551Fallbacks_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404PAW473456_0040A, 20)) = -1;
			}
			catch
			{
				//try-fault
				_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA &= 4294967291u;
				throw;
			}
		}
		if ((_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA & 8) == 0)
		{
			_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA |= 8u;
			try
			{
				System.Runtime.CompilerServices.Unsafe.As<_0024ArrayType_0024_0024_0024BY05W4SurfaceFormat_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref _003CModule_003E._003FA0x49afb68d_002E_003Fbgra4444Fallbacks_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404PAW473456_0040A, 20)) = -1;
			}
			catch
			{
				//try-fault
				_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA &= 4294967287u;
				throw;
			}
		}
		if ((_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA & 0x10) == 0)
		{
			_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA |= 16u;
			try
			{
				System.Runtime.CompilerServices.Unsafe.As<_0024ArrayType_0024_0024_0024BY05W4SurfaceFormat_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref _003CModule_003E._003FA0x49afb68d_002E_003Frgba1010102Fallbacks_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404PAW473456_0040A, 20)) = -1;
			}
			catch
			{
				//try-fault
				_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA &= 4294967279u;
				throw;
			}
		}
		if ((_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA & 0x20) == 0)
		{
			_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA |= 32u;
			try
			{
				System.Runtime.CompilerServices.Unsafe.As<_0024ArrayType_0024_0024_0024BY06W4SurfaceFormat_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref _003CModule_003E._003FA0x49afb68d_002E_003Frg32Fallbacks_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404PAW473456_0040A, 24)) = -1;
			}
			catch
			{
				//try-fault
				_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA &= 4294967263u;
				throw;
			}
		}
		if ((_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA & 0x40) == 0)
		{
			_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA |= 64u;
			try
			{
				System.Runtime.CompilerServices.Unsafe.As<_0024ArrayType_0024_0024_0024BY05W4SurfaceFormat_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref _003CModule_003E._003FA0x49afb68d_002E_003Frgba64Fallbacks_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404PAW473456_0040A, 20)) = -1;
			}
			catch
			{
				//try-fault
				_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA &= 4294967231u;
				throw;
			}
		}
		if ((_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA & 0x80) == 0)
		{
			_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA |= 128u;
			try
			{
				System.Runtime.CompilerServices.Unsafe.As<_0024ArrayType_0024_0024_0024BY0N_0040W4SurfaceFormat_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref _003CModule_003E._003FA0x49afb68d_002E_003FsingleFallbacks_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404PAW473456_0040A, 48)) = -1;
			}
			catch
			{
				//try-fault
				_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA &= 4294967167u;
				throw;
			}
		}
		if ((_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA & 0x100) == 0)
		{
			_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA |= 256u;
			try
			{
				System.Runtime.CompilerServices.Unsafe.As<_0024ArrayType_0024_0024_0024BY0L_0040W4SurfaceFormat_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref _003CModule_003E._003FA0x49afb68d_002E_003Fvector2Fallbacks_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404PAW473456_0040A, 40)) = -1;
			}
			catch
			{
				//try-fault
				_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA &= 4294967039u;
				throw;
			}
		}
		if ((_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA & 0x200) == 0)
		{
			_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA |= 512u;
			try
			{
				System.Runtime.CompilerServices.Unsafe.As<_0024ArrayType_0024_0024_0024BY07W4SurfaceFormat_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref _003CModule_003E._003FA0x49afb68d_002E_003Fvector4Fallbacks_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404PAW473456_0040A, 28)) = -1;
			}
			catch
			{
				//try-fault
				_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA &= 4294966783u;
				throw;
			}
		}
		if ((_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA & 0x400) == 0)
		{
			_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA |= 1024u;
			try
			{
				System.Runtime.CompilerServices.Unsafe.As<_0024ArrayType_0024_0024_0024BY0N_0040W4SurfaceFormat_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref _003CModule_003E._003FA0x49afb68d_002E_003FhalfSingleFallbacks_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404PAW473456_0040A, 48)) = -1;
			}
			catch
			{
				//try-fault
				_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA &= 4294966271u;
				throw;
			}
		}
		if ((_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA & 0x800) == 0)
		{
			_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA |= 2048u;
			try
			{
				System.Runtime.CompilerServices.Unsafe.As<_0024ArrayType_0024_0024_0024BY0L_0040W4SurfaceFormat_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref _003CModule_003E._003FA0x49afb68d_002E_003FhalfVector2Fallbacks_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404PAW473456_0040A, 40)) = -1;
			}
			catch
			{
				//try-fault
				_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA &= 4294965247u;
				throw;
			}
		}
		if ((_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA & 0x1000) == 0)
		{
			_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA |= 4096u;
			try
			{
				System.Runtime.CompilerServices.Unsafe.As<_0024ArrayType_0024_0024_0024BY07W4SurfaceFormat_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref _003CModule_003E._003FA0x49afb68d_002E_003FhalfVector4Fallbacks_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404PAW473456_0040A, 28)) = -1;
			}
			catch
			{
				//try-fault
				_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA &= 4294963199u;
				throw;
			}
		}
		if ((_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA & 0x2000) == 0)
		{
			_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA |= 8192u;
			try
			{
				System.Runtime.CompilerServices.Unsafe.As<_0024ArrayType_0024_0024_0024BY06W4SurfaceFormat_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref _003CModule_003E._003FA0x49afb68d_002E_003FdefaultFallbacks_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404PAW473456_0040A, 24)) = -1;
			}
			catch
			{
				//try-fault
				_003CModule_003E._003FA0x49afb68d_002E_003F_0024S2_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404IA &= 4294959103u;
				throw;
			}
		}
		SurfaceFormat* ptr = format switch
		{
			SurfaceFormat.Color => (SurfaceFormat*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _003CModule_003E._003FA0x49afb68d_002E_003FcolorFallbacks_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404PAW473456_0040A), 
			SurfaceFormat.Bgr565 => (SurfaceFormat*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _003CModule_003E._003FA0x49afb68d_002E_003Fbgr565Fallbacks_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404PAW473456_0040A), 
			SurfaceFormat.Bgra5551 => (SurfaceFormat*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _003CModule_003E._003FA0x49afb68d_002E_003Fbgra5551Fallbacks_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404PAW473456_0040A), 
			SurfaceFormat.Bgra4444 => (SurfaceFormat*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _003CModule_003E._003FA0x49afb68d_002E_003Fbgra4444Fallbacks_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404PAW473456_0040A), 
			SurfaceFormat.Rgba1010102 => (SurfaceFormat*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _003CModule_003E._003FA0x49afb68d_002E_003Frgba1010102Fallbacks_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404PAW473456_0040A), 
			SurfaceFormat.Rg32 => (SurfaceFormat*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _003CModule_003E._003FA0x49afb68d_002E_003Frg32Fallbacks_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404PAW473456_0040A), 
			SurfaceFormat.Rgba64 => (SurfaceFormat*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _003CModule_003E._003FA0x49afb68d_002E_003Frgba64Fallbacks_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404PAW473456_0040A), 
			SurfaceFormat.Single => (SurfaceFormat*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _003CModule_003E._003FA0x49afb68d_002E_003FsingleFallbacks_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404PAW473456_0040A), 
			SurfaceFormat.Vector2 => (SurfaceFormat*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _003CModule_003E._003FA0x49afb68d_002E_003Fvector2Fallbacks_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404PAW473456_0040A), 
			SurfaceFormat.Vector4 => (SurfaceFormat*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _003CModule_003E._003FA0x49afb68d_002E_003Fvector4Fallbacks_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404PAW473456_0040A), 
			SurfaceFormat.HalfSingle => (SurfaceFormat*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _003CModule_003E._003FA0x49afb68d_002E_003FhalfSingleFallbacks_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404PAW473456_0040A), 
			SurfaceFormat.HalfVector2 => (SurfaceFormat*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _003CModule_003E._003FA0x49afb68d_002E_003FhalfVector2Fallbacks_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404PAW473456_0040A), 
			SurfaceFormat.HalfVector4 => (SurfaceFormat*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _003CModule_003E._003FA0x49afb68d_002E_003FhalfVector4Fallbacks_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404PAW473456_0040A), 
			SurfaceFormat.HdrBlendable => (SurfaceFormat*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _003CModule_003E._003FA0x49afb68d_002E_003FhalfVector4Fallbacks_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404PAW473456_0040A), 
			_ => (SurfaceFormat*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _003CModule_003E._003FA0x49afb68d_002E_003FdefaultFallbacks_0040_003F4_003F_003FQuerySurfaceFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4SurfaceFormat_00403456_0040W473456_0040_NW4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404PAW473456_0040A), 
		};
		int num = 0;
		if (*ptr > (SurfaceFormat)(-1))
		{
			do
			{
				if (!IsValidSurfaceFormat(*(SurfaceFormat*)(sizeof(SurfaceFormat) * num + (byte*)ptr), isBackBuffer, deviceType, profileCapabilities))
				{
					num++;
					continue;
				}
				return *(SurfaceFormat*)(sizeof(SurfaceFormat) * num + (byte*)ptr);
			}
			while (*(int*)(sizeof(SurfaceFormat) * num + (byte*)ptr) > -1);
		}
		throw new InvalidOperationException(FrameworkResources.DriverError);
	}

	private unsafe DepthFormat QueryDepthFormat(DepthFormat depthFormat, SurfaceFormat surfaceFormat, _D3DDEVTYPE deviceType, ProfileCapabilities profileCapabilities)
	{
		if (IsValidDepthFormat(depthFormat, surfaceFormat, deviceType, profileCapabilities))
		{
			return depthFormat;
		}
		int num = 0;
		if (System.Runtime.CompilerServices.Unsafe.As<_0024ArrayType_0024_0024_0024BY03W4DepthFormat_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040, int>(ref _003CModule_003E._003FA0x49afb68d_002E_003FfallbackFormats_0040_003F4_003F_003FQueryDepthFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4DepthFormat_00403456_0040W473456_0040W4SurfaceFormat_00403456_0040W4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404PAW473456_0040A) != 0)
		{
			do
			{
				int num2 = *(int*)((ref *(_003F*)(sizeof(DepthFormat) * num)) + (ref System.Runtime.CompilerServices.Unsafe.As<_0024ArrayType_0024_0024_0024BY03W4DepthFormat_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040, _003F>(ref _003CModule_003E._003FA0x49afb68d_002E_003FfallbackFormats_0040_003F4_003F_003FQueryDepthFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4DepthFormat_00403456_0040W473456_0040W4SurfaceFormat_00403456_0040W4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404PAW473456_0040A)));
				if (num2 != (int)depthFormat)
				{
					DepthFormat depthFormat2 = (DepthFormat)num2;
					if (depthFormat2 != 0)
					{
						if (!profileCapabilities.ValidDepthFormats.Contains(depthFormat2))
						{
							goto IL_007f;
						}
						if (deviceType != (_D3DDEVTYPE)4)
						{
							int num3 = *(int*)pComPtr + 48;
							if (((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DDEVTYPE, _D3DFORMAT, _D3DFORMAT, _D3DFORMAT, int>)(int)(*(uint*)num3))((nint)pComPtr, adapter, deviceType, ProfileChecker.IRRELEVANT_ADAPTER_FORMAT, _003CModule_003E.ConvertXnaFormatToWindows(surfaceFormat), _003CModule_003E.ConvertXnaFormatToWindows(depthFormat2)) < 0)
							{
								goto IL_007f;
							}
						}
					}
					return *(DepthFormat*)((ref *(_003F*)(sizeof(DepthFormat) * num)) + (ref System.Runtime.CompilerServices.Unsafe.As<_0024ArrayType_0024_0024_0024BY03W4DepthFormat_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040, _003F>(ref _003CModule_003E._003FA0x49afb68d_002E_003FfallbackFormats_0040_003F4_003F_003FQueryDepthFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4DepthFormat_00403456_0040W473456_0040W4SurfaceFormat_00403456_0040W4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404PAW473456_0040A)));
				}
				goto IL_007f;
				IL_007f:
				num++;
			}
			while (*(int*)((ref *(_003F*)(sizeof(DepthFormat) * num)) + (ref System.Runtime.CompilerServices.Unsafe.As<_0024ArrayType_0024_0024_0024BY03W4DepthFormat_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040, _003F>(ref _003CModule_003E._003FA0x49afb68d_002E_003FfallbackFormats_0040_003F4_003F_003FQueryDepthFormat_0040GraphicsAdapter_0040Graphics_0040Framework_0040Xna_0040Microsoft_0040_0040A_0024AAM_003FAW4DepthFormat_00403456_0040W473456_0040W4SurfaceFormat_00403456_0040W4_D3DDEVTYPE_0040_0040P_0024AAVProfileCapabilities_00403456_0040_0040Z_00404PAW473456_0040A))) != 0);
		}
		throw new InvalidOperationException(FrameworkResources.DriverError);
	}

	private unsafe int QueryMultiSampleCount(int multiSampleCount, SurfaceFormat surfaceFormat, _D3DDEVTYPE deviceType)
	{
		if (multiSampleCount <= 1)
		{
			return 0;
		}
		if (multiSampleCount > 16)
		{
			multiSampleCount = 16;
		}
		do
		{
			int num = *(int*)pComPtr + 44;
			if (((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DDEVTYPE, _D3DFORMAT, int, _D3DMULTISAMPLE_TYPE, uint*, int>)(int)(*(uint*)num))((nint)pComPtr, adapter, deviceType, _003CModule_003E.ConvertXnaFormatToWindows(surfaceFormat), 0, (_D3DMULTISAMPLE_TYPE)multiSampleCount, null) < 0)
			{
				multiSampleCount--;
				continue;
			}
			return multiSampleCount;
		}
		while (multiSampleCount > 1);
		return 0;
	}

	[return: MarshalAs(UnmanagedType.U1)]
	private unsafe bool IsValidSurfaceFormat(SurfaceFormat format, [MarshalAs(UnmanagedType.U1)] bool isBackBuffer, _D3DDEVTYPE deviceType, ProfileCapabilities profileCapabilities)
	{
		if (!profileCapabilities.ValidTextureFormats.Contains(format))
		{
			return false;
		}
		bool flag = profileCapabilities.InvalidBlendFormats.Contains(format);
		if (isBackBuffer)
		{
			if (flag)
			{
				return false;
			}
			if (deviceType != (_D3DDEVTYPE)4)
			{
				int num = *(int*)pComPtr + 36;
				if (((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DDEVTYPE, _D3DFORMAT, _D3DFORMAT, int, int>)(int)(*(uint*)num))((nint)pComPtr, adapter, deviceType, _003CModule_003E.ConvertXnaFormatToWindowsAdapterFormat(format), _003CModule_003E.ConvertXnaFormatToWindows(format), 0) < 0)
				{
					return false;
				}
			}
		}
		uint num2 = 1u;
		if (!flag)
		{
			num2 = 524289u;
		}
		int num3 = *(int*)pComPtr + 40;
		return ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DDEVTYPE, _D3DFORMAT, uint, _D3DRESOURCETYPE, _D3DFORMAT, int>)(int)(*(uint*)num3))((nint)pComPtr, adapter, deviceType, ProfileChecker.IRRELEVANT_ADAPTER_FORMAT, num2, (_D3DRESOURCETYPE)3, _003CModule_003E.ConvertXnaFormatToWindows(format)) >= 0;
	}

	[return: MarshalAs(UnmanagedType.U1)]
	private unsafe bool IsValidDepthFormat(DepthFormat depthFormat, SurfaceFormat surfaceFormat, _D3DDEVTYPE deviceType, ProfileCapabilities profileCapabilities)
	{
		if (depthFormat == DepthFormat.None)
		{
			return true;
		}
		if (!profileCapabilities.ValidDepthFormats.Contains(depthFormat))
		{
			return false;
		}
		if (deviceType != (_D3DDEVTYPE)4)
		{
			int num = *(int*)pComPtr + 48;
			if (((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DDEVTYPE, _D3DFORMAT, _D3DFORMAT, _D3DFORMAT, int>)(int)(*(uint*)num))((nint)pComPtr, adapter, deviceType, ProfileChecker.IRRELEVANT_ADAPTER_FORMAT, _003CModule_003E.ConvertXnaFormatToWindows(surfaceFormat), _003CModule_003E.ConvertXnaFormatToWindows(depthFormat)) < 0)
			{
				return false;
			}
		}
		return true;
	}

	[return: MarshalAs(UnmanagedType.U1)]
	internal unsafe bool IsProfileSupported(_D3DDEVTYPE deviceType, GraphicsProfile graphicsProfile)
	{
		return ProfileChecker.IsProfileSupported(pComPtr, adapter, deviceType, graphicsProfile);
	}

	[return: MarshalAs(UnmanagedType.U1)]
	public unsafe bool IsProfileSupported(GraphicsProfile graphicsProfile)
	{
		return ProfileChecker.IsProfileSupported(deviceType: (!_003Cbacking_store_003EUseNullDevice) ? ((!_003Cbacking_store_003EUseReferenceDevice) ? ((_D3DDEVTYPE)1) : ((_D3DDEVTYPE)2)) : ((_D3DDEVTYPE)4), pD3D: pComPtr, adapter: adapter, graphicsProfile: graphicsProfile);
	}
}

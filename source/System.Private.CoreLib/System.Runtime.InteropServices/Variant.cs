using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices;

[StructLayout(LayoutKind.Explicit)]
[SupportedOSPlatform("windows")]
internal struct Variant
{
	private struct TypeUnion
	{
		public ushort _vt;

		public ushort _wReserved1;

		public ushort _wReserved2;

		public ushort _wReserved3;

		public UnionTypes _unionTypes;
	}

	private struct Record
	{
		public IntPtr _record;

		public IntPtr _recordInfo;
	}

	[StructLayout(LayoutKind.Explicit)]
	private struct UnionTypes
	{
		[FieldOffset(0)]
		public sbyte _i1;

		[FieldOffset(0)]
		public short _i2;

		[FieldOffset(0)]
		public int _i4;

		[FieldOffset(0)]
		public long _i8;

		[FieldOffset(0)]
		public byte _ui1;

		[FieldOffset(0)]
		public ushort _ui2;

		[FieldOffset(0)]
		public uint _ui4;

		[FieldOffset(0)]
		public ulong _ui8;

		[FieldOffset(0)]
		public int _int;

		[FieldOffset(0)]
		public uint _uint;

		[FieldOffset(0)]
		public short _bool;

		[FieldOffset(0)]
		public int _error;

		[FieldOffset(0)]
		public float _r4;

		[FieldOffset(0)]
		public double _r8;

		[FieldOffset(0)]
		public long _cy;

		[FieldOffset(0)]
		public double _date;

		[FieldOffset(0)]
		public IntPtr _bstr;

		[FieldOffset(0)]
		public IntPtr _unknown;

		[FieldOffset(0)]
		public IntPtr _dispatch;

		[FieldOffset(0)]
		public IntPtr _pvarVal;

		[FieldOffset(0)]
		public IntPtr _byref;

		[FieldOffset(0)]
		public Record _record;
	}

	[FieldOffset(0)]
	private TypeUnion _typeUnion;

	[FieldOffset(0)]
	private decimal _decimal;

	public VarEnum VariantType
	{
		get
		{
			return (VarEnum)_typeUnion._vt;
		}
		set
		{
			_typeUnion._vt = (ushort)value;
		}
	}

	public bool IsEmpty => _typeUnion._vt == 0;

	public bool IsByRef => (_typeUnion._vt & 0x4000) != 0;

	public sbyte AsI1 => _typeUnion._unionTypes._i1;

	public short AsI2 => _typeUnion._unionTypes._i2;

	public int AsI4 => _typeUnion._unionTypes._i4;

	public long AsI8 => _typeUnion._unionTypes._i8;

	public byte AsUi1 => _typeUnion._unionTypes._ui1;

	public ushort AsUi2 => _typeUnion._unionTypes._ui2;

	public uint AsUi4 => _typeUnion._unionTypes._ui4;

	public ulong AsUi8 => _typeUnion._unionTypes._ui8;

	public int AsInt => _typeUnion._unionTypes._int;

	public uint AsUint => _typeUnion._unionTypes._uint;

	public bool AsBool => _typeUnion._unionTypes._bool != 0;

	public int AsError => _typeUnion._unionTypes._error;

	public float AsR4 => _typeUnion._unionTypes._r4;

	public double AsR8 => _typeUnion._unionTypes._r8;

	public decimal AsDecimal
	{
		get
		{
			Variant variant = this;
			variant._typeUnion._vt = 0;
			return variant._decimal;
		}
	}

	public decimal AsCy => decimal.FromOACurrency(_typeUnion._unionTypes._cy);

	public DateTime AsDate => DateTime.FromOADate(_typeUnion._unionTypes._date);

	public string AsBstr
	{
		get
		{
			if (_typeUnion._unionTypes._bstr == IntPtr.Zero)
			{
				return null;
			}
			return Marshal.PtrToStringBSTR(_typeUnion._unionTypes._bstr);
		}
	}

	public object AsUnknown
	{
		get
		{
			if (_typeUnion._unionTypes._unknown == IntPtr.Zero)
			{
				return null;
			}
			return Marshal.GetObjectForIUnknown(_typeUnion._unionTypes._unknown);
		}
	}

	public object AsDispatch
	{
		get
		{
			if (_typeUnion._unionTypes._dispatch == IntPtr.Zero)
			{
				return null;
			}
			return Marshal.GetObjectForIUnknown(_typeUnion._unionTypes._dispatch);
		}
	}

	public IntPtr AsByRefVariant => _typeUnion._unionTypes._pvarVal;

	public unsafe void CopyFromIndirect(object value)
	{
		VarEnum varEnum = VariantType & (VarEnum)(-16385);
		if (value == null)
		{
			if (varEnum == VarEnum.VT_DISPATCH || varEnum == VarEnum.VT_UNKNOWN || varEnum == VarEnum.VT_BSTR)
			{
				*(IntPtr*)(void*)_typeUnion._unionTypes._byref = IntPtr.Zero;
			}
			return;
		}
		if ((varEnum & VarEnum.VT_ARRAY) != 0)
		{
			System.Runtime.CompilerServices.Unsafe.SkipInit(out Variant variant);
			Marshal.GetNativeVariantForObject(value, (IntPtr)(&variant));
			*(IntPtr*)(void*)_typeUnion._unionTypes._byref = variant._typeUnion._unionTypes._byref;
			return;
		}
		switch (varEnum)
		{
		case VarEnum.VT_I1:
			*(sbyte*)(void*)_typeUnion._unionTypes._byref = (sbyte)value;
			break;
		case VarEnum.VT_UI1:
			*(byte*)(void*)_typeUnion._unionTypes._byref = (byte)value;
			break;
		case VarEnum.VT_I2:
			*(short*)(void*)_typeUnion._unionTypes._byref = (short)value;
			break;
		case VarEnum.VT_UI2:
			*(ushort*)(void*)_typeUnion._unionTypes._byref = (ushort)value;
			break;
		case VarEnum.VT_BOOL:
			*(short*)(void*)_typeUnion._unionTypes._byref = (short)(((bool)value) ? (-1) : 0);
			break;
		case VarEnum.VT_I4:
		case VarEnum.VT_INT:
			*(int*)(void*)_typeUnion._unionTypes._byref = (int)value;
			break;
		case VarEnum.VT_UI4:
		case VarEnum.VT_UINT:
			*(uint*)(void*)_typeUnion._unionTypes._byref = (uint)value;
			break;
		case VarEnum.VT_ERROR:
			*(int*)(void*)_typeUnion._unionTypes._byref = ((ErrorWrapper)value).ErrorCode;
			break;
		case VarEnum.VT_I8:
			*(long*)(void*)_typeUnion._unionTypes._byref = (long)value;
			break;
		case VarEnum.VT_UI8:
			*(ulong*)(void*)_typeUnion._unionTypes._byref = (ulong)value;
			break;
		case VarEnum.VT_R4:
			*(float*)(void*)_typeUnion._unionTypes._byref = (float)value;
			break;
		case VarEnum.VT_R8:
			*(double*)(void*)_typeUnion._unionTypes._byref = (double)value;
			break;
		case VarEnum.VT_DATE:
			*(double*)(void*)_typeUnion._unionTypes._byref = ((DateTime)value).ToOADate();
			break;
		case VarEnum.VT_UNKNOWN:
			*(IntPtr*)(void*)_typeUnion._unionTypes._byref = Marshal.GetIUnknownForObject(value);
			break;
		case VarEnum.VT_DISPATCH:
			*(IntPtr*)(void*)_typeUnion._unionTypes._byref = Marshal.GetIDispatchForObject(value);
			break;
		case VarEnum.VT_BSTR:
			*(IntPtr*)(void*)_typeUnion._unionTypes._byref = Marshal.StringToBSTR((string)value);
			break;
		case VarEnum.VT_CY:
			*(long*)(void*)_typeUnion._unionTypes._byref = decimal.ToOACurrency((decimal)value);
			break;
		case VarEnum.VT_DECIMAL:
			*(decimal*)(void*)_typeUnion._unionTypes._byref = (decimal)value;
			break;
		case VarEnum.VT_VARIANT:
			Marshal.GetNativeVariantForObject(value, _typeUnion._unionTypes._byref);
			break;
		default:
			throw new ArgumentException();
		}
	}

	public unsafe object ToObject()
	{
		if (IsEmpty)
		{
			return null;
		}
		switch (VariantType)
		{
		case VarEnum.VT_NULL:
			return DBNull.Value;
		case VarEnum.VT_I1:
			return AsI1;
		case VarEnum.VT_I2:
			return AsI2;
		case VarEnum.VT_I4:
			return AsI4;
		case VarEnum.VT_I8:
			return AsI8;
		case VarEnum.VT_UI1:
			return AsUi1;
		case VarEnum.VT_UI2:
			return AsUi2;
		case VarEnum.VT_UI4:
			return AsUi4;
		case VarEnum.VT_UI8:
			return AsUi8;
		case VarEnum.VT_INT:
			return AsInt;
		case VarEnum.VT_UINT:
			return AsUint;
		case VarEnum.VT_BOOL:
			return AsBool;
		case VarEnum.VT_ERROR:
			return AsError;
		case VarEnum.VT_R4:
			return AsR4;
		case VarEnum.VT_R8:
			return AsR8;
		case VarEnum.VT_DECIMAL:
			return AsDecimal;
		case VarEnum.VT_CY:
			return AsCy;
		case VarEnum.VT_DATE:
			return AsDate;
		case VarEnum.VT_BSTR:
			return AsBstr;
		case VarEnum.VT_UNKNOWN:
			return AsUnknown;
		case VarEnum.VT_DISPATCH:
			return AsDispatch;
		default:
			fixed (Variant* ptr = &this)
			{
				void* ptr2 = ptr;
				return Marshal.GetObjectForNativeVariant((IntPtr)ptr2);
			}
		}
	}

	public unsafe void Clear()
	{
		VarEnum variantType = VariantType;
		if ((variantType & VarEnum.VT_BYREF) != 0)
		{
			VariantType = VarEnum.VT_EMPTY;
		}
		else if ((variantType & VarEnum.VT_ARRAY) != 0 || variantType == VarEnum.VT_BSTR || variantType == VarEnum.VT_UNKNOWN || variantType == VarEnum.VT_DISPATCH || variantType == VarEnum.VT_VARIANT || variantType == VarEnum.VT_RECORD)
		{
			fixed (Variant* ptr = &this)
			{
				void* ptr2 = ptr;
				Interop.OleAut32.VariantClear((IntPtr)ptr2);
			}
		}
		else
		{
			VariantType = VarEnum.VT_EMPTY;
		}
	}
}

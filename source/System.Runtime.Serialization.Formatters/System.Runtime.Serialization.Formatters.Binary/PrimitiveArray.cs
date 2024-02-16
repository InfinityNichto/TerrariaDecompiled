using System.Globalization;

namespace System.Runtime.Serialization.Formatters.Binary;

internal sealed class PrimitiveArray
{
	private readonly InternalPrimitiveTypeE _code;

	private readonly bool[] _booleanA;

	private readonly char[] _charA;

	private readonly double[] _doubleA;

	private readonly short[] _int16A;

	private readonly int[] _int32A;

	private readonly long[] _int64A;

	private readonly sbyte[] _sbyteA;

	private readonly float[] _singleA;

	private readonly ushort[] _uint16A;

	private readonly uint[] _uint32A;

	private readonly ulong[] _uint64A;

	internal PrimitiveArray(InternalPrimitiveTypeE code, Array array)
	{
		_code = code;
		switch (code)
		{
		case InternalPrimitiveTypeE.Boolean:
			_booleanA = (bool[])array;
			break;
		case InternalPrimitiveTypeE.Char:
			_charA = (char[])array;
			break;
		case InternalPrimitiveTypeE.Double:
			_doubleA = (double[])array;
			break;
		case InternalPrimitiveTypeE.Int16:
			_int16A = (short[])array;
			break;
		case InternalPrimitiveTypeE.Int32:
			_int32A = (int[])array;
			break;
		case InternalPrimitiveTypeE.Int64:
			_int64A = (long[])array;
			break;
		case InternalPrimitiveTypeE.SByte:
			_sbyteA = (sbyte[])array;
			break;
		case InternalPrimitiveTypeE.Single:
			_singleA = (float[])array;
			break;
		case InternalPrimitiveTypeE.UInt16:
			_uint16A = (ushort[])array;
			break;
		case InternalPrimitiveTypeE.UInt32:
			_uint32A = (uint[])array;
			break;
		case InternalPrimitiveTypeE.UInt64:
			_uint64A = (ulong[])array;
			break;
		case InternalPrimitiveTypeE.Byte:
		case InternalPrimitiveTypeE.Currency:
		case InternalPrimitiveTypeE.Decimal:
		case InternalPrimitiveTypeE.TimeSpan:
		case InternalPrimitiveTypeE.DateTime:
			break;
		}
	}

	internal void SetValue(string value, int index)
	{
		switch (_code)
		{
		case InternalPrimitiveTypeE.Boolean:
			_booleanA[index] = bool.Parse(value);
			break;
		case InternalPrimitiveTypeE.Char:
			if (value[0] == '_' && value.Equals("_0x00_"))
			{
				_charA[index] = '\0';
			}
			else
			{
				_charA[index] = char.Parse(value);
			}
			break;
		case InternalPrimitiveTypeE.Double:
			_doubleA[index] = double.Parse(value, CultureInfo.InvariantCulture);
			break;
		case InternalPrimitiveTypeE.Int16:
			_int16A[index] = short.Parse(value, CultureInfo.InvariantCulture);
			break;
		case InternalPrimitiveTypeE.Int32:
			_int32A[index] = int.Parse(value, CultureInfo.InvariantCulture);
			break;
		case InternalPrimitiveTypeE.Int64:
			_int64A[index] = long.Parse(value, CultureInfo.InvariantCulture);
			break;
		case InternalPrimitiveTypeE.SByte:
			_sbyteA[index] = sbyte.Parse(value, CultureInfo.InvariantCulture);
			break;
		case InternalPrimitiveTypeE.Single:
			_singleA[index] = float.Parse(value, CultureInfo.InvariantCulture);
			break;
		case InternalPrimitiveTypeE.UInt16:
			_uint16A[index] = ushort.Parse(value, CultureInfo.InvariantCulture);
			break;
		case InternalPrimitiveTypeE.UInt32:
			_uint32A[index] = uint.Parse(value, CultureInfo.InvariantCulture);
			break;
		case InternalPrimitiveTypeE.UInt64:
			_uint64A[index] = ulong.Parse(value, CultureInfo.InvariantCulture);
			break;
		case InternalPrimitiveTypeE.Byte:
		case InternalPrimitiveTypeE.Currency:
		case InternalPrimitiveTypeE.Decimal:
		case InternalPrimitiveTypeE.TimeSpan:
		case InternalPrimitiveTypeE.DateTime:
			break;
		}
	}
}

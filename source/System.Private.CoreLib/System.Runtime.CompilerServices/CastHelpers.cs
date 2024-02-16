using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.CompilerServices;

internal static class CastHelpers
{
	[DebuggerDisplay("Source = {_source}; Target = {_targetAndResult & ~1}; Result = {_targetAndResult & 1}; VersionNum = {_version & ((1 << 29) - 1)}; Distance = {_version >> 29};")]
	private struct CastCacheEntry
	{
		internal int _version;

		internal nuint _source;

		internal nuint _targetAndResult;
	}

	private enum CastResult
	{
		CannotCast,
		CanCast,
		MaybeCast
	}

	internal struct ArrayElement
	{
		public object Value;
	}

	private static int[] s_table;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int KeyToBucket(ref int tableData, nuint source, nuint target)
	{
		int num = HashShift(ref tableData);
		ulong num2 = BitOperations.RotateLeft(source, 32) ^ target;
		return (int)((long)num2 * -7046029254386353131L >>> num);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static ref int TableData(int[] table)
	{
		return ref MemoryMarshal.GetArrayDataReference(table);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static ref CastCacheEntry Element(ref int tableData, int index)
	{
		return ref Unsafe.Add(ref Unsafe.As<int, CastCacheEntry>(ref tableData), index + 1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int HashShift(ref int tableData)
	{
		return tableData;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int TableMask(ref int tableData)
	{
		return Unsafe.Add(ref tableData, 1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static CastResult TryGet(nuint source, nuint target)
	{
		ref int tableData = ref TableData(s_table);
		int num = KeyToBucket(ref tableData, source, target);
		int num2 = 0;
		while (num2 < 8)
		{
			ref CastCacheEntry reference = ref Element(ref tableData, num);
			int num3 = Volatile.Read(ref reference._version);
			nuint source2 = reference._source;
			num3 &= -2;
			if (source2 == source)
			{
				nuint targetAndResult = reference._targetAndResult;
				targetAndResult ^= target;
				if (targetAndResult <= 1)
				{
					Interlocked.ReadMemoryBarrier();
					if (num3 != reference._version)
					{
						break;
					}
					return (CastResult)targetAndResult;
				}
			}
			if (num3 == 0)
			{
				break;
			}
			num2++;
			num = (num + num2) & TableMask(ref tableData);
		}
		return CastResult.MaybeCast;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private unsafe static extern object IsInstanceOfAny_NoCacheLookup(void* toTypeHnd, object obj);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private unsafe static extern object ChkCastAny_NoCacheLookup(void* toTypeHnd, object obj);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private unsafe static extern ref byte Unbox_Helper(void* toTypeHnd, object obj);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void WriteBarrier(ref object dst, object obj);

	[DebuggerHidden]
	[StackTraceHidden]
	[DebuggerStepThrough]
	private unsafe static object IsInstanceOfAny(void* toTypeHnd, object obj)
	{
		if (obj != null)
		{
			void* methodTable = RuntimeHelpers.GetMethodTable(obj);
			if (methodTable != toTypeHnd)
			{
				CastResult castResult = TryGet((nuint)methodTable, (nuint)toTypeHnd);
				if (castResult != CastResult.CanCast)
				{
					if (castResult != 0)
					{
						return IsInstanceOfAny_NoCacheLookup(toTypeHnd, obj);
					}
					obj = null;
				}
			}
		}
		return obj;
	}

	[DebuggerHidden]
	[StackTraceHidden]
	[DebuggerStepThrough]
	private unsafe static object IsInstanceOfInterface(void* toTypeHnd, object obj)
	{
		MethodTable* methodTable;
		nint num;
		MethodTable** ptr;
		if (obj != null)
		{
			methodTable = RuntimeHelpers.GetMethodTable(obj);
			num = methodTable->InterfaceCount;
			if (num == 0)
			{
				goto IL_0083;
			}
			ptr = methodTable->InterfaceMap;
			if (num < 4)
			{
				goto IL_006c;
			}
			while (*ptr != toTypeHnd && ptr[1] != toTypeHnd && ptr[2] != toTypeHnd && ptr[3] != toTypeHnd)
			{
				ptr += 4;
				num -= 4;
				if (num >= 4)
				{
					continue;
				}
				goto IL_0069;
			}
		}
		goto IL_008e;
		IL_006c:
		while (*ptr != toTypeHnd)
		{
			ptr++;
			num--;
			if (num > 0)
			{
				continue;
			}
			goto IL_0083;
		}
		goto IL_008e;
		IL_0083:
		if (!methodTable->NonTrivialInterfaceCast)
		{
			obj = null;
			goto IL_008e;
		}
		return IsInstance_Helper(toTypeHnd, obj);
		IL_008e:
		return obj;
		IL_0069:
		if (num != 0)
		{
			goto IL_006c;
		}
		goto IL_0083;
	}

	[DebuggerHidden]
	[StackTraceHidden]
	[DebuggerStepThrough]
	private unsafe static object IsInstanceOfClass(void* toTypeHnd, object obj)
	{
		if (obj == null || RuntimeHelpers.GetMethodTable(obj) == toTypeHnd)
		{
			return obj;
		}
		MethodTable* parentMethodTable = RuntimeHelpers.GetMethodTable(obj)->ParentMethodTable;
		while (parentMethodTable != toTypeHnd)
		{
			if (parentMethodTable != null)
			{
				parentMethodTable = parentMethodTable->ParentMethodTable;
				if (parentMethodTable == toTypeHnd)
				{
					break;
				}
				if (parentMethodTable != null)
				{
					parentMethodTable = parentMethodTable->ParentMethodTable;
					if (parentMethodTable == toTypeHnd)
					{
						break;
					}
					if (parentMethodTable != null)
					{
						parentMethodTable = parentMethodTable->ParentMethodTable;
						if (parentMethodTable == toTypeHnd)
						{
							break;
						}
						if (parentMethodTable != null)
						{
							parentMethodTable = parentMethodTable->ParentMethodTable;
							continue;
						}
					}
				}
			}
			obj = null;
			break;
		}
		return obj;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DebuggerHidden]
	[StackTraceHidden]
	[DebuggerStepThrough]
	private unsafe static object IsInstance_Helper(void* toTypeHnd, object obj)
	{
		return TryGet((nuint)RuntimeHelpers.GetMethodTable(obj), (nuint)toTypeHnd) switch
		{
			CastResult.CanCast => obj, 
			CastResult.CannotCast => null, 
			_ => IsInstanceOfAny_NoCacheLookup(toTypeHnd, obj), 
		};
	}

	[DebuggerHidden]
	[StackTraceHidden]
	[DebuggerStepThrough]
	private unsafe static object ChkCastAny(void* toTypeHnd, object obj)
	{
		if (obj != null)
		{
			void* methodTable = RuntimeHelpers.GetMethodTable(obj);
			if (methodTable != toTypeHnd)
			{
				CastResult castResult = TryGet((nuint)methodTable, (nuint)toTypeHnd);
				if (castResult != CastResult.CanCast)
				{
					return ChkCastAny_NoCacheLookup(toTypeHnd, obj);
				}
			}
		}
		return obj;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DebuggerHidden]
	[StackTraceHidden]
	[DebuggerStepThrough]
	private unsafe static object ChkCast_Helper(void* toTypeHnd, object obj)
	{
		CastResult castResult = TryGet((nuint)RuntimeHelpers.GetMethodTable(obj), (nuint)toTypeHnd);
		if (castResult == CastResult.CanCast)
		{
			return obj;
		}
		return ChkCastAny_NoCacheLookup(toTypeHnd, obj);
	}

	[DebuggerHidden]
	[StackTraceHidden]
	[DebuggerStepThrough]
	private unsafe static object ChkCastInterface(void* toTypeHnd, object obj)
	{
		nint num;
		MethodTable** ptr;
		if (obj != null)
		{
			MethodTable* methodTable = RuntimeHelpers.GetMethodTable(obj);
			num = methodTable->InterfaceCount;
			if (num == 0)
			{
				goto IL_0084;
			}
			ptr = methodTable->InterfaceMap;
			if (num < 4)
			{
				goto IL_0069;
			}
			while (*ptr != toTypeHnd && ptr[1] != toTypeHnd && ptr[2] != toTypeHnd && ptr[3] != toTypeHnd)
			{
				ptr += 4;
				num -= 4;
				if (num >= 4)
				{
					continue;
				}
				goto IL_0066;
			}
		}
		goto IL_0082;
		IL_0082:
		return obj;
		IL_0069:
		while (*ptr != toTypeHnd)
		{
			ptr++;
			num--;
			if (num > 0)
			{
				continue;
			}
			goto IL_0084;
		}
		goto IL_0082;
		IL_0066:
		if (num != 0)
		{
			goto IL_0069;
		}
		goto IL_0084;
		IL_0084:
		return ChkCast_Helper(toTypeHnd, obj);
	}

	[DebuggerHidden]
	[StackTraceHidden]
	[DebuggerStepThrough]
	private unsafe static object ChkCastClass(void* toTypeHnd, object obj)
	{
		if (obj == null || RuntimeHelpers.GetMethodTable(obj) == toTypeHnd)
		{
			return obj;
		}
		return ChkCastClassSpecial(toTypeHnd, obj);
	}

	[DebuggerHidden]
	[StackTraceHidden]
	[DebuggerStepThrough]
	private unsafe static object ChkCastClassSpecial(void* toTypeHnd, object obj)
	{
		MethodTable* ptr = RuntimeHelpers.GetMethodTable(obj);
		while (true)
		{
			ptr = ptr->ParentMethodTable;
			if (ptr != toTypeHnd)
			{
				if (ptr == null)
				{
					break;
				}
				ptr = ptr->ParentMethodTable;
				if (ptr != toTypeHnd)
				{
					if (ptr == null)
					{
						break;
					}
					ptr = ptr->ParentMethodTable;
					if (ptr != toTypeHnd)
					{
						if (ptr == null)
						{
							break;
						}
						ptr = ptr->ParentMethodTable;
						if (ptr != toTypeHnd)
						{
							if (ptr == null)
							{
								break;
							}
							continue;
						}
					}
				}
			}
			return obj;
		}
		return ChkCast_Helper(toTypeHnd, obj);
	}

	[DebuggerHidden]
	[StackTraceHidden]
	[DebuggerStepThrough]
	private unsafe static ref byte Unbox(void* toTypeHnd, object obj)
	{
		if (RuntimeHelpers.GetMethodTable(obj) == toTypeHnd)
		{
			return ref obj.GetRawData();
		}
		return ref Unbox_Helper(toTypeHnd, obj);
	}

	[DebuggerHidden]
	[StackTraceHidden]
	[DebuggerStepThrough]
	private static ref object ThrowArrayMismatchException()
	{
		throw new ArrayTypeMismatchException();
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	[DebuggerHidden]
	[StackTraceHidden]
	[DebuggerStepThrough]
	private unsafe static ref object LdelemaRef(Array array, int index, void* type)
	{
		ref object value = ref Unsafe.As<ArrayElement[]>(array)[index].Value;
		void* elementType = RuntimeHelpers.GetMethodTable(array)->ElementType;
		if (elementType == type)
		{
			return ref value;
		}
		return ref ThrowArrayMismatchException();
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	[DebuggerHidden]
	[StackTraceHidden]
	[DebuggerStepThrough]
	private unsafe static void StelemRef(Array array, int index, object obj)
	{
		ref object value = ref Unsafe.As<ArrayElement[]>(array)[index].Value;
		void* elementType = RuntimeHelpers.GetMethodTable(array)->ElementType;
		if (obj != null)
		{
			if (elementType == RuntimeHelpers.GetMethodTable(obj) || array.GetType() == typeof(object[]))
			{
				WriteBarrier(ref value, obj);
			}
			else
			{
				StelemRef_Helper(ref value, elementType, obj);
			}
		}
		else
		{
			value = null;
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DebuggerHidden]
	[StackTraceHidden]
	[DebuggerStepThrough]
	private unsafe static void StelemRef_Helper(ref object element, void* elementType, object obj)
	{
		CastResult castResult = TryGet((nuint)RuntimeHelpers.GetMethodTable(obj), (nuint)elementType);
		if (castResult == CastResult.CanCast)
		{
			WriteBarrier(ref element, obj);
		}
		else
		{
			StelemRef_Helper_NoCacheLookup(ref element, elementType, obj);
		}
	}

	[DebuggerHidden]
	[StackTraceHidden]
	[DebuggerStepThrough]
	private unsafe static void StelemRef_Helper_NoCacheLookup(ref object element, void* elementType, object obj)
	{
		obj = IsInstanceOfAny_NoCacheLookup(elementType, obj);
		if (obj != null)
		{
			WriteBarrier(ref element, obj);
			return;
		}
		throw new ArrayTypeMismatchException();
	}
}

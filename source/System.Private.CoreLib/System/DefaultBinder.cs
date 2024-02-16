using System.Globalization;
using System.Reflection;

namespace System;

internal class DefaultBinder : Binder
{
	[Flags]
	private enum Primitives
	{
		Boolean = 8,
		Char = 0x10,
		SByte = 0x20,
		Byte = 0x40,
		Int16 = 0x80,
		UInt16 = 0x100,
		Int32 = 0x200,
		UInt32 = 0x400,
		Int64 = 0x800,
		UInt64 = 0x1000,
		Single = 0x2000,
		Double = 0x4000,
		Decimal = 0x8000,
		DateTime = 0x10000,
		String = 0x40000
	}

	internal sealed class BinderState
	{
		internal readonly int[] _argsMap;

		internal readonly int _originalSize;

		internal readonly bool _isParamArray;

		internal BinderState(int[] argsMap, int originalSize, bool isParamArray)
		{
			_argsMap = argsMap;
			_originalSize = originalSize;
			_isParamArray = isParamArray;
		}
	}

	private static readonly Primitives[] s_primitiveConversions = new Primitives[19]
	{
		(Primitives)0,
		(Primitives)0,
		(Primitives)0,
		Primitives.Boolean,
		Primitives.Char | Primitives.UInt16 | Primitives.Int32 | Primitives.UInt32 | Primitives.Int64 | Primitives.UInt64 | Primitives.Single | Primitives.Double,
		Primitives.SByte | Primitives.Int16 | Primitives.Int32 | Primitives.Int64 | Primitives.Single | Primitives.Double,
		Primitives.Char | Primitives.Byte | Primitives.Int16 | Primitives.UInt16 | Primitives.Int32 | Primitives.UInt32 | Primitives.Int64 | Primitives.UInt64 | Primitives.Single | Primitives.Double,
		Primitives.Int16 | Primitives.Int32 | Primitives.Int64 | Primitives.Single | Primitives.Double,
		Primitives.UInt16 | Primitives.Int32 | Primitives.UInt32 | Primitives.Int64 | Primitives.UInt64 | Primitives.Single | Primitives.Double,
		Primitives.Int32 | Primitives.Int64 | Primitives.Single | Primitives.Double,
		Primitives.UInt32 | Primitives.Int64 | Primitives.UInt64 | Primitives.Single | Primitives.Double,
		Primitives.Int64 | Primitives.Single | Primitives.Double,
		Primitives.UInt64 | Primitives.Single | Primitives.Double,
		Primitives.Single | Primitives.Double,
		Primitives.Double,
		Primitives.Decimal,
		Primitives.DateTime,
		(Primitives)0,
		Primitives.String
	};

	public sealed override MethodBase BindToMethod(BindingFlags bindingAttr, MethodBase[] match, ref object[] args, ParameterModifier[] modifiers, CultureInfo cultureInfo, string[] names, out object state)
	{
		if (match == null || match.Length == 0)
		{
			throw new ArgumentException(SR.Arg_EmptyArray, "match");
		}
		MethodBase[] array = (MethodBase[])match.Clone();
		state = null;
		int[][] array2 = new int[array.Length][];
		for (int i = 0; i < array.Length; i++)
		{
			ParameterInfo[] parametersNoCopy = array[i].GetParametersNoCopy();
			array2[i] = new int[(parametersNoCopy.Length > args.Length) ? parametersNoCopy.Length : args.Length];
			if (names == null)
			{
				for (int j = 0; j < args.Length; j++)
				{
					array2[i][j] = j;
				}
			}
			else if (!CreateParamOrder(array2[i], parametersNoCopy, names))
			{
				array[i] = null;
			}
		}
		Type[] array3 = new Type[array.Length];
		Type[] array4 = new Type[args.Length];
		for (int i = 0; i < args.Length; i++)
		{
			if (args[i] != null)
			{
				array4[i] = args[i].GetType();
			}
		}
		int num = 0;
		bool flag = (bindingAttr & BindingFlags.OptionalParamBinding) != 0;
		for (int i = 0; i < array.Length; i++)
		{
			Type type = null;
			if (array[i] == null)
			{
				continue;
			}
			ParameterInfo[] parametersNoCopy2 = array[i].GetParametersNoCopy();
			if (parametersNoCopy2.Length == 0)
			{
				if (args.Length == 0 || (array[i].CallingConvention & CallingConventions.VarArgs) != 0)
				{
					array2[num] = array2[i];
					array[num++] = array[i];
				}
				continue;
			}
			int j;
			if (parametersNoCopy2.Length > args.Length)
			{
				for (j = args.Length; j < parametersNoCopy2.Length - 1 && parametersNoCopy2[j].DefaultValue != DBNull.Value; j++)
				{
				}
				if (j != parametersNoCopy2.Length - 1)
				{
					continue;
				}
				if (parametersNoCopy2[j].DefaultValue == DBNull.Value)
				{
					if (!parametersNoCopy2[j].ParameterType.IsArray || !parametersNoCopy2[j].IsDefined(typeof(ParamArrayAttribute), inherit: true))
					{
						continue;
					}
					type = parametersNoCopy2[j].ParameterType.GetElementType();
				}
			}
			else if (parametersNoCopy2.Length < args.Length)
			{
				int num2 = parametersNoCopy2.Length - 1;
				if (!parametersNoCopy2[num2].ParameterType.IsArray || !parametersNoCopy2[num2].IsDefined(typeof(ParamArrayAttribute), inherit: true) || array2[i][num2] != num2)
				{
					continue;
				}
				type = parametersNoCopy2[num2].ParameterType.GetElementType();
			}
			else
			{
				int num3 = parametersNoCopy2.Length - 1;
				if (parametersNoCopy2[num3].ParameterType.IsArray && parametersNoCopy2[num3].IsDefined(typeof(ParamArrayAttribute), inherit: true) && array2[i][num3] == num3 && !parametersNoCopy2[num3].ParameterType.IsAssignableFrom(array4[num3]))
				{
					type = parametersNoCopy2[num3].ParameterType.GetElementType();
				}
			}
			int num4 = ((type != null) ? (parametersNoCopy2.Length - 1) : args.Length);
			for (j = 0; j < num4; j++)
			{
				Type type2 = parametersNoCopy2[j].ParameterType;
				if (type2.IsByRef)
				{
					type2 = type2.GetElementType();
				}
				if (type2 == array4[array2[i][j]] || (flag && args[array2[i][j]] == Type.Missing) || args[array2[i][j]] == null || type2 == typeof(object))
				{
					continue;
				}
				if (type2.IsPrimitive)
				{
					if (array4[array2[i][j]] == null || !CanChangePrimitive(args[array2[i][j]]?.GetType(), type2))
					{
						break;
					}
				}
				else if (!(array4[array2[i][j]] == null) && !type2.IsAssignableFrom(array4[array2[i][j]]) && (!array4[array2[i][j]].IsCOMObject || !type2.IsInstanceOfType(args[array2[i][j]])))
				{
					break;
				}
			}
			if (type != null && j == parametersNoCopy2.Length - 1)
			{
				for (; j < args.Length; j++)
				{
					if (type.IsPrimitive)
					{
						if (array4[j] == null || !CanChangePrimitive(args[j]?.GetType(), type))
						{
							break;
						}
					}
					else if (!(array4[j] == null) && !type.IsAssignableFrom(array4[j]) && (!array4[j].IsCOMObject || !type.IsInstanceOfType(args[j])))
					{
						break;
					}
				}
			}
			if (j == args.Length)
			{
				array2[num] = array2[i];
				array3[num] = type;
				array[num++] = array[i];
			}
		}
		switch (num)
		{
		case 0:
			throw new MissingMethodException(SR.MissingMember);
		case 1:
		{
			if (names != null)
			{
				state = new BinderState((int[])array2[0].Clone(), args.Length, array3[0] != null);
				ReorderParams(array2[0], args);
			}
			ParameterInfo[] parametersNoCopy4 = array[0].GetParametersNoCopy();
			if (parametersNoCopy4.Length == args.Length)
			{
				if (array3[0] != null)
				{
					object[] array8 = new object[parametersNoCopy4.Length];
					int num8 = parametersNoCopy4.Length - 1;
					Array.Copy(args, array8, num8);
					array8[num8] = Array.CreateInstance(array3[0], 1);
					((Array)array8[num8]).SetValue(args[num8], 0);
					args = array8;
				}
			}
			else if (parametersNoCopy4.Length > args.Length)
			{
				object[] array9 = new object[parametersNoCopy4.Length];
				int i;
				for (i = 0; i < args.Length; i++)
				{
					array9[i] = args[i];
				}
				for (; i < parametersNoCopy4.Length - 1; i++)
				{
					array9[i] = parametersNoCopy4[i].DefaultValue;
				}
				if (array3[0] != null)
				{
					array9[i] = Array.CreateInstance(array3[0], 0);
				}
				else
				{
					array9[i] = parametersNoCopy4[i].DefaultValue;
				}
				args = array9;
			}
			else if ((array[0].CallingConvention & CallingConventions.VarArgs) == 0)
			{
				object[] array10 = new object[parametersNoCopy4.Length];
				int num9 = parametersNoCopy4.Length - 1;
				Array.Copy(args, array10, num9);
				array10[num9] = Array.CreateInstance(array3[0], args.Length - num9);
				Array.Copy(args, num9, (Array)array10[num9], 0, args.Length - num9);
				args = array10;
			}
			return array[0];
		}
		default:
		{
			int num5 = 0;
			bool flag2 = false;
			for (int i = 1; i < num; i++)
			{
				switch (FindMostSpecificMethod(array[num5], array2[num5], array3[num5], array[i], array2[i], array3[i], array4, args))
				{
				case 0:
					flag2 = true;
					break;
				case 2:
					num5 = i;
					flag2 = false;
					break;
				}
			}
			if (flag2)
			{
				throw new AmbiguousMatchException(SR.Arg_AmbiguousMatchException);
			}
			if (names != null)
			{
				state = new BinderState((int[])array2[num5].Clone(), args.Length, array3[num5] != null);
				ReorderParams(array2[num5], args);
			}
			ParameterInfo[] parametersNoCopy3 = array[num5].GetParametersNoCopy();
			if (parametersNoCopy3.Length == args.Length)
			{
				if (array3[num5] != null)
				{
					object[] array5 = new object[parametersNoCopy3.Length];
					int num6 = parametersNoCopy3.Length - 1;
					Array.Copy(args, array5, num6);
					array5[num6] = Array.CreateInstance(array3[num5], 1);
					((Array)array5[num6]).SetValue(args[num6], 0);
					args = array5;
				}
			}
			else if (parametersNoCopy3.Length > args.Length)
			{
				object[] array6 = new object[parametersNoCopy3.Length];
				int i;
				for (i = 0; i < args.Length; i++)
				{
					array6[i] = args[i];
				}
				for (; i < parametersNoCopy3.Length - 1; i++)
				{
					array6[i] = parametersNoCopy3[i].DefaultValue;
				}
				if (array3[num5] != null)
				{
					array6[i] = Array.CreateInstance(array3[num5], 0);
				}
				else
				{
					array6[i] = parametersNoCopy3[i].DefaultValue;
				}
				args = array6;
			}
			else if ((array[num5].CallingConvention & CallingConventions.VarArgs) == 0)
			{
				object[] array7 = new object[parametersNoCopy3.Length];
				int num7 = parametersNoCopy3.Length - 1;
				Array.Copy(args, array7, num7);
				array7[num7] = Array.CreateInstance(array3[num5], args.Length - num7);
				Array.Copy(args, num7, (Array)array7[num7], 0, args.Length - num7);
				args = array7;
			}
			return array[num5];
		}
		}
	}

	public sealed override FieldInfo BindToField(BindingFlags bindingAttr, FieldInfo[] match, object value, CultureInfo cultureInfo)
	{
		if (match == null)
		{
			throw new ArgumentNullException("match");
		}
		int num = 0;
		FieldInfo[] array = (FieldInfo[])match.Clone();
		if ((bindingAttr & BindingFlags.SetField) != 0)
		{
			Type type = value.GetType();
			for (int i = 0; i < array.Length; i++)
			{
				Type fieldType = array[i].FieldType;
				if (fieldType == type)
				{
					array[num++] = array[i];
				}
				else if (value == Empty.Value && fieldType.IsClass)
				{
					array[num++] = array[i];
				}
				else if (fieldType == typeof(object))
				{
					array[num++] = array[i];
				}
				else if (fieldType.IsPrimitive)
				{
					if (CanChangePrimitive(type, fieldType))
					{
						array[num++] = array[i];
					}
				}
				else if (fieldType.IsAssignableFrom(type))
				{
					array[num++] = array[i];
				}
			}
			switch (num)
			{
			case 0:
				throw new MissingFieldException(SR.MissingField);
			case 1:
				return array[0];
			}
		}
		int num2 = 0;
		bool flag = false;
		for (int i = 1; i < num; i++)
		{
			switch (FindMostSpecificField(array[num2], array[i]))
			{
			case 0:
				flag = true;
				break;
			case 2:
				num2 = i;
				flag = false;
				break;
			}
		}
		if (flag)
		{
			throw new AmbiguousMatchException(SR.Arg_AmbiguousMatchException);
		}
		return array[num2];
	}

	public sealed override MethodBase SelectMethod(BindingFlags bindingAttr, MethodBase[] match, Type[] types, ParameterModifier[] modifiers)
	{
		Type[] array = new Type[types.Length];
		for (int i = 0; i < types.Length; i++)
		{
			array[i] = types[i].UnderlyingSystemType;
			if (!array[i].IsRuntimeImplemented() && !(array[i] is SignatureType))
			{
				throw new ArgumentException(SR.Arg_MustBeType, "types");
			}
		}
		types = array;
		if (match == null || match.Length == 0)
		{
			throw new ArgumentException(SR.Arg_EmptyArray, "match");
		}
		MethodBase[] array2 = (MethodBase[])match.Clone();
		int num = 0;
		for (int i = 0; i < array2.Length; i++)
		{
			ParameterInfo[] parametersNoCopy = array2[i].GetParametersNoCopy();
			if (parametersNoCopy.Length != types.Length)
			{
				continue;
			}
			int j;
			for (j = 0; j < types.Length; j++)
			{
				Type parameterType = parametersNoCopy[j].ParameterType;
				if (types[j].MatchesParameterTypeExactly(parametersNoCopy[j]) || parameterType == typeof(object))
				{
					continue;
				}
				Type type = types[j];
				if (type is SignatureType signatureType)
				{
					if (!(array2[i] is MethodInfo genericMethod))
					{
						break;
					}
					type = signatureType.TryResolveAgainstGenericMethod(genericMethod);
					if (type == null)
					{
						break;
					}
				}
				if (parameterType.IsPrimitive)
				{
					if (!type.UnderlyingSystemType.IsRuntimeImplemented() || !CanChangePrimitive(type.UnderlyingSystemType, parameterType.UnderlyingSystemType))
					{
						break;
					}
				}
				else if (!parameterType.IsAssignableFrom(type))
				{
					break;
				}
			}
			if (j == types.Length)
			{
				array2[num++] = array2[i];
			}
		}
		switch (num)
		{
		case 0:
			return null;
		case 1:
			return array2[0];
		default:
		{
			int num2 = 0;
			bool flag = false;
			int[] array3 = new int[types.Length];
			for (int i = 0; i < types.Length; i++)
			{
				array3[i] = i;
			}
			for (int i = 1; i < num; i++)
			{
				switch (FindMostSpecificMethod(array2[num2], array3, null, array2[i], array3, null, types, null))
				{
				case 0:
					flag = true;
					break;
				case 2:
					flag = false;
					num2 = i;
					break;
				}
			}
			if (flag)
			{
				throw new AmbiguousMatchException(SR.Arg_AmbiguousMatchException);
			}
			return array2[num2];
		}
		}
	}

	public sealed override PropertyInfo SelectProperty(BindingFlags bindingAttr, PropertyInfo[] match, Type returnType, Type[] indexes, ParameterModifier[] modifiers)
	{
		if (indexes != null)
		{
			foreach (Type type in indexes)
			{
				if (type == null)
				{
					throw new ArgumentNullException("indexes");
				}
			}
		}
		if (match == null || match.Length == 0)
		{
			throw new ArgumentException(SR.Arg_EmptyArray, "match");
		}
		PropertyInfo[] array = (PropertyInfo[])match.Clone();
		int j = 0;
		int num = 0;
		int num2 = ((indexes != null) ? indexes.Length : 0);
		for (int k = 0; k < array.Length; k++)
		{
			if (indexes != null)
			{
				ParameterInfo[] indexParameters = array[k].GetIndexParameters();
				if (indexParameters.Length != num2)
				{
					continue;
				}
				for (j = 0; j < num2; j++)
				{
					Type parameterType = indexParameters[j].ParameterType;
					if (parameterType == indexes[j] || parameterType == typeof(object))
					{
						continue;
					}
					if (parameterType.IsPrimitive)
					{
						if (!indexes[j].UnderlyingSystemType.IsRuntimeImplemented() || !CanChangePrimitive(indexes[j].UnderlyingSystemType, parameterType.UnderlyingSystemType))
						{
							break;
						}
					}
					else if (!parameterType.IsAssignableFrom(indexes[j]))
					{
						break;
					}
				}
			}
			if (j != num2)
			{
				continue;
			}
			if (returnType != null)
			{
				if (array[k].PropertyType.IsPrimitive)
				{
					if (!returnType.UnderlyingSystemType.IsRuntimeImplemented() || !CanChangePrimitive(returnType.UnderlyingSystemType, array[k].PropertyType.UnderlyingSystemType))
					{
						continue;
					}
				}
				else if (!array[k].PropertyType.IsAssignableFrom(returnType))
				{
					continue;
				}
			}
			array[num++] = array[k];
		}
		switch (num)
		{
		case 0:
			return null;
		case 1:
			return array[0];
		default:
		{
			int num3 = 0;
			bool flag = false;
			int[] array2 = new int[num2];
			for (int k = 0; k < num2; k++)
			{
				array2[k] = k;
			}
			for (int k = 1; k < num; k++)
			{
				int num4 = FindMostSpecificType(array[num3].PropertyType, array[k].PropertyType, returnType);
				if (num4 == 0 && indexes != null)
				{
					num4 = FindMostSpecific(array[num3].GetIndexParameters(), array2, null, array[k].GetIndexParameters(), array2, null, indexes, null);
				}
				if (num4 == 0)
				{
					num4 = FindMostSpecificProperty(array[num3], array[k]);
					if (num4 == 0)
					{
						flag = true;
					}
				}
				if (num4 == 2)
				{
					flag = false;
					num3 = k;
				}
			}
			if (flag)
			{
				throw new AmbiguousMatchException(SR.Arg_AmbiguousMatchException);
			}
			return array[num3];
		}
		}
	}

	public override object ChangeType(object value, Type type, CultureInfo cultureInfo)
	{
		throw new NotSupportedException(SR.NotSupported_ChangeType);
	}

	public sealed override void ReorderArgumentArray(ref object[] args, object state)
	{
		BinderState binderState = (BinderState)state;
		ReorderParams(binderState._argsMap, args);
		if (binderState._isParamArray)
		{
			int num = args.Length - 1;
			if (args.Length == binderState._originalSize)
			{
				args[num] = ((object[])args[num])[0];
				return;
			}
			object[] array = new object[args.Length];
			Array.Copy(args, array, num);
			int num2 = num;
			int num3 = 0;
			while (num2 < array.Length)
			{
				array[num2] = ((object[])args[num])[num3];
				num2++;
				num3++;
			}
			args = array;
		}
		else if (args.Length > binderState._originalSize)
		{
			object[] array2 = new object[binderState._originalSize];
			Array.Copy(args, array2, binderState._originalSize);
			args = array2;
		}
	}

	public static MethodBase ExactBinding(MethodBase[] match, Type[] types, ParameterModifier[] modifiers)
	{
		if (match == null)
		{
			throw new ArgumentNullException("match");
		}
		MethodBase[] array = new MethodBase[match.Length];
		int num = 0;
		for (int i = 0; i < match.Length; i++)
		{
			ParameterInfo[] parametersNoCopy = match[i].GetParametersNoCopy();
			if (parametersNoCopy.Length == 0)
			{
				continue;
			}
			int j;
			for (j = 0; j < types.Length; j++)
			{
				Type parameterType = parametersNoCopy[j].ParameterType;
				if (!parameterType.Equals(types[j]))
				{
					break;
				}
			}
			if (j >= types.Length)
			{
				array[num] = match[i];
				num++;
			}
		}
		return num switch
		{
			0 => null, 
			1 => array[0], 
			_ => FindMostDerivedNewSlotMeth(array, num), 
		};
	}

	public static PropertyInfo ExactPropertyBinding(PropertyInfo[] match, Type returnType, Type[] types, ParameterModifier[] modifiers)
	{
		if (match == null)
		{
			throw new ArgumentNullException("match");
		}
		PropertyInfo propertyInfo = null;
		int num = ((types != null) ? types.Length : 0);
		for (int i = 0; i < match.Length; i++)
		{
			ParameterInfo[] indexParameters = match[i].GetIndexParameters();
			int j;
			for (j = 0; j < num; j++)
			{
				Type parameterType = indexParameters[j].ParameterType;
				if (parameterType != types[j])
				{
					break;
				}
			}
			if (j >= num && (!(returnType != null) || !(returnType != match[i].PropertyType)))
			{
				if (propertyInfo != null)
				{
					throw new AmbiguousMatchException(SR.Arg_AmbiguousMatchException);
				}
				propertyInfo = match[i];
			}
		}
		return propertyInfo;
	}

	private static int FindMostSpecific(ParameterInfo[] p1, int[] paramOrder1, Type paramArrayType1, ParameterInfo[] p2, int[] paramOrder2, Type paramArrayType2, Type[] types, object[] args)
	{
		if (paramArrayType1 != null && paramArrayType2 == null)
		{
			return 2;
		}
		if (paramArrayType2 != null && paramArrayType1 == null)
		{
			return 1;
		}
		bool flag = false;
		bool flag2 = false;
		for (int i = 0; i < types.Length; i++)
		{
			if (args != null && args[i] == Type.Missing)
			{
				continue;
			}
			Type type = ((!(paramArrayType1 != null) || paramOrder1[i] < p1.Length - 1) ? p1[paramOrder1[i]].ParameterType : paramArrayType1);
			Type type2 = ((!(paramArrayType2 != null) || paramOrder2[i] < p2.Length - 1) ? p2[paramOrder2[i]].ParameterType : paramArrayType2);
			if (!(type == type2))
			{
				switch (FindMostSpecificType(type, type2, types[i]))
				{
				case 0:
					return 0;
				case 1:
					flag = true;
					break;
				case 2:
					flag2 = true;
					break;
				}
			}
		}
		if (flag == flag2)
		{
			if (!flag && args != null)
			{
				if (p1.Length > p2.Length)
				{
					return 1;
				}
				if (p2.Length > p1.Length)
				{
					return 2;
				}
			}
			return 0;
		}
		if (!flag)
		{
			return 2;
		}
		return 1;
	}

	private static int FindMostSpecificType(Type c1, Type c2, Type t)
	{
		if (c1 == c2)
		{
			return 0;
		}
		if (t is SignatureType pattern)
		{
			if (pattern.MatchesExactly(c1))
			{
				return 1;
			}
			if (pattern.MatchesExactly(c2))
			{
				return 2;
			}
		}
		else
		{
			if (c1 == t)
			{
				return 1;
			}
			if (c2 == t)
			{
				return 2;
			}
		}
		if (c1.IsByRef || c2.IsByRef)
		{
			if (c1.IsByRef && c2.IsByRef)
			{
				c1 = c1.GetElementType();
				c2 = c2.GetElementType();
			}
			else if (c1.IsByRef)
			{
				if (c1.GetElementType() == c2)
				{
					return 2;
				}
				c1 = c1.GetElementType();
			}
			else
			{
				if (c2.GetElementType() == c1)
				{
					return 1;
				}
				c2 = c2.GetElementType();
			}
		}
		bool flag;
		bool flag2;
		if (c1.IsPrimitive && c2.IsPrimitive)
		{
			flag = CanChangePrimitive(c2, c1);
			flag2 = CanChangePrimitive(c1, c2);
		}
		else
		{
			flag = c1.IsAssignableFrom(c2);
			flag2 = c2.IsAssignableFrom(c1);
		}
		if (flag == flag2)
		{
			return 0;
		}
		if (flag)
		{
			return 2;
		}
		return 1;
	}

	private static int FindMostSpecificMethod(MethodBase m1, int[] paramOrder1, Type paramArrayType1, MethodBase m2, int[] paramOrder2, Type paramArrayType2, Type[] types, object[] args)
	{
		int num = FindMostSpecific(m1.GetParametersNoCopy(), paramOrder1, paramArrayType1, m2.GetParametersNoCopy(), paramOrder2, paramArrayType2, types, args);
		if (num != 0)
		{
			return num;
		}
		if (CompareMethodSig(m1, m2))
		{
			int hierarchyDepth = GetHierarchyDepth(m1.DeclaringType);
			int hierarchyDepth2 = GetHierarchyDepth(m2.DeclaringType);
			if (hierarchyDepth == hierarchyDepth2)
			{
				return 0;
			}
			if (hierarchyDepth < hierarchyDepth2)
			{
				return 2;
			}
			return 1;
		}
		return 0;
	}

	private static int FindMostSpecificField(FieldInfo cur1, FieldInfo cur2)
	{
		if (cur1.Name == cur2.Name)
		{
			int hierarchyDepth = GetHierarchyDepth(cur1.DeclaringType);
			int hierarchyDepth2 = GetHierarchyDepth(cur2.DeclaringType);
			if (hierarchyDepth == hierarchyDepth2)
			{
				return 0;
			}
			if (hierarchyDepth < hierarchyDepth2)
			{
				return 2;
			}
			return 1;
		}
		return 0;
	}

	private static int FindMostSpecificProperty(PropertyInfo cur1, PropertyInfo cur2)
	{
		if (cur1.Name == cur2.Name)
		{
			int hierarchyDepth = GetHierarchyDepth(cur1.DeclaringType);
			int hierarchyDepth2 = GetHierarchyDepth(cur2.DeclaringType);
			if (hierarchyDepth == hierarchyDepth2)
			{
				return 0;
			}
			if (hierarchyDepth < hierarchyDepth2)
			{
				return 2;
			}
			return 1;
		}
		return 0;
	}

	public static bool CompareMethodSig(MethodBase m1, MethodBase m2)
	{
		ParameterInfo[] parametersNoCopy = m1.GetParametersNoCopy();
		ParameterInfo[] parametersNoCopy2 = m2.GetParametersNoCopy();
		if (parametersNoCopy.Length != parametersNoCopy2.Length)
		{
			return false;
		}
		int num = parametersNoCopy.Length;
		for (int i = 0; i < num; i++)
		{
			if (parametersNoCopy[i].ParameterType != parametersNoCopy2[i].ParameterType)
			{
				return false;
			}
		}
		return true;
	}

	private static int GetHierarchyDepth(Type t)
	{
		int num = 0;
		Type type = t;
		do
		{
			num++;
			type = type.BaseType;
		}
		while (type != null);
		return num;
	}

	internal static MethodBase FindMostDerivedNewSlotMeth(MethodBase[] match, int cMatches)
	{
		int num = 0;
		MethodBase result = null;
		for (int i = 0; i < cMatches; i++)
		{
			int hierarchyDepth = GetHierarchyDepth(match[i].DeclaringType);
			if (hierarchyDepth == num)
			{
				throw new AmbiguousMatchException(SR.Arg_AmbiguousMatchException);
			}
			if (hierarchyDepth > num)
			{
				num = hierarchyDepth;
				result = match[i];
			}
		}
		return result;
	}

	private static void ReorderParams(int[] paramOrder, object[] vars)
	{
		object[] array = new object[vars.Length];
		for (int i = 0; i < vars.Length; i++)
		{
			array[i] = vars[i];
		}
		for (int j = 0; j < vars.Length; j++)
		{
			vars[j] = array[paramOrder[j]];
		}
	}

	private static bool CreateParamOrder(int[] paramOrder, ParameterInfo[] pars, string[] names)
	{
		bool[] array = new bool[pars.Length];
		for (int i = 0; i < pars.Length; i++)
		{
			paramOrder[i] = -1;
		}
		for (int j = 0; j < names.Length; j++)
		{
			int k;
			for (k = 0; k < pars.Length; k++)
			{
				if (names[j].Equals(pars[k].Name))
				{
					paramOrder[k] = j;
					array[j] = true;
					break;
				}
			}
			if (k == pars.Length)
			{
				return false;
			}
		}
		int l = 0;
		for (int m = 0; m < pars.Length; m++)
		{
			if (paramOrder[m] != -1)
			{
				continue;
			}
			for (; l < pars.Length; l++)
			{
				if (!array[l])
				{
					paramOrder[m] = l;
					l++;
					break;
				}
			}
		}
		return true;
	}

	internal static bool CanChangePrimitive(Type source, Type target)
	{
		if ((source == typeof(IntPtr) && target == typeof(IntPtr)) || (source == typeof(UIntPtr) && target == typeof(UIntPtr)))
		{
			return true;
		}
		Primitives primitives = s_primitiveConversions[(int)Type.GetTypeCode(source)];
		Primitives primitives2 = (Primitives)(1 << (int)Type.GetTypeCode(target));
		return (primitives & primitives2) != 0;
	}
}

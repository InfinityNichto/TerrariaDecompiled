using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace System.Reflection.Emit;

public class CustomAttributeBuilder
{
	internal ConstructorInfo m_con;

	private object[] m_constructorArgs;

	private byte[] m_blob;

	public CustomAttributeBuilder(ConstructorInfo con, object?[] constructorArgs)
		: this(con, constructorArgs, Array.Empty<PropertyInfo>(), Array.Empty<object>(), Array.Empty<FieldInfo>(), Array.Empty<object>())
	{
	}

	public CustomAttributeBuilder(ConstructorInfo con, object?[] constructorArgs, PropertyInfo[] namedProperties, object?[] propertyValues)
		: this(con, constructorArgs, namedProperties, propertyValues, Array.Empty<FieldInfo>(), Array.Empty<object>())
	{
	}

	public CustomAttributeBuilder(ConstructorInfo con, object?[] constructorArgs, FieldInfo[] namedFields, object?[] fieldValues)
		: this(con, constructorArgs, Array.Empty<PropertyInfo>(), Array.Empty<object>(), namedFields, fieldValues)
	{
	}

	public CustomAttributeBuilder(ConstructorInfo con, object?[] constructorArgs, PropertyInfo[] namedProperties, object?[] propertyValues, FieldInfo[] namedFields, object?[] fieldValues)
	{
		if (con == null)
		{
			throw new ArgumentNullException("con");
		}
		if (constructorArgs == null)
		{
			throw new ArgumentNullException("constructorArgs");
		}
		if (namedProperties == null)
		{
			throw new ArgumentNullException("namedProperties");
		}
		if (propertyValues == null)
		{
			throw new ArgumentNullException("propertyValues");
		}
		if (namedFields == null)
		{
			throw new ArgumentNullException("namedFields");
		}
		if (fieldValues == null)
		{
			throw new ArgumentNullException("fieldValues");
		}
		if (namedProperties.Length != propertyValues.Length)
		{
			throw new ArgumentException(SR.Arg_ArrayLengthsDiffer, "namedProperties, propertyValues");
		}
		if (namedFields.Length != fieldValues.Length)
		{
			throw new ArgumentException(SR.Arg_ArrayLengthsDiffer, "namedFields, fieldValues");
		}
		if ((con.Attributes & MethodAttributes.Static) == MethodAttributes.Static || (con.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Private)
		{
			throw new ArgumentException(SR.Argument_BadConstructor);
		}
		if ((con.CallingConvention & CallingConventions.Standard) != CallingConventions.Standard)
		{
			throw new ArgumentException(SR.Argument_BadConstructorCallConv);
		}
		m_con = con;
		m_constructorArgs = new object[constructorArgs.Length];
		Array.Copy(constructorArgs, m_constructorArgs, constructorArgs.Length);
		Type[] parameterTypes = con.GetParameterTypes();
		if (parameterTypes.Length != constructorArgs.Length)
		{
			throw new ArgumentException(SR.Argument_BadParameterCountsForConstructor);
		}
		for (int i = 0; i < parameterTypes.Length; i++)
		{
			if (!ValidateType(parameterTypes[i]))
			{
				throw new ArgumentException(SR.Argument_BadTypeInCustomAttribute);
			}
		}
		for (int i = 0; i < parameterTypes.Length; i++)
		{
			object obj = constructorArgs[i];
			if (obj == null)
			{
				if (parameterTypes[i].IsValueType)
				{
					throw new ArgumentNullException($"{"constructorArgs"}[{i}]");
				}
			}
			else
			{
				VerifyTypeAndPassedObjectType(parameterTypes[i], obj.GetType(), $"{"constructorArgs"}[{i}]");
			}
		}
		MemoryStream output = new MemoryStream();
		BinaryWriter binaryWriter = new BinaryWriter(output);
		binaryWriter.Write((ushort)1);
		for (int i = 0; i < constructorArgs.Length; i++)
		{
			EmitValue(binaryWriter, parameterTypes[i], constructorArgs[i]);
		}
		binaryWriter.Write((ushort)(namedProperties.Length + namedFields.Length));
		for (int i = 0; i < namedProperties.Length; i++)
		{
			PropertyInfo propertyInfo = namedProperties[i];
			if (propertyInfo == null)
			{
				throw new ArgumentNullException("namedProperties[" + i + "]");
			}
			Type propertyType = propertyInfo.PropertyType;
			object obj2 = propertyValues[i];
			if (obj2 == null && propertyType.IsValueType)
			{
				throw new ArgumentNullException("propertyValues[" + i + "]");
			}
			if (!ValidateType(propertyType))
			{
				throw new ArgumentException(SR.Argument_BadTypeInCustomAttribute);
			}
			if (!propertyInfo.CanWrite)
			{
				throw new ArgumentException(SR.Argument_NotAWritableProperty);
			}
			if (propertyInfo.DeclaringType != con.DeclaringType && !(con.DeclaringType is TypeBuilderInstantiation) && !con.DeclaringType.IsSubclassOf(propertyInfo.DeclaringType) && !TypeBuilder.IsTypeEqual(propertyInfo.DeclaringType, con.DeclaringType) && (!(propertyInfo.DeclaringType is TypeBuilder) || !con.DeclaringType.IsSubclassOf(((TypeBuilder)propertyInfo.DeclaringType).BakedRuntimeType)))
			{
				throw new ArgumentException(SR.Argument_BadPropertyForConstructorBuilder);
			}
			if (obj2 != null)
			{
				VerifyTypeAndPassedObjectType(propertyType, obj2.GetType(), $"{"propertyValues"}[{i}]");
			}
			binaryWriter.Write((byte)84);
			EmitType(binaryWriter, propertyType);
			EmitString(binaryWriter, namedProperties[i].Name);
			EmitValue(binaryWriter, propertyType, obj2);
		}
		for (int i = 0; i < namedFields.Length; i++)
		{
			FieldInfo fieldInfo = namedFields[i];
			if (fieldInfo == null)
			{
				throw new ArgumentNullException("namedFields[" + i + "]");
			}
			Type fieldType = fieldInfo.FieldType;
			object obj3 = fieldValues[i];
			if (obj3 == null && fieldType.IsValueType)
			{
				throw new ArgumentNullException("fieldValues[" + i + "]");
			}
			if (!ValidateType(fieldType))
			{
				throw new ArgumentException(SR.Argument_BadTypeInCustomAttribute);
			}
			if (fieldInfo.DeclaringType != con.DeclaringType && !(con.DeclaringType is TypeBuilderInstantiation) && !con.DeclaringType.IsSubclassOf(fieldInfo.DeclaringType) && !TypeBuilder.IsTypeEqual(fieldInfo.DeclaringType, con.DeclaringType) && (!(fieldInfo.DeclaringType is TypeBuilder) || !con.DeclaringType.IsSubclassOf(((TypeBuilder)namedFields[i].DeclaringType).BakedRuntimeType)))
			{
				throw new ArgumentException(SR.Argument_BadFieldForConstructorBuilder);
			}
			if (obj3 != null)
			{
				VerifyTypeAndPassedObjectType(fieldType, obj3.GetType(), $"{"fieldValues"}[{i}]");
			}
			binaryWriter.Write((byte)83);
			EmitType(binaryWriter, fieldType);
			EmitString(binaryWriter, fieldInfo.Name);
			EmitValue(binaryWriter, fieldType, obj3);
		}
		m_blob = ((MemoryStream)binaryWriter.BaseStream).ToArray();
	}

	private bool ValidateType(Type t)
	{
		if (t.IsPrimitive)
		{
			if (t != typeof(IntPtr))
			{
				return t != typeof(UIntPtr);
			}
			return false;
		}
		if (t == typeof(string) || t == typeof(Type))
		{
			return true;
		}
		if (t.IsEnum)
		{
			TypeCode typeCode = Type.GetTypeCode(Enum.GetUnderlyingType(t));
			if ((uint)(typeCode - 5) <= 7u)
			{
				return true;
			}
			return false;
		}
		if (t.IsArray)
		{
			if (t.GetArrayRank() == 1)
			{
				return ValidateType(t.GetElementType());
			}
			return false;
		}
		return t == typeof(object);
	}

	private static void VerifyTypeAndPassedObjectType(Type type, Type passedType, string paramName)
	{
		if (type != typeof(object) && Type.GetTypeCode(passedType) != Type.GetTypeCode(type))
		{
			throw new ArgumentException(SR.Argument_ConstantDoesntMatch);
		}
		if (passedType == typeof(IntPtr) || passedType == typeof(UIntPtr))
		{
			throw new ArgumentException(SR.Format(SR.Argument_BadParameterTypeForCAB, passedType), paramName);
		}
	}

	private static void EmitType(BinaryWriter writer, Type type)
	{
		if (type.IsPrimitive)
		{
			switch (Type.GetTypeCode(type))
			{
			case TypeCode.SByte:
				writer.Write((byte)4);
				break;
			case TypeCode.Byte:
				writer.Write((byte)5);
				break;
			case TypeCode.Char:
				writer.Write((byte)3);
				break;
			case TypeCode.Boolean:
				writer.Write((byte)2);
				break;
			case TypeCode.Int16:
				writer.Write((byte)6);
				break;
			case TypeCode.UInt16:
				writer.Write((byte)7);
				break;
			case TypeCode.Int32:
				writer.Write((byte)8);
				break;
			case TypeCode.UInt32:
				writer.Write((byte)9);
				break;
			case TypeCode.Int64:
				writer.Write((byte)10);
				break;
			case TypeCode.UInt64:
				writer.Write((byte)11);
				break;
			case TypeCode.Single:
				writer.Write((byte)12);
				break;
			case TypeCode.Double:
				writer.Write((byte)13);
				break;
			}
		}
		else if (type.IsEnum)
		{
			writer.Write((byte)85);
			EmitString(writer, type.AssemblyQualifiedName);
		}
		else if (type == typeof(string))
		{
			writer.Write((byte)14);
		}
		else if (type == typeof(Type))
		{
			writer.Write((byte)80);
		}
		else if (type.IsArray)
		{
			writer.Write((byte)29);
			EmitType(writer, type.GetElementType());
		}
		else
		{
			writer.Write((byte)81);
		}
	}

	private static void EmitString(BinaryWriter writer, string str)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(str);
		uint num = (uint)bytes.Length;
		if (num <= 127)
		{
			writer.Write((byte)num);
		}
		else if (num <= 16383)
		{
			writer.Write(BinaryPrimitives.ReverseEndianness((short)(num | 0x8000)));
		}
		else
		{
			writer.Write(BinaryPrimitives.ReverseEndianness(num | 0xC0000000u));
		}
		writer.Write(bytes);
	}

	private static void EmitValue(BinaryWriter writer, Type type, object value)
	{
		if (type.IsEnum)
		{
			switch (Type.GetTypeCode(Enum.GetUnderlyingType(type)))
			{
			case TypeCode.SByte:
				writer.Write((sbyte)value);
				break;
			case TypeCode.Byte:
				writer.Write((byte)value);
				break;
			case TypeCode.Int16:
				writer.Write((short)value);
				break;
			case TypeCode.UInt16:
				writer.Write((ushort)value);
				break;
			case TypeCode.Int32:
				writer.Write((int)value);
				break;
			case TypeCode.UInt32:
				writer.Write((uint)value);
				break;
			case TypeCode.Int64:
				writer.Write((long)value);
				break;
			case TypeCode.UInt64:
				writer.Write((ulong)value);
				break;
			}
			return;
		}
		if (type == typeof(string))
		{
			if (value == null)
			{
				writer.Write(byte.MaxValue);
			}
			else
			{
				EmitString(writer, (string)value);
			}
			return;
		}
		if (type == typeof(Type))
		{
			if (value == null)
			{
				writer.Write(byte.MaxValue);
				return;
			}
			string text = TypeNameBuilder.ToString((Type)value, TypeNameBuilder.Format.AssemblyQualifiedName);
			if (text == null)
			{
				throw new ArgumentException(SR.Format(SR.Argument_InvalidTypeForCA, value.GetType()));
			}
			EmitString(writer, text);
			return;
		}
		if (type.IsArray)
		{
			if (value == null)
			{
				writer.Write(uint.MaxValue);
				return;
			}
			Array array = (Array)value;
			Type elementType = type.GetElementType();
			writer.Write(array.Length);
			for (int i = 0; i < array.Length; i++)
			{
				EmitValue(writer, elementType, array.GetValue(i));
			}
			return;
		}
		if (type.IsPrimitive)
		{
			switch (Type.GetTypeCode(type))
			{
			case TypeCode.SByte:
				writer.Write((sbyte)value);
				break;
			case TypeCode.Byte:
				writer.Write((byte)value);
				break;
			case TypeCode.Char:
				writer.Write(Convert.ToUInt16((char)value));
				break;
			case TypeCode.Boolean:
				writer.Write((byte)(((bool)value) ? 1u : 0u));
				break;
			case TypeCode.Int16:
				writer.Write((short)value);
				break;
			case TypeCode.UInt16:
				writer.Write((ushort)value);
				break;
			case TypeCode.Int32:
				writer.Write((int)value);
				break;
			case TypeCode.UInt32:
				writer.Write((uint)value);
				break;
			case TypeCode.Int64:
				writer.Write((long)value);
				break;
			case TypeCode.UInt64:
				writer.Write((ulong)value);
				break;
			case TypeCode.Single:
				writer.Write((float)value);
				break;
			case TypeCode.Double:
				writer.Write((double)value);
				break;
			}
			return;
		}
		if (type == typeof(object))
		{
			Type type2 = ((value == null) ? typeof(string) : ((value is Type) ? typeof(Type) : value.GetType()));
			if (type2 == typeof(object))
			{
				throw new ArgumentException(SR.Format(SR.Argument_BadParameterTypeForCAB, type2));
			}
			EmitType(writer, type2);
			EmitValue(writer, type2, value);
			return;
		}
		string p = "null";
		if (value != null)
		{
			p = value.GetType().ToString();
		}
		throw new ArgumentException(SR.Format(SR.Argument_BadParameterTypeForCAB, p));
	}

	internal void CreateCustomAttribute(ModuleBuilder mod, int tkOwner)
	{
		TypeBuilder.DefineCustomAttribute(mod, tkOwner, mod.GetConstructorToken(m_con), m_blob);
	}
}

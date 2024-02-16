using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Security;
using System.Xml;

namespace System.Runtime.Serialization.Json;

internal sealed class JsonFormatWriterGenerator
{
	private sealed class CriticalHelper
	{
		private CodeGenerator _ilg;

		private ArgBuilder _xmlWriterArg;

		private ArgBuilder _contextArg;

		private ArgBuilder _dataContractArg;

		private LocalBuilder _objectLocal;

		private ArgBuilder _memberNamesArg;

		private int _typeIndex = 1;

		private int _childElementIndex;

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		internal JsonFormatClassWriterDelegate GenerateClassWriter(ClassDataContract classContract)
		{
			_ilg = new CodeGenerator();
			bool flag = classContract.RequiresMemberAccessForWrite(null);
			try
			{
				BeginMethod(_ilg, "Write" + DataContract.SanitizeTypeName(classContract.StableName.Name) + "ToJson", typeof(JsonFormatClassWriterDelegate), flag);
			}
			catch (SecurityException securityException)
			{
				if (!flag)
				{
					throw;
				}
				classContract.RequiresMemberAccessForWrite(securityException);
			}
			InitArgs(classContract.UnderlyingType);
			_memberNamesArg = _ilg.GetArg(4);
			WriteClass(classContract);
			return (JsonFormatClassWriterDelegate)_ilg.EndMethod();
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		internal JsonFormatCollectionWriterDelegate GenerateCollectionWriter(CollectionDataContract collectionContract)
		{
			_ilg = new CodeGenerator();
			bool flag = collectionContract.RequiresMemberAccessForWrite(null);
			try
			{
				BeginMethod(_ilg, "Write" + DataContract.SanitizeTypeName(collectionContract.StableName.Name) + "ToJson", typeof(JsonFormatCollectionWriterDelegate), flag);
			}
			catch (SecurityException securityException)
			{
				if (!flag)
				{
					throw;
				}
				collectionContract.RequiresMemberAccessForWrite(securityException);
			}
			InitArgs(collectionContract.UnderlyingType);
			if (collectionContract.IsReadOnlyContract)
			{
				ThrowIfCannotSerializeReadOnlyTypes(collectionContract);
			}
			WriteCollection(collectionContract);
			return (JsonFormatCollectionWriterDelegate)_ilg.EndMethod();
		}

		private void BeginMethod(CodeGenerator ilg, string methodName, Type delegateType, bool allowPrivateMemberAccess)
		{
			MethodInfo invokeMethod = GetInvokeMethod(delegateType);
			ParameterInfo[] parameters = invokeMethod.GetParameters();
			Type[] array = new Type[parameters.Length];
			for (int i = 0; i < parameters.Length; i++)
			{
				array[i] = parameters[i].ParameterType;
			}
			DynamicMethod dynamicMethod = new DynamicMethod(methodName, invokeMethod.ReturnType, array, typeof(JsonFormatWriterGenerator).Module, allowPrivateMemberAccess);
			ilg.BeginMethod(dynamicMethod, delegateType, methodName, array, allowPrivateMemberAccess);
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private void InitArgs(Type objType)
		{
			_xmlWriterArg = _ilg.GetArg(0);
			_contextArg = _ilg.GetArg(2);
			_dataContractArg = _ilg.GetArg(3);
			_objectLocal = _ilg.DeclareLocal(objType, "objSerialized");
			ArgBuilder arg = _ilg.GetArg(1);
			_ilg.Load(arg);
			if (objType == Globals.TypeOfDateTimeOffsetAdapter)
			{
				_ilg.ConvertValue(arg.ArgType, Globals.TypeOfDateTimeOffset);
				_ilg.Call(XmlFormatGeneratorStatics.GetDateTimeOffsetAdapterMethod);
			}
			else if (objType == Globals.TypeOfMemoryStreamAdapter)
			{
				_ilg.ConvertValue(arg.ArgType, Globals.TypeOfMemoryStream);
				_ilg.Call(XmlFormatGeneratorStatics.GetMemoryStreamAdapterMethod);
			}
			else if (objType.IsGenericType && objType.GetGenericTypeDefinition() == Globals.TypeOfKeyValuePairAdapter)
			{
				ClassDataContract classDataContract = (ClassDataContract)DataContract.GetDataContract(objType);
				_ilg.ConvertValue(arg.ArgType, Globals.TypeOfKeyValuePair.MakeGenericType(classDataContract.KeyValuePairGenericArguments));
				_ilg.New(classDataContract.KeyValuePairAdapterConstructorInfo);
			}
			else
			{
				_ilg.ConvertValue(arg.ArgType, objType);
			}
			_ilg.Stloc(_objectLocal);
		}

		private void ThrowIfCannotSerializeReadOnlyTypes(CollectionDataContract classContract)
		{
			ThrowIfCannotSerializeReadOnlyTypes(XmlFormatGeneratorStatics.CollectionSerializationExceptionMessageProperty);
		}

		private void ThrowIfCannotSerializeReadOnlyTypes(PropertyInfo serializationExceptionMessageProperty)
		{
			_ilg.Load(_contextArg);
			_ilg.LoadMember(XmlFormatGeneratorStatics.SerializeReadOnlyTypesProperty);
			_ilg.IfNot();
			_ilg.Load(_dataContractArg);
			_ilg.LoadMember(serializationExceptionMessageProperty);
			_ilg.Load(null);
			_ilg.Call(XmlFormatGeneratorStatics.ThrowInvalidDataContractExceptionMethod);
			_ilg.EndIf();
		}

		private void InvokeOnSerializing(ClassDataContract classContract)
		{
			if (classContract.BaseContract != null)
			{
				InvokeOnSerializing(classContract.BaseContract);
			}
			if (classContract.OnSerializing != null)
			{
				_ilg.LoadAddress(_objectLocal);
				_ilg.Load(_contextArg);
				_ilg.Call(XmlFormatGeneratorStatics.GetStreamingContextMethod);
				_ilg.Call(classContract.OnSerializing);
			}
		}

		private void InvokeOnSerialized(ClassDataContract classContract)
		{
			if (classContract.BaseContract != null)
			{
				InvokeOnSerialized(classContract.BaseContract);
			}
			if (classContract.OnSerialized != null)
			{
				_ilg.LoadAddress(_objectLocal);
				_ilg.Load(_contextArg);
				_ilg.Call(XmlFormatGeneratorStatics.GetStreamingContextMethod);
				_ilg.Call(classContract.OnSerialized);
			}
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private void WriteClass(ClassDataContract classContract)
		{
			InvokeOnSerializing(classContract);
			if (classContract.IsISerializable)
			{
				_ilg.Call(_contextArg, JsonFormatGeneratorStatics.WriteJsonISerializableMethod, _xmlWriterArg, _objectLocal);
			}
			else if (classContract.HasExtensionData)
			{
				LocalBuilder localBuilder = _ilg.DeclareLocal(Globals.TypeOfExtensionDataObject, "extensionData");
				_ilg.Load(_objectLocal);
				_ilg.ConvertValue(_objectLocal.LocalType, Globals.TypeOfIExtensibleDataObject);
				_ilg.LoadMember(JsonFormatGeneratorStatics.ExtensionDataProperty);
				_ilg.Store(localBuilder);
				_ilg.Call(_contextArg, XmlFormatGeneratorStatics.WriteExtensionDataMethod, _xmlWriterArg, localBuilder, -1);
				WriteMembers(classContract, localBuilder, classContract);
			}
			else
			{
				WriteMembers(classContract, null, classContract);
			}
			InvokeOnSerialized(classContract);
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private int WriteMembers(ClassDataContract classContract, LocalBuilder extensionDataLocal, ClassDataContract derivedMostClassContract)
		{
			int num = ((classContract.BaseContract != null) ? WriteMembers(classContract.BaseContract, extensionDataLocal, derivedMostClassContract) : 0);
			int count = classContract.Members.Count;
			_ilg.Call(_contextArg, XmlFormatGeneratorStatics.IncrementItemCountMethod, count);
			int num2 = 0;
			while (num2 < count)
			{
				DataMember dataMember = classContract.Members[num2];
				Type memberType = dataMember.MemberType;
				LocalBuilder localBuilder = null;
				_ilg.Load(_contextArg);
				_ilg.Call(dataMember.IsGetOnlyCollection ? XmlFormatGeneratorStatics.StoreIsGetOnlyCollectionMethod : XmlFormatGeneratorStatics.ResetIsGetOnlyCollectionMethod);
				if (!dataMember.EmitDefaultValue)
				{
					localBuilder = LoadMemberValue(dataMember);
					_ilg.IfNotDefaultValue(localBuilder);
				}
				bool flag = DataContractJsonSerializer.CheckIfXmlNameRequiresMapping(classContract.MemberNames[num2]);
				if (flag || !TryWritePrimitive(memberType, localBuilder, dataMember.MemberInfo, null, null, num2 + _childElementIndex))
				{
					if (flag)
					{
						_ilg.Call(null, JsonFormatGeneratorStatics.WriteJsonNameWithMappingMethod, _xmlWriterArg, _memberNamesArg, num2 + _childElementIndex);
					}
					else
					{
						WriteStartElement(null, num2 + _childElementIndex);
					}
					if (localBuilder == null)
					{
						localBuilder = LoadMemberValue(dataMember);
					}
					WriteValue(localBuilder);
					WriteEndElement();
				}
				if (classContract.HasExtensionData)
				{
					_ilg.Call(_contextArg, XmlFormatGeneratorStatics.WriteExtensionDataMethod, _xmlWriterArg, extensionDataLocal, num);
				}
				if (!dataMember.EmitDefaultValue)
				{
					if (dataMember.IsRequired)
					{
						_ilg.Else();
						_ilg.Call(null, XmlFormatGeneratorStatics.ThrowRequiredMemberMustBeEmittedMethod, dataMember.Name, classContract.UnderlyingType);
					}
					_ilg.EndIf();
				}
				num2++;
				num++;
			}
			_typeIndex++;
			_childElementIndex += count;
			return num;
		}

		private LocalBuilder LoadMemberValue(DataMember member)
		{
			_ilg.LoadAddress(_objectLocal);
			_ilg.LoadMember(member.MemberInfo);
			LocalBuilder localBuilder = _ilg.DeclareLocal(member.MemberType, member.Name + "Value");
			_ilg.Stloc(localBuilder);
			return localBuilder;
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private void WriteCollection(CollectionDataContract collectionContract)
		{
			LocalBuilder localBuilder = _ilg.DeclareLocal(typeof(XmlDictionaryString), "itemName");
			_ilg.Load(_contextArg);
			_ilg.LoadMember(JsonFormatGeneratorStatics.CollectionItemNameProperty);
			_ilg.Store(localBuilder);
			if (collectionContract.Kind == CollectionKind.Array)
			{
				Type itemType2 = collectionContract.ItemType;
				LocalBuilder localBuilder2 = _ilg.DeclareLocal(Globals.TypeOfInt, "i");
				_ilg.Call(_contextArg, XmlFormatGeneratorStatics.IncrementArrayCountMethod, _xmlWriterArg, _objectLocal);
				if (!TryWritePrimitiveArray(collectionContract.UnderlyingType, itemType2, _objectLocal, localBuilder))
				{
					WriteArrayAttribute();
					_ilg.For(localBuilder2, 0, _objectLocal);
					if (!TryWritePrimitive(itemType2, null, null, localBuilder2, localBuilder, 0))
					{
						WriteStartElement(localBuilder, 0);
						_ilg.LoadArrayElement(_objectLocal, localBuilder2);
						LocalBuilder localBuilder3 = _ilg.DeclareLocal(itemType2, "memberValue");
						_ilg.Stloc(localBuilder3);
						WriteValue(localBuilder3);
						WriteEndElement();
					}
					_ilg.EndFor();
				}
				return;
			}
			MethodInfo methodInfo = null;
			switch (collectionContract.Kind)
			{
			case CollectionKind.Dictionary:
			case CollectionKind.List:
			case CollectionKind.Collection:
				methodInfo = XmlFormatGeneratorStatics.IncrementCollectionCountMethod;
				break;
			case CollectionKind.GenericList:
			case CollectionKind.GenericCollection:
				methodInfo = MakeIncrementCollectionCountGenericMethod(collectionContract.ItemType);
				break;
			case CollectionKind.GenericDictionary:
				methodInfo = MakeIncrementCollectionCountGenericMethod(Globals.TypeOfKeyValuePair.MakeGenericType(collectionContract.ItemType.GetGenericArguments()));
				break;
			}
			if (methodInfo != null)
			{
				_ilg.Call(_contextArg, methodInfo, _xmlWriterArg, _objectLocal);
			}
			bool flag = false;
			bool flag2 = false;
			Type type = null;
			Type[] array = null;
			if (collectionContract.Kind == CollectionKind.GenericDictionary)
			{
				flag2 = true;
				array = collectionContract.ItemType.GetGenericArguments();
				type = Globals.TypeOfGenericDictionaryEnumerator.MakeGenericType(array);
			}
			else if (collectionContract.Kind == CollectionKind.Dictionary)
			{
				flag = true;
				array = new Type[2]
				{
					Globals.TypeOfObject,
					Globals.TypeOfObject
				};
				type = Globals.TypeOfDictionaryEnumerator;
			}
			else
			{
				type = collectionContract.GetEnumeratorMethod.ReturnType;
			}
			MethodInfo methodInfo2 = type.GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.Public, Type.EmptyTypes);
			MethodInfo methodInfo3 = type.GetMethod("get_Current", BindingFlags.Instance | BindingFlags.Public, Type.EmptyTypes);
			if (methodInfo2 == null || methodInfo3 == null)
			{
				if (type.IsInterface)
				{
					if (methodInfo2 == null)
					{
						methodInfo2 = JsonFormatGeneratorStatics.MoveNextMethod;
					}
					if (methodInfo3 == null)
					{
						methodInfo3 = JsonFormatGeneratorStatics.GetCurrentMethod;
					}
				}
				else
				{
					Type interfaceType = Globals.TypeOfIEnumerator;
					CollectionKind kind = collectionContract.Kind;
					if (kind == CollectionKind.GenericDictionary || kind == CollectionKind.GenericCollection || kind == CollectionKind.GenericEnumerable)
					{
						Type[] interfaces = type.GetInterfaces();
						Type[] array2 = interfaces;
						foreach (Type type2 in array2)
						{
							if (type2.IsGenericType && type2.GetGenericTypeDefinition() == Globals.TypeOfIEnumeratorGeneric && type2.GetGenericArguments()[0] == collectionContract.ItemType)
							{
								interfaceType = type2;
								break;
							}
						}
					}
					if (methodInfo2 == null)
					{
						methodInfo2 = CollectionDataContract.GetTargetMethodWithName("MoveNext", type, interfaceType);
					}
					if (methodInfo3 == null)
					{
						methodInfo3 = CollectionDataContract.GetTargetMethodWithName("get_Current", type, interfaceType);
					}
				}
			}
			Type returnType = methodInfo3.ReturnType;
			LocalBuilder localBuilder4 = _ilg.DeclareLocal(returnType, "currentValue");
			LocalBuilder localBuilder5 = _ilg.DeclareLocal(type, "enumerator");
			_ilg.Call(_objectLocal, collectionContract.GetEnumeratorMethod);
			if (flag)
			{
				ConstructorInfo constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { Globals.TypeOfIDictionaryEnumerator });
				_ilg.ConvertValue(collectionContract.GetEnumeratorMethod.ReturnType, Globals.TypeOfIDictionaryEnumerator);
				_ilg.New(constructor);
			}
			else if (flag2)
			{
				Type type3 = Globals.TypeOfIEnumeratorGeneric.MakeGenericType(Globals.TypeOfKeyValuePair.MakeGenericType(array));
				ConstructorInfo constructor2 = type.GetConstructor(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { type3 });
				_ilg.ConvertValue(collectionContract.GetEnumeratorMethod.ReturnType, type3);
				_ilg.New(constructor2);
			}
			_ilg.Stloc(localBuilder5);
			bool flag3 = flag || flag2;
			if (flag3)
			{
				Type type4 = Globals.TypeOfKeyValue.MakeGenericType(array);
				PropertyInfo property = type4.GetProperty("Key");
				PropertyInfo property2 = type4.GetProperty("Value");
				_ilg.Load(_contextArg);
				_ilg.LoadMember(JsonFormatGeneratorStatics.UseSimpleDictionaryFormatWriteProperty);
				_ilg.If();
				WriteObjectAttribute();
				LocalBuilder localBuilder6 = _ilg.DeclareLocal(Globals.TypeOfString, "key");
				LocalBuilder localBuilder7 = _ilg.DeclareLocal(array[1], "value");
				_ilg.ForEach(localBuilder4, returnType, type, localBuilder5, methodInfo3);
				_ilg.LoadAddress(localBuilder4);
				_ilg.LoadMember(property);
				_ilg.ToString(array[0]);
				_ilg.Stloc(localBuilder6);
				_ilg.LoadAddress(localBuilder4);
				_ilg.LoadMember(property2);
				_ilg.Stloc(localBuilder7);
				WriteStartElement(localBuilder6, 0);
				WriteValue(localBuilder7);
				WriteEndElement();
				_ilg.EndForEach(methodInfo2);
				_ilg.Else();
			}
			WriteArrayAttribute();
			_ilg.ForEach(localBuilder4, returnType, type, localBuilder5, methodInfo3);
			if (methodInfo == null)
			{
				_ilg.Call(_contextArg, XmlFormatGeneratorStatics.IncrementItemCountMethod, 1);
			}
			if (!TryWritePrimitive(returnType, localBuilder4, null, null, localBuilder, 0))
			{
				WriteStartElement(localBuilder, 0);
				if (flag2 || flag)
				{
					_ilg.Call(_dataContractArg, JsonFormatGeneratorStatics.GetItemContractMethod);
					_ilg.Call(JsonFormatGeneratorStatics.GetRevisedItemContractMethod);
					_ilg.Call(JsonFormatGeneratorStatics.GetJsonDataContractMethod);
					_ilg.Load(_xmlWriterArg);
					_ilg.Load(localBuilder4);
					_ilg.ConvertValue(localBuilder4.LocalType, Globals.TypeOfObject);
					_ilg.Load(_contextArg);
					_ilg.Load(localBuilder4.LocalType);
					_ilg.LoadMember(JsonFormatGeneratorStatics.TypeHandleProperty);
					_ilg.Call(JsonFormatGeneratorStatics.WriteJsonValueMethod);
				}
				else
				{
					WriteValue(localBuilder4);
				}
				WriteEndElement();
			}
			_ilg.EndForEach(methodInfo2);
			if (flag3)
			{
				_ilg.EndIf();
			}
			[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod", Justification = "The call to MakeGenericMethod is safe due to the fact that IncrementCollectionCountGeneric is not annotated.")]
			static MethodInfo MakeIncrementCollectionCountGenericMethod(Type itemType)
			{
				return XmlFormatGeneratorStatics.IncrementCollectionCountGenericMethod.MakeGenericMethod(itemType);
			}
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private bool TryWritePrimitive(Type type, LocalBuilder value, MemberInfo memberInfo, LocalBuilder arrayItemIndex, LocalBuilder name, int nameIndex)
		{
			PrimitiveDataContract primitiveDataContract = PrimitiveDataContract.GetPrimitiveDataContract(type);
			if (primitiveDataContract == null || primitiveDataContract.UnderlyingType == Globals.TypeOfObject)
			{
				return false;
			}
			if (type.IsValueType)
			{
				_ilg.Load(_xmlWriterArg);
			}
			else
			{
				_ilg.Load(_contextArg);
				_ilg.Load(_xmlWriterArg);
			}
			if (value != null)
			{
				_ilg.Load(value);
			}
			else if (memberInfo != null)
			{
				_ilg.LoadAddress(_objectLocal);
				_ilg.LoadMember(memberInfo);
			}
			else
			{
				_ilg.LoadArrayElement(_objectLocal, arrayItemIndex);
			}
			if (name != null)
			{
				_ilg.Load(name);
			}
			else
			{
				_ilg.LoadArrayElement(_memberNamesArg, nameIndex);
			}
			_ilg.Load(null);
			_ilg.Call(primitiveDataContract.XmlFormatWriterMethod);
			return true;
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private bool TryWritePrimitiveArray(Type type, Type itemType, LocalBuilder value, LocalBuilder itemName)
		{
			PrimitiveDataContract primitiveDataContract = PrimitiveDataContract.GetPrimitiveDataContract(itemType);
			if (primitiveDataContract == null)
			{
				return false;
			}
			string text = null;
			switch (Type.GetTypeCode(itemType))
			{
			case TypeCode.Boolean:
				text = "WriteJsonBooleanArray";
				break;
			case TypeCode.DateTime:
				text = "WriteJsonDateTimeArray";
				break;
			case TypeCode.Decimal:
				text = "WriteJsonDecimalArray";
				break;
			case TypeCode.Int32:
				text = "WriteJsonInt32Array";
				break;
			case TypeCode.Int64:
				text = "WriteJsonInt64Array";
				break;
			case TypeCode.Single:
				text = "WriteJsonSingleArray";
				break;
			case TypeCode.Double:
				text = "WriteJsonDoubleArray";
				break;
			}
			if (text != null)
			{
				WriteArrayAttribute();
				MethodInfo method = typeof(JsonWriterDelegator).GetMethod(text, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new Type[3]
				{
					type,
					typeof(XmlDictionaryString),
					typeof(XmlDictionaryString)
				});
				_ilg.Call(_xmlWriterArg, method, value, itemName, null);
				return true;
			}
			return false;
		}

		private void WriteArrayAttribute()
		{
			_ilg.Call(_xmlWriterArg, JsonFormatGeneratorStatics.WriteAttributeStringMethod, null, "type", string.Empty, "array");
		}

		private void WriteObjectAttribute()
		{
			_ilg.Call(_xmlWriterArg, JsonFormatGeneratorStatics.WriteAttributeStringMethod, null, "type", null, "object");
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private void WriteValue(LocalBuilder memberValue)
		{
			Type type = memberValue.LocalType;
			if (type.IsPointer)
			{
				_ilg.Load(memberValue);
				_ilg.Load(type);
				_ilg.Call(JsonFormatGeneratorStatics.BoxPointer);
				type = typeof(Pointer);
				memberValue = _ilg.DeclareLocal(type, "memberValueRefPointer");
				_ilg.Store(memberValue);
			}
			bool flag = type.IsGenericType && type.GetGenericTypeDefinition() == Globals.TypeOfNullable;
			if (type.IsValueType && !flag)
			{
				PrimitiveDataContract primitiveDataContract = PrimitiveDataContract.GetPrimitiveDataContract(type);
				if (primitiveDataContract != null)
				{
					_ilg.Call(_xmlWriterArg, primitiveDataContract.XmlFormatContentWriterMethod, memberValue);
				}
				else
				{
					InternalSerialize(XmlFormatGeneratorStatics.InternalSerializeMethod, memberValue, type, writeXsiType: false);
				}
				return;
			}
			if (flag)
			{
				memberValue = UnwrapNullableObject(memberValue);
				type = memberValue.LocalType;
			}
			else
			{
				_ilg.Load(memberValue);
				_ilg.Load(null);
				_ilg.Ceq();
			}
			_ilg.If();
			_ilg.Call(_contextArg, XmlFormatGeneratorStatics.WriteNullMethod, _xmlWriterArg, type, DataContract.IsTypeSerializable(type));
			_ilg.Else();
			PrimitiveDataContract primitiveDataContract2 = PrimitiveDataContract.GetPrimitiveDataContract(type);
			if (primitiveDataContract2 != null && primitiveDataContract2.UnderlyingType != Globals.TypeOfObject)
			{
				if (flag)
				{
					_ilg.Call(_xmlWriterArg, primitiveDataContract2.XmlFormatContentWriterMethod, memberValue);
				}
				else
				{
					_ilg.Call(_contextArg, primitiveDataContract2.XmlFormatContentWriterMethod, _xmlWriterArg, memberValue);
				}
			}
			else
			{
				if (type == Globals.TypeOfObject || type == Globals.TypeOfValueType || ((IList)Globals.TypeOfNullable.GetInterfaces()).Contains((object?)type))
				{
					_ilg.Load(memberValue);
					_ilg.ConvertValue(memberValue.LocalType, Globals.TypeOfObject);
					memberValue = _ilg.DeclareLocal(Globals.TypeOfObject, "unwrappedMemberValue");
					type = memberValue.LocalType;
					_ilg.Stloc(memberValue);
					_ilg.If(memberValue, Cmp.EqualTo, null);
					_ilg.Call(_contextArg, XmlFormatGeneratorStatics.WriteNullMethod, _xmlWriterArg, type, DataContract.IsTypeSerializable(type));
					_ilg.Else();
				}
				InternalSerialize(flag ? XmlFormatGeneratorStatics.InternalSerializeMethod : XmlFormatGeneratorStatics.InternalSerializeReferenceMethod, memberValue, type, writeXsiType: false);
				if (type == Globals.TypeOfObject)
				{
					_ilg.EndIf();
				}
			}
			_ilg.EndIf();
		}

		private void InternalSerialize(MethodInfo methodInfo, LocalBuilder memberValue, Type memberType, bool writeXsiType)
		{
			_ilg.Load(_contextArg);
			_ilg.Load(_xmlWriterArg);
			_ilg.Load(memberValue);
			_ilg.ConvertValue(memberValue.LocalType, Globals.TypeOfObject);
			LocalBuilder localBuilder = _ilg.DeclareLocal(typeof(RuntimeTypeHandle), "typeHandleValue");
			_ilg.Call(memberValue, XmlFormatGeneratorStatics.GetTypeMethod);
			_ilg.Call(XmlFormatGeneratorStatics.GetTypeHandleMethod);
			_ilg.Stloc(localBuilder);
			_ilg.LoadAddress(localBuilder);
			_ilg.Ldtoken(memberType);
			_ilg.Call(typeof(RuntimeTypeHandle).GetMethod("Equals", new Type[1] { typeof(RuntimeTypeHandle) }));
			_ilg.Load(writeXsiType);
			_ilg.Load(DataContract.GetId(memberType.TypeHandle));
			_ilg.Ldtoken(memberType);
			_ilg.Call(methodInfo);
		}

		private LocalBuilder UnwrapNullableObject(LocalBuilder memberValue)
		{
			Type type = memberValue.LocalType;
			Label label = _ilg.DefineLabel();
			Label label2 = _ilg.DefineLabel();
			_ilg.LoadAddress(memberValue);
			while (type.IsGenericType && type.GetGenericTypeDefinition() == Globals.TypeOfNullable)
			{
				Type type2 = type.GetGenericArguments()[0];
				_ilg.Dup();
				_ilg.Call(typeof(Nullable<>).MakeGenericType(type2).GetMethod("get_HasValue"));
				_ilg.Brfalse(label);
				_ilg.Call(typeof(Nullable<>).MakeGenericType(type2).GetMethod("get_Value"));
				type = type2;
			}
			memberValue = _ilg.DeclareLocal(type, "nullableUnwrappedMemberValue");
			_ilg.Stloc(memberValue);
			_ilg.Load(false);
			_ilg.Br(label2);
			_ilg.MarkLabel(label);
			_ilg.Pop();
			_ilg.LoadAddress(memberValue);
			_ilg.InitObj(type);
			_ilg.Load(true);
			_ilg.MarkLabel(label2);
			return memberValue;
		}

		private void WriteStartElement(LocalBuilder nameLocal, int nameIndex)
		{
			_ilg.Load(_xmlWriterArg);
			if (nameLocal == null)
			{
				_ilg.LoadArrayElement(_memberNamesArg, nameIndex);
			}
			else
			{
				_ilg.Load(nameLocal);
			}
			_ilg.Load(null);
			if (nameLocal != null && nameLocal.LocalType == typeof(string))
			{
				_ilg.Call(JsonFormatGeneratorStatics.WriteStartElementStringMethod);
			}
			else
			{
				_ilg.Call(JsonFormatGeneratorStatics.WriteStartElementMethod);
			}
		}

		private void WriteEndElement()
		{
			_ilg.Call(_xmlWriterArg, JsonFormatGeneratorStatics.WriteEndElementMethod);
		}
	}

	private readonly CriticalHelper _helper;

	public JsonFormatWriterGenerator()
	{
		_helper = new CriticalHelper();
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal JsonFormatClassWriterDelegate GenerateClassWriter(ClassDataContract classContract)
	{
		return _helper.GenerateClassWriter(classContract);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal JsonFormatCollectionWriterDelegate GenerateCollectionWriter(CollectionDataContract collectionContract)
	{
		return _helper.GenerateCollectionWriter(collectionContract);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern", Justification = "The trimmer will never remove the Invoke method from delegates.")]
	internal static MethodInfo GetInvokeMethod(Type delegateType)
	{
		return delegateType.GetMethod("Invoke");
	}
}

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Security;
using System.Xml;

namespace System.Runtime.Serialization.Json;

internal sealed class JsonFormatReaderGenerator
{
	private sealed class CriticalHelper
	{
		private enum KeyParseMode
		{
			Fail,
			AsString,
			UsingParseEnum,
			UsingCustomParse
		}

		private CodeGenerator _ilg;

		private LocalBuilder _objectLocal;

		private Type _objectType;

		private ArgBuilder _xmlReaderArg;

		private ArgBuilder _contextArg;

		private ArgBuilder _memberNamesArg;

		private ArgBuilder _collectionContractArg;

		private ArgBuilder _emptyDictionaryStringArg;

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		public JsonFormatClassReaderDelegate GenerateClassReader(ClassDataContract classContract)
		{
			_ilg = new CodeGenerator();
			bool flag = classContract.RequiresMemberAccessForRead(null);
			try
			{
				BeginMethod(_ilg, "Read" + DataContract.SanitizeTypeName(classContract.StableName.Name) + "FromJson", typeof(JsonFormatClassReaderDelegate), flag);
			}
			catch (SecurityException securityException)
			{
				if (!flag)
				{
					throw;
				}
				classContract.RequiresMemberAccessForRead(securityException);
			}
			InitArgs();
			CreateObject(classContract);
			_ilg.Call(_contextArg, XmlFormatGeneratorStatics.AddNewObjectMethod, _objectLocal);
			InvokeOnDeserializing(classContract);
			if (classContract.IsISerializable)
			{
				ReadISerializable(classContract);
			}
			else
			{
				ReadClass(classContract);
			}
			if (Globals.TypeOfIDeserializationCallback.IsAssignableFrom(classContract.UnderlyingType))
			{
				_ilg.Call(_objectLocal, JsonFormatGeneratorStatics.OnDeserializationMethod, null);
			}
			InvokeOnDeserialized(classContract);
			if (!InvokeFactoryMethod(classContract))
			{
				_ilg.Load(_objectLocal);
				if (classContract.UnderlyingType == Globals.TypeOfDateTimeOffsetAdapter)
				{
					_ilg.ConvertValue(_objectLocal.LocalType, Globals.TypeOfDateTimeOffsetAdapter);
					_ilg.Call(XmlFormatGeneratorStatics.GetDateTimeOffsetMethod);
					_ilg.ConvertValue(Globals.TypeOfDateTimeOffset, _ilg.CurrentMethod.ReturnType);
				}
				else if (classContract.UnderlyingType == Globals.TypeOfMemoryStreamAdapter)
				{
					_ilg.ConvertValue(_objectLocal.LocalType, Globals.TypeOfMemoryStreamAdapter);
					_ilg.Call(XmlFormatGeneratorStatics.GetMemoryStreamMethod);
					_ilg.ConvertValue(Globals.TypeOfMemoryStream, _ilg.CurrentMethod.ReturnType);
				}
				else if (classContract.IsKeyValuePairAdapter)
				{
					_ilg.Call(classContract.GetKeyValuePairMethodInfo);
					_ilg.ConvertValue(Globals.TypeOfKeyValuePair.MakeGenericType(classContract.KeyValuePairGenericArguments), _ilg.CurrentMethod.ReturnType);
				}
				else
				{
					_ilg.ConvertValue(_objectLocal.LocalType, _ilg.CurrentMethod.ReturnType);
				}
			}
			return (JsonFormatClassReaderDelegate)_ilg.EndMethod();
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		public JsonFormatCollectionReaderDelegate GenerateCollectionReader(CollectionDataContract collectionContract)
		{
			_ilg = GenerateCollectionReaderHelper(collectionContract, isGetOnlyCollection: false);
			ReadCollection(collectionContract);
			_ilg.Load(_objectLocal);
			_ilg.ConvertValue(_objectLocal.LocalType, _ilg.CurrentMethod.ReturnType);
			return (JsonFormatCollectionReaderDelegate)_ilg.EndMethod();
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		public JsonFormatGetOnlyCollectionReaderDelegate GenerateGetOnlyCollectionReader(CollectionDataContract collectionContract)
		{
			_ilg = GenerateCollectionReaderHelper(collectionContract, isGetOnlyCollection: true);
			ReadGetOnlyCollection(collectionContract);
			return (JsonFormatGetOnlyCollectionReaderDelegate)_ilg.EndMethod();
		}

		private CodeGenerator GenerateCollectionReaderHelper(CollectionDataContract collectionContract, bool isGetOnlyCollection)
		{
			_ilg = new CodeGenerator();
			bool flag = collectionContract.RequiresMemberAccessForRead(null);
			try
			{
				if (isGetOnlyCollection)
				{
					BeginMethod(_ilg, "Read" + DataContract.SanitizeTypeName(collectionContract.StableName.Name) + "FromJsonIsGetOnly", typeof(JsonFormatGetOnlyCollectionReaderDelegate), flag);
				}
				else
				{
					BeginMethod(_ilg, "Read" + DataContract.SanitizeTypeName(collectionContract.StableName.Name) + "FromJson", typeof(JsonFormatCollectionReaderDelegate), flag);
				}
			}
			catch (SecurityException securityException)
			{
				if (!flag)
				{
					throw;
				}
				collectionContract.RequiresMemberAccessForRead(securityException);
			}
			InitArgs();
			_collectionContractArg = _ilg.GetArg(4);
			return _ilg;
		}

		private void BeginMethod(CodeGenerator ilg, string methodName, Type delegateType, bool allowPrivateMemberAccess)
		{
			MethodInfo invokeMethod = JsonFormatWriterGenerator.GetInvokeMethod(delegateType);
			ParameterInfo[] parameters = invokeMethod.GetParameters();
			Type[] array = new Type[parameters.Length];
			for (int i = 0; i < parameters.Length; i++)
			{
				array[i] = parameters[i].ParameterType;
			}
			DynamicMethod dynamicMethod = new DynamicMethod(methodName, invokeMethod.ReturnType, array, typeof(JsonFormatReaderGenerator).Module, allowPrivateMemberAccess);
			ilg.BeginMethod(dynamicMethod, delegateType, methodName, array, allowPrivateMemberAccess);
		}

		private void InitArgs()
		{
			_xmlReaderArg = _ilg.GetArg(0);
			_contextArg = _ilg.GetArg(1);
			_emptyDictionaryStringArg = _ilg.GetArg(2);
			_memberNamesArg = _ilg.GetArg(3);
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private void CreateObject(ClassDataContract classContract)
		{
			_objectType = classContract.UnderlyingType;
			Type objectType = classContract.ObjectType;
			_objectLocal = _ilg.DeclareLocal(objectType, "objectDeserialized");
			if (classContract.UnderlyingType == Globals.TypeOfDBNull)
			{
				_ilg.LoadMember(Globals.TypeOfDBNull.GetField("Value"));
				_ilg.Stloc(_objectLocal);
			}
			else if (classContract.IsNonAttributedType)
			{
				if (objectType.IsValueType)
				{
					_ilg.Ldloca(_objectLocal);
					_ilg.InitObj(objectType);
				}
				else
				{
					_ilg.New(classContract.GetNonAttributedTypeConstructor());
					_ilg.Stloc(_objectLocal);
				}
			}
			else
			{
				_ilg.Call(null, JsonFormatGeneratorStatics.GetUninitializedObjectMethod, classContract.TypeForInitialization);
				_ilg.ConvertValue(Globals.TypeOfObject, objectType);
				_ilg.Stloc(_objectLocal);
			}
		}

		private void InvokeOnDeserializing(ClassDataContract classContract)
		{
			if (classContract.BaseContract != null)
			{
				InvokeOnDeserializing(classContract.BaseContract);
			}
			if (classContract.OnDeserializing != null)
			{
				_ilg.LoadAddress(_objectLocal);
				_ilg.ConvertAddress(_objectLocal.LocalType, _objectType);
				_ilg.Load(_contextArg);
				_ilg.LoadMember(XmlFormatGeneratorStatics.GetStreamingContextMethod);
				_ilg.Call(classContract.OnDeserializing);
			}
		}

		private void InvokeOnDeserialized(ClassDataContract classContract)
		{
			if (classContract.BaseContract != null)
			{
				InvokeOnDeserialized(classContract.BaseContract);
			}
			if (classContract.OnDeserialized != null)
			{
				_ilg.LoadAddress(_objectLocal);
				_ilg.ConvertAddress(_objectLocal.LocalType, _objectType);
				_ilg.Load(_contextArg);
				_ilg.LoadMember(XmlFormatGeneratorStatics.GetStreamingContextMethod);
				_ilg.Call(classContract.OnDeserialized);
			}
		}

		private bool HasFactoryMethod(ClassDataContract classContract)
		{
			return Globals.TypeOfIObjectReference.IsAssignableFrom(classContract.UnderlyingType);
		}

		private bool InvokeFactoryMethod(ClassDataContract classContract)
		{
			if (HasFactoryMethod(classContract))
			{
				_ilg.Load(_contextArg);
				_ilg.LoadAddress(_objectLocal);
				_ilg.ConvertAddress(_objectLocal.LocalType, Globals.TypeOfIObjectReference);
				_ilg.Load(Globals.NewObjectId);
				_ilg.Call(XmlFormatGeneratorStatics.GetRealObjectMethod);
				_ilg.ConvertValue(Globals.TypeOfObject, _ilg.CurrentMethod.ReturnType);
				return true;
			}
			return false;
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private void ReadClass(ClassDataContract classContract)
		{
			if (classContract.HasExtensionData)
			{
				LocalBuilder localBuilder = _ilg.DeclareLocal(Globals.TypeOfExtensionDataObject, "extensionData");
				_ilg.New(JsonFormatGeneratorStatics.ExtensionDataObjectCtor);
				_ilg.Store(localBuilder);
				ReadMembers(classContract, localBuilder);
				for (ClassDataContract classDataContract = classContract; classDataContract != null; classDataContract = classDataContract.BaseContract)
				{
					MethodInfo extensionDataSetMethod = classDataContract.ExtensionDataSetMethod;
					if (extensionDataSetMethod != null)
					{
						_ilg.Call(_objectLocal, extensionDataSetMethod, localBuilder);
					}
				}
			}
			else
			{
				ReadMembers(classContract, null);
			}
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private void ReadMembers(ClassDataContract classContract, LocalBuilder extensionDataLocal)
		{
			int num = classContract.MemberNames.Length;
			_ilg.Call(_contextArg, XmlFormatGeneratorStatics.IncrementItemCountMethod, num);
			BitFlagsGenerator bitFlagsGenerator = new BitFlagsGenerator(num, _ilg, classContract.UnderlyingType.Name + "_ExpectedElements");
			byte[] array = new byte[bitFlagsGenerator.GetLocalCount()];
			SetRequiredElements(classContract, array);
			SetExpectedElements(bitFlagsGenerator, 0);
			LocalBuilder localBuilder = _ilg.DeclareLocal(Globals.TypeOfInt, "memberIndex", -1);
			Label label = _ilg.DefineLabel();
			Label label2 = _ilg.DefineLabel();
			object forState = _ilg.For(null, null, null);
			_ilg.Call(null, XmlFormatGeneratorStatics.MoveToNextElementMethod, _xmlReaderArg);
			_ilg.IfFalseBreak(forState);
			_ilg.Call(_contextArg, JsonFormatGeneratorStatics.GetJsonMemberIndexMethod, _xmlReaderArg, _memberNamesArg, localBuilder, extensionDataLocal);
			if (num > 0)
			{
				Label[] memberLabels = _ilg.Switch(num);
				ReadMembers(classContract, bitFlagsGenerator, memberLabels, label, localBuilder);
				_ilg.EndSwitch();
			}
			else
			{
				_ilg.Pop();
			}
			_ilg.EndFor();
			CheckRequiredElements(bitFlagsGenerator, array, label2);
			Label label3 = _ilg.DefineLabel();
			_ilg.Br(label3);
			_ilg.MarkLabel(label);
			_ilg.Call(null, JsonFormatGeneratorStatics.ThrowDuplicateMemberExceptionMethod, _objectLocal, _memberNamesArg, localBuilder);
			_ilg.MarkLabel(label2);
			_ilg.Load(_objectLocal);
			_ilg.ConvertValue(_objectLocal.LocalType, Globals.TypeOfObject);
			_ilg.Load(_memberNamesArg);
			bitFlagsGenerator.LoadArray();
			LoadArray(array, "requiredElements");
			_ilg.Call(JsonFormatGeneratorStatics.ThrowMissingRequiredMembersMethod);
			_ilg.MarkLabel(label3);
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private int ReadMembers(ClassDataContract classContract, BitFlagsGenerator expectedElements, Label[] memberLabels, Label throwDuplicateMemberLabel, LocalBuilder memberIndexLocal)
		{
			int num = ((classContract.BaseContract != null) ? ReadMembers(classContract.BaseContract, expectedElements, memberLabels, throwDuplicateMemberLabel, memberIndexLocal) : 0);
			int num2 = 0;
			while (num2 < classContract.Members.Count)
			{
				DataMember dataMember = classContract.Members[num2];
				Type memberType = dataMember.MemberType;
				_ilg.Case(memberLabels[num], dataMember.Name);
				_ilg.Set(memberIndexLocal, num);
				expectedElements.Load(num);
				_ilg.Brfalse(throwDuplicateMemberLabel);
				LocalBuilder localBuilder = null;
				if (dataMember.IsGetOnlyCollection)
				{
					_ilg.LoadAddress(_objectLocal);
					_ilg.LoadMember(dataMember.MemberInfo);
					localBuilder = _ilg.DeclareLocal(memberType, dataMember.Name + "Value");
					_ilg.Stloc(localBuilder);
					_ilg.Call(_contextArg, XmlFormatGeneratorStatics.StoreCollectionMemberInfoMethod, localBuilder);
					ReadValue(memberType, dataMember.Name);
				}
				else
				{
					_ilg.Call(_contextArg, XmlFormatGeneratorStatics.ResetCollectionMemberInfoMethod);
					localBuilder = ReadValue(memberType, dataMember.Name);
					_ilg.LoadAddress(_objectLocal);
					_ilg.ConvertAddress(_objectLocal.LocalType, _objectType);
					_ilg.Ldloc(localBuilder);
					_ilg.StoreMember(dataMember.MemberInfo);
				}
				ResetExpectedElements(expectedElements, num);
				_ilg.EndCase();
				num2++;
				num++;
			}
			return num;
		}

		private void CheckRequiredElements(BitFlagsGenerator expectedElements, byte[] requiredElements, Label throwMissingRequiredMembersLabel)
		{
			for (int i = 0; i < requiredElements.Length; i++)
			{
				_ilg.Load(expectedElements.GetLocal(i));
				_ilg.Load(requiredElements[i]);
				_ilg.And();
				_ilg.Load(0);
				_ilg.Ceq();
				_ilg.Brfalse(throwMissingRequiredMembersLabel);
			}
		}

		private void LoadArray(byte[] array, string name)
		{
			LocalBuilder localBuilder = _ilg.DeclareLocal(Globals.TypeOfByteArray, name);
			_ilg.NewArray(typeof(byte), array.Length);
			_ilg.Store(localBuilder);
			for (int i = 0; i < array.Length; i++)
			{
				_ilg.StoreArrayElement(localBuilder, i, array[i]);
			}
			_ilg.Load(localBuilder);
		}

		private int SetRequiredElements(ClassDataContract contract, byte[] requiredElements)
		{
			int num = ((contract.BaseContract != null) ? SetRequiredElements(contract.BaseContract, requiredElements) : 0);
			List<DataMember> members = contract.Members;
			int num2 = 0;
			while (num2 < members.Count)
			{
				if (members[num2].IsRequired)
				{
					BitFlagsGenerator.SetBit(requiredElements, num);
				}
				num2++;
				num++;
			}
			return num;
		}

		private void SetExpectedElements(BitFlagsGenerator expectedElements, int startIndex)
		{
			int bitCount = expectedElements.GetBitCount();
			for (int i = startIndex; i < bitCount; i++)
			{
				expectedElements.Store(i, value: true);
			}
		}

		private void ResetExpectedElements(BitFlagsGenerator expectedElements, int index)
		{
			expectedElements.Store(index, value: false);
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private void ReadISerializable(ClassDataContract classContract)
		{
			ConstructorInfo constructor = classContract.UnderlyingType.GetConstructor(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, JsonFormatGeneratorStatics.SerInfoCtorArgs);
			if (constructor == null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.SerializationInfo_ConstructorNotFound, DataContract.GetClrTypeFullName(classContract.UnderlyingType))));
			}
			_ilg.LoadAddress(_objectLocal);
			_ilg.ConvertAddress(_objectLocal.LocalType, _objectType);
			_ilg.Call(_contextArg, XmlFormatGeneratorStatics.ReadSerializationInfoMethod, _xmlReaderArg, classContract.UnderlyingType);
			_ilg.Load(_contextArg);
			_ilg.LoadMember(XmlFormatGeneratorStatics.GetStreamingContextMethod);
			_ilg.Call(constructor);
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private LocalBuilder ReadValue(Type type, string name)
		{
			LocalBuilder localBuilder = _ilg.DeclareLocal(type, "valueRead");
			LocalBuilder localBuilder2 = null;
			int num = 0;
			while (type.IsGenericType && type.GetGenericTypeDefinition() == Globals.TypeOfNullable)
			{
				num++;
				type = type.GetGenericArguments()[0];
			}
			PrimitiveDataContract primitiveDataContract = PrimitiveDataContract.GetPrimitiveDataContract(type);
			if ((primitiveDataContract != null && primitiveDataContract.UnderlyingType != Globals.TypeOfObject) || num != 0 || type.IsValueType)
			{
				LocalBuilder localBuilder3 = _ilg.DeclareLocal(Globals.TypeOfString, "objectIdRead");
				_ilg.Call(_contextArg, XmlFormatGeneratorStatics.ReadAttributesMethod, _xmlReaderArg);
				_ilg.Call(_contextArg, XmlFormatGeneratorStatics.ReadIfNullOrRefMethod, _xmlReaderArg, type, DataContract.IsTypeSerializable(type));
				_ilg.Stloc(localBuilder3);
				_ilg.If(localBuilder3, Cmp.EqualTo, null);
				if (num != 0)
				{
					_ilg.LoadAddress(localBuilder);
					_ilg.InitObj(localBuilder.LocalType);
				}
				else if (type.IsValueType)
				{
					ThrowSerializationException(System.SR.Format(System.SR.ValueTypeCannotBeNull, DataContract.GetClrTypeFullName(type)));
				}
				else
				{
					_ilg.Load(null);
					_ilg.Stloc(localBuilder);
				}
				_ilg.ElseIfIsEmptyString(localBuilder3);
				_ilg.Call(_contextArg, XmlFormatGeneratorStatics.GetObjectIdMethod);
				_ilg.Stloc(localBuilder3);
				if (type.IsValueType)
				{
					_ilg.IfNotIsEmptyString(localBuilder3);
					ThrowSerializationException(System.SR.Format(System.SR.ValueTypeCannotHaveId, DataContract.GetClrTypeFullName(type)));
					_ilg.EndIf();
				}
				if (num != 0)
				{
					localBuilder2 = localBuilder;
					localBuilder = _ilg.DeclareLocal(type, "innerValueRead");
				}
				if (primitiveDataContract != null && primitiveDataContract.UnderlyingType != Globals.TypeOfObject)
				{
					_ilg.Call(_xmlReaderArg, primitiveDataContract.XmlFormatReaderMethod);
					_ilg.Stloc(localBuilder);
					if (!type.IsValueType)
					{
						_ilg.Call(_contextArg, XmlFormatGeneratorStatics.AddNewObjectMethod, localBuilder);
					}
				}
				else
				{
					InternalDeserialize(localBuilder, type, name);
				}
				_ilg.Else();
				if (type.IsValueType)
				{
					ThrowSerializationException(System.SR.Format(System.SR.ValueTypeCannotHaveRef, DataContract.GetClrTypeFullName(type)));
				}
				else
				{
					_ilg.Call(_contextArg, XmlFormatGeneratorStatics.GetExistingObjectMethod, localBuilder3, type, name, string.Empty);
					_ilg.ConvertValue(Globals.TypeOfObject, type);
					_ilg.Stloc(localBuilder);
				}
				_ilg.EndIf();
				if (localBuilder2 != null)
				{
					_ilg.If(localBuilder3, Cmp.NotEqualTo, null);
					WrapNullableObject(localBuilder, localBuilder2, num);
					_ilg.EndIf();
					localBuilder = localBuilder2;
				}
			}
			else
			{
				InternalDeserialize(localBuilder, type, name);
			}
			return localBuilder;
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private void InternalDeserialize(LocalBuilder value, Type type, string name)
		{
			_ilg.Load(_contextArg);
			_ilg.Load(_xmlReaderArg);
			_ilg.Load(DataContract.GetId(type.TypeHandle));
			_ilg.Ldtoken(type);
			_ilg.Load(name);
			_ilg.Load(string.Empty);
			_ilg.Call(XmlFormatGeneratorStatics.InternalDeserializeMethod);
			if (type.IsPointer)
			{
				_ilg.Call(JsonFormatGeneratorStatics.UnboxPointer);
			}
			else
			{
				_ilg.ConvertValue(Globals.TypeOfObject, type);
			}
			_ilg.Stloc(value);
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private void WrapNullableObject(LocalBuilder innerValue, LocalBuilder outerValue, int nullables)
		{
			Type type = innerValue.LocalType;
			Type localType = outerValue.LocalType;
			_ilg.LoadAddress(outerValue);
			_ilg.Load(innerValue);
			for (int i = 1; i < nullables; i++)
			{
				Type type2 = Globals.TypeOfNullable.MakeGenericType(type);
				_ilg.New(type2.GetConstructor(new Type[1] { type }));
				type = type2;
			}
			_ilg.Call(localType.GetConstructor(new Type[1] { type }));
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private void ReadCollection(CollectionDataContract collectionContract)
		{
			Type type = collectionContract.UnderlyingType;
			Type itemType2 = collectionContract.ItemType;
			bool flag = collectionContract.Kind == CollectionKind.Array;
			ConstructorInfo constructor = collectionContract.Constructor;
			if (type.IsInterface)
			{
				switch (collectionContract.Kind)
				{
				case CollectionKind.GenericDictionary:
					type = Globals.TypeOfDictionaryGeneric.MakeGenericType(itemType2.GetGenericArguments());
					constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
					break;
				case CollectionKind.Dictionary:
					type = Globals.TypeOfHashtable;
					constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
					break;
				case CollectionKind.GenericList:
				case CollectionKind.GenericCollection:
				case CollectionKind.List:
				case CollectionKind.GenericEnumerable:
				case CollectionKind.Collection:
				case CollectionKind.Enumerable:
					type = itemType2.MakeArrayType();
					flag = true;
					break;
				}
			}
			_objectLocal = _ilg.DeclareLocal(type, "objectDeserialized");
			if (!flag)
			{
				if (type.IsValueType)
				{
					_ilg.Ldloca(_objectLocal);
					_ilg.InitObj(type);
				}
				else
				{
					_ilg.New(constructor);
					_ilg.Stloc(_objectLocal);
					_ilg.Call(_contextArg, XmlFormatGeneratorStatics.AddNewObjectMethod, _objectLocal);
				}
			}
			bool flag2 = collectionContract.Kind == CollectionKind.Dictionary || collectionContract.Kind == CollectionKind.GenericDictionary;
			if (flag2)
			{
				_ilg.Load(_contextArg);
				_ilg.LoadMember(JsonFormatGeneratorStatics.UseSimpleDictionaryFormatReadProperty);
				_ilg.If();
				ReadSimpleDictionary(collectionContract, itemType2);
				_ilg.Else();
			}
			LocalBuilder localBuilder = _ilg.DeclareLocal(Globals.TypeOfString, "objectIdRead");
			_ilg.Call(_contextArg, XmlFormatGeneratorStatics.GetObjectIdMethod);
			_ilg.Stloc(localBuilder);
			bool flag3 = false;
			if (flag && TryReadPrimitiveArray(itemType2))
			{
				flag3 = true;
				_ilg.IfNot();
			}
			LocalBuilder localBuilder2 = null;
			if (flag)
			{
				localBuilder2 = _ilg.DeclareLocal(type, "growingCollection");
				_ilg.NewArray(itemType2, 32);
				_ilg.Stloc(localBuilder2);
			}
			LocalBuilder localBuilder3 = _ilg.DeclareLocal(Globals.TypeOfInt, "i");
			object forState = _ilg.For(localBuilder3, 0, int.MaxValue);
			IsStartElement(_memberNamesArg, _emptyDictionaryStringArg);
			_ilg.If();
			_ilg.Call(_contextArg, XmlFormatGeneratorStatics.IncrementItemCountMethod, 1);
			LocalBuilder value = ReadCollectionItem(collectionContract, itemType2);
			if (flag)
			{
				MethodInfo methodInfo = MakeGenericMethod(XmlFormatGeneratorStatics.EnsureArraySizeMethod, itemType2);
				_ilg.Call(null, methodInfo, localBuilder2, localBuilder3);
				_ilg.Stloc(localBuilder2);
				_ilg.StoreArrayElement(localBuilder2, localBuilder3, value);
			}
			else
			{
				StoreCollectionValue(_objectLocal, value, collectionContract);
			}
			_ilg.Else();
			IsEndElement();
			_ilg.If();
			_ilg.Break(forState);
			_ilg.Else();
			HandleUnexpectedItemInCollection(localBuilder3);
			_ilg.EndIf();
			_ilg.EndIf();
			_ilg.EndFor();
			if (flag)
			{
				MethodInfo methodInfo2 = MakeGenericMethod(XmlFormatGeneratorStatics.TrimArraySizeMethod, itemType2);
				_ilg.Call(null, methodInfo2, localBuilder2, localBuilder3);
				_ilg.Stloc(_objectLocal);
				_ilg.Call(_contextArg, XmlFormatGeneratorStatics.AddNewObjectWithIdMethod, localBuilder, _objectLocal);
			}
			if (flag3)
			{
				_ilg.Else();
				_ilg.Call(_contextArg, XmlFormatGeneratorStatics.AddNewObjectWithIdMethod, localBuilder, _objectLocal);
				_ilg.EndIf();
			}
			if (flag2)
			{
				_ilg.EndIf();
			}
			[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod", Justification = "The call to MakeGenericMethod is safe due to the fact that EnsureArraySizeMethod and TrimArraySizeMethod are not annotated.")]
			static MethodInfo MakeGenericMethod(MethodInfo method, Type itemType)
			{
				return method.MakeGenericMethod(itemType);
			}
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private void ReadSimpleDictionary(CollectionDataContract collectionContract, Type keyValueType)
		{
			Type[] genericArguments = keyValueType.GetGenericArguments();
			Type type = genericArguments[0];
			Type type2 = genericArguments[1];
			int num = 0;
			Type type3 = type;
			while (type.IsGenericType && type.GetGenericTypeDefinition() == Globals.TypeOfNullable)
			{
				num++;
				type = type.GetGenericArguments()[0];
			}
			ClassDataContract classDataContract = (ClassDataContract)collectionContract.ItemContract;
			DataContract memberTypeContract = classDataContract.Members[0].MemberTypeContract;
			KeyParseMode keyParseMode = KeyParseMode.Fail;
			if (type == Globals.TypeOfString || type == Globals.TypeOfObject)
			{
				keyParseMode = KeyParseMode.AsString;
			}
			else if (type.IsEnum)
			{
				keyParseMode = KeyParseMode.UsingParseEnum;
			}
			else if (memberTypeContract.ParseMethod != null)
			{
				keyParseMode = KeyParseMode.UsingCustomParse;
			}
			if (keyParseMode == KeyParseMode.Fail)
			{
				ThrowSerializationException(System.SR.Format(System.SR.KeyTypeCannotBeParsedInSimpleDictionary, DataContract.GetClrTypeFullName(collectionContract.UnderlyingType), DataContract.GetClrTypeFullName(type)));
				return;
			}
			LocalBuilder localBuilder = _ilg.DeclareLocal(typeof(XmlNodeType), "nodeType");
			_ilg.BeginWhileCondition();
			_ilg.Call(_xmlReaderArg, JsonFormatGeneratorStatics.MoveToContentMethod);
			_ilg.Stloc(localBuilder);
			_ilg.Load(localBuilder);
			_ilg.Load(XmlNodeType.EndElement);
			_ilg.BeginWhileBody(Cmp.NotEqualTo);
			_ilg.Load(localBuilder);
			_ilg.Load(XmlNodeType.Element);
			_ilg.If(Cmp.NotEqualTo);
			ThrowUnexpectedStateException(XmlNodeType.Element);
			_ilg.EndIf();
			_ilg.Call(_contextArg, XmlFormatGeneratorStatics.IncrementItemCountMethod, 1);
			if (keyParseMode == KeyParseMode.UsingParseEnum)
			{
				_ilg.Load(type);
			}
			_ilg.Load(_xmlReaderArg);
			_ilg.Call(JsonFormatGeneratorStatics.GetJsonMemberNameMethod);
			switch (keyParseMode)
			{
			case KeyParseMode.UsingParseEnum:
				_ilg.Call(JsonFormatGeneratorStatics.ParseEnumMethod);
				_ilg.ConvertValue(Globals.TypeOfObject, type);
				break;
			case KeyParseMode.UsingCustomParse:
				_ilg.Call(memberTypeContract.ParseMethod);
				break;
			}
			LocalBuilder localBuilder2 = _ilg.DeclareLocal(type, "key");
			_ilg.Stloc(localBuilder2);
			if (num > 0)
			{
				LocalBuilder localBuilder3 = _ilg.DeclareLocal(type3, "keyOriginal");
				WrapNullableObject(localBuilder2, localBuilder3, num);
				localBuilder2 = localBuilder3;
			}
			LocalBuilder pairValue = ReadValue(type2, string.Empty);
			StoreKeyValuePair(_objectLocal, collectionContract, localBuilder2, pairValue);
			_ilg.EndWhile();
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private void ReadGetOnlyCollection(CollectionDataContract collectionContract)
		{
			Type underlyingType = collectionContract.UnderlyingType;
			Type itemType = collectionContract.ItemType;
			bool flag = collectionContract.Kind == CollectionKind.Array;
			LocalBuilder localBuilder = _ilg.DeclareLocal(Globals.TypeOfInt, "arraySize");
			_objectLocal = _ilg.DeclareLocal(underlyingType, "objectDeserialized");
			_ilg.Load(_contextArg);
			_ilg.LoadMember(XmlFormatGeneratorStatics.GetCollectionMemberMethod);
			_ilg.ConvertValue(Globals.TypeOfObject, underlyingType);
			_ilg.Stloc(_objectLocal);
			bool flag2 = collectionContract.Kind == CollectionKind.Dictionary || collectionContract.Kind == CollectionKind.GenericDictionary;
			if (flag2)
			{
				_ilg.Load(_contextArg);
				_ilg.LoadMember(JsonFormatGeneratorStatics.UseSimpleDictionaryFormatReadProperty);
				_ilg.If();
				if (!underlyingType.IsValueType)
				{
					_ilg.If(_objectLocal, Cmp.EqualTo, null);
					_ilg.Call(null, XmlFormatGeneratorStatics.ThrowNullValueReturnedForGetOnlyCollectionExceptionMethod, underlyingType);
					_ilg.EndIf();
				}
				ReadSimpleDictionary(collectionContract, itemType);
				_ilg.Call(_contextArg, XmlFormatGeneratorStatics.CheckEndOfArrayMethod, _xmlReaderArg, localBuilder, _memberNamesArg, _emptyDictionaryStringArg);
				_ilg.Else();
			}
			IsStartElement(_memberNamesArg, _emptyDictionaryStringArg);
			_ilg.If();
			if (!underlyingType.IsValueType)
			{
				_ilg.If(_objectLocal, Cmp.EqualTo, null);
				_ilg.Call(null, XmlFormatGeneratorStatics.ThrowNullValueReturnedForGetOnlyCollectionExceptionMethod, underlyingType);
				_ilg.EndIf();
			}
			if (flag)
			{
				_ilg.Load(_objectLocal);
				_ilg.Call(XmlFormatGeneratorStatics.GetArrayLengthMethod);
				_ilg.Stloc(localBuilder);
			}
			LocalBuilder localBuilder2 = _ilg.DeclareLocal(Globals.TypeOfInt, "i");
			object forState = _ilg.For(localBuilder2, 0, int.MaxValue);
			IsStartElement(_memberNamesArg, _emptyDictionaryStringArg);
			_ilg.If();
			_ilg.Call(_contextArg, XmlFormatGeneratorStatics.IncrementItemCountMethod, 1);
			LocalBuilder value = ReadCollectionItem(collectionContract, itemType);
			if (flag)
			{
				_ilg.If(localBuilder, Cmp.EqualTo, localBuilder2);
				_ilg.Call(null, XmlFormatGeneratorStatics.ThrowArrayExceededSizeExceptionMethod, localBuilder, underlyingType);
				_ilg.Else();
				_ilg.StoreArrayElement(_objectLocal, localBuilder2, value);
				_ilg.EndIf();
			}
			else
			{
				StoreCollectionValue(_objectLocal, value, collectionContract);
			}
			_ilg.Else();
			IsEndElement();
			_ilg.If();
			_ilg.Break(forState);
			_ilg.Else();
			HandleUnexpectedItemInCollection(localBuilder2);
			_ilg.EndIf();
			_ilg.EndIf();
			_ilg.EndFor();
			_ilg.Call(_contextArg, XmlFormatGeneratorStatics.CheckEndOfArrayMethod, _xmlReaderArg, localBuilder, _memberNamesArg, _emptyDictionaryStringArg);
			_ilg.EndIf();
			if (flag2)
			{
				_ilg.EndIf();
			}
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private bool TryReadPrimitiveArray(Type itemType)
		{
			PrimitiveDataContract primitiveDataContract = PrimitiveDataContract.GetPrimitiveDataContract(itemType);
			if (primitiveDataContract == null)
			{
				return false;
			}
			string text = null;
			switch (itemType.GetTypeCode())
			{
			case TypeCode.Boolean:
				text = "TryReadBooleanArray";
				break;
			case TypeCode.Decimal:
				text = "TryReadDecimalArray";
				break;
			case TypeCode.Int32:
				text = "TryReadInt32Array";
				break;
			case TypeCode.Int64:
				text = "TryReadInt64Array";
				break;
			case TypeCode.Single:
				text = "TryReadSingleArray";
				break;
			case TypeCode.Double:
				text = "TryReadDoubleArray";
				break;
			case TypeCode.DateTime:
				text = "TryReadJsonDateTimeArray";
				break;
			}
			if (text != null)
			{
				_ilg.Load(_xmlReaderArg);
				_ilg.ConvertValue(typeof(XmlReaderDelegator), typeof(JsonReaderDelegator));
				_ilg.Load(_contextArg);
				_ilg.Load(_memberNamesArg);
				_ilg.Load(_emptyDictionaryStringArg);
				_ilg.Load(-1);
				_ilg.Ldloca(_objectLocal);
				_ilg.Call(typeof(JsonReaderDelegator).GetMethod(text, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));
				return true;
			}
			return false;
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private LocalBuilder ReadCollectionItem(CollectionDataContract collectionContract, Type itemType)
		{
			if (collectionContract.Kind == CollectionKind.Dictionary || collectionContract.Kind == CollectionKind.GenericDictionary)
			{
				_ilg.Call(_contextArg, XmlFormatGeneratorStatics.ResetAttributesMethod);
				LocalBuilder localBuilder = _ilg.DeclareLocal(itemType, "valueRead");
				_ilg.Load(_collectionContractArg);
				_ilg.Call(JsonFormatGeneratorStatics.GetItemContractMethod);
				_ilg.Call(JsonFormatGeneratorStatics.GetRevisedItemContractMethod);
				_ilg.Load(_xmlReaderArg);
				_ilg.Load(_contextArg);
				_ilg.Call(JsonFormatGeneratorStatics.ReadJsonValueMethod);
				_ilg.ConvertValue(Globals.TypeOfObject, itemType);
				_ilg.Stloc(localBuilder);
				return localBuilder;
			}
			return ReadValue(itemType, "item");
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private void StoreCollectionValue(LocalBuilder collection, LocalBuilder value, CollectionDataContract collectionContract)
		{
			if (collectionContract.Kind == CollectionKind.GenericDictionary || collectionContract.Kind == CollectionKind.Dictionary)
			{
				ClassDataContract classDataContract = DataContract.GetDataContract(value.LocalType) as ClassDataContract;
				DataMember dataMember = classDataContract.Members[0];
				DataMember dataMember2 = classDataContract.Members[1];
				LocalBuilder localBuilder = _ilg.DeclareLocal(dataMember.MemberType, dataMember.Name);
				LocalBuilder localBuilder2 = _ilg.DeclareLocal(dataMember2.MemberType, dataMember2.Name);
				_ilg.LoadAddress(value);
				_ilg.LoadMember(dataMember.MemberInfo);
				_ilg.Stloc(localBuilder);
				_ilg.LoadAddress(value);
				_ilg.LoadMember(dataMember2.MemberInfo);
				_ilg.Stloc(localBuilder2);
				StoreKeyValuePair(collection, collectionContract, localBuilder, localBuilder2);
			}
			else
			{
				_ilg.Call(collection, collectionContract.AddMethod, value);
				if (collectionContract.AddMethod.ReturnType != Globals.TypeOfVoid)
				{
					_ilg.Pop();
				}
			}
		}

		private void StoreKeyValuePair(LocalBuilder collection, CollectionDataContract collectionContract, LocalBuilder pairKey, LocalBuilder pairValue)
		{
			_ilg.Call(collection, collectionContract.AddMethod, pairKey, pairValue);
			if (collectionContract.AddMethod.ReturnType != Globals.TypeOfVoid)
			{
				_ilg.Pop();
			}
		}

		private void HandleUnexpectedItemInCollection(LocalBuilder iterator)
		{
			IsStartElement();
			_ilg.If();
			_ilg.Call(_contextArg, XmlFormatGeneratorStatics.SkipUnknownElementMethod, _xmlReaderArg);
			_ilg.Dec(iterator);
			_ilg.Else();
			ThrowUnexpectedStateException(XmlNodeType.Element);
			_ilg.EndIf();
		}

		private void IsStartElement(ArgBuilder nameArg, ArgBuilder nsArg)
		{
			_ilg.Call(_xmlReaderArg, JsonFormatGeneratorStatics.IsStartElementMethod2, nameArg, nsArg);
		}

		private void IsStartElement()
		{
			_ilg.Call(_xmlReaderArg, JsonFormatGeneratorStatics.IsStartElementMethod0);
		}

		private void IsEndElement()
		{
			_ilg.Load(_xmlReaderArg);
			_ilg.LoadMember(JsonFormatGeneratorStatics.NodeTypeProperty);
			_ilg.Load(XmlNodeType.EndElement);
			_ilg.Ceq();
		}

		private void ThrowUnexpectedStateException(XmlNodeType expectedState)
		{
			_ilg.Call(null, XmlFormatGeneratorStatics.CreateUnexpectedStateExceptionMethod, expectedState, _xmlReaderArg);
			_ilg.Throw();
		}

		private void ThrowSerializationException(string msg, params object[] values)
		{
			if (values != null && values.Length != 0)
			{
				_ilg.CallStringFormat(msg, values);
			}
			else
			{
				_ilg.Load(msg);
			}
			ThrowSerializationException();
		}

		private void ThrowSerializationException()
		{
			_ilg.New(JsonFormatGeneratorStatics.SerializationExceptionCtor);
			_ilg.Throw();
		}
	}

	private readonly CriticalHelper _helper;

	public JsonFormatReaderGenerator()
	{
		_helper = new CriticalHelper();
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public JsonFormatClassReaderDelegate GenerateClassReader(ClassDataContract classContract)
	{
		return _helper.GenerateClassReader(classContract);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public JsonFormatCollectionReaderDelegate GenerateCollectionReader(CollectionDataContract collectionContract)
	{
		return _helper.GenerateCollectionReader(collectionContract);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public JsonFormatGetOnlyCollectionReaderDelegate GenerateGetOnlyCollectionReader(CollectionDataContract collectionContract)
	{
		return _helper.GenerateGetOnlyCollectionReader(collectionContract);
	}
}

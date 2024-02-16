using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Security;
using System.Xml;

namespace System.Runtime.Serialization;

internal sealed class XmlFormatReaderGenerator
{
	private sealed class CriticalHelper
	{
		private CodeGenerator _ilg;

		private LocalBuilder _objectLocal;

		private Type _objectType;

		private ArgBuilder _xmlReaderArg;

		private ArgBuilder _contextArg;

		private ArgBuilder _memberNamesArg;

		private ArgBuilder _memberNamespacesArg;

		private ArgBuilder _collectionContractArg;

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private XmlFormatClassReaderDelegate CreateReflectionXmlClassReader(ClassDataContract classContract)
		{
			return new ReflectionXmlClassReader(classContract).ReflectionReadClass;
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		public XmlFormatClassReaderDelegate GenerateClassReader(ClassDataContract classContract)
		{
			if (DataContractSerializer.Option == SerializationOption.ReflectionOnly)
			{
				return CreateReflectionXmlClassReader(classContract);
			}
			_ilg = new CodeGenerator();
			bool flag = classContract.RequiresMemberAccessForRead(null);
			try
			{
				_ilg.BeginMethod("Read" + classContract.StableName.Name + "FromXml", Globals.TypeOfXmlFormatClassReaderDelegate, flag);
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
			LocalBuilder localBuilder = null;
			if (HasFactoryMethod(classContract))
			{
				localBuilder = _ilg.DeclareLocal(Globals.TypeOfString, "objectIdRead");
				_ilg.Call(_contextArg, XmlFormatGeneratorStatics.GetObjectIdMethod);
				_ilg.Stloc(localBuilder);
			}
			if (classContract.IsISerializable)
			{
				ReadISerializable(classContract);
			}
			else
			{
				ReadClass(classContract);
			}
			InvokeFactoryMethod(classContract, localBuilder);
			if (Globals.TypeOfIDeserializationCallback.IsAssignableFrom(classContract.UnderlyingType))
			{
				_ilg.Call(_objectLocal, XmlFormatGeneratorStatics.OnDeserializationMethod, null);
			}
			InvokeOnDeserialized(classContract);
			if (localBuilder == null)
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
			return (XmlFormatClassReaderDelegate)_ilg.EndMethod();
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private XmlFormatCollectionReaderDelegate CreateReflectionXmlCollectionReader()
		{
			return new ReflectionXmlCollectionReader().ReflectionReadCollection;
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		public XmlFormatCollectionReaderDelegate GenerateCollectionReader(CollectionDataContract collectionContract)
		{
			if (DataContractSerializer.Option == SerializationOption.ReflectionOnly)
			{
				return CreateReflectionXmlCollectionReader();
			}
			_ilg = GenerateCollectionReaderHelper(collectionContract, isGetOnlyCollection: false);
			ReadCollection(collectionContract);
			_ilg.Load(_objectLocal);
			_ilg.ConvertValue(_objectLocal.LocalType, _ilg.CurrentMethod.ReturnType);
			return (XmlFormatCollectionReaderDelegate)_ilg.EndMethod();
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private XmlFormatGetOnlyCollectionReaderDelegate CreateReflectionReadGetOnlyCollectionReader()
		{
			return new ReflectionXmlCollectionReader().ReflectionReadGetOnlyCollection;
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		public XmlFormatGetOnlyCollectionReaderDelegate GenerateGetOnlyCollectionReader(CollectionDataContract collectionContract)
		{
			if (DataContractSerializer.Option == SerializationOption.ReflectionOnly)
			{
				return CreateReflectionReadGetOnlyCollectionReader();
			}
			_ilg = GenerateCollectionReaderHelper(collectionContract, isGetOnlyCollection: true);
			ReadGetOnlyCollection(collectionContract);
			return (XmlFormatGetOnlyCollectionReaderDelegate)_ilg.EndMethod();
		}

		private CodeGenerator GenerateCollectionReaderHelper(CollectionDataContract collectionContract, bool isGetOnlyCollection)
		{
			_ilg = new CodeGenerator();
			bool flag = collectionContract.RequiresMemberAccessForRead(null);
			try
			{
				if (isGetOnlyCollection)
				{
					_ilg.BeginMethod("Read" + collectionContract.StableName.Name + "FromXmlIsGetOnly", Globals.TypeOfXmlFormatGetOnlyCollectionReaderDelegate, flag);
				}
				else
				{
					_ilg.BeginMethod("Read" + collectionContract.StableName.Name + "FromXml" + string.Empty, Globals.TypeOfXmlFormatCollectionReaderDelegate, flag);
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

		private void InitArgs()
		{
			_xmlReaderArg = _ilg.GetArg(0);
			_contextArg = _ilg.GetArg(1);
			_memberNamesArg = _ilg.GetArg(2);
			_memberNamespacesArg = _ilg.GetArg(3);
		}

		[MemberNotNull("_objectType")]
		[MemberNotNull("_objectLocal")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private void CreateObject(ClassDataContract classContract)
		{
			Type type = (_objectType = classContract.UnderlyingType);
			if (type.IsValueType && !classContract.IsNonAttributedType)
			{
				type = Globals.TypeOfValueType;
			}
			_objectLocal = _ilg.DeclareLocal(type, "objectDeserialized");
			if (classContract.UnderlyingType == Globals.TypeOfDBNull)
			{
				_ilg.LoadMember(GetDBNullValueField());
				_ilg.Stloc(_objectLocal);
			}
			else if (classContract.IsNonAttributedType)
			{
				if (type.IsValueType)
				{
					_ilg.Ldloca(_objectLocal);
					_ilg.InitObj(type);
				}
				else
				{
					_ilg.New(classContract.GetNonAttributedTypeConstructor());
					_ilg.Stloc(_objectLocal);
				}
			}
			else
			{
				_ilg.Call(null, XmlFormatGeneratorStatics.GetUninitializedObjectMethod, DataContract.GetIdForInitialization(classContract));
				_ilg.ConvertValue(Globals.TypeOfObject, type);
				_ilg.Stloc(_objectLocal);
			}
		}

		private static FieldInfo GetDBNullValueField()
		{
			return typeof(DBNull).GetField("Value");
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

		private bool InvokeFactoryMethod(ClassDataContract classContract, LocalBuilder objectId)
		{
			if (HasFactoryMethod(classContract))
			{
				_ilg.Load(_contextArg);
				_ilg.LoadAddress(_objectLocal);
				_ilg.ConvertAddress(_objectLocal.LocalType, Globals.TypeOfIObjectReference);
				_ilg.Load(objectId);
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
				_ilg.New(XmlFormatGeneratorStatics.ExtensionDataObjectCtor);
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
			LocalBuilder localBuilder = _ilg.DeclareLocal(Globals.TypeOfInt, "memberIndex", -1);
			int firstRequiredMember;
			bool[] requiredMembers = GetRequiredMembers(classContract, out firstRequiredMember);
			bool flag = firstRequiredMember < num;
			LocalBuilder localBuilder2 = (flag ? _ilg.DeclareLocal(Globals.TypeOfInt, "requiredIndex", firstRequiredMember) : null);
			object forState = _ilg.For(null, null, null);
			_ilg.Call(null, XmlFormatGeneratorStatics.MoveToNextElementMethod, _xmlReaderArg);
			_ilg.IfFalseBreak(forState);
			if (flag)
			{
				_ilg.Call(_contextArg, XmlFormatGeneratorStatics.GetMemberIndexWithRequiredMembersMethod, _xmlReaderArg, _memberNamesArg, _memberNamespacesArg, localBuilder, localBuilder2, extensionDataLocal);
			}
			else
			{
				_ilg.Call(_contextArg, XmlFormatGeneratorStatics.GetMemberIndexMethod, _xmlReaderArg, _memberNamesArg, _memberNamespacesArg, localBuilder, extensionDataLocal);
			}
			Label[] memberLabels = _ilg.Switch(num);
			ReadMembers(classContract, requiredMembers, memberLabels, localBuilder, localBuilder2);
			_ilg.EndSwitch();
			_ilg.EndFor();
			if (flag)
			{
				_ilg.If(localBuilder2, Cmp.LessThan, num);
				_ilg.Call(null, XmlFormatGeneratorStatics.ThrowRequiredMemberMissingExceptionMethod, _xmlReaderArg, localBuilder, localBuilder2, _memberNamesArg);
				_ilg.EndIf();
			}
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private int ReadMembers(ClassDataContract classContract, bool[] requiredMembers, Label[] memberLabels, LocalBuilder memberIndexLocal, LocalBuilder requiredIndexLocal)
		{
			int num = ((classContract.BaseContract != null) ? ReadMembers(classContract.BaseContract, requiredMembers, memberLabels, memberIndexLocal, requiredIndexLocal) : 0);
			int num2 = 0;
			while (num2 < classContract.Members.Count)
			{
				DataMember dataMember = classContract.Members[num2];
				Type memberType = dataMember.MemberType;
				_ilg.Case(memberLabels[num], dataMember.Name);
				if (dataMember.IsRequired)
				{
					int i;
					for (i = num + 1; i < requiredMembers.Length && !requiredMembers[i]; i++)
					{
					}
					_ilg.Set(requiredIndexLocal, i);
				}
				LocalBuilder localBuilder = null;
				if (dataMember.IsGetOnlyCollection)
				{
					_ilg.LoadAddress(_objectLocal);
					_ilg.LoadMember(dataMember.MemberInfo);
					localBuilder = _ilg.DeclareLocal(memberType, dataMember.Name + "Value");
					_ilg.Stloc(localBuilder);
					_ilg.Call(_contextArg, XmlFormatGeneratorStatics.StoreCollectionMemberInfoMethod, localBuilder);
					ReadValue(memberType, dataMember.Name, classContract.StableName.Namespace);
				}
				else
				{
					_ilg.Call(_contextArg, XmlFormatGeneratorStatics.ResetCollectionMemberInfoMethod);
					localBuilder = ReadValue(memberType, dataMember.Name, classContract.StableName.Namespace);
					_ilg.LoadAddress(_objectLocal);
					_ilg.ConvertAddress(_objectLocal.LocalType, _objectType);
					_ilg.Ldloc(localBuilder);
					_ilg.StoreMember(dataMember.MemberInfo);
				}
				_ilg.Set(memberIndexLocal, num);
				_ilg.EndCase();
				num2++;
				num++;
			}
			return num;
		}

		private bool[] GetRequiredMembers(ClassDataContract contract, out int firstRequiredMember)
		{
			int num = contract.MemberNames.Length;
			bool[] array = new bool[num];
			GetRequiredMembers(contract, array);
			firstRequiredMember = 0;
			while (firstRequiredMember < num && !array[firstRequiredMember])
			{
				firstRequiredMember++;
			}
			return array;
		}

		private int GetRequiredMembers(ClassDataContract contract, bool[] requiredMembers)
		{
			int num = ((contract.BaseContract != null) ? GetRequiredMembers(contract.BaseContract, requiredMembers) : 0);
			List<DataMember> members = contract.Members;
			int num2 = 0;
			while (num2 < members.Count)
			{
				requiredMembers[num] = members[num2].IsRequired;
				num2++;
				num++;
			}
			return num;
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private void ReadISerializable(ClassDataContract classContract)
		{
			ConstructorInfo iSerializableConstructor = classContract.GetISerializableConstructor();
			_ilg.LoadAddress(_objectLocal);
			_ilg.ConvertAddress(_objectLocal.LocalType, _objectType);
			_ilg.Call(_contextArg, XmlFormatGeneratorStatics.ReadSerializationInfoMethod, _xmlReaderArg, classContract.UnderlyingType);
			_ilg.Load(_contextArg);
			_ilg.LoadMember(XmlFormatGeneratorStatics.GetStreamingContextMethod);
			_ilg.Call(iSerializableConstructor);
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private LocalBuilder ReadValue(Type type, string name, string ns)
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
					ThrowValidationException(System.SR.Format(System.SR.ValueTypeCannotBeNull, DataContract.GetClrTypeFullName(type)));
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
					ThrowValidationException(System.SR.Format(System.SR.ValueTypeCannotHaveId, DataContract.GetClrTypeFullName(type)));
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
					InternalDeserialize(localBuilder, type, name, ns);
				}
				_ilg.Else();
				if (type.IsValueType)
				{
					ThrowValidationException(System.SR.Format(System.SR.ValueTypeCannotHaveRef, DataContract.GetClrTypeFullName(type)));
				}
				else
				{
					_ilg.Call(_contextArg, XmlFormatGeneratorStatics.GetExistingObjectMethod, localBuilder3, type, name, ns);
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
				InternalDeserialize(localBuilder, type, name, ns);
			}
			return localBuilder;
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private void InternalDeserialize(LocalBuilder value, Type type, string name, string ns)
		{
			_ilg.Load(_contextArg);
			_ilg.Load(_xmlReaderArg);
			_ilg.Load(DataContract.GetId(type.TypeHandle));
			_ilg.Ldtoken(type);
			_ilg.Load(name);
			_ilg.Load(ns);
			_ilg.Call(XmlFormatGeneratorStatics.InternalDeserializeMethod);
			_ilg.ConvertValue(Globals.TypeOfObject, type);
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

		[MemberNotNull("_objectLocal")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private void ReadCollection(CollectionDataContract collectionContract)
		{
			Type type = collectionContract.UnderlyingType;
			Type itemType = collectionContract.ItemType;
			bool flag = collectionContract.Kind == CollectionKind.Array;
			ConstructorInfo constructorInfo = collectionContract.Constructor;
			if (type.IsInterface)
			{
				switch (collectionContract.Kind)
				{
				case CollectionKind.GenericDictionary:
					type = Globals.TypeOfDictionaryGeneric.MakeGenericType(itemType.GetGenericArguments());
					constructorInfo = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public, Type.EmptyTypes);
					break;
				case CollectionKind.Dictionary:
					type = Globals.TypeOfHashtable;
					constructorInfo = XmlFormatGeneratorStatics.HashtableCtor;
					break;
				case CollectionKind.GenericList:
				case CollectionKind.GenericCollection:
				case CollectionKind.List:
				case CollectionKind.GenericEnumerable:
				case CollectionKind.Collection:
				case CollectionKind.Enumerable:
					type = itemType.MakeArrayType();
					flag = true;
					break;
				}
			}
			string itemName = collectionContract.ItemName;
			string @namespace = collectionContract.StableName.Namespace;
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
					_ilg.New(constructorInfo);
					_ilg.Stloc(_objectLocal);
					_ilg.Call(_contextArg, XmlFormatGeneratorStatics.AddNewObjectMethod, _objectLocal);
				}
			}
			LocalBuilder localBuilder = _ilg.DeclareLocal(Globals.TypeOfInt, "arraySize");
			_ilg.Call(_contextArg, XmlFormatGeneratorStatics.GetArraySizeMethod);
			_ilg.Stloc(localBuilder);
			LocalBuilder localBuilder2 = _ilg.DeclareLocal(Globals.TypeOfString, "objectIdRead");
			_ilg.Call(_contextArg, XmlFormatGeneratorStatics.GetObjectIdMethod);
			_ilg.Stloc(localBuilder2);
			bool flag2 = false;
			if (flag && TryReadPrimitiveArray(type, itemType, localBuilder))
			{
				flag2 = true;
				_ilg.IfNot();
			}
			_ilg.If(localBuilder, Cmp.EqualTo, -1);
			LocalBuilder localBuilder3 = null;
			if (flag)
			{
				localBuilder3 = _ilg.DeclareLocal(type, "growingCollection");
				_ilg.NewArray(itemType, 32);
				_ilg.Stloc(localBuilder3);
			}
			LocalBuilder localBuilder4 = _ilg.DeclareLocal(Globals.TypeOfInt, "i");
			object forState = _ilg.For(localBuilder4, 0, int.MaxValue);
			IsStartElement(_memberNamesArg, _memberNamespacesArg);
			_ilg.If();
			_ilg.Call(_contextArg, XmlFormatGeneratorStatics.IncrementItemCountMethod, 1);
			LocalBuilder value = ReadCollectionItem(collectionContract, itemType, itemName, @namespace);
			if (flag)
			{
				MethodInfo methodInfo = XmlFormatGeneratorStatics.EnsureArraySizeMethod.MakeGenericMethod(itemType);
				_ilg.Call(null, methodInfo, localBuilder3, localBuilder4);
				_ilg.Stloc(localBuilder3);
				_ilg.StoreArrayElement(localBuilder3, localBuilder4, value);
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
			HandleUnexpectedItemInCollection(localBuilder4);
			_ilg.EndIf();
			_ilg.EndIf();
			_ilg.EndFor();
			if (flag)
			{
				MethodInfo methodInfo2 = XmlFormatGeneratorStatics.TrimArraySizeMethod.MakeGenericMethod(itemType);
				_ilg.Call(null, methodInfo2, localBuilder3, localBuilder4);
				_ilg.Stloc(_objectLocal);
				_ilg.Call(_contextArg, XmlFormatGeneratorStatics.AddNewObjectWithIdMethod, localBuilder2, _objectLocal);
			}
			_ilg.Else();
			_ilg.Call(_contextArg, XmlFormatGeneratorStatics.IncrementItemCountMethod, localBuilder);
			if (flag)
			{
				_ilg.NewArray(itemType, localBuilder);
				_ilg.Stloc(_objectLocal);
				_ilg.Call(_contextArg, XmlFormatGeneratorStatics.AddNewObjectMethod, _objectLocal);
			}
			LocalBuilder localBuilder5 = _ilg.DeclareLocal(Globals.TypeOfInt, "j");
			_ilg.For(localBuilder5, 0, localBuilder);
			IsStartElement(_memberNamesArg, _memberNamespacesArg);
			_ilg.If();
			LocalBuilder value2 = ReadCollectionItem(collectionContract, itemType, itemName, @namespace);
			if (flag)
			{
				_ilg.StoreArrayElement(_objectLocal, localBuilder5, value2);
			}
			else
			{
				StoreCollectionValue(_objectLocal, value2, collectionContract);
			}
			_ilg.Else();
			HandleUnexpectedItemInCollection(localBuilder5);
			_ilg.EndIf();
			_ilg.EndFor();
			_ilg.Call(_contextArg, XmlFormatGeneratorStatics.CheckEndOfArrayMethod, _xmlReaderArg, localBuilder, _memberNamesArg, _memberNamespacesArg);
			_ilg.EndIf();
			if (flag2)
			{
				_ilg.Else();
				_ilg.Call(_contextArg, XmlFormatGeneratorStatics.AddNewObjectWithIdMethod, localBuilder2, _objectLocal);
				_ilg.EndIf();
			}
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private void ReadGetOnlyCollection(CollectionDataContract collectionContract)
		{
			Type underlyingType = collectionContract.UnderlyingType;
			Type itemType = collectionContract.ItemType;
			bool flag = collectionContract.Kind == CollectionKind.Array;
			string itemName = collectionContract.ItemName;
			string @namespace = collectionContract.StableName.Namespace;
			_objectLocal = _ilg.DeclareLocal(underlyingType, "objectDeserialized");
			_ilg.Load(_contextArg);
			_ilg.LoadMember(XmlFormatGeneratorStatics.GetCollectionMemberMethod);
			_ilg.ConvertValue(Globals.TypeOfObject, underlyingType);
			_ilg.Stloc(_objectLocal);
			IsStartElement(_memberNamesArg, _memberNamespacesArg);
			_ilg.If();
			_ilg.If(_objectLocal, Cmp.EqualTo, null);
			_ilg.Call(null, XmlFormatGeneratorStatics.ThrowNullValueReturnedForGetOnlyCollectionExceptionMethod, underlyingType);
			_ilg.Else();
			LocalBuilder localBuilder = _ilg.DeclareLocal(Globals.TypeOfInt, "arraySize");
			if (flag)
			{
				_ilg.Load(_objectLocal);
				_ilg.Call(XmlFormatGeneratorStatics.GetArrayLengthMethod);
				_ilg.Stloc(localBuilder);
			}
			_ilg.Call(_contextArg, XmlFormatGeneratorStatics.AddNewObjectMethod, _objectLocal);
			LocalBuilder localBuilder2 = _ilg.DeclareLocal(Globals.TypeOfInt, "i");
			object forState = _ilg.For(localBuilder2, 0, int.MaxValue);
			IsStartElement(_memberNamesArg, _memberNamespacesArg);
			_ilg.If();
			_ilg.Call(_contextArg, XmlFormatGeneratorStatics.IncrementItemCountMethod, 1);
			LocalBuilder value = ReadCollectionItem(collectionContract, itemType, itemName, @namespace);
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
			_ilg.Call(_contextArg, XmlFormatGeneratorStatics.CheckEndOfArrayMethod, _xmlReaderArg, localBuilder, _memberNamesArg, _memberNamespacesArg);
			_ilg.EndIf();
			_ilg.EndIf();
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private bool TryReadPrimitiveArray(Type type, Type itemType, LocalBuilder size)
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
			case TypeCode.DateTime:
				text = "TryReadDateTimeArray";
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
			}
			if (text != null)
			{
				_ilg.Load(_xmlReaderArg);
				_ilg.Load(_contextArg);
				_ilg.Load(_memberNamesArg);
				_ilg.Load(_memberNamespacesArg);
				_ilg.Load(size);
				_ilg.Ldloca(_objectLocal);
				_ilg.Call(typeof(XmlReaderDelegator).GetMethod(text, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));
				return true;
			}
			return false;
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private LocalBuilder ReadCollectionItem(CollectionDataContract collectionContract, Type itemType, string itemName, string itemNs)
		{
			if (collectionContract.Kind == CollectionKind.Dictionary || collectionContract.Kind == CollectionKind.GenericDictionary)
			{
				_ilg.Call(_contextArg, XmlFormatGeneratorStatics.ResetAttributesMethod);
				LocalBuilder localBuilder = _ilg.DeclareLocal(itemType, "valueRead");
				_ilg.Load(_collectionContractArg);
				_ilg.Call(XmlFormatGeneratorStatics.GetItemContractMethod);
				_ilg.Load(_xmlReaderArg);
				_ilg.Load(_contextArg);
				_ilg.Call(XmlFormatGeneratorStatics.ReadXmlValueMethod);
				_ilg.ConvertValue(Globals.TypeOfObject, itemType);
				_ilg.Stloc(localBuilder);
				return localBuilder;
			}
			return ReadValue(itemType, itemName, itemNs);
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
				_ilg.Call(collection, collectionContract.AddMethod, localBuilder, localBuilder2);
				if (collectionContract.AddMethod.ReturnType != Globals.TypeOfVoid)
				{
					_ilg.Pop();
				}
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
			_ilg.Call(_xmlReaderArg, XmlFormatGeneratorStatics.IsStartElementMethod2, nameArg, nsArg);
		}

		private void IsStartElement()
		{
			_ilg.Call(_xmlReaderArg, XmlFormatGeneratorStatics.IsStartElementMethod0);
		}

		private void IsEndElement()
		{
			_ilg.Load(_xmlReaderArg);
			_ilg.LoadMember(XmlFormatGeneratorStatics.NodeTypeProperty);
			_ilg.Load(XmlNodeType.EndElement);
			_ilg.Ceq();
		}

		private void ThrowUnexpectedStateException(XmlNodeType expectedState)
		{
			_ilg.Call(null, XmlFormatGeneratorStatics.CreateUnexpectedStateExceptionMethod, expectedState, _xmlReaderArg);
			_ilg.Throw();
		}

		private void ThrowValidationException(string msg, params object[] values)
		{
			_ilg.Load(msg);
			ThrowValidationException();
		}

		private void ThrowValidationException()
		{
			_ilg.Call(XmlFormatGeneratorStatics.CreateSerializationExceptionMethod);
			_ilg.Throw();
		}
	}

	private readonly CriticalHelper _helper;

	public XmlFormatReaderGenerator()
	{
		_helper = new CriticalHelper();
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public XmlFormatClassReaderDelegate GenerateClassReader(ClassDataContract classContract)
	{
		return _helper.GenerateClassReader(classContract);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public XmlFormatCollectionReaderDelegate GenerateCollectionReader(CollectionDataContract collectionContract)
	{
		return _helper.GenerateCollectionReader(collectionContract);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public XmlFormatGetOnlyCollectionReaderDelegate GenerateGetOnlyCollectionReader(CollectionDataContract collectionContract)
	{
		return _helper.GenerateGetOnlyCollectionReader(collectionContract);
	}

	internal static object UnsafeGetUninitializedObject([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type type)
	{
		return RuntimeHelpers.GetUninitializedObject(type);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static object UnsafeGetUninitializedObject(int id)
	{
		Type typeForInitialization = DataContract.GetDataContractForInitialization(id).TypeForInitialization;
		return UnsafeGetUninitializedObject(typeForInitialization);
	}
}

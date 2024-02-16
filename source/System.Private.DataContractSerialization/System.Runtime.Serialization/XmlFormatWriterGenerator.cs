using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Security;
using System.Xml;

namespace System.Runtime.Serialization;

internal sealed class XmlFormatWriterGenerator
{
	private sealed class CriticalHelper
	{
		private CodeGenerator _ilg;

		private ArgBuilder _xmlWriterArg;

		private ArgBuilder _contextArg;

		private ArgBuilder _dataContractArg;

		private LocalBuilder _objectLocal;

		private LocalBuilder _contractNamespacesLocal;

		private LocalBuilder _memberNamesLocal;

		private LocalBuilder _childElementNamespacesLocal;

		private int _typeIndex = 1;

		private int _childElementIndex;

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private XmlFormatClassWriterDelegate CreateReflectionXmlFormatClassWriterDelegate()
		{
			return new ReflectionXmlFormatWriter().ReflectionWriteClass;
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		internal XmlFormatClassWriterDelegate GenerateClassWriter(ClassDataContract classContract)
		{
			if (DataContractSerializer.Option == SerializationOption.ReflectionOnly)
			{
				return CreateReflectionXmlFormatClassWriterDelegate();
			}
			_ilg = new CodeGenerator();
			bool flag = classContract.RequiresMemberAccessForWrite(null);
			try
			{
				_ilg.BeginMethod("Write" + classContract.StableName.Name + "ToXml", Globals.TypeOfXmlFormatClassWriterDelegate, flag);
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
			WriteClass(classContract);
			return (XmlFormatClassWriterDelegate)_ilg.EndMethod();
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private XmlFormatCollectionWriterDelegate CreateReflectionXmlFormatCollectionWriterDelegate()
		{
			return new ReflectionXmlFormatWriter().ReflectionWriteCollection;
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		internal XmlFormatCollectionWriterDelegate GenerateCollectionWriter(CollectionDataContract collectionContract)
		{
			if (DataContractSerializer.Option == SerializationOption.ReflectionOnly)
			{
				return CreateReflectionXmlFormatCollectionWriterDelegate();
			}
			_ilg = new CodeGenerator();
			bool flag = collectionContract.RequiresMemberAccessForWrite(null);
			try
			{
				_ilg.BeginMethod("Write" + collectionContract.StableName.Name + "ToXml", Globals.TypeOfXmlFormatCollectionWriterDelegate, flag);
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
			WriteCollection(collectionContract);
			return (XmlFormatCollectionWriterDelegate)_ilg.EndMethod();
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
				_ilg.Call(_contextArg, XmlFormatGeneratorStatics.WriteISerializableMethod, _xmlWriterArg, _objectLocal);
			}
			else
			{
				if (classContract.ContractNamespaces.Length > 1)
				{
					_contractNamespacesLocal = _ilg.DeclareLocal(typeof(XmlDictionaryString[]), "contractNamespaces");
					_ilg.Load(_dataContractArg);
					_ilg.LoadMember(XmlFormatGeneratorStatics.ContractNamespacesField);
					_ilg.Store(_contractNamespacesLocal);
				}
				_memberNamesLocal = _ilg.DeclareLocal(typeof(XmlDictionaryString[]), "memberNames");
				_ilg.Load(_dataContractArg);
				_ilg.LoadMember(XmlFormatGeneratorStatics.MemberNamesField);
				_ilg.Store(_memberNamesLocal);
				for (int i = 0; i < classContract.ChildElementNamespaces.Length; i++)
				{
					if (classContract.ChildElementNamespaces[i] != null)
					{
						_childElementNamespacesLocal = _ilg.DeclareLocal(typeof(XmlDictionaryString[]), "childElementNamespaces");
						_ilg.Load(_dataContractArg);
						_ilg.LoadMember(XmlFormatGeneratorStatics.ChildElementNamespacesProperty);
						_ilg.Store(_childElementNamespacesLocal);
					}
				}
				if (classContract.HasExtensionData)
				{
					LocalBuilder localBuilder = _ilg.DeclareLocal(Globals.TypeOfExtensionDataObject, "extensionData");
					_ilg.Load(_objectLocal);
					_ilg.ConvertValue(_objectLocal.LocalType, Globals.TypeOfIExtensibleDataObject);
					_ilg.LoadMember(XmlFormatGeneratorStatics.ExtensionDataProperty);
					_ilg.Store(localBuilder);
					_ilg.Call(_contextArg, XmlFormatGeneratorStatics.WriteExtensionDataMethod, _xmlWriterArg, localBuilder, -1);
					WriteMembers(classContract, localBuilder, classContract);
				}
				else
				{
					WriteMembers(classContract, null, classContract);
				}
			}
			InvokeOnSerialized(classContract);
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private int WriteMembers(ClassDataContract classContract, LocalBuilder extensionDataLocal, ClassDataContract derivedMostClassContract)
		{
			int num = ((classContract.BaseContract != null) ? WriteMembers(classContract.BaseContract, extensionDataLocal, derivedMostClassContract) : 0);
			LocalBuilder localBuilder = _ilg.DeclareLocal(typeof(XmlDictionaryString), "ns");
			if (_contractNamespacesLocal == null)
			{
				_ilg.Load(_dataContractArg);
				_ilg.LoadMember(XmlFormatGeneratorStatics.NamespaceProperty);
			}
			else
			{
				_ilg.LoadArrayElement(_contractNamespacesLocal, _typeIndex - 1);
			}
			_ilg.Store(localBuilder);
			int count = classContract.Members.Count;
			_ilg.Call(_contextArg, XmlFormatGeneratorStatics.IncrementItemCountMethod, count);
			int num2 = 0;
			while (num2 < count)
			{
				DataMember dataMember = classContract.Members[num2];
				Type memberType = dataMember.MemberType;
				LocalBuilder localBuilder2 = null;
				_ilg.Load(_contextArg);
				_ilg.Call(dataMember.IsGetOnlyCollection ? XmlFormatGeneratorStatics.StoreIsGetOnlyCollectionMethod : XmlFormatGeneratorStatics.ResetIsGetOnlyCollectionMethod);
				if (!dataMember.EmitDefaultValue)
				{
					localBuilder2 = LoadMemberValue(dataMember);
					_ilg.IfNotDefaultValue(localBuilder2);
				}
				bool flag = CheckIfMemberHasConflict(dataMember, classContract, derivedMostClassContract);
				if (flag || !TryWritePrimitive(memberType, localBuilder2, dataMember.MemberInfo, null, localBuilder, null, num2 + _childElementIndex))
				{
					WriteStartElement(memberType, classContract.Namespace, localBuilder, null, num2 + _childElementIndex);
					if (classContract.ChildElementNamespaces[num2 + _childElementIndex] != null)
					{
						_ilg.Load(_xmlWriterArg);
						_ilg.LoadArrayElement(_childElementNamespacesLocal, num2 + _childElementIndex);
						_ilg.Call(XmlFormatGeneratorStatics.WriteNamespaceDeclMethod);
					}
					if (localBuilder2 == null)
					{
						localBuilder2 = LoadMemberValue(dataMember);
					}
					WriteValue(localBuilder2, flag);
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
			LocalBuilder localBuilder = _ilg.DeclareLocal(typeof(XmlDictionaryString), "itemNamespace");
			_ilg.Load(_dataContractArg);
			_ilg.LoadMember(XmlFormatGeneratorStatics.NamespaceProperty);
			_ilg.Store(localBuilder);
			LocalBuilder localBuilder2 = _ilg.DeclareLocal(typeof(XmlDictionaryString), "itemName");
			_ilg.Load(_dataContractArg);
			_ilg.LoadMember(XmlFormatGeneratorStatics.CollectionItemNameProperty);
			_ilg.Store(localBuilder2);
			if (collectionContract.ChildElementNamespace != null)
			{
				_ilg.Load(_xmlWriterArg);
				_ilg.Load(_dataContractArg);
				_ilg.LoadMember(XmlFormatGeneratorStatics.ChildElementNamespaceProperty);
				_ilg.Call(XmlFormatGeneratorStatics.WriteNamespaceDeclMethod);
			}
			if (collectionContract.Kind == CollectionKind.Array)
			{
				Type itemType = collectionContract.ItemType;
				LocalBuilder localBuilder3 = _ilg.DeclareLocal(Globals.TypeOfInt, "i");
				_ilg.Call(_contextArg, XmlFormatGeneratorStatics.IncrementArrayCountMethod, _xmlWriterArg, _objectLocal);
				if (!TryWritePrimitiveArray(collectionContract.UnderlyingType, itemType, _objectLocal, localBuilder2, localBuilder))
				{
					_ilg.For(localBuilder3, 0, _objectLocal);
					if (!TryWritePrimitive(itemType, null, null, localBuilder3, localBuilder, localBuilder2, 0))
					{
						WriteStartElement(itemType, collectionContract.Namespace, localBuilder, localBuilder2, 0);
						_ilg.LoadArrayElement(_objectLocal, localBuilder3);
						LocalBuilder localBuilder4 = _ilg.DeclareLocal(itemType, "memberValue");
						_ilg.Stloc(localBuilder4);
						WriteValue(localBuilder4, writeXsiType: false);
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
				methodInfo = XmlFormatGeneratorStatics.IncrementCollectionCountGenericMethod.MakeGenericMethod(collectionContract.ItemType);
				break;
			case CollectionKind.GenericDictionary:
				methodInfo = XmlFormatGeneratorStatics.IncrementCollectionCountGenericMethod.MakeGenericMethod(Globals.TypeOfKeyValuePair.MakeGenericType(collectionContract.ItemType.GetGenericArguments()));
				break;
			}
			if (methodInfo != null)
			{
				_ilg.Call(_contextArg, methodInfo, _xmlWriterArg, _objectLocal);
			}
			bool flag = false;
			bool flag2 = false;
			Type type = null;
			Type[] typeArguments = null;
			if (collectionContract.Kind == CollectionKind.GenericDictionary)
			{
				flag2 = true;
				typeArguments = collectionContract.ItemType.GetGenericArguments();
				type = Globals.TypeOfGenericDictionaryEnumerator.MakeGenericType(typeArguments);
			}
			else if (collectionContract.Kind == CollectionKind.Dictionary)
			{
				flag = true;
				typeArguments = new Type[2]
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
						methodInfo2 = XmlFormatGeneratorStatics.MoveNextMethod;
					}
					if (methodInfo3 == null)
					{
						methodInfo3 = XmlFormatGeneratorStatics.GetCurrentMethod;
					}
				}
				else
				{
					Type interfaceType = Globals.TypeOfIEnumerator;
					CollectionKind kind = collectionContract.Kind;
					if (kind == CollectionKind.GenericDictionary || kind == CollectionKind.GenericCollection || kind == CollectionKind.GenericEnumerable)
					{
						Type[] interfaces = type.GetInterfaces();
						Type[] array = interfaces;
						foreach (Type type2 in array)
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
			LocalBuilder localBuilder5 = _ilg.DeclareLocal(returnType, "currentValue");
			LocalBuilder localBuilder6 = _ilg.DeclareLocal(type, "enumerator");
			_ilg.Call(_objectLocal, collectionContract.GetEnumeratorMethod);
			if (flag)
			{
				_ilg.ConvertValue(collectionContract.GetEnumeratorMethod.ReturnType, Globals.TypeOfIDictionaryEnumerator);
				_ilg.New(XmlFormatGeneratorStatics.DictionaryEnumeratorCtor);
			}
			else if (flag2)
			{
				Type type3 = Globals.TypeOfIEnumeratorGeneric.MakeGenericType(Globals.TypeOfKeyValuePair.MakeGenericType(typeArguments));
				ConstructorInfo constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { type3 });
				_ilg.ConvertValue(collectionContract.GetEnumeratorMethod.ReturnType, type3);
				_ilg.New(constructor);
			}
			_ilg.Stloc(localBuilder6);
			_ilg.ForEach(localBuilder5, returnType, type, localBuilder6, methodInfo3);
			if (methodInfo == null)
			{
				_ilg.Call(_contextArg, XmlFormatGeneratorStatics.IncrementItemCountMethod, 1);
			}
			if (!TryWritePrimitive(returnType, localBuilder5, null, null, localBuilder, localBuilder2, 0))
			{
				WriteStartElement(returnType, collectionContract.Namespace, localBuilder, localBuilder2, 0);
				if (flag2 || flag)
				{
					_ilg.Call(_dataContractArg, XmlFormatGeneratorStatics.GetItemContractMethod);
					_ilg.Load(_xmlWriterArg);
					_ilg.Load(localBuilder5);
					_ilg.ConvertValue(localBuilder5.LocalType, Globals.TypeOfObject);
					_ilg.Load(_contextArg);
					_ilg.Call(XmlFormatGeneratorStatics.WriteXmlValueMethod);
				}
				else
				{
					WriteValue(localBuilder5, writeXsiType: false);
				}
				WriteEndElement();
			}
			_ilg.EndForEach(methodInfo2);
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private bool TryWritePrimitive(Type type, LocalBuilder value, MemberInfo memberInfo, LocalBuilder arrayItemIndex, LocalBuilder ns, LocalBuilder name, int nameIndex)
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
				_ilg.LoadArrayElement(_memberNamesLocal, nameIndex);
			}
			_ilg.Load(ns);
			_ilg.Call(primitiveDataContract.XmlFormatWriterMethod);
			return true;
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private bool TryWritePrimitiveArray(Type type, Type itemType, LocalBuilder value, LocalBuilder itemName, LocalBuilder itemNamespace)
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
				text = "WriteBooleanArray";
				break;
			case TypeCode.DateTime:
				text = "WriteDateTimeArray";
				break;
			case TypeCode.Decimal:
				text = "WriteDecimalArray";
				break;
			case TypeCode.Int32:
				text = "WriteInt32Array";
				break;
			case TypeCode.Int64:
				text = "WriteInt64Array";
				break;
			case TypeCode.Single:
				text = "WriteSingleArray";
				break;
			case TypeCode.Double:
				text = "WriteDoubleArray";
				break;
			}
			if (text != null)
			{
				_ilg.Load(_xmlWriterArg);
				_ilg.Load(value);
				_ilg.Load(itemName);
				_ilg.Load(itemNamespace);
				_ilg.Call(typeof(XmlWriterDelegator).GetMethod(text, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new Type[3]
				{
					type,
					typeof(XmlDictionaryString),
					typeof(XmlDictionaryString)
				}));
				return true;
			}
			return false;
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private void WriteValue(LocalBuilder memberValue, bool writeXsiType)
		{
			Type localType = memberValue.LocalType;
			bool flag = localType.IsGenericType && localType.GetGenericTypeDefinition() == Globals.TypeOfNullable;
			if (localType.IsValueType && !flag)
			{
				PrimitiveDataContract primitiveDataContract = PrimitiveDataContract.GetPrimitiveDataContract(localType);
				if (primitiveDataContract != null && !writeXsiType)
				{
					_ilg.Call(_xmlWriterArg, primitiveDataContract.XmlFormatContentWriterMethod, memberValue);
				}
				else
				{
					InternalSerialize(XmlFormatGeneratorStatics.InternalSerializeMethod, memberValue, localType, writeXsiType);
				}
				return;
			}
			if (flag)
			{
				memberValue = UnwrapNullableObject(memberValue);
				localType = memberValue.LocalType;
			}
			else
			{
				_ilg.Load(memberValue);
				_ilg.Load(null);
				_ilg.Ceq();
			}
			_ilg.If();
			_ilg.Call(_contextArg, XmlFormatGeneratorStatics.WriteNullMethod, _xmlWriterArg, localType, DataContract.IsTypeSerializable(localType));
			_ilg.Else();
			PrimitiveDataContract primitiveDataContract2 = PrimitiveDataContract.GetPrimitiveDataContract(localType);
			if (primitiveDataContract2 != null && primitiveDataContract2.UnderlyingType != Globals.TypeOfObject && !writeXsiType)
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
				if (localType == Globals.TypeOfObject || localType == Globals.TypeOfValueType || ((IList)Globals.TypeOfNullable.GetInterfaces()).Contains((object?)localType))
				{
					_ilg.Load(memberValue);
					_ilg.ConvertValue(memberValue.LocalType, Globals.TypeOfObject);
					memberValue = _ilg.DeclareLocal(Globals.TypeOfObject, "unwrappedMemberValue");
					localType = memberValue.LocalType;
					_ilg.Stloc(memberValue);
					_ilg.If(memberValue, Cmp.EqualTo, null);
					_ilg.Call(_contextArg, XmlFormatGeneratorStatics.WriteNullMethod, _xmlWriterArg, localType, DataContract.IsTypeSerializable(localType));
					_ilg.Else();
				}
				InternalSerialize(flag ? XmlFormatGeneratorStatics.InternalSerializeMethod : XmlFormatGeneratorStatics.InternalSerializeReferenceMethod, memberValue, localType, writeXsiType);
				if (localType == Globals.TypeOfObject)
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
			_ilg.Call(null, XmlFormatGeneratorStatics.IsMemberTypeSameAsMemberValue, memberValue, memberType);
			_ilg.Load(writeXsiType);
			_ilg.Load(DataContract.GetId(memberType.TypeHandle));
			_ilg.Ldtoken(memberType);
			_ilg.Call(methodInfo);
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private LocalBuilder UnwrapNullableObject(LocalBuilder memberValue)
		{
			Type type = memberValue.LocalType;
			Label label = _ilg.DefineLabel();
			Label label2 = _ilg.DefineLabel();
			_ilg.Load(memberValue);
			while (type.IsGenericType && type.GetGenericTypeDefinition() == Globals.TypeOfNullable)
			{
				Type type2 = type.GetGenericArguments()[0];
				_ilg.Dup();
				_ilg.Call(XmlFormatGeneratorStatics.GetHasValueMethod.MakeGenericMethod(type2));
				_ilg.Brfalse(label);
				_ilg.Call(XmlFormatGeneratorStatics.GetNullableValueMethod.MakeGenericMethod(type2));
				type = type2;
			}
			memberValue = _ilg.DeclareLocal(type, "nullableUnwrappedMemberValue");
			_ilg.Stloc(memberValue);
			_ilg.Load(false);
			_ilg.Br(label2);
			_ilg.MarkLabel(label);
			_ilg.Pop();
			_ilg.Call(XmlFormatGeneratorStatics.GetDefaultValueMethod.MakeGenericMethod(type));
			_ilg.Stloc(memberValue);
			_ilg.Load(true);
			_ilg.MarkLabel(label2);
			return memberValue;
		}

		private bool NeedsPrefix(Type type, XmlDictionaryString ns)
		{
			if (type == Globals.TypeOfXmlQualifiedName)
			{
				if (ns != null && ns.Value != null)
				{
					return ns.Value.Length > 0;
				}
				return false;
			}
			return false;
		}

		private void WriteStartElement(Type type, XmlDictionaryString ns, LocalBuilder namespaceLocal, LocalBuilder nameLocal, int nameIndex)
		{
			bool flag = NeedsPrefix(type, ns);
			_ilg.Load(_xmlWriterArg);
			if (flag)
			{
				_ilg.Load("q");
			}
			if (nameLocal == null)
			{
				_ilg.LoadArrayElement(_memberNamesLocal, nameIndex);
			}
			else
			{
				_ilg.Load(nameLocal);
			}
			_ilg.Load(namespaceLocal);
			_ilg.Call(flag ? XmlFormatGeneratorStatics.WriteStartElementMethod3 : XmlFormatGeneratorStatics.WriteStartElementMethod2);
		}

		private void WriteEndElement()
		{
			_ilg.Call(_xmlWriterArg, XmlFormatGeneratorStatics.WriteEndElementMethod);
		}

		private bool CheckIfMemberHasConflict(DataMember member, ClassDataContract classContract, ClassDataContract derivedMostClassContract)
		{
			if (CheckIfConflictingMembersHaveDifferentTypes(member))
			{
				return true;
			}
			string name = member.Name;
			string @namespace = classContract.StableName.Namespace;
			ClassDataContract classDataContract = derivedMostClassContract;
			while (classDataContract != null && classDataContract != classContract)
			{
				if (@namespace == classDataContract.StableName.Namespace)
				{
					List<DataMember> members = classDataContract.Members;
					for (int i = 0; i < members.Count; i++)
					{
						if (name == members[i].Name)
						{
							return CheckIfConflictingMembersHaveDifferentTypes(members[i]);
						}
					}
				}
				classDataContract = classDataContract.BaseContract;
			}
			return false;
		}

		private bool CheckIfConflictingMembersHaveDifferentTypes(DataMember member)
		{
			while (member.ConflictingMember != null)
			{
				if (member.MemberType != member.ConflictingMember.MemberType)
				{
					return true;
				}
				member = member.ConflictingMember;
			}
			return false;
		}
	}

	private readonly CriticalHelper _helper;

	public XmlFormatWriterGenerator()
	{
		_helper = new CriticalHelper();
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal XmlFormatClassWriterDelegate GenerateClassWriter(ClassDataContract classContract)
	{
		return _helper.GenerateClassWriter(classContract);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal XmlFormatCollectionWriterDelegate GenerateCollectionWriter(CollectionDataContract collectionContract)
	{
		return _helper.GenerateCollectionWriter(collectionContract);
	}
}

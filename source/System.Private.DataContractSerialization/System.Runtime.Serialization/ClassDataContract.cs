using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Threading;
using System.Xml;

namespace System.Runtime.Serialization;

internal sealed class ClassDataContract : DataContract
{
	private sealed class ClassDataContractCriticalHelper : DataContractCriticalHelper
	{
		internal struct Member
		{
			internal DataMember member;

			internal string ns;

			internal int baseTypeIndex;

			internal Member(DataMember member, string ns, int baseTypeIndex)
			{
				this.member = member;
				this.ns = ns;
				this.baseTypeIndex = baseTypeIndex;
			}
		}

		internal sealed class DataMemberConflictComparer : IComparer<Member>
		{
			internal static DataMemberConflictComparer Singleton = new DataMemberConflictComparer();

			public int Compare(Member x, Member y)
			{
				int num = string.CompareOrdinal(x.ns, y.ns);
				if (num != 0)
				{
					return num;
				}
				int num2 = string.CompareOrdinal(x.member.Name, y.member.Name);
				if (num2 != 0)
				{
					return num2;
				}
				return x.baseTypeIndex - y.baseTypeIndex;
			}
		}

		private static Type[] s_serInfoCtorArgs;

		private static readonly MethodInfo s_getKeyValuePairMethod = typeof(KeyValuePairAdapter<, >).GetMethod("GetKeyValuePair", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

		private static readonly ConstructorInfo s_ctorGenericMethod = typeof(KeyValuePairAdapter<, >).GetConstructor(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { typeof(KeyValuePair<, >).MakeGenericType(typeof(KeyValuePairAdapter<, >).GetGenericArguments()) });

		private ClassDataContract _baseContract;

		private List<DataMember> _members;

		private MethodInfo _onSerializing;

		private MethodInfo _onSerialized;

		private MethodInfo _onDeserializing;

		private MethodInfo _onDeserialized;

		private MethodInfo _extensionDataSetMethod;

		private Dictionary<XmlQualifiedName, DataContract> _knownDataContracts;

		private bool _isISerializable;

		private bool _isKnownTypeAttributeChecked;

		private bool _isMethodChecked;

		private bool _isNonAttributedType;

		private bool _hasDataContract;

		private bool _hasExtensionData;

		private readonly bool _isScriptObject;

		private XmlDictionaryString[] _childElementNamespaces;

		private XmlFormatClassReaderDelegate _xmlFormatReaderDelegate;

		private XmlFormatClassWriterDelegate _xmlFormatWriterDelegate;

		public XmlDictionaryString[] ContractNamespaces;

		public XmlDictionaryString[] MemberNames;

		public XmlDictionaryString[] MemberNamespaces;

		private bool _isKeyValuePairAdapter;

		private Type[] _keyValuePairGenericArguments;

		private ConstructorInfo _keyValuePairCtorInfo;

		private MethodInfo _getKeyValuePairMethodInfo;

		internal ClassDataContract BaseContract
		{
			get
			{
				return _baseContract;
			}
			set
			{
				_baseContract = value;
				if (_baseContract != null && base.IsValueType)
				{
					ThrowInvalidDataContractException(System.SR.Format(System.SR.ValueTypeCannotHaveBaseType, base.StableName.Name, base.StableName.Namespace, _baseContract.StableName.Name, _baseContract.StableName.Namespace));
				}
			}
		}

		internal List<DataMember> Members => _members;

		internal MethodInfo OnSerializing
		{
			get
			{
				EnsureMethodsImported();
				return _onSerializing;
			}
		}

		internal MethodInfo OnSerialized
		{
			get
			{
				EnsureMethodsImported();
				return _onSerialized;
			}
		}

		internal MethodInfo OnDeserializing
		{
			get
			{
				EnsureMethodsImported();
				return _onDeserializing;
			}
		}

		internal MethodInfo OnDeserialized
		{
			get
			{
				EnsureMethodsImported();
				return _onDeserialized;
			}
		}

		internal MethodInfo ExtensionDataSetMethod
		{
			get
			{
				EnsureMethodsImported();
				return _extensionDataSetMethod;
			}
		}

		internal override Dictionary<XmlQualifiedName, DataContract> KnownDataContracts
		{
			[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
			get
			{
				if (_knownDataContracts != null)
				{
					return _knownDataContracts;
				}
				if (!_isKnownTypeAttributeChecked && base.UnderlyingType != null)
				{
					lock (this)
					{
						if (!_isKnownTypeAttributeChecked)
						{
							_knownDataContracts = DataContract.ImportKnownTypeAttributes(base.UnderlyingType);
							Interlocked.MemoryBarrier();
							_isKnownTypeAttributeChecked = true;
						}
					}
				}
				return _knownDataContracts;
			}
			set
			{
				_knownDataContracts = value;
			}
		}

		internal override bool IsISerializable
		{
			get
			{
				return _isISerializable;
			}
			set
			{
				_isISerializable = value;
			}
		}

		internal bool HasDataContract => _hasDataContract;

		internal bool HasExtensionData
		{
			get
			{
				return _hasExtensionData;
			}
			set
			{
				_hasExtensionData = value;
			}
		}

		internal bool IsNonAttributedType => _isNonAttributedType;

		internal bool IsKeyValuePairAdapter => _isKeyValuePairAdapter;

		internal bool IsScriptObject => _isScriptObject;

		internal Type[] KeyValuePairGenericArguments => _keyValuePairGenericArguments;

		internal ConstructorInfo KeyValuePairAdapterConstructorInfo => _keyValuePairCtorInfo;

		internal MethodInfo GetKeyValuePairMethodInfo => _getKeyValuePairMethodInfo;

		internal XmlFormatClassWriterDelegate XmlFormatWriterDelegate
		{
			get
			{
				return _xmlFormatWriterDelegate;
			}
			set
			{
				_xmlFormatWriterDelegate = value;
			}
		}

		internal XmlFormatClassReaderDelegate XmlFormatReaderDelegate
		{
			get
			{
				return _xmlFormatReaderDelegate;
			}
			set
			{
				_xmlFormatReaderDelegate = value;
			}
		}

		public XmlDictionaryString[] ChildElementNamespaces
		{
			get
			{
				return _childElementNamespaces;
			}
			set
			{
				_childElementNamespaces = value;
			}
		}

		private static Type[] SerInfoCtorArgs
		{
			get
			{
				if (s_serInfoCtorArgs == null)
				{
					s_serInfoCtorArgs = new Type[2]
					{
						typeof(SerializationInfo),
						typeof(StreamingContext)
					};
				}
				return s_serInfoCtorArgs;
			}
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		internal ClassDataContractCriticalHelper([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
			: base(type)
		{
			XmlQualifiedName stableNameAndSetHasDataContract = GetStableNameAndSetHasDataContract(type);
			if (type == Globals.TypeOfDBNull)
			{
				base.StableName = stableNameAndSetHasDataContract;
				_members = new List<DataMember>();
				XmlDictionary xmlDictionary = new XmlDictionary(2);
				base.Name = xmlDictionary.Add(base.StableName.Name);
				base.Namespace = xmlDictionary.Add(base.StableName.Namespace);
				ContractNamespaces = (MemberNames = (MemberNamespaces = Array.Empty<XmlDictionaryString>()));
				EnsureMethodsImported();
				return;
			}
			Type type2 = type.BaseType;
			_isISerializable = Globals.TypeOfISerializable.IsAssignableFrom(type);
			SetIsNonAttributedType(type);
			if (_isISerializable)
			{
				if (HasDataContract)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(System.SR.Format(System.SR.ISerializableCannotHaveDataContract, DataContract.GetClrTypeFullName(type))));
				}
				if (type2 != null && (!type2.IsSerializable || !Globals.TypeOfISerializable.IsAssignableFrom(type2)))
				{
					type2 = null;
				}
			}
			SetKeyValuePairAdapterFlags(type);
			base.IsValueType = type.IsValueType;
			if (type2 != null && type2 != Globals.TypeOfObject && type2 != Globals.TypeOfValueType && type2 != Globals.TypeOfUri)
			{
				DataContract dataContract = DataContract.GetDataContract(type2);
				if (dataContract is CollectionDataContract)
				{
					BaseContract = ((CollectionDataContract)dataContract).SharedTypeContract as ClassDataContract;
				}
				else
				{
					BaseContract = dataContract as ClassDataContract;
				}
				if (BaseContract != null && BaseContract.IsNonAttributedType && !_isNonAttributedType)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(System.SR.Format(System.SR.AttributedTypesCannotInheritFromNonAttributedSerializableTypes, DataContract.GetClrTypeFullName(type), DataContract.GetClrTypeFullName(type2))));
				}
			}
			else
			{
				BaseContract = null;
			}
			_hasExtensionData = Globals.TypeOfIExtensibleDataObject.IsAssignableFrom(type);
			if (_hasExtensionData && !HasDataContract && !IsNonAttributedType)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(System.SR.Format(System.SR.OnlyDataContractTypesCanHaveExtensionData, DataContract.GetClrTypeFullName(type))));
			}
			if (_isISerializable)
			{
				SetDataContractName(stableNameAndSetHasDataContract);
			}
			else
			{
				base.StableName = stableNameAndSetHasDataContract;
				ImportDataMembers();
				XmlDictionary xmlDictionary2 = new XmlDictionary(2 + Members.Count);
				base.Name = xmlDictionary2.Add(base.StableName.Name);
				base.Namespace = xmlDictionary2.Add(base.StableName.Namespace);
				int num = 0;
				int num2 = 0;
				if (BaseContract == null)
				{
					MemberNames = new XmlDictionaryString[Members.Count];
					MemberNamespaces = new XmlDictionaryString[Members.Count];
					ContractNamespaces = new XmlDictionaryString[1];
				}
				else
				{
					num = BaseContract.MemberNames.Length;
					MemberNames = new XmlDictionaryString[Members.Count + num];
					Array.Copy(BaseContract.MemberNames, MemberNames, num);
					MemberNamespaces = new XmlDictionaryString[Members.Count + num];
					Array.Copy(BaseContract.MemberNamespaces, MemberNamespaces, num);
					num2 = BaseContract.ContractNamespaces.Length;
					ContractNamespaces = new XmlDictionaryString[1 + num2];
					Array.Copy(BaseContract.ContractNamespaces, ContractNamespaces, num2);
				}
				ContractNamespaces[num2] = base.Namespace;
				for (int i = 0; i < Members.Count; i++)
				{
					MemberNames[i + num] = xmlDictionary2.Add(Members[i].Name);
					MemberNamespaces[i + num] = base.Namespace;
				}
			}
			EnsureMethodsImported();
			_isScriptObject = IsNonAttributedType && Globals.TypeOfScriptObject_IsAssignableFrom(base.UnderlyingType);
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		internal ClassDataContractCriticalHelper([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type type, XmlDictionaryString ns, string[] memberNames)
			: base(type)
		{
			base.StableName = new XmlQualifiedName(GetStableNameAndSetHasDataContract(type).Name, ns.Value);
			ImportDataMembers();
			XmlDictionary xmlDictionary = new XmlDictionary(1 + Members.Count);
			base.Name = xmlDictionary.Add(base.StableName.Name);
			base.Namespace = ns;
			ContractNamespaces = new XmlDictionaryString[1] { base.Namespace };
			MemberNames = new XmlDictionaryString[Members.Count];
			MemberNamespaces = new XmlDictionaryString[Members.Count];
			for (int i = 0; i < Members.Count; i++)
			{
				Members[i].Name = memberNames[i];
				MemberNames[i] = xmlDictionary.Add(Members[i].Name);
				MemberNamespaces[i] = base.Namespace;
			}
			EnsureMethodsImported();
		}

		private void EnsureIsReferenceImported(Type type)
		{
			bool flag = false;
			DataContractAttribute dataContractAttribute;
			bool flag2 = DataContract.TryGetDCAttribute(type, out dataContractAttribute);
			if (BaseContract != null)
			{
				if (flag2 && dataContractAttribute.IsReferenceSetExplicitly)
				{
					bool isReference = BaseContract.IsReference;
					if ((isReference && !dataContractAttribute.IsReference) || (!isReference && dataContractAttribute.IsReference))
					{
						DataContract.ThrowInvalidDataContractException(System.SR.Format(System.SR.InconsistentIsReference, DataContract.GetClrTypeFullName(type), dataContractAttribute.IsReference, DataContract.GetClrTypeFullName(BaseContract.UnderlyingType), BaseContract.IsReference), type);
					}
					else
					{
						flag = dataContractAttribute.IsReference;
					}
				}
				else
				{
					flag = BaseContract.IsReference;
				}
			}
			else if (flag2 && dataContractAttribute.IsReference)
			{
				flag = dataContractAttribute.IsReference;
			}
			if (flag && type.IsValueType)
			{
				DataContract.ThrowInvalidDataContractException(System.SR.Format(System.SR.ValueTypeCannotHaveIsReference, DataContract.GetClrTypeFullName(type), true, false), type);
			}
			else
			{
				base.IsReference = flag;
			}
		}

		[MemberNotNull("_members")]
		[MemberNotNull("Members")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private void ImportDataMembers()
		{
			Type underlyingType = base.UnderlyingType;
			EnsureIsReferenceImported(underlyingType);
			List<DataMember> list = new List<DataMember>();
			Dictionary<string, DataMember> memberNamesTable = new Dictionary<string, DataMember>();
			bool flag = !_isNonAttributedType || IsKnownSerializableType(underlyingType);
			MemberInfo[] array = (flag ? underlyingType.GetMembers(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) : underlyingType.GetMembers(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public));
			foreach (MemberInfo memberInfo in array)
			{
				if (HasDataContract)
				{
					object[] array2 = memberInfo.GetCustomAttributes(typeof(DataMemberAttribute), inherit: false).ToArray();
					if (array2 == null || array2.Length == 0)
					{
						continue;
					}
					if (array2.Length > 1)
					{
						ThrowInvalidDataContractException(System.SR.Format(System.SR.TooManyDataMembers, DataContract.GetClrTypeFullName(memberInfo.DeclaringType), memberInfo.Name));
					}
					DataMember dataMember = new DataMember(memberInfo);
					if (memberInfo is PropertyInfo)
					{
						PropertyInfo propertyInfo = (PropertyInfo)memberInfo;
						MethodInfo getMethod = propertyInfo.GetMethod;
						if (getMethod != null && IsMethodOverriding(getMethod))
						{
							continue;
						}
						MethodInfo setMethod = propertyInfo.SetMethod;
						if (setMethod != null && IsMethodOverriding(setMethod))
						{
							continue;
						}
						if (getMethod == null)
						{
							ThrowInvalidDataContractException(System.SR.Format(System.SR.NoGetMethodForProperty, propertyInfo.DeclaringType, propertyInfo.Name));
						}
						if (setMethod == null && !SetIfGetOnlyCollection(dataMember))
						{
							ThrowInvalidDataContractException(System.SR.Format(System.SR.NoSetMethodForProperty, propertyInfo.DeclaringType, propertyInfo.Name));
						}
						if (getMethod.GetParameters().Length != 0)
						{
							ThrowInvalidDataContractException(System.SR.Format(System.SR.IndexedPropertyCannotBeSerialized, propertyInfo.DeclaringType, propertyInfo.Name));
						}
					}
					else if (!(memberInfo is FieldInfo))
					{
						ThrowInvalidDataContractException(System.SR.Format(System.SR.InvalidMember, DataContract.GetClrTypeFullName(underlyingType), memberInfo.Name));
					}
					DataMemberAttribute dataMemberAttribute = (DataMemberAttribute)array2[0];
					if (dataMemberAttribute.IsNameSetExplicitly)
					{
						if (dataMemberAttribute.Name == null || dataMemberAttribute.Name.Length == 0)
						{
							ThrowInvalidDataContractException(System.SR.Format(System.SR.InvalidDataMemberName, memberInfo.Name, DataContract.GetClrTypeFullName(underlyingType)));
						}
						dataMember.Name = dataMemberAttribute.Name;
					}
					else
					{
						dataMember.Name = memberInfo.Name;
					}
					dataMember.Name = DataContract.EncodeLocalName(dataMember.Name);
					dataMember.IsNullable = DataContract.IsTypeNullable(dataMember.MemberType);
					dataMember.IsRequired = dataMemberAttribute.IsRequired;
					if (dataMemberAttribute.IsRequired && base.IsReference)
					{
						DataContractCriticalHelper.ThrowInvalidDataContractException(System.SR.Format(System.SR.IsRequiredDataMemberOnIsReferenceDataContractType, DataContract.GetClrTypeFullName(memberInfo.DeclaringType), memberInfo.Name, true), underlyingType);
					}
					dataMember.EmitDefaultValue = dataMemberAttribute.EmitDefaultValue;
					dataMember.Order = dataMemberAttribute.Order;
					CheckAndAddMember(list, dataMember, memberNamesTable);
					continue;
				}
				if (!flag)
				{
					FieldInfo fieldInfo = memberInfo as FieldInfo;
					PropertyInfo propertyInfo2 = memberInfo as PropertyInfo;
					if ((fieldInfo == null && propertyInfo2 == null) || (fieldInfo != null && fieldInfo.IsInitOnly))
					{
						continue;
					}
					object[] array3 = memberInfo.GetCustomAttributes(typeof(IgnoreDataMemberAttribute), inherit: false).ToArray();
					if (array3 != null && array3.Length != 0)
					{
						if (array3.Length <= 1)
						{
							continue;
						}
						ThrowInvalidDataContractException(System.SR.Format(System.SR.TooManyIgnoreDataMemberAttributes, DataContract.GetClrTypeFullName(memberInfo.DeclaringType), memberInfo.Name));
					}
					DataMember dataMember2 = new DataMember(memberInfo);
					if (propertyInfo2 != null)
					{
						MethodInfo getMethod2 = propertyInfo2.GetGetMethod();
						if (getMethod2 == null || IsMethodOverriding(getMethod2) || getMethod2.GetParameters().Length != 0)
						{
							continue;
						}
						MethodInfo setMethod2 = propertyInfo2.SetMethod;
						if (setMethod2 == null)
						{
							if (!SetIfGetOnlyCollection(dataMember2))
							{
								continue;
							}
						}
						else if (!setMethod2.IsPublic || IsMethodOverriding(setMethod2))
						{
							continue;
						}
					}
					dataMember2.Name = DataContract.EncodeLocalName(memberInfo.Name);
					dataMember2.IsNullable = DataContract.IsTypeNullable(dataMember2.MemberType);
					CheckAndAddMember(list, dataMember2, memberNamesTable);
					continue;
				}
				FieldInfo fieldInfo2 = memberInfo as FieldInfo;
				if (!((!IsKnownSerializableType(underlyingType)) ? (fieldInfo2 != null && !fieldInfo2.IsNotSerialized) : CanSerializeMember(fieldInfo2)))
				{
					continue;
				}
				DataMember dataMember3 = new DataMember(memberInfo);
				dataMember3.Name = DataContract.EncodeLocalName(memberInfo.Name);
				object[] customAttributes = fieldInfo2.GetCustomAttributes(Globals.TypeOfOptionalFieldAttribute, inherit: false);
				if (customAttributes == null || customAttributes.Length == 0)
				{
					if (base.IsReference)
					{
						DataContractCriticalHelper.ThrowInvalidDataContractException(System.SR.Format(System.SR.NonOptionalFieldMemberOnIsReferenceSerializableType, DataContract.GetClrTypeFullName(memberInfo.DeclaringType), memberInfo.Name, true), underlyingType);
					}
					dataMember3.IsRequired = true;
				}
				dataMember3.IsNullable = DataContract.IsTypeNullable(dataMember3.MemberType);
				CheckAndAddMember(list, dataMember3, memberNamesTable);
			}
			if (list.Count > 1)
			{
				list.Sort(DataMemberComparer.Singleton);
			}
			SetIfMembersHaveConflict(list);
			Interlocked.MemoryBarrier();
			_members = list;
		}

		private static bool CanSerializeMember(FieldInfo field)
		{
			if (field != null)
			{
				return !IsNonSerializedMember(field.DeclaringType, field.Name);
			}
			return false;
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private bool SetIfGetOnlyCollection(DataMember memberContract)
		{
			if (CollectionDataContract.IsCollection(memberContract.MemberType, constructorRequired: false) && !memberContract.MemberType.IsValueType)
			{
				memberContract.IsGetOnlyCollection = true;
				return true;
			}
			return false;
		}

		private void SetIfMembersHaveConflict(List<DataMember> members)
		{
			if (BaseContract == null)
			{
				return;
			}
			int num = 0;
			List<Member> list = new List<Member>();
			foreach (DataMember member in members)
			{
				list.Add(new Member(member, base.StableName.Namespace, num));
			}
			for (ClassDataContract baseContract = BaseContract; baseContract != null; baseContract = baseContract.BaseContract)
			{
				num++;
				foreach (DataMember member2 in baseContract.Members)
				{
					list.Add(new Member(member2, baseContract.StableName.Namespace, num));
				}
			}
			IComparer<Member> singleton = DataMemberConflictComparer.Singleton;
			list.Sort(singleton);
			int num2;
			for (num2 = 0; num2 < list.Count - 1; num2++)
			{
				int num3 = num2;
				int i = num2;
				bool flag = false;
				for (; i < list.Count - 1 && string.CompareOrdinal(list[i].member.Name, list[i + 1].member.Name) == 0 && string.CompareOrdinal(list[i].ns, list[i + 1].ns) == 0; i++)
				{
					list[i].member.ConflictingMember = list[i + 1].member;
					if (!flag)
					{
						flag = list[i + 1].member.HasConflictingNameAndType || list[i].member.MemberType != list[i + 1].member.MemberType;
					}
				}
				if (flag)
				{
					for (int j = num3; j <= i; j++)
					{
						list[j].member.HasConflictingNameAndType = true;
					}
				}
				num2 = i + 1;
			}
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private XmlQualifiedName GetStableNameAndSetHasDataContract(Type type)
		{
			return DataContract.GetStableName(type, out _hasDataContract);
		}

		private void SetIsNonAttributedType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] Type type)
		{
			_isNonAttributedType = !type.IsSerializable && !_hasDataContract && IsNonAttributedTypeValidForSerialization(type);
		}

		private static bool IsMethodOverriding(MethodInfo method)
		{
			if (method.IsVirtual)
			{
				return (method.Attributes & MethodAttributes.VtableLayoutMask) == 0;
			}
			return false;
		}

		internal void EnsureMethodsImported()
		{
			if (_isMethodChecked || !(base.UnderlyingType != null))
			{
				return;
			}
			lock (this)
			{
				if (_isMethodChecked)
				{
					return;
				}
				Type underlyingType = base.UnderlyingType;
				MethodInfo[] methods = underlyingType.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				foreach (MethodInfo methodInfo in methods)
				{
					Type prevAttributeType = null;
					ParameterInfo[] parameters = methodInfo.GetParameters();
					if (HasExtensionData && IsValidExtensionDataSetMethod(methodInfo, parameters))
					{
						if (methodInfo.Name == "System.Runtime.Serialization.IExtensibleDataObject.set_ExtensionData" || !methodInfo.IsPublic)
						{
							_extensionDataSetMethod = XmlFormatGeneratorStatics.ExtensionDataSetExplicitMethodInfo;
						}
						else
						{
							_extensionDataSetMethod = methodInfo;
						}
					}
					if (IsValidCallback(methodInfo, parameters, Globals.TypeOfOnSerializingAttribute, _onSerializing, ref prevAttributeType))
					{
						_onSerializing = methodInfo;
					}
					if (IsValidCallback(methodInfo, parameters, Globals.TypeOfOnSerializedAttribute, _onSerialized, ref prevAttributeType))
					{
						_onSerialized = methodInfo;
					}
					if (IsValidCallback(methodInfo, parameters, Globals.TypeOfOnDeserializingAttribute, _onDeserializing, ref prevAttributeType))
					{
						_onDeserializing = methodInfo;
					}
					if (IsValidCallback(methodInfo, parameters, Globals.TypeOfOnDeserializedAttribute, _onDeserialized, ref prevAttributeType))
					{
						_onDeserialized = methodInfo;
					}
				}
				Interlocked.MemoryBarrier();
				_isMethodChecked = true;
			}
		}

		private bool IsValidExtensionDataSetMethod(MethodInfo method, ParameterInfo[] parameters)
		{
			if (method.Name == "System.Runtime.Serialization.IExtensibleDataObject.set_ExtensionData" || method.Name == "set_ExtensionData")
			{
				if (_extensionDataSetMethod != null)
				{
					ThrowInvalidDataContractException(System.SR.Format(System.SR.DuplicateExtensionDataSetMethod, method, _extensionDataSetMethod, DataContract.GetClrTypeFullName(method.DeclaringType)));
				}
				if (method.ReturnType != Globals.TypeOfVoid)
				{
					DataContract.ThrowInvalidDataContractException(System.SR.Format(System.SR.ExtensionDataSetMustReturnVoid, DataContract.GetClrTypeFullName(method.DeclaringType), method), method.DeclaringType);
				}
				if (parameters == null || parameters.Length != 1 || parameters[0].ParameterType != Globals.TypeOfExtensionDataObject)
				{
					DataContract.ThrowInvalidDataContractException(System.SR.Format(System.SR.ExtensionDataSetParameterInvalid, DataContract.GetClrTypeFullName(method.DeclaringType), method, Globals.TypeOfExtensionDataObject), method.DeclaringType);
				}
				return true;
			}
			return false;
		}

		private static bool IsValidCallback(MethodInfo method, ParameterInfo[] parameters, Type attributeType, MethodInfo currentCallback, ref Type prevAttributeType)
		{
			if (method.IsDefined(attributeType, inherit: false))
			{
				if (currentCallback != null)
				{
					DataContract.ThrowInvalidDataContractException(System.SR.Format(System.SR.DuplicateCallback, method, currentCallback, DataContract.GetClrTypeFullName(method.DeclaringType), attributeType), method.DeclaringType);
				}
				else if (prevAttributeType != null)
				{
					DataContract.ThrowInvalidDataContractException(System.SR.Format(System.SR.DuplicateAttribute, prevAttributeType, attributeType, DataContract.GetClrTypeFullName(method.DeclaringType), method), method.DeclaringType);
				}
				else if (method.IsVirtual)
				{
					DataContract.ThrowInvalidDataContractException(System.SR.Format(System.SR.CallbacksCannotBeVirtualMethods, method, DataContract.GetClrTypeFullName(method.DeclaringType), attributeType), method.DeclaringType);
				}
				else
				{
					if (method.ReturnType != Globals.TypeOfVoid)
					{
						DataContract.ThrowInvalidDataContractException(System.SR.Format(System.SR.CallbackMustReturnVoid, DataContract.GetClrTypeFullName(method.DeclaringType), method), method.DeclaringType);
					}
					if (parameters == null || parameters.Length != 1 || parameters[0].ParameterType != Globals.TypeOfStreamingContext)
					{
						DataContract.ThrowInvalidDataContractException(System.SR.Format(System.SR.CallbackParameterInvalid, DataContract.GetClrTypeFullName(method.DeclaringType), method, Globals.TypeOfStreamingContext), method.DeclaringType);
					}
					prevAttributeType = attributeType;
				}
				return true;
			}
			return false;
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private void SetKeyValuePairAdapterFlags([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
		{
			if (type.IsGenericType && type.GetGenericTypeDefinition() == Globals.TypeOfKeyValuePairAdapter)
			{
				_isKeyValuePairAdapter = true;
				_keyValuePairGenericArguments = type.GetGenericArguments();
				_keyValuePairCtorInfo = (ConstructorInfo)type.GetMemberWithSameMetadataDefinitionAs(s_ctorGenericMethod);
				_getKeyValuePairMethodInfo = (MethodInfo)type.GetMemberWithSameMetadataDefinitionAs(s_getKeyValuePairMethod);
			}
		}

		internal ConstructorInfo GetISerializableConstructor()
		{
			if (!IsISerializable)
			{
				return null;
			}
			ConstructorInfo constructor = base.UnderlyingType.GetConstructor(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, SerInfoCtorArgs);
			if (constructor == null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.SerializationInfo_ConstructorNotFound, DataContract.GetClrTypeFullName(base.UnderlyingType))));
			}
			return constructor;
		}

		internal ConstructorInfo GetNonAttributedTypeConstructor()
		{
			if (!IsNonAttributedType)
			{
				return null;
			}
			Type underlyingType = base.UnderlyingType;
			if (underlyingType.IsValueType)
			{
				return null;
			}
			ConstructorInfo constructor = underlyingType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
			if (constructor == null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(System.SR.Format(System.SR.NonAttributedSerializableTypesMustHaveDefaultConstructor, DataContract.GetClrTypeFullName(underlyingType))));
			}
			return constructor;
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		internal ClassDataContractCriticalHelper Clone()
		{
			ClassDataContractCriticalHelper classDataContractCriticalHelper = new ClassDataContractCriticalHelper(base.UnderlyingType);
			classDataContractCriticalHelper._baseContract = _baseContract;
			classDataContractCriticalHelper._childElementNamespaces = _childElementNamespaces;
			classDataContractCriticalHelper.ContractNamespaces = ContractNamespaces;
			classDataContractCriticalHelper._hasDataContract = _hasDataContract;
			classDataContractCriticalHelper._isMethodChecked = _isMethodChecked;
			classDataContractCriticalHelper._isNonAttributedType = _isNonAttributedType;
			classDataContractCriticalHelper.IsReference = base.IsReference;
			classDataContractCriticalHelper.IsValueType = base.IsValueType;
			classDataContractCriticalHelper.MemberNames = MemberNames;
			classDataContractCriticalHelper.MemberNamespaces = MemberNamespaces;
			classDataContractCriticalHelper._members = _members;
			classDataContractCriticalHelper.Name = base.Name;
			classDataContractCriticalHelper.Namespace = base.Namespace;
			classDataContractCriticalHelper._onDeserialized = _onDeserialized;
			classDataContractCriticalHelper._onDeserializing = _onDeserializing;
			classDataContractCriticalHelper._onSerialized = _onSerialized;
			classDataContractCriticalHelper._onSerializing = _onSerializing;
			classDataContractCriticalHelper.StableName = base.StableName;
			classDataContractCriticalHelper.TopLevelElementName = TopLevelElementName;
			classDataContractCriticalHelper.TopLevelElementNamespace = TopLevelElementNamespace;
			classDataContractCriticalHelper._xmlFormatReaderDelegate = _xmlFormatReaderDelegate;
			classDataContractCriticalHelper._xmlFormatWriterDelegate = _xmlFormatWriterDelegate;
			return classDataContractCriticalHelper;
		}
	}

	internal sealed class DataMemberComparer : IComparer<DataMember>
	{
		internal static DataMemberComparer Singleton = new DataMemberComparer();

		public int Compare(DataMember x, DataMember y)
		{
			int num = x.Order - y.Order;
			if (num != 0)
			{
				return num;
			}
			return string.CompareOrdinal(x.Name, y.Name);
		}
	}

	public XmlDictionaryString[] ContractNamespaces;

	public XmlDictionaryString[] MemberNames;

	public XmlDictionaryString[] MemberNamespaces;

	private XmlDictionaryString[] _childElementNamespaces;

	private ClassDataContractCriticalHelper _helper;

	private bool _isScriptObject;

	internal const DynamicallyAccessedMemberTypes DataContractPreserveMemberTypes = DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties;

	private ConstructorInfo _nonAttributedTypeConstructor;

	private Func<object> _makeNewInstance;

	private static readonly Dictionary<string, string[]> s_knownSerializableTypeInfos = new Dictionary<string, string[]>
	{
		{
			"System.Collections.Generic.KeyValuePair`2",
			Array.Empty<string>()
		},
		{
			"System.Collections.Generic.Queue`1",
			new string[1] { "_syncRoot" }
		},
		{
			"System.Collections.Generic.Stack`1",
			new string[1] { "_syncRoot" }
		},
		{
			"System.Collections.ObjectModel.ReadOnlyCollection`1",
			new string[1] { "_syncRoot" }
		},
		{
			"System.Collections.ObjectModel.ReadOnlyDictionary`2",
			new string[3] { "_syncRoot", "_keys", "_values" }
		},
		{
			"System.Tuple`1",
			Array.Empty<string>()
		},
		{
			"System.Tuple`2",
			Array.Empty<string>()
		},
		{
			"System.Tuple`3",
			Array.Empty<string>()
		},
		{
			"System.Tuple`4",
			Array.Empty<string>()
		},
		{
			"System.Tuple`5",
			Array.Empty<string>()
		},
		{
			"System.Tuple`6",
			Array.Empty<string>()
		},
		{
			"System.Tuple`7",
			Array.Empty<string>()
		},
		{
			"System.Tuple`8",
			Array.Empty<string>()
		},
		{
			"System.Collections.Queue",
			new string[1] { "_syncRoot" }
		},
		{
			"System.Collections.Stack",
			new string[1] { "_syncRoot" }
		},
		{
			"System.Globalization.CultureInfo",
			Array.Empty<string>()
		},
		{
			"System.Version",
			Array.Empty<string>()
		}
	};

	internal ClassDataContract BaseContract => _helper.BaseContract;

	internal List<DataMember> Members => _helper.Members;

	public XmlDictionaryString[] ChildElementNamespaces
	{
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			if (_childElementNamespaces == null)
			{
				lock (this)
				{
					if (_childElementNamespaces == null)
					{
						if (_helper.ChildElementNamespaces == null)
						{
							XmlDictionaryString[] childElementNamespaces = CreateChildElementNamespaces();
							Interlocked.MemoryBarrier();
							_helper.ChildElementNamespaces = childElementNamespaces;
						}
						_childElementNamespaces = _helper.ChildElementNamespaces;
					}
				}
			}
			return _childElementNamespaces;
		}
		set
		{
			_childElementNamespaces = value;
		}
	}

	internal MethodInfo OnSerializing => _helper.OnSerializing;

	internal MethodInfo OnSerialized => _helper.OnSerialized;

	internal MethodInfo OnDeserializing => _helper.OnDeserializing;

	internal MethodInfo OnDeserialized => _helper.OnDeserialized;

	internal MethodInfo ExtensionDataSetMethod => _helper.ExtensionDataSetMethod;

	public override Dictionary<XmlQualifiedName, DataContract> KnownDataContracts
	{
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			return _helper.KnownDataContracts;
		}
	}

	public override bool IsISerializable
	{
		get
		{
			return _helper.IsISerializable;
		}
		set
		{
			_helper.IsISerializable = value;
		}
	}

	internal bool IsNonAttributedType => _helper.IsNonAttributedType;

	public bool HasExtensionData
	{
		get
		{
			return _helper.HasExtensionData;
		}
		set
		{
			_helper.HasExtensionData = value;
		}
	}

	[MemberNotNullWhen(true, "KeyValuePairGenericArguments")]
	[MemberNotNullWhen(true, "KeyValuePairAdapterConstructorInfo")]
	[MemberNotNullWhen(true, "GetKeyValuePairMethodInfo")]
	internal bool IsKeyValuePairAdapter
	{
		[MemberNotNullWhen(true, "KeyValuePairGenericArguments")]
		[MemberNotNullWhen(true, "KeyValuePairAdapterConstructorInfo")]
		[MemberNotNullWhen(true, "GetKeyValuePairMethodInfo")]
		get
		{
			return _helper.IsKeyValuePairAdapter;
		}
	}

	internal Type[] KeyValuePairGenericArguments => _helper.KeyValuePairGenericArguments;

	internal ConstructorInfo KeyValuePairAdapterConstructorInfo => _helper.KeyValuePairAdapterConstructorInfo;

	internal MethodInfo GetKeyValuePairMethodInfo => _helper.GetKeyValuePairMethodInfo;

	private Func<object> MakeNewInstance
	{
		get
		{
			if (_makeNewInstance == null)
			{
				_makeNewInstance = FastInvokerBuilder.GetMakeNewInstanceFunc(base.UnderlyingType);
			}
			return _makeNewInstance;
		}
	}

	internal XmlFormatClassWriterDelegate XmlFormatWriterDelegate
	{
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			if (_helper.XmlFormatWriterDelegate == null)
			{
				lock (this)
				{
					if (_helper.XmlFormatWriterDelegate == null)
					{
						XmlFormatClassWriterDelegate xmlFormatWriterDelegate = CreateXmlFormatWriterDelegate();
						Interlocked.MemoryBarrier();
						_helper.XmlFormatWriterDelegate = xmlFormatWriterDelegate;
					}
				}
			}
			return _helper.XmlFormatWriterDelegate;
		}
		set
		{
		}
	}

	internal XmlFormatClassReaderDelegate XmlFormatReaderDelegate
	{
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			if (_helper.XmlFormatReaderDelegate == null)
			{
				lock (this)
				{
					if (_helper.XmlFormatReaderDelegate == null)
					{
						XmlFormatClassReaderDelegate xmlFormatReaderDelegate = CreateXmlFormatReaderDelegate();
						Interlocked.MemoryBarrier();
						_helper.XmlFormatReaderDelegate = xmlFormatReaderDelegate;
					}
				}
			}
			return _helper.XmlFormatReaderDelegate;
		}
		set
		{
		}
	}

	internal Type ObjectType
	{
		get
		{
			Type type = base.UnderlyingType;
			if (type.IsValueType && !IsNonAttributedType)
			{
				type = Globals.TypeOfValueType;
			}
			return type;
		}
	}

	internal Type UnadaptedClassType
	{
		get
		{
			if (IsKeyValuePairAdapter)
			{
				return Globals.TypeOfKeyValuePair.MakeGenericType(KeyValuePairGenericArguments);
			}
			return base.UnderlyingType;
		}
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal ClassDataContract(Type type)
		: base(new ClassDataContractCriticalHelper(type))
	{
		InitClassDataContract();
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private ClassDataContract(Type type, XmlDictionaryString ns, string[] memberNames)
		: base(new ClassDataContractCriticalHelper(type, ns, memberNames))
	{
		InitClassDataContract();
	}

	[MemberNotNull("_helper")]
	private void InitClassDataContract()
	{
		_helper = base.Helper as ClassDataContractCriticalHelper;
		ContractNamespaces = _helper.ContractNamespaces;
		MemberNames = _helper.MemberNames;
		MemberNamespaces = _helper.MemberNamespaces;
		_isScriptObject = _helper.IsScriptObject;
	}

	internal ConstructorInfo GetISerializableConstructor()
	{
		return _helper.GetISerializableConstructor();
	}

	internal ConstructorInfo GetNonAttributedTypeConstructor()
	{
		if (_nonAttributedTypeConstructor == null)
		{
			_nonAttributedTypeConstructor = _helper.GetNonAttributedTypeConstructor();
		}
		return _nonAttributedTypeConstructor;
	}

	internal bool CreateNewInstanceViaDefaultConstructor([NotNullWhen(true)] out object obj)
	{
		ConstructorInfo nonAttributedTypeConstructor = GetNonAttributedTypeConstructor();
		if (nonAttributedTypeConstructor == null)
		{
			obj = null;
			return false;
		}
		if (nonAttributedTypeConstructor.IsPublic)
		{
			obj = MakeNewInstance();
		}
		else
		{
			obj = nonAttributedTypeConstructor.Invoke(Array.Empty<object>());
		}
		return true;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private XmlFormatClassWriterDelegate CreateXmlFormatWriterDelegate()
	{
		return new XmlFormatWriterGenerator().GenerateClassWriter(this);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private XmlFormatClassReaderDelegate CreateXmlFormatReaderDelegate()
	{
		return new XmlFormatReaderGenerator().GenerateClassReader(this);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static ClassDataContract CreateClassDataContractForKeyValue(Type type, XmlDictionaryString ns, string[] memberNames)
	{
		ClassDataContract classDataContract = (ClassDataContract)DataContract.GetDataContractFromGeneratedAssembly(type);
		if (classDataContract == null)
		{
			return new ClassDataContract(type, ns, memberNames);
		}
		ClassDataContract classDataContract2 = classDataContract.Clone();
		classDataContract2.UpdateNamespaceAndMembers(type, ns, memberNames);
		return classDataContract2;
	}

	internal static void CheckAndAddMember(List<DataMember> members, DataMember memberContract, Dictionary<string, DataMember> memberNamesTable)
	{
		if (memberNamesTable.TryGetValue(memberContract.Name, out var value))
		{
			Type declaringType = memberContract.MemberInfo.DeclaringType;
			DataContract.ThrowInvalidDataContractException(System.SR.Format(declaringType.IsEnum ? System.SR.DupEnumMemberValue : System.SR.DupMemberName, value.MemberInfo.Name, memberContract.MemberInfo.Name, DataContract.GetClrTypeFullName(declaringType), memberContract.Name), declaringType);
		}
		memberNamesTable.Add(memberContract.Name, memberContract);
		members.Add(memberContract);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static XmlDictionaryString GetChildNamespaceToDeclare(DataContract dataContract, Type childType, XmlDictionary dictionary)
	{
		childType = DataContract.UnwrapNullableType(childType);
		if (!childType.IsEnum && !Globals.TypeOfIXmlSerializable.IsAssignableFrom(childType) && DataContract.GetBuiltInDataContract(childType) == null && childType != Globals.TypeOfDBNull)
		{
			string @namespace = DataContract.GetStableName(childType).Namespace;
			if (@namespace.Length > 0 && @namespace != dataContract.Namespace.Value)
			{
				return dictionary.Add(@namespace);
			}
		}
		return null;
	}

	private static bool IsArraySegment(Type t)
	{
		if (t.IsGenericType)
		{
			return t.GetGenericTypeDefinition() == typeof(ArraySegment<>);
		}
		return false;
	}

	internal static bool IsNonAttributedTypeValidForSerialization([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] Type type)
	{
		if (type.IsArray)
		{
			return false;
		}
		if (type.IsEnum)
		{
			return false;
		}
		if (type.IsGenericParameter)
		{
			return false;
		}
		if (Globals.TypeOfIXmlSerializable.IsAssignableFrom(type))
		{
			return false;
		}
		if (type.IsPointer)
		{
			return false;
		}
		if (type.IsDefined(Globals.TypeOfCollectionDataContractAttribute, inherit: false))
		{
			return false;
		}
		Type[] interfaces = type.GetInterfaces();
		if (!IsArraySegment(type))
		{
			Type[] array = interfaces;
			foreach (Type type2 in array)
			{
				if (CollectionDataContract.IsCollectionInterface(type2))
				{
					return false;
				}
			}
		}
		if (type.IsSerializable)
		{
			return false;
		}
		if (Globals.TypeOfISerializable.IsAssignableFrom(type))
		{
			return false;
		}
		if (type.IsDefined(Globals.TypeOfDataContractAttribute, inherit: false))
		{
			return false;
		}
		if (type.IsValueType)
		{
			return type.IsVisible;
		}
		if (type.IsVisible)
		{
			return type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes) != null;
		}
		return false;
	}

	private static string GetGeneralTypeName(Type type)
	{
		if (!type.IsGenericType || type.IsGenericParameter)
		{
			return type.FullName;
		}
		return type.GetGenericTypeDefinition().FullName;
	}

	internal static bool IsKnownSerializableType(Type type)
	{
		string generalTypeName = GetGeneralTypeName(type);
		return s_knownSerializableTypeInfos.ContainsKey(generalTypeName);
	}

	internal static bool IsNonSerializedMember(Type type, string memberName)
	{
		string generalTypeName = GetGeneralTypeName(type);
		if (s_knownSerializableTypeInfos.TryGetValue(generalTypeName, out var value))
		{
			return value.Contains(memberName);
		}
		return false;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private XmlDictionaryString[] CreateChildElementNamespaces()
	{
		if (Members == null)
		{
			return null;
		}
		XmlDictionaryString[] array = null;
		if (BaseContract != null)
		{
			array = BaseContract.ChildElementNamespaces;
		}
		int num = ((array != null) ? array.Length : 0);
		XmlDictionaryString[] array2 = new XmlDictionaryString[Members.Count + num];
		if (num > 0)
		{
			Array.Copy(array, array2, array.Length);
		}
		XmlDictionary dictionary = new XmlDictionary();
		for (int i = 0; i < Members.Count; i++)
		{
			array2[i + num] = GetChildNamespaceToDeclare(this, Members[i].MemberType, dictionary);
		}
		return array2;
	}

	private void EnsureMethodsImported()
	{
		_helper.EnsureMethodsImported();
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override void WriteXmlValue(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context)
	{
		if (_isScriptObject)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(System.SR.Format(System.SR.UnexpectedContractType, DataContract.GetClrTypeFullName(GetType()), DataContract.GetClrTypeFullName(base.UnderlyingType))));
		}
		XmlFormatWriterDelegate(xmlWriter, obj, context, this);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override object ReadXmlValue(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context)
	{
		if (_isScriptObject)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(System.SR.Format(System.SR.UnexpectedContractType, DataContract.GetClrTypeFullName(GetType()), DataContract.GetClrTypeFullName(base.UnderlyingType))));
		}
		xmlReader.Read();
		object result = XmlFormatReaderDelegate(xmlReader, context, MemberNames, MemberNamespaces);
		xmlReader.ReadEndElement();
		return result;
	}

	internal bool RequiresMemberAccessForRead(SecurityException securityException)
	{
		EnsureMethodsImported();
		if (!DataContract.IsTypeVisible(base.UnderlyingType))
		{
			if (securityException != null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new SecurityException(System.SR.Format(System.SR.PartialTrustDataContractTypeNotPublic, DataContract.GetClrTypeFullName(base.UnderlyingType)), securityException));
			}
			return true;
		}
		if (BaseContract != null && BaseContract.RequiresMemberAccessForRead(securityException))
		{
			return true;
		}
		if (DataContract.ConstructorRequiresMemberAccess(GetNonAttributedTypeConstructor()))
		{
			if (Globals.TypeOfScriptObject_IsAssignableFrom(base.UnderlyingType))
			{
				return true;
			}
			if (securityException != null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new SecurityException(System.SR.Format(System.SR.PartialTrustNonAttributedSerializableTypeNoPublicConstructor, DataContract.GetClrTypeFullName(base.UnderlyingType)), securityException));
			}
			return true;
		}
		if (DataContract.MethodRequiresMemberAccess(OnDeserializing))
		{
			if (securityException != null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new SecurityException(System.SR.Format(System.SR.PartialTrustDataContractOnDeserializingNotPublic, DataContract.GetClrTypeFullName(base.UnderlyingType), OnDeserializing.Name), securityException));
			}
			return true;
		}
		if (DataContract.MethodRequiresMemberAccess(OnDeserialized))
		{
			if (securityException != null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new SecurityException(System.SR.Format(System.SR.PartialTrustDataContractOnDeserializedNotPublic, DataContract.GetClrTypeFullName(base.UnderlyingType), OnDeserialized.Name), securityException));
			}
			return true;
		}
		if (Members != null)
		{
			for (int i = 0; i < Members.Count; i++)
			{
				if (!Members[i].RequiresMemberAccessForSet())
				{
					continue;
				}
				if (securityException != null)
				{
					if (Members[i].MemberInfo is FieldInfo)
					{
						throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new SecurityException(System.SR.Format(System.SR.PartialTrustDataContractFieldSetNotPublic, DataContract.GetClrTypeFullName(base.UnderlyingType), Members[i].MemberInfo.Name), securityException));
					}
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new SecurityException(System.SR.Format(System.SR.PartialTrustDataContractPropertySetNotPublic, DataContract.GetClrTypeFullName(base.UnderlyingType), Members[i].MemberInfo.Name), securityException));
				}
				return true;
			}
		}
		return false;
	}

	internal bool RequiresMemberAccessForWrite(SecurityException securityException)
	{
		EnsureMethodsImported();
		if (!DataContract.IsTypeVisible(base.UnderlyingType))
		{
			if (securityException != null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new SecurityException(System.SR.Format(System.SR.PartialTrustDataContractTypeNotPublic, DataContract.GetClrTypeFullName(base.UnderlyingType)), securityException));
			}
			return true;
		}
		if (BaseContract != null && BaseContract.RequiresMemberAccessForWrite(securityException))
		{
			return true;
		}
		if (DataContract.MethodRequiresMemberAccess(OnSerializing))
		{
			if (securityException != null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new SecurityException(System.SR.Format(System.SR.PartialTrustDataContractOnSerializingNotPublic, DataContract.GetClrTypeFullName(base.UnderlyingType), OnSerializing.Name), securityException));
			}
			return true;
		}
		if (DataContract.MethodRequiresMemberAccess(OnSerialized))
		{
			if (securityException != null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new SecurityException(System.SR.Format(System.SR.PartialTrustDataContractOnSerializedNotPublic, DataContract.GetClrTypeFullName(base.UnderlyingType), OnSerialized.Name), securityException));
			}
			return true;
		}
		if (Members != null)
		{
			for (int i = 0; i < Members.Count; i++)
			{
				if (!Members[i].RequiresMemberAccessForGet())
				{
					continue;
				}
				if (securityException != null)
				{
					if (Members[i].MemberInfo is FieldInfo)
					{
						throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new SecurityException(System.SR.Format(System.SR.PartialTrustDataContractFieldGetNotPublic, DataContract.GetClrTypeFullName(base.UnderlyingType), Members[i].MemberInfo.Name), securityException));
					}
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new SecurityException(System.SR.Format(System.SR.PartialTrustDataContractPropertyGetNotPublic, DataContract.GetClrTypeFullName(base.UnderlyingType), Members[i].MemberInfo.Name), securityException));
				}
				return true;
			}
		}
		return false;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal ClassDataContract Clone()
	{
		ClassDataContract classDataContract = new ClassDataContract(base.UnderlyingType);
		classDataContract._helper = _helper.Clone();
		classDataContract.ContractNamespaces = ContractNamespaces;
		classDataContract.ChildElementNamespaces = ChildElementNamespaces;
		classDataContract.MemberNames = MemberNames;
		classDataContract.MemberNamespaces = MemberNamespaces;
		classDataContract.XmlFormatWriterDelegate = XmlFormatWriterDelegate;
		classDataContract.XmlFormatReaderDelegate = XmlFormatReaderDelegate;
		return classDataContract;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal void UpdateNamespaceAndMembers(Type type, XmlDictionaryString ns, string[] memberNames)
	{
		base.StableName = new XmlQualifiedName(DataContract.GetStableName(type).Name, ns.Value);
		Namespace = ns;
		XmlDictionary xmlDictionary = new XmlDictionary(1 + memberNames.Length);
		base.Name = xmlDictionary.Add(base.StableName.Name);
		Namespace = ns;
		ContractNamespaces = new XmlDictionaryString[1] { ns };
		MemberNames = new XmlDictionaryString[memberNames.Length];
		MemberNamespaces = new XmlDictionaryString[memberNames.Length];
		for (int i = 0; i < memberNames.Length; i++)
		{
			MemberNames[i] = xmlDictionary.Add(memberNames[i]);
			MemberNamespaces[i] = ns;
		}
	}
}
